using System.Net;
using System.Text;
using NUnit.Framework;
using VaultGuard.Services.Aliases;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.BackEnd.Tests.Services;

/// <summary>Email aliases: the right request for SimpleLogin and addy.io, and only the site's name is sent.</summary>
[TestFixture]
public class EmailAliasTests
{
    private sealed class Settings : IAppSettingsService
    {
        public readonly Dictionary<string, string> Values = new();
        public string SettingsFilePath => "";
        public string Get(string key, string fallback = "") => Values.TryGetValue(key, out var v) ? v : fallback;
        public void Set(string key, string value) => Values[key] = value;
        public void Set(string key, bool value) => Values[key] = value.ToString();
        public void Set(string key, int value) => Values[key] = value.ToString();
        public bool GetBool(string key, bool fallback = false) => bool.TryParse(Get(key), out var b) ? b : fallback;
        public int GetInt(string key, int fallback = 0) => int.TryParse(Get(key), out var i) ? i : fallback;
        public bool Contains(string key) => Values.ContainsKey(key);
        public void Remove(string key) => Values.Remove(key);
        public void Reload() { }
        public bool Save() => true;
    }

    private sealed class Fake : HttpMessageHandler
    {
        public HttpRequestMessage? Req; public string? Body;
        public HttpStatusCode Status = HttpStatusCode.Created; public string Reply = "{}";
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct)
        {
            Req = r; Body = r.Content == null ? null : await r.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(Status) { Content = new StringContent(Reply, Encoding.UTF8, "application/json") };
        }
    }

    private static (EmailAliasService, Fake, Settings) Make(string provider, string reply)
    {
        var s = new Settings();
        s.Set("EmailAlias:Provider", provider); s.Set("EmailAliasToken", "tok-123");
        var f = new Fake { Reply = reply };
        return (new EmailAliasService(s, new HttpClient(f)), f, s);
    }

    [Test]
    public async Task SimpleLogin_RandomAlias()
    {
        var (svc, f, _) = Make("SimpleLogin", "{\"alias\":\"shop.xyz123@simplelogin.com\",\"id\":7}");
        var r = await svc.CreateAsync("shop.example");
        Assert.That(r.Email, Is.EqualTo("shop.xyz123@simplelogin.com"), r.Error);
        Assert.That(f.Req!.RequestUri!.ToString(), Is.EqualTo("https://app.simplelogin.io/api/alias/random/new?hostname=shop.example"));
        Assert.That(f.Req.Headers.GetValues("Authentication").Single(), Is.EqualTo("tok-123"));
        Assert.That(f.Body, Does.Contain("shop.example (created by Vault Guard)"));
    }

    [Test]
    public async Task Addy_RandomAlias()
    {
        var (svc, f, _) = Make("Addy", "{\"data\":{\"id\":\"x\",\"email\":\"a1b2c3@anonaddy.me\"}}");
        var r = await svc.CreateAsync("shop.example");
        Assert.That(r.Email, Is.EqualTo("a1b2c3@anonaddy.me"), r.Error);
        Assert.That(f.Req!.RequestUri!.ToString(), Is.EqualTo("https://app.addy.io/api/v1/aliases"));
        Assert.That(f.Req.Headers.Authorization!.ToString(), Is.EqualTo("Bearer tok-123"));
        Assert.That(f.Body, Does.Contain("random_characters"));
    }

    [Test]
    public async Task Errors_AreExplained()
    {
        var (svc, f, s) = Make("SimpleLogin", "{\"error\":\"Wrong api key\"}");
        f.Status = HttpStatusCode.Unauthorized;
        Assert.That((await svc.CreateAsync("a.example")).Error, Does.Contain("didn't accept the API key"));

        s.Set("EmailAlias:ServerUrl", "http://aliases.example.com");
        Assert.That((await svc.CreateAsync("a.example")).Error, Does.Contain("https://"));

        s.Set("EmailAlias:Provider", "None");
        Assert.That(svc.IsConfigured, Is.False);
        Assert.That((await svc.CreateAsync("a.example")).Error, Does.Contain("Set up email aliases"));
    }
}
