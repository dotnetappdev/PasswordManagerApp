using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using VaultGuard.Crypto.Extensions;
using VaultGuard.Crypto.Interfaces;
using VaultGuard.DAL;
using VaultGuard.Models;
using VaultGuard.Services.BrowserLink;
using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Security;
using VaultGuard.Services.Services;

namespace VaultGuard.BackEnd.Tests.Services;

/// <summary>
/// The browser link is end-to-end encrypted: pairing is an ECDH key exchange (only public keys cross the link),
/// and every request and answer is AES-256-GCM encrypted, so the helper and the pipe never carry a password.
/// </summary>
[TestFixture]
public class BrowserLinkEncryptionTests
{
    private string _dir = null!;
    private ServiceProvider _sp = null!;
    private BrowserLinkServer _server = null!;
    private string _sid = null!;

    private sealed class Approve : IBrowserLinkApprover
    {
        public Task<bool> ApproveAsync(string d, CancellationToken ct) => Task.FromResult(true);
    }

    [SetUp]
    public void SetUp()
    {
        _dir = Path.Combine(Path.GetTempPath(), $"link-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_dir);
        var s = new ServiceCollection();
        s.AddLogging(); s.AddCryptographyServices(); s.AddSingleton<IVaultSessionService, VaultSessionService>();
        s.AddDbContext<VaultGuardDbContext>(o => o.UseSqlite($"Data Source={Path.Combine(_dir, "v.db")};Pooling=False"));
        s.AddScoped<VaultGuard.DAL.Interfaces.IVaultGuardDbContext>(p => p.GetRequiredService<VaultGuardDbContext>());
        s.AddScoped<IPasswordEncryptionService, PasswordEncryptionService>();
        s.AddScoped<Fido2NetLib.IFido2>(_ => new Fido2NetLib.Fido2(new Fido2NetLib.Fido2Configuration { ServerDomain = "localhost", ServerName = "t", Origins = new HashSet<string> { "https://localhost" } }));
        s.AddScoped<IPasskeyService, PasskeyService>();
        _sp = s.BuildServiceProvider();
        var crypto = _sp.GetRequiredService<IPasswordCryptoService>();
        var sessions = _sp.GetRequiredService<IVaultSessionService>();
        var sid = _sid = sessions.InitializeSession("me", crypto.DeriveMasterKey("pw", crypto.GenerateUserSalt()));
        using (var scope = _sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VaultGuardDbContext>();
            db.Database.EnsureCreated();
            db.Users.Add(new ApplicationUser { Id = "me", UserName = "me", Email = "me@x" });
            var item = new PasswordItem { Title = "Bank", Type = ItemType.Login, UserId = "me",
                LoginItem = new LoginItem { WebsiteUrl = "https://bank.example", Username = "alice", Password = "Secret-Pw-42", UserId = "me" } };
            VaultCipher.ForSession(sessions, sid)!.Seal(item);
            db.PasswordItems.Add(item);
            db.SaveChanges();
        }
        _server = new BrowserLinkServer(_sp, new Approve(), "Vault Guard", "test", new BrowserLinkPairings(Path.Combine(_dir, "pairings.json")));
    }

    [TearDown]
    public async Task TearDown()
    {
        await _server.DisposeAsync();
        await _sp.DisposeAsync();
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_dir, true); } catch { }
    }

    private static JsonElement J(object o) => JsonDocument.Parse(JsonSerializer.Serialize(o)).RootElement;
    private static string S(object o) => JsonSerializer.Serialize(o);

    private async Task<(string KeyId, byte[] Key)> PairAsync()
    {
        var (pub, priv) = BrowserLinkCrypto.NewClient();
        using (priv)
        {
            var reply = J(await _server.HandleAsync(J(new { action = "pair", clientPublicKey = pub, browser = "Test" })));
            Assert.That(reply.GetProperty("success").GetBoolean(), Is.True);
            Assert.That(reply.TryGetProperty("secret", out _), Is.False, "no shared secret is ever sent");
            var appPub = reply.GetProperty("appPublicKey").GetString()!;
            return (reply.GetProperty("keyId").GetString()!, BrowserLinkCrypto.ClientKey(priv, pub, appPub));
        }
    }

    private async Task<(object Raw, string Iv)> SendAsync(string keyId, byte[] key, string action, object fields)
    {
        var (iv, ct) = BrowserLinkCrypto.SealRequest(key, action, keyId, fields);
        return (await _server.HandleAsync(J(new { action, keyId, iv, ct })), iv);
    }

    [Test]
    public async Task PasswordTravelsOnlyEncrypted_AndDecryptsForThePairedBrowser()
    {
        var (keyId, key) = await PairAsync();
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var hello = J(await SendAsync(keyId, key, "hello", new { ts = now }).ContinueWith(t => t.Result.Raw));
        Assert.That(hello.GetProperty("paired").GetBoolean(), Is.True);
        Assert.That(hello.GetProperty("protocol").GetInt32(), Is.EqualTo(3));

        var (raw, iv) = await SendAsync(keyId, key, "getCredentials", new { domain = "bank.example", ts = now });
        var wire = S(raw);
        Assert.That(wire, Does.Not.Contain("Secret-Pw-42"), "the answer on the pipe must be ciphertext");
        Assert.That(wire, Does.Not.Contain("alice"));

        var env = J(raw).GetProperty("enc");
        using var answer = BrowserLinkCrypto.OpenResponse(key, "getCredentials", keyId, iv, env.GetProperty("iv").GetString()!, env.GetProperty("ct").GetString()!);
        var cred = answer.RootElement.GetProperty("credentials")[0];
        Assert.That(cred.GetProperty("password").GetString(), Is.EqualTo("Secret-Pw-42"));
    }

    [Test]
    public async Task PlainOrForgedOrStaleRequests_AreRefused()
    {
        var (keyId, key) = await PairAsync();
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Unencrypted request (what protocol 2 sent), even with a valid key id: refused.
        var plain = J(await _server.HandleAsync(J(new { action = "getCredentials", keyId, domain = "bank.example" })));
        Assert.That(plain.GetProperty("code").GetString(), Is.EqualTo("NOT_PAIRED"));

        // Encrypted with someone else's key.
        var wrongKey = new byte[32]; Random.Shared.NextBytes(wrongKey);
        var forged = J((await SendAsync(keyId, wrongKey, "getCredentials", new { domain = "bank.example", ts = now })).Raw);
        Assert.That(forged.GetProperty("code").GetString(), Is.EqualTo("NOT_PAIRED"));

        // Sealed for one action, sent as another (AAD binds the action).
        var (iv, ct) = BrowserLinkCrypto.SealRequest(key, "getTotpCode", keyId, new { ts = now });
        var swapped = J(await _server.HandleAsync(J(new { action = "getCredentials", keyId, iv, ct })));
        Assert.That(swapped.GetProperty("code").GetString(), Is.EqualTo("NOT_PAIRED"));

        // Old (replayed) request.
        var stale = J((await SendAsync(keyId, key, "getCredentials", new { domain = "bank.example", ts = now - 10 * 60_000 })).Raw);
        Assert.That(stale.GetProperty("code").GetString(), Is.EqualTo("NOT_PAIRED"));

        // Old-style pairing request without a public key: asked to update, nothing issued.
        var oldPair = J(await _server.HandleAsync(J(new { action = "pair", browser = "Old" })));
        Assert.That(oldPair.GetProperty("code").GetString(), Is.EqualTo("UPDATE_EXTENSION"));
    }

    [Test]
    public void PairingsFile_HoldsNoUsableKeyOnWindows_AndNothingReadableOtherwiseOutsideTheProfile()
    {
        var file = Path.Combine(_dir, "p.json");
        var pairings = new BrowserLinkPairings(file);
        var key = Encoding.ASCII.GetBytes("0123456789abcdef0123456789abcdef");
        var id = pairings.Create("Test", key);
        Assert.That(pairings.KeyFor(id), Is.EqualTo(key));
        Assert.That(pairings.KeyFor("nope"), Is.Null);
        if (OperatingSystem.IsWindows())
            Assert.That(File.ReadAllText(file), Does.Contain("dpapi:").And.Not.Contain(Convert.ToBase64String(key)));
        pairings.RevokeAll();
        Assert.That(pairings.KeyFor(id), Is.Null);
    }

    private async Task<JsonElement> CallAsync(string keyId, byte[] key, string action, object fields)
    {
        var (raw, iv) = await SendAsync(keyId, key, action, fields);
        var e = J(raw);
        if (!e.TryGetProperty("enc", out var env)) return e;
        using var doc = BrowserLinkCrypto.OpenResponse(key, action, keyId, iv, env.GetProperty("iv").GetString()!, env.GetProperty("ct").GetString()!);
        return doc.RootElement.Clone();
    }

    [Test]
    public async Task Passkeys_CreateAndSign_OnlyForTheTabsSite_AndKeepTheSessionKey()
    {
        var (keyId, key) = await PairAsync();
        long Now() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var sessions = _sp.GetRequiredService<IVaultSessionService>();
        var before = (byte[])sessions.GetMasterKey(_sid)!.Clone();

        var created = await CallAsync(keyId, key, "passkeyCreate", new { rpId = "example.com", site = "login.example.com", userName = "alice", userHandle = "dTE", ts = Now() });
        Assert.That(created.GetProperty("success").GetBoolean(), Is.True, created.ToString());
        Assert.That(sessions.GetMasterKey(_sid), Is.EqualTo(before), "using a passkey must not wipe the unlocked session's key");

        var check = await CallAsync(keyId, key, "passkeyGet", new { rpId = "example.com", site = "example.com", check = true, ts = Now() });
        Assert.That(check.GetProperty("available").GetBoolean(), Is.True);
        Assert.That(check.GetProperty("userName").GetString(), Is.EqualTo("alice"));

        string ClientData(string origin) => BrowserLinkCrypto.ToB64(Encoding.UTF8.GetBytes($"{{\"type\":\"webauthn.get\",\"challenge\":\"AAAA\",\"origin\":\"{origin}\"}}"));
        var signed = await CallAsync(keyId, key, "passkeyGet", new { rpId = "example.com", site = "example.com", clientDataJSON = ClientData("https://example.com"), ts = Now() });
        Assert.That(signed.GetProperty("success").GetBoolean(), Is.True, signed.ToString());
        Assert.That(signed.GetProperty("signature").GetString(), Is.Not.Empty);

        // A look-alike site asking for example.com's passkey, a page whose signed origin is elsewhere, and a
        // passkey for a public suffix: all refused.
        var evil = await CallAsync(keyId, key, "passkeyGet", new { rpId = "example.com", site = "example.com.evil.net", clientDataJSON = ClientData("https://example.com.evil.net"), ts = Now() });
        Assert.That(evil.GetProperty("success").GetBoolean(), Is.False);
        var wrongOrigin = await CallAsync(keyId, key, "passkeyGet", new { rpId = "example.com", site = "example.com", clientDataJSON = ClientData("https://evil.net"), ts = Now() });
        Assert.That(wrongOrigin.GetProperty("success").GetBoolean(), Is.False);
        var suffix = await CallAsync(keyId, key, "passkeyCreate", new { rpId = "co.uk", site = "bank.co.uk", ts = Now() });
        Assert.That(suffix.GetProperty("success").GetBoolean(), Is.False);
    }
}
