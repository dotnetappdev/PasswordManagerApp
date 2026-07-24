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
/// "Sign in with Google" for the WPF desktop app - identity verification only, exactly like the
/// Blazor host's Google sign-in (see SsoConfiguration's doc comment): it hands back a verified
/// email so the login screen's profile picker can auto-select the matching local account, and
/// never touches the master password or vault encryption key.
///
/// Uses Google's "Desktop app" OAuth client type, a public client using the loopback redirect +
/// PKCE flow (RFC 8252) - the same pattern already used by OneDriveBackupService for OneDrive
/// sign-in. No third-party OIDC library: just the BCL HttpListener plus a plain HTTPS POST to
/// Google's token endpoint, then reading the "email" claim out of the returned id_token's payload.
/// </summary>
public class GoogleSsoService : IGoogleSsoService
{
    private readonly ILogger<GoogleSsoService> _logger;
    private readonly HttpClient _http;
    private readonly SsoConfiguration _config;

    private const string AuthEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string Scope = "openid email profile";

    public GoogleSsoService(ILogger<GoogleSsoService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _config = configuration.GetSection("Sso").Get<SsoConfiguration>() ?? new SsoConfiguration();
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("VaultGuard/1.0");
    }

    public bool IsConfigured => _config.IsGoogleDesktopConfigured;

    public async Task<GoogleSsoResult> SignInAsync(CancellationToken ct = default)
    {
        if (!IsConfigured)
            return Fail("Google sign-in is not configured.");

        var clientId = _config.GoogleDesktopClientId!;
        var clientSecret = _config.GoogleDesktopClientSecret!;

        var verifier = GenerateCodeVerifier();
        var challenge = GenerateCodeChallenge(verifier);

        var port = FindFreePort();
        // RFC 8252 recommends the literal loopback IP (127.0.0.1) over "localhost" for native apps,
        // and Google's "Desktop app" OAuth client type accepts any port on it without a registered
        // redirect URI - no setup needed beyond creating the client in Google Cloud Console.
        var redirectUri = $"http://127.0.0.1:{port}";
        var listenerPrefix = $"{redirectUri}/";

        var authUrl = $"{AuthEndpoint}?response_type=code" +
                      $"&client_id={Uri.EscapeDataString(clientId)}" +
                      $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                      $"&scope={Uri.EscapeDataString(Scope)}" +
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
            _logger.LogError(ex, "Google SSO loopback listener failed");
            return Fail($"Could not start the local sign-in listener: {ex.Message}");
        }

        if (string.IsNullOrEmpty(code))
        {
            return Fail(string.IsNullOrEmpty(authError)
                ? "No authorization code was returned by Google. The sign-in may have been cancelled."
                : $"Google returned an error: {authError}");
        }

        var form = new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code",
            ["code_verifier"] = verifier,
        };

        HttpResponseMessage resp;
        string respBody;
        try
        {
            resp = await _http.PostAsync(TokenEndpoint, new FormUrlEncodedContent(form), ct);
            respBody = await resp.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google SSO token exchange request failed");
            return Fail($"Could not reach Google's token endpoint: {ex.Message}");
        }

        if (!resp.IsSuccessStatusCode)
        {
            var detail = TryExtractOAuthError(respBody) ?? respBody;
            _logger.LogError("Google SSO token exchange failed: {Status} {Body}", resp.StatusCode, respBody);
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
            _logger.LogError(ex, "Could not parse Google's token response");
            return Fail("Google's token response could not be parsed.");
        }

        if (string.IsNullOrEmpty(idToken))
            return Fail("Google did not return an ID token.");

        var email = TryGetVerifiedEmailFromIdToken(idToken);
        if (string.IsNullOrWhiteSpace(email))
            return Fail("Google's ID token did not include a verified email address.");

        return new GoogleSsoResult { Success = true, Email = email };
    }

    // The id_token is a JWT; we only need the payload for the "email"/"email_verified" claims to
    // speed up profile selection (not a security decision), so we read it without verifying the
    // signature - the same trust boundary the Blazor host's Google callback uses.
    private static string? TryGetVerifiedEmailFromIdToken(string idToken)
    {
        var parts = idToken.Split('.');
        if (parts.Length < 2) return null;

        var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
        using var doc = JsonDocument.Parse(payloadJson);
        var root = doc.RootElement;

        var emailVerified = root.TryGetProperty("email_verified", out var ev) &&
                             ((ev.ValueKind == JsonValueKind.True) ||
                              (ev.ValueKind == JsonValueKind.String && ev.GetString() == "true"));
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

    private static GoogleSsoResult Fail(string msg) => new() { Success = false, ErrorMessage = msg };

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
