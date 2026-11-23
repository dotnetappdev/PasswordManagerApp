using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.Services.Aliases;

public enum EmailAliasProvider { None, SimpleLogin, Addy }

/// <summary>
/// Creates a unique forwarding email address ("alias") for a site with the user's own SimpleLogin or addy.io account,
/// so each site gets a different address that forwards to their real inbox and can be switched off if it leaks or
/// gets spam. Settings (shared settings.json): EmailAlias:Provider, EmailAlias:ServerUrl (optional, for a self-hosted
/// server) and EmailAliasToken (the provider's API key; encrypted at rest on Windows, see LocalSettingsProtector).
/// Only the site's name goes to the provider, as the alias's note.
/// </summary>
public sealed class EmailAliasService
{
    public sealed record Result(bool Success, string? Email, string? Error);

    private readonly IAppSettingsService _settings;
    private readonly HttpClient _http;

    public EmailAliasService(IAppSettingsService settings, HttpClient? http = null)
    {
        _settings = settings;
        _http = http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
    }

    public EmailAliasProvider Provider =>
        Enum.TryParse<EmailAliasProvider>(_settings.Get("EmailAlias:Provider"), ignoreCase: true, out var p) ? p : EmailAliasProvider.None;

    public bool IsConfigured => Provider != EmailAliasProvider.None && !string.IsNullOrWhiteSpace(_settings.Get("EmailAliasToken"));

    public static string DefaultServer(EmailAliasProvider p) => p switch
    {
        EmailAliasProvider.SimpleLogin => "https://app.simplelogin.io",
        EmailAliasProvider.Addy => "https://app.addy.io",
        _ => ""
    };

    /// <summary>Creates a new random alias, noted with the site it's for.</summary>
    public async Task<Result> CreateAsync(string? site, CancellationToken ct = default)
    {
        var provider = Provider;
        var token = _settings.Get("EmailAliasToken");
        if (provider == EmailAliasProvider.None || string.IsNullOrWhiteSpace(token))
            return new Result(false, null, "Set up email aliases first (Settings → Email aliases).");
        var server = _settings.Get("EmailAlias:ServerUrl");
        if (string.IsNullOrWhiteSpace(server)) server = DefaultServer(provider);
        if (!Uri.TryCreate(server.TrimEnd('/'), UriKind.Absolute, out var baseUri) || (baseUri.Scheme != "https" && !baseUri.IsLoopback))
            return new Result(false, null, "The alias server address must start with https://.");
        var note = string.IsNullOrWhiteSpace(site) ? "Created by Vault Guard" : $"{site} (created by Vault Guard)";

        try
        {
            using var req = provider == EmailAliasProvider.SimpleLogin
                ? new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, "/api/alias/random/new" + (string.IsNullOrWhiteSpace(site) ? "" : "?hostname=" + Uri.EscapeDataString(site))))
                  { Content = JsonContent.Create(new { note }) }
                : new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, "/api/v1/aliases"))
                  { Content = JsonContent.Create(new { description = note, format = "random_characters" }) };
            if (provider == EmailAliasProvider.SimpleLogin) req.Headers.Add("Authentication", token);
            else
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                req.Headers.Add("X-Requested-With", "XMLHttpRequest");
            }
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var resp = await _http.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
                return new Result(false, null, resp.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden
                    ? $"{Name(provider)} didn't accept the API key."
                    : $"{Name(provider)} couldn't create an alias ({(int)resp.StatusCode}){ErrorText(body)}.");
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var email = provider == EmailAliasProvider.SimpleLogin
                ? (root.TryGetProperty("alias", out var a) ? a.GetString() : root.TryGetProperty("email", out var e) ? e.GetString() : null)
                : root.TryGetProperty("data", out var d) && d.TryGetProperty("email", out var de) ? de.GetString() : null;
            return string.IsNullOrWhiteSpace(email) || !email.Contains('@')
                ? new Result(false, null, $"{Name(provider)} sent an unexpected answer.")
                : new Result(true, email, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new Result(false, null, $"Couldn't reach {Name(provider)}: {ex.Message}");
        }
    }

    public static string Name(EmailAliasProvider p) => p == EmailAliasProvider.Addy ? "addy.io" : p == EmailAliasProvider.SimpleLogin ? "SimpleLogin" : "the alias service";

    private static string ErrorText(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var msg = doc.RootElement.TryGetProperty("error", out var e) ? e.ToString() : doc.RootElement.TryGetProperty("message", out var m) ? m.GetString() : null;
            return string.IsNullOrWhiteSpace(msg) ? "" : $": {msg}";
        }
        catch { return ""; }
    }
}
