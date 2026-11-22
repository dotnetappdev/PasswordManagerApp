using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using VaultGuard.Crypto.Extensions;
using VaultGuard.Crypto.Interfaces;
using VaultGuard.DAL;
using VaultGuard.Models;
using VaultGuard.Services.Security;
using VaultGuard.Services.Services;

namespace VaultGuard.BackEnd.Tests.Services;

/// <summary>
/// "Couldn't decrypt this password": items sealed under a key derived from a stale cached salt are re-keyed, and a
/// reveal never borrows another profile's unlocked session.
/// </summary>
[TestFixture]
public class VaultKeyRepairTests
{
    private string _dbFile = null!;
    private DbContextOptions<VaultGuardDbContext> _options = null!;
    private IPasswordCryptoService _crypto = null!;

    [SetUp]
    public void SetUp()
    {
        _dbFile = Path.Combine(Path.GetTempPath(), $"rekey-{Guid.NewGuid():N}.db");
        _options = new DbContextOptionsBuilder<VaultGuardDbContext>().UseSqlite($"Data Source={_dbFile};Pooling=False").Options;
        using var db = new VaultGuardDbContext(_options);
        db.Database.EnsureCreated();
        db.Users.Add(new ApplicationUser { Id = "u1", UserName = "a", Email = "a@example.com" });
        db.Users.Add(new ApplicationUser { Id = "u2", UserName = "b", Email = "b@example.com" });
        db.SaveChanges();
        _crypto = new ServiceCollection().AddLogging().AddCryptographyServices().BuildServiceProvider().GetRequiredService<IPasswordCryptoService>();
    }

    [TearDown]
    public void TearDown()
    {
        SqliteConnection.ClearAllPools();
        try { File.Delete(_dbFile); } catch { }
    }

    private void AddLogin(VaultCipher cipher, string userId, string title, string password)
    {
        using var db = new VaultGuardDbContext(_options);
        var item = new PasswordItem { UserId = userId, Title = title, Type = ItemType.Login,
            LoginItem = new LoginItem { UserId = userId, Username = "me", Password = password } };
        cipher.Seal(item);
        VaultItemSecrets.Forget(item);
        db.PasswordItems.Add(item);
        db.SaveChanges();
    }

    private string? Open(VaultCipher cipher, string title)
    {
        using var db = new VaultGuardDbContext(_options);
        var item = db.PasswordItems.Include(p => p.LoginItem).Include(p => p.CustomFields).Single(p => p.Title == title);
        return cipher.Open(item).Count == 0 ? item.LoginItem!.Password : null;
    }

    [Test]
    public async Task ItemsSealedWithStaleSaltKey_AreReKeyed_OthersUntouched()
    {
        var pw = "Master-Pw-1!";
        using var current = VaultCipher.ForMasterPassword(_crypto, pw, _crypto.GenerateUserSalt());
        using var stale = VaultCipher.ForMasterPassword(_crypto, pw, _crypto.GenerateUserSalt());
        using var foreign = VaultCipher.ForMasterPassword(_crypto, "someone-else", _crypto.GenerateUserSalt());

        AddLogin(stale, "u1", "Activision", "imported-pw");
        AddLogin(current, "u1", "Good", "good-pw");
        AddLogin(foreign, "u1", "Lost", "lost-pw");
        Assert.That(Open(current, "Activision"), Is.Null, "precondition: unreadable with the current key");

        VaultKeyRepair.Result result;
        using (var db = new VaultGuardDbContext(_options))
            result = await VaultKeyRepair.ReKeyAsync(db, "u1", current, stale);

        Assert.That(result.Repaired, Is.EqualTo(1));
        Assert.That(result.StillUnreadable, Is.EqualTo(1));
        Assert.That(Open(current, "Activision"), Is.EqualTo("imported-pw"));
        Assert.That(Open(current, "Good"), Is.EqualTo("good-pw"));
        Assert.That(Open(foreign, "Lost"), Is.EqualTo("lost-pw"), "an item neither key opens is left as it was");
    }

    [Test]
    public void Reveal_UsesTheOwnersSession_NotAnotherProfiles()
    {
        var sessions = new VaultSessionService(_crypto, NullLogger<VaultSessionService>.Instance);
        var otherSid = sessions.InitializeSession("u2", _crypto.DeriveMasterKey("other", _crypto.GenerateUserSalt()));
        var mineSid = sessions.InitializeSession("u1", _crypto.DeriveMasterKey("mine", _crypto.GenerateUserSalt()));
        var reveal = new PasswordRevealService(sessions, NullLogger<PasswordRevealService>.Instance);

        var login = new LoginItem { UserId = "u1", Password = "s3cret" };
        Assert.That(reveal.TryEncryptLoginPassword(login, mineSid), Is.True);

        // No or a wrong stored session id: it must pick u1's own session, even though u2's was unlocked first.
        Assert.That(sessions.GetActiveSessionIdForUser("u1"), Is.EqualTo(mineSid));
        Assert.That(reveal.RevealPasswordAsync(login, "").Result, Is.EqualTo("s3cret"));
        Assert.That(reveal.RevealPasswordAsync(login, otherSid).Result, Is.EqualTo("s3cret"));

        sessions.LockVault(mineSid);
        Assert.That(reveal.RevealPasswordAsync(login, otherSid).Result, Is.Null);
        Assert.That(reveal.Diagnose("u1", otherSid), Is.EqualTo(VaultGuard.Services.Interfaces.RevealProblem.Locked));
    }
}
