using Allure.NUnit;
using Allure.NUnit.Attributes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using VaultGuard.Crypto.Extensions;
using VaultGuard.Crypto.Interfaces;
using VaultGuard.DAL;
using VaultGuard.Models;
using VaultGuard.Services;
using VaultGuard.Services.Services;

namespace VaultGuard.BackEnd.Tests.Services;

/// <summary>
/// LoginItem.Notes is [NotMapped]; only EncryptedNotes (+ NotesNonce/NotesAuthTag) is stored. Imports write it,
/// and the detail views (WPF, Web /passwords, shared Vault/Home) only show notes once they've been decrypted on
/// selection. This checks both stored layouts survive a save + reload from a fresh context and decrypt again —
/// through RevealField and through OpenItem (what the views call when an item is selected).
/// </summary>
[TestFixture]
[AllureNUnit]
[AllureEpic("Vault Data Management")]
[AllureFeature("Encryption")]
[AllureSuite("Login notes reveal (round trip)")]
public class LoginNotesRevealRoundTripTests
{
    private const string UserId = "user-1";
    private const string NoteText = "Recovery codes:\nabc-123 ünïcødé SECRETNOTE-RT";
    private string _dbFile = null!;
    private DbContextOptions<VaultGuardDbContext> _options = null!;
    private VaultSessionService _sessions = null!;
    private PasswordRevealService _reveal = null!;
    private string _sid = null!;

    [SetUp]
    public void SetUp()
    {
        _dbFile = Path.Combine(Path.GetTempPath(), $"notes-rt-{Guid.NewGuid():N}.db");
        _options = new DbContextOptionsBuilder<VaultGuardDbContext>().UseSqlite($"Data Source={_dbFile};Pooling=False").Options;
        using (var db = new VaultGuardDbContext(_options))
        {
            db.Database.EnsureCreated();
            db.Users.Add(new ApplicationUser { Id = UserId, UserName = "me", Email = "me@example.com" });
            db.SaveChanges();
        }
        var crypto = new ServiceCollection().AddLogging().AddCryptographyServices().BuildServiceProvider().GetRequiredService<IPasswordCryptoService>();
        _sessions = new VaultSessionService(crypto, NullLogger<VaultSessionService>.Instance);
        _sid = _sessions.InitializeSession(UserId, crypto.DeriveMasterKey("Master-Pw-1!", crypto.GenerateUserSalt()));
        _reveal = new PasswordRevealService(_sessions, NullLogger<PasswordRevealService>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        SqliteConnection.ClearAllPools();
        try { File.Delete(_dbFile); } catch { /* temp file */ }
    }

    private async Task<int> SaveAsync(LoginItem login)
    {
        using var db = new VaultGuardDbContext(_options);
        var item = new PasswordItem { Title = "GitHub", Type = ItemType.Login, UserId = UserId, LoginItem = login };
        return (await new PasswordItemService(db).CreateAsync(item)).Id;
    }

    private async Task<PasswordItem> ReloadAsync(int id)
    {
        using var db = new VaultGuardDbContext(_options);
        return (await new PasswordItemService(db).GetByIdAsync(id))!;
    }

    [Test]
    [AllureName("Pipe-joined notes (cipher|nonce|tag in EncryptedNotes) decrypt after save + reload")]
    public async Task PipeJoinedNotes_RoundTrip()
    {
        var id = await SaveAsync(new LoginItem { UserId = UserId, Username = "me", EncryptedNotes = _sessions.EncryptPassword(NoteText, _sid) });

        var item = await ReloadAsync(id);
        Assert.That(item.LoginItem!.Notes, Is.Null, "plaintext notes must not be stored");
        Assert.That(item.LoginItem.EncryptedNotes, Does.Not.Contain("SECRETNOTE"));
        Assert.That(_reveal.RevealField(item.LoginItem.EncryptedNotes, item.LoginItem.NotesNonce, item.LoginItem.NotesAuthTag, _sid),
            Is.EqualTo(NoteText));

        Assert.That(_reveal.OpenItem(item, _sid), Is.True);
        Assert.That(item.LoginItem.Notes, Is.EqualTo(NoteText));
    }

    [Test]
    [AllureName("Split-layout notes (as written by imports / TrySealItem) decrypt after save + reload")]
    public async Task SplitNotes_RoundTrip()
    {
        var login = new LoginItem { UserId = UserId, Username = "me", Notes = NoteText };
        Assert.That(_reveal.TrySealItem(new PasswordItem { LoginItem = login }, _sid), Is.True);
        login.Notes = null; // what ImportService does after sealing (VaultItemSecrets.Forget)
        Assert.That(login.NotesNonce, Is.Not.Empty);
        var id = await SaveAsync(login);

        var item = await ReloadAsync(id);
        Assert.That(_reveal.RevealField(item.LoginItem!.EncryptedNotes, item.LoginItem.NotesNonce, item.LoginItem.NotesAuthTag, _sid),
            Is.EqualTo(NoteText));
        Assert.That(_reveal.OpenItem(item, _sid), Is.True);
        Assert.That(item.LoginItem.Notes, Is.EqualTo(NoteText));
    }

    [Test]
    [AllureName("Notes stay stored (and hidden) when the vault is locked; a re-save doesn't wipe them")]
    public async Task LockedVault_DoesNotRevealOrWipeNotes()
    {
        var id = await SaveAsync(new LoginItem { UserId = UserId, Username = "me", EncryptedNotes = _sessions.EncryptPassword(NoteText, _sid) });
        var stored = (await ReloadAsync(id)).LoginItem!.EncryptedNotes;

        var item = await ReloadAsync(id);
        _sessions.LockVault(_sid);
        Assert.That(_reveal.RevealField(item.LoginItem!.EncryptedNotes, item.LoginItem.NotesNonce, item.LoginItem.NotesAuthTag, _sid), Is.Null,
            "a locked vault must reveal nothing (and never the ciphertext)");
        Assert.That(_reveal.OpenItem(item, _sid), Is.False);
        Assert.That(item.LoginItem.Notes, Is.Null);
        // Nothing to seal (Notes empty) → the stored ciphertext is left alone.
        Assert.That(VaultGuard.Services.Security.VaultItemSecrets.HasPlaintext(item), Is.False);
        Assert.That(item.LoginItem.EncryptedNotes, Is.EqualTo(stored));
    }
}
