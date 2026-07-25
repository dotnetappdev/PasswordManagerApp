using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VaultGuard.Models.Configuration;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.Services.Services;

/// <summary>
/// "Sign in with &lt;identity provider&gt;" for the WPF desktop app - identity verification only,
/// exactly like the Blazor host's SSO (see SsoConfiguration's doc comment): it hands back a verified
/// email so the login screen's profile picker can auto-select the matching local account, and never
/// touches the master password or vault encryption key.
///
/// Provider-agnostic by design: every provider in Sso:Providers is plain OIDC. Its Authority is
/// combined with the standard "/.well-known/openid-configuration" discovery document to find the
/// real authorization/token endpoints at sign-in time, so Google, Microsoft Entra ID / Azure AD,
/// ADFS in OIDC mode, Okta, Keycloak, or any other OIDC-compliant IdP all work through this one code
/// path with zero vendor-specific branches - only configuration changes.
///
/// Uses the public-client loopback redirect + PKCE flow (RFC 8252) for every provider, the same
/// pattern already used by OneDriveBackupService for OneDrive sign-in. No third-party OIDC library:
/// just the BCL HttpListener plus plain HTTPS calls to the IdP's discovery/token endpoints, then
/// reading the "email" claim out of the returned id_token's payload.
/// </summary>
public class OidcSsoService : IOidcSsoService
{
    private readonly ILogger<OidcSsoService> _logger;
    private readonly HttpClient _http;
    private readonly SsoConfiguration _config;

    public OidcSsoService(ILogger<OidcSsoService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _config = configuration.GetSection("Sso").Get<SsoConfiguration>() ?? new SsoConfiguration();
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("VaultGuard/1.0");
    }

    public IReadOnlyList<SsoProviderConfig> ConfiguredProviders => _config.ConfiguredProviders.ToList();

    public async Task<OidcSsoResult> SignInAsync(string providerId, CancellationToken ct = default)
    {
        var provider = ConfiguredProviders.FirstOrDefault(p => string.Equals(p.Id, providerId, StringComparison.OrdinalIgnoreCase));
        if (provider == null)
            return Fail($"'{providerId}' is not a configured sign-in provider.");

        var (authorizationEndpoint, tokenEndpoint) = await ResolveEndpointsAsync(provider, ct);
        if (authorizationEndpoint == null || tokenEndpoint == null)
            return Fail($"Could not determine {provider.DisplayName}'s sign-in endpoints. Check its Authority in appsettings.json.");

        var verifier = GenerateCodeVerifier();
        var challenge = GenerateCodeChallenge(verifier);

        var port = FindFreePort();
        // RFC 8252 recommends the literal loopback IP (127.0.0.1) over "localhost" for native apps.
        // Most OIDC "Desktop app" / public native client registrations accept any port on it without
        // a pre-registered redirect URI; a handful (Google is the best-known example) still require
        // the client to have been created with that client type, but no per-provider code differs.
        var redirectUri = $"http://127.0.0.1:{port}";
        var listenerPrefix = $"{redirectUri}/";

        var authUrl = $"{authorizationEndpoint}?response_type=code" +
                      $"&client_id={Uri.EscapeDataString(provider.ClientId!)}" +
                      $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                      $"&scope={Uri.EscapeDataString(provider.Scopes)}" +
                      $"&code_challenge={challenge}" +
                      $"&code_challenge_method=S256";

        string? code = null;
        string? authError = null;
        try
        {
            Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

            using var listener = new HttpListener();
            listener.Prefixes.Add(listenerPrefix);
            listener.Start();
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(TimeSpan.FromMinutes(5));
                var context = await listener.GetContextAsync().WaitAsync(timeoutCts.Token);
                code = context.Request.QueryString["code"];
                authError = context.Request.QueryString["error_description"] ?? context.Request.QueryString["error"];
                var html = string.IsNullOrEmpty(code)
                    ? $"<html><body><h2>VaultGuard: sign-in failed — {WebUtility.HtmlEncode(authError ?? "no authorization code returned")}. You can close this tab.</h2></body></html>"
                    : "<html><body><h2>VaultGuard: sign-in complete — you can close this tab.</h2></body></html>";
                var buf = Encoding.UTF8.GetBytes(html);
                context.Response.ContentLength64 = buf.Length;
                await context.Response.OutputStream.WriteAsync(buf, timeoutCts.Token);
                context.Response.Close();
            }
            finally
            {
                listener.Stop();
            }
        }
        catch (OperationCanceledException)
        {
            return Fail("Sign-in timed out waiting for the browser to redirect back (5 minutes).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SSO loopback listener failed for provider {Provider}", provider.Id);
            return Fail($"Could not start the local sign-in listener: {ex.Message}");
        }

        if (string.IsNullOrEmpty(code))
        {
            return Fail(string.IsNullOrEmpty(authError)
                ? $"No authorization code was returned by {provider.DisplayName}. The sign-in may have been cancelled."
                : $"{provider.DisplayName} returned an error: {authError}");
        }

        var form = new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = provider.ClientId!,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code",
            ["code_verifier"] = verifier,
        };
        if (!string.IsNullOrWhiteSpace(provider.ClientSecret))
            form["client_secret"] = provider.ClientSecret;

        HttpResponseMessage resp;
        string respBody;
        try
        {
            resp = await _http.PostAsync(tokenEndpoint, new FormUrlEncodedContent(form), ct);
            respBody = await resp.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SSO token exchange request failed for provider {Provider}", provider.Id);
            return Fail($"Could not reach {provider.DisplayName}'s token endpoint: {ex.Message}");
        }

        if (!resp.IsSuccessStatusCode)
        {
            var detail = TryExtractOAuthError(respBody) ?? respBody;
            _logger.LogError("SSO token exchange failed for {Provider}: {Status} {Body}", provider.Id, resp.StatusCode, respBody);
            return Fail($"Token exchange failed ({(int)resp.StatusCode} {resp.StatusCode}): {detail}");
        }

        string? idToken;
        try
        {
            using var doc = JsonDocument.Parse(respBody);
            idToken = doc.RootElement.TryGetProperty("id_token", out var t) ? t.GetString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not parse {Provider}'s token response", provider.Id);
            return Fail($"{provider.DisplayName}'s token response could not be parsed.");
        }

        if (string.IsNullOrEmpty(idToken))
            return Fail($"{provider.DisplayName} did not return an ID token.");

        var email = TryGetVerifiedEmailFromIdToken(idToken);
        if (string.IsNullOrWhiteSpace(email))
            return Fail($"{provider.DisplayName}'s ID token did not include a verified email address.");

        return new OidcSsoResult { Success = true, Email = email };
    }

    // Discovers the authorization/token endpoints from the IdP's standard OIDC discovery document
    // (Authority + "/.well-known/openid-configuration") - this is the one piece of machinery that
    // makes any OIDC-compliant IdP work without provider-specific code. Falls back to the explicit
    // override endpoints in config for the rare IdP without a usable discovery document.
    private async Task<(string? AuthorizationEndpoint, string? TokenEndpoint)> ResolveEndpointsAsync(SsoProviderConfig provider, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(provider.AuthorizationEndpointOverride) && !string.IsNullOrWhiteSpace(provider.TokenEndpointOverride))
            return (provider.AuthorizationEndpointOverride, provider.TokenEndpointOverride);

        if (string.IsNullOrWhiteSpace(provider.Authority))
            return (provider.AuthorizationEndpointOverride, provider.TokenEndpointOverride);

        try
        {
            var discoveryUrl = $"{provider.Authority.TrimEnd('/')}/.well-known/openid-configuration";
            var respBody = await _http.GetStringAsync(discoveryUrl, ct);
            using var doc = JsonDocument.Parse(respBody);
            var root = doc.RootElement;

            var authEndpoint = provider.AuthorizationEndpointOverride
                ?? (root.TryGetProperty("authorization_endpoint", out var a) ? a.GetString() : null);
            var tokenEndpoint = provider.TokenEndpointOverride
                ?? (root.TryGetProperty("token_endpoint", out var t) ? t.GetString() : null);

            return (authEndpoint, tokenEndpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OIDC discovery failed for provider {Provider} (Authority: {Authority})", provider.Id, provider.Authority);
            return (provider.AuthorizationEndpointOverride, provider.TokenEndpointOverride);
        }
    }

    // The id_token is a JWT; we only need the payload for the "email"/"email_verified" claims to
    // speed up profile selection (not a security decision), so we read it without verifying the
    // signature - the same trust boundary the Blazor host's OIDC callback uses.
    private static string? TryGetVerifiedEmailFromIdToken(string idToken)
    {
        var parts = idToken.Split('.');
        if (parts.Length < 2) return null;

        var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
        using var doc = JsonDocument.Parse(payloadJson);
        var root = doc.RootElement;

        // Not every IdP sets email_verified (some omit it entirely for enterprise directories where
        // the IdP itself is the source of truth for verification) - only treat it as a hard
        // requirement when the claim is actually present and explicitly false.
        var emailVerifiedClaimPresent = root.TryGetProperty("email_verified", out var ev);
        var emailVerified = !emailVerifiedClaimPresent ||
                             ev.ValueKind == JsonValueKind.True ||
                             (ev.ValueKind == JsonValueKind.String && ev.GetString() == "true");
        if (!emailVerified) return null;

        return root.TryGetProperty("email", out var e) ? e.GetString() : null;
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }

    private static string? TryExtractOAuthError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var code = root.TryGetProperty("error", out var e) ? e.GetString() : null;
            var desc = root.TryGetProperty("error_description", out var d) ? d.GetString() : null;
            return desc != null ? $"{code}: {desc}" : code;
        }
        catch (Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", logEx); return null; }
    }

    private static OidcSsoResult Fail(string msg) => new() { Success = false, ErrorMessage = msg };

    private static int FindFreePort()
    {
        var l = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }

    private static string GenerateCodeVerifier()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string GenerateCodeChallenge(string verifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
