using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using VaultGuard.Crypto.Extensions;
using VaultGuard.Crypto.Interfaces;
using VaultGuard.DAL;
using VaultGuard.Models;
using VaultGuard.Services.Emergency;
using VaultGuard.Services.Security;

namespace VaultGuard.BackEnd.Tests.Services;

/// <summary>Emergency access: a trusted contact reads the vault only after the wait (or approval), never before.</summary>
[TestFixture]
public class EmergencyAccessTests
{
    private string _dir = null!;
    private DbContextOptions<VaultGuardDbContext> _options = null!;
    private IPasswordCryptoService _crypto = null!;
    private byte[] _aliceKey = null!, _bobKey = null!;

    [SetUp]
    public void SetUp()
    {
        _dir = Path.Combine(Path.GetTempPath(), $"ea-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_dir);
        _options = new DbContextOptionsBuilder<VaultGuardDbContext>().UseSqlite($"Data Source={Path.Combine(_dir, "v.db")};Pooling=False").Options;
        _crypto = new ServiceCollection().AddLogging().AddCryptographyServices().BuildServiceProvider().GetRequiredService<IPasswordCryptoService>();
        _aliceKey = _crypto.DeriveMasterKey("alice-pw", _crypto.GenerateUserSalt());
        _bobKey = _crypto.DeriveMasterKey("bob-pw", _crypto.GenerateUserSalt());
        using var db = new VaultGuardDbContext(_options);
        db.Database.EnsureCreated();
        db.Users.Add(new ApplicationUser { Id = "alice", UserName = "alice", Email = "alice@x.test" });
        db.Users.Add(new ApplicationUser { Id = "bob", UserName = "bob", Email = "bob@x.test" });
        var item = new PasswordItem { Title = "Bank", Type = ItemType.Login, UserId = "alice",
            LoginItem = new LoginItem { UserId = "alice", Username = "alice", Password = "Alice-Bank-Pw" } };
        using (var c = VaultCipher.ForKey(_crypto, (byte[])_aliceKey.Clone(), ownsKey: true)) c.Seal(item);
        db.PasswordItems.Add(item);
        db.SaveChanges();
    }

    [TearDown] public void TearDown() { SqliteConnection.ClearAllPools(); try { Directory.Delete(_dir, true); } catch { } }

    private EmergencyAccessService Svc() => new(Path.Combine(_dir, "ea"), _crypto);
    private VaultGuardDbContext Db() => new(_options);

    [Test]
    public async Task FullFlow_WaitOrApproval_ThenReadOnlyVault()
    {
        var svc = Svc();
        using var db = Db();
        var alice = db.Users.Single(u => u.Id == "alice");
        var (grant, error) = svc.AddContact(db, alice, "bob@x.test", 7);
        Assert.That(error, Is.Null);
        Assert.That(svc.Request(grant!.Id, "bob"), Is.True, "Bob may ask even before keys are exchanged");
        Assert.That(svc.OpenVaultKey(grant.Id, "bob", _bobKey, out _), Is.Null, "not before the wait");

        svc.OnUnlocked("bob", _bobKey);     // Bob signs in: gets a key pair
        svc.OnUnlocked("alice", _aliceKey); // Alice signs in: her key is wrapped for Bob
        var fileText = string.Concat(Directory.GetFiles(Path.Combine(_dir, "ea"), "*.json", SearchOption.AllDirectories).Select(File.ReadAllText));
        Assert.That(fileText, Does.Not.Contain(Convert.ToBase64String(_aliceKey)), "the vault key is never stored readable");

        Assert.That(svc.OpenVaultKey(grant.Id, "bob", _bobKey, out var wait), Is.Null);
        Assert.That(wait, Does.Contain("opens on"));
        Assert.That(svc.PendingRequestsFor("alice"), Has.Count.EqualTo(1));

        Assert.That(svc.Approve(grant.Id, "bob"), Is.False, "only Alice can approve");
        Assert.That(svc.Approve(grant.Id, "alice"), Is.True);
        Assert.That(svc.OpenVaultKey(grant.Id, "mallory", _bobKey, out _), Is.Null, "only Bob");
        var key = svc.OpenVaultKey(grant.Id, "bob", _bobKey, out _);
        Assert.That(key, Is.EqualTo(_aliceKey));
        var items = await svc.ReadItemsAsync(db, grant.Id, "bob", key!);
        Assert.That(items.Single().LoginItem!.Password, Is.EqualTo("Alice-Bank-Pw"));

        Assert.That(svc.Remove(grant.Id, "alice"), Is.True);
        Assert.That(svc.OpenVaultKey(grant.Id, "bob", _bobKey, out _), Is.Null, "revoked");
    }

    [Test]
    public void Decline_ClosesTheRequest_AndWaitingOpensItOtherwise()
    {
        var svc = Svc();
        using var db = Db();
        var (grant, _) = svc.AddContact(db, db.Users.Single(u => u.Id == "alice"), "bob@x.test", 1);
        svc.OnUnlocked("bob", _bobKey); svc.OnUnlocked("alice", _aliceKey);
        svc.Request(grant!.Id, "bob");
        Assert.That(svc.Decline(grant.Id, "alice"), Is.True);
        Assert.That(svc.OpenVaultKey(grant.Id, "bob", _bobKey, out _), Is.Null);

        // Simulate the waiting period passing without an answer.
        svc.Request(grant.Id, "bob");
        var path = Directory.GetFiles(Path.Combine(_dir, "ea", "grants")).Single();
        var json = File.ReadAllText(path);
        var g = System.Text.Json.JsonSerializer.Deserialize<EmergencyGrant>(json)!;
        g.RequestedUtc = DateTime.UtcNow.AddDays(-2);
        File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(g));
        Assert.That(svc.OpenVaultKey(grant.Id, "bob", _bobKey, out _), Is.EqualTo(_aliceKey));
    }

    [Test]
    public void NewMasterPassword_IsCarriedOverOnNextUnlock()
    {
        var svc = Svc();
        using var db = Db();
        var (grant, _) = svc.AddContact(db, db.Users.Single(u => u.Id == "alice"), "bob@x.test", 1);
        svc.OnUnlocked("bob", _bobKey); svc.OnUnlocked("alice", _aliceKey);
        svc.Request(grant!.Id, "bob"); svc.Approve(grant.Id, "alice");

        var newAliceKey = _crypto.DeriveMasterKey("alice-new", _crypto.GenerateUserSalt());
        svc.OnUnlocked("alice", newAliceKey);
        Assert.That(svc.OpenVaultKey(grant.Id, "bob", _bobKey, out _), Is.EqualTo(newAliceKey));

        // Bob changes his master password: he gets new keys, and Alice re-wraps on her next unlock.
        var newBobKey = _crypto.DeriveMasterKey("bob-new", _crypto.GenerateUserSalt());
        svc.OnUnlocked("bob", newBobKey);
        Assert.That(svc.OpenVaultKey(grant.Id, "bob", newBobKey, out var why), Is.Null);
        Assert.That(why, Does.Contain("keys changed"));
        svc.OnUnlocked("alice", newAliceKey);
        Assert.That(svc.OpenVaultKey(grant.Id, "bob", newBobKey, out _), Is.EqualTo(newAliceKey));
    }

    [Test]
    public void AddContact_Validates()
    {
        var svc = Svc();
        using var db = Db();
        var alice = db.Users.Single(u => u.Id == "alice");
        Assert.That(svc.AddContact(db, alice, "nobody@x.test", 7).Error, Does.Contain("no Vault Guard account"));
        Assert.That(svc.AddContact(db, alice, "alice@x.test", 7).Error, Does.Contain("other than yourself"));
        Assert.That(svc.AddContact(db, alice, "bob@x.test", 3).Error, Is.Not.Null);
        Assert.That(svc.AddContact(db, alice, "bob@x.test", 7).Error, Is.Null);
        Assert.That(svc.AddContact(db, alice, "bob@x.test", 7).Error, Does.Contain("already"));
    }
}
