using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NUnit.Framework;
using VaultGuard.Services.Sharing;

namespace VaultGuard.BackEnd.Tests.Services;

/// <summary>Secure sharing: only ciphertext reaches the server, links run out, and the passphrase matters.</summary>
[TestFixture]
public class SecureShareTests
{
    private string _dir = null!;
    [SetUp] public void SetUp() => _dir = Path.Combine(Path.GetTempPath(), $"shares-{Guid.NewGuid():N}");
    [TearDown] public void TearDown() { try { Directory.Delete(_dir, true); } catch { } }

    private static readonly ShareContent Item = new("Bank", "alice", "Pa55-Share-9", "https://bank.example", "PIN 4321");

    [Test]
    public void Encrypt_RoundTrips_AndNeedsThePassphraseWhenSet()
    {
        var (env, key) = SecureShareCrypto.Encrypt(Item, null);
        Assert.That(SecureShareCrypto.Decrypt(env, key, null), Is.EqualTo(Item));
        Assert.That(JsonSerializer.Serialize(env), Does.Not.Contain("Pa55-Share-9").And.Not.Contain("alice"));

        var (env2, key2) = SecureShareCrypto.Encrypt(Item, "correct horse");
        Assert.That(env2.Salt, Is.Not.Null);
        Assert.That(SecureShareCrypto.Decrypt(env2, key2, "correct horse"), Is.EqualTo(Item));
        Assert.Throws(Is.InstanceOf<CryptographicException>(), () => SecureShareCrypto.Decrypt(env2, key2, "wrong"));
        Assert.Throws(Is.InstanceOf<CryptographicException>(), () => SecureShareCrypto.Decrypt(env2, key2, null));
        // the link key alone, or another link's key, opens nothing
        var (_, otherKey) = SecureShareCrypto.Encrypt(Item, null);
        Assert.Throws(Is.InstanceOf<CryptographicException>(), () => SecureShareCrypto.Decrypt(env, otherKey, null));
    }

    [Test]
    public void Store_HoldsOnlyCiphertext_AndRunsOut()
    {
        var store = new FileShareStore(_dir);
        var (env, _) = SecureShareCrypto.Encrypt(Item, null);
        var (id, _, error) = store.Create(env, TimeSpan.FromHours(1), 2, "u1");
        Assert.That(error, Is.Null);
        var onDisk = string.Concat(Directory.GetFiles(_dir).Select(File.ReadAllText));
        Assert.That(onDisk, Does.Not.Contain("Pa55-Share-9").And.Not.Contain("alice").And.Not.Contain("bank.example"));

        Assert.That(store.TakeView(id!)?.ViewsLeft, Is.EqualTo(1));
        Assert.That(store.TakeView(id!)?.ViewsLeft, Is.EqualTo(0));
        Assert.That(store.TakeView(id!), Is.Null, "used up");
        Assert.That(Directory.GetFiles(_dir), Is.Empty, "deleted once used up");

        var (id2, _, _) = store.Create(env, TimeSpan.FromHours(1), null, "u1");
        Assert.That(store.Delete(id2!, "someone-else"), Is.False);
        Assert.That(store.Delete(id2!, "u1"), Is.True);
        Assert.That(store.TakeView(id2!), Is.Null);
        Assert.That(store.TakeView("../../etc/passwd"), Is.Null);
    }

    [Test]
    public void Store_CapsLifetimeAndViews()
    {
        var store = new FileShareStore(_dir);
        var (env, _) = SecureShareCrypto.Encrypt(Item, null);
        var (_, expires, _) = store.Create(env, TimeSpan.FromDays(400), 1000, "u1");
        Assert.That(expires, Is.LessThanOrEqualTo(DateTime.UtcNow + FileShareStore.MaxLifetime + TimeSpan.FromMinutes(1)));
    }

    private sealed class FakeServer : HttpMessageHandler
    {
        public HttpRequestMessage? Request; public string? Body;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Request = request; Body = await request.Content!.ReadAsStringAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent("{\"id\":\"abcDEF123456789\",\"expiresUtc\":\"2030-01-01T00:00:00Z\"}", Encoding.UTF8, "application/json") };
        }
    }

    [Test]
    public async Task Client_SendsOnlyCiphertextWithTheApiKey_AndPutsTheKeyInTheFragment()
    {
        var server = new FakeServer();
        var r = await SecureShareClient.CreateAsync(new HttpClient(server), "https://vault.example.com/", "key-123", Item,
            new ShareOptions(TimeSpan.FromDays(1), 1, null));
        Assert.That(r.Success, Is.True, r.Error);
        Assert.That(r.Link, Does.StartWith("https://vault.example.com/s/abcDEF123456789#"));
        Assert.That(server.Request!.RequestUri!.AbsolutePath, Is.EqualTo("/api/shares"));
        Assert.That(server.Request.Headers.GetValues("X-API-Key").Single(), Is.EqualTo("key-123"));
        Assert.That(server.Body, Does.Not.Contain("Pa55-Share-9").And.Not.Contain("alice"));
        var key = r.Link!.Split('#')[1];
        Assert.That(server.Body, Does.Not.Contain(key), "the link key never goes to the server");

        var insecure = await SecureShareClient.CreateAsync(new HttpClient(server), "http://vault.example.com", "k", Item, new ShareOptions(TimeSpan.FromDays(1), 1, null));
        Assert.That(insecure.Success, Is.False);
    }
}
