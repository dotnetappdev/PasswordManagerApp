using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VaultGuard.Models.DTOs;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.Services.Services;

/// <summary>
/// OneDrive backup service using the Microsoft Graph API. Supports personal Microsoft accounts only.
///
/// Stores backups in a visible "VaultGuard" folder at the root of the user's OneDrive, so people
/// can see and manage their encrypted backups directly in OneDrive. The folder is created
/// automatically on first upload via Graph path-addressing.
/// OAuth2 tokens are stored in the local app-data settings file, encrypted with DPAPI on Windows.
///
/// OAuth flow: public-client loopback redirect with PKCE (RFC 8252) — no client secret needed,
/// as long as the Azure AD app registration's platform is "Mobile and desktop applications".
/// </summary>
public class OneDriveBackupService : IOneDriveBackupService
{
    private readonly ILogger<OneDriveBackupService> _logger;
    private readonly HttpClient _http;

    private const string TokenFile = "onedrive_token.json";
    private const string SettingsDir = "VaultGuard";
    private const string BackupMimeType = "application/octet-stream";
    private const string GraphBase = "https://graph.microsoft.com/v1.0";
    // Visible "VaultGuard" folder under the user's Documents in OneDrive — i.e. Documents/VaultGuard,
    // the folder people actually see in the OneDrive web/app. Path-addressed (auto-created on first
    // upload). Full Files.ReadWrite is required to write outside the hidden app folder.
    private const string BackupFolderPath = "Documents/VaultGuard";
    private const string BackupFolderUrl = GraphBase + "/me/drive/root:/" + BackupFolderPath;
    private const string Scope = "offline_access Files.ReadWrite User.Read";

    // VaultGuard's own "Mobile and desktop applications" Azure AD app registration — a public
    // client with no secret, registered to support personal Microsoft accounts. Shipping this
    // means personal users never have to register their own Azure app.
    // Read from configuration (CloudBackup:OneDrive:PersonalClientId in appsettings.json) so the id
    // can be rotated without a code change; falls back to a placeholder if not configured.
    private const string FallbackPersonalClientId = "00000000-0000-0000-0000-000000000000";
    private readonly string _personalClientId;

    private const string TokenEndpoint = "https://login.microsoftonline.com/consumers/oauth2/v2.0/token";
    private const string AuthEndpoint = "https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize";

    public string ServiceName => "OneDrive";
    public long MaxBackupSizeBytes => 100 * 1024 * 1024; // 100 MB

    private StoredToken? _token;

    public OneDriveAccountInfo? AccountInfo { get; private set; }
    public string? LastError { get; private set; }

    public OneDriveBackupService(ILogger<OneDriveBackupService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _personalClientId = configuration["CloudBackup:OneDrive:PersonalClientId"] is { Length: > 0 } configured
            ? configured
            : FallbackPersonalClientId;
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("VaultGuard/1.0");
        LoadStoredCredentials();
    }

    // ─── Auth ───────────────────────────────────────────────────────────────

    private async Task<bool> ConnectWithOAuthAsync(string clientId, CancellationToken ct = default)
    {
        LastError = null;
        if (string.IsNullOrWhiteSpace(clientId))
        {
            LastError = "No client ID was supplied.";
            return false;
        }

        // PKCE
        var verifier = GenerateCodeVerifier();
        var challenge = GenerateCodeChallenge(verifier);

        var port = FindFreePort();
        // Azure AD's loopback "any free port" exemption only matches the literal host "localhost"
        // registered with NO trailing slash/path (just "http://localhost"), and the dynamic value
        // sent in the request must match exactly — including no trailing slash. 127.0.0.1 does NOT
        // get this treatment, and a trailing slash on the dynamic URI breaks the match too, both
        // causing "invalid_request: redirect_uri is not valid" even with the right URI registered.
        // HttpListener prefixes, however, MUST end in "/" — so we need two slightly different strings.
        var redirectUri = $"http://localhost:{port}";
        var listenerPrefix = $"{redirectUri}/";

        var authUrl = $"{AuthEndpoint}?response_type=code" +
                      $"&client_id={Uri.EscapeDataString(clientId)}" +
                      $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                      $"&scope={Uri.EscapeDataString(Scope)}" +
                      $"&code_challenge={challenge}" +
                      $"&code_challenge_method=S256" +
                      $"&response_mode=query";

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
                    ? $"<html><body><h2>VaultGuard: sign-in failed — {System.Net.WebUtility.HtmlEncode(authError ?? "no authorization code returned")}. You can close this tab.</h2></body></html>"
                    : "<html><body><h2>VaultGuard: authorisation complete — you can close this tab.</h2></body></html>";
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
            LastError = "Sign-in timed out waiting for the browser to redirect back (5 minutes). Make sure you completed the Microsoft sign-in page.";
            return false;
        }
        catch (Exception ex)
        {
            LastError = $"Could not start the local sign-in listener: {ex.Message}";
            _logger.LogError(ex, "OneDrive loopback listener failed");
            return false;
        }

        if (string.IsNullOrEmpty(code))
        {
            LastError = string.IsNullOrEmpty(authError)
                ? "No authorization code was returned by Microsoft. The sign-in may have been cancelled."
                : $"Microsoft returned an error: {authError}";
            return false;
        }

        var form = new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = clientId,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code",
            ["code_verifier"] = verifier,
            ["scope"] = Scope,
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
            LastError = $"Could not reach Microsoft's token endpoint: {ex.Message}";
            _logger.LogError(ex, "OneDrive token exchange request failed");
            return false;
        }

        if (!resp.IsSuccessStatusCode)
        {
            // AAD error responses are JSON with an "error" + "error_description" — surface that
            // verbatim instead of just the HTTP status, since it's the only way to tell e.g. an
            // app-registration misconfiguration apart from a redirect URI mismatch.
            var detail = TryExtractOAuthError(respBody) ?? respBody;
            LastError = $"Token exchange failed ({(int)resp.StatusCode} {resp.StatusCode}): {detail}";
            _logger.LogError("OneDrive token exchange failed: {Status} {Body}", resp.StatusCode, respBody);
            return false;
        }

        var tok = JsonSerializer.Deserialize<OAuthTokenResponse>(respBody);
        if (tok == null)
        {
            LastError = "Microsoft's token response could not be parsed.";
            return false;
        }

        _token = new StoredToken
        {
            AccessToken = tok.AccessToken,
            RefreshToken = tok.RefreshToken ?? string.Empty,
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tok.ExpiresIn - 60),
            ClientId = clientId,
        };

        SaveStoredCredentials();
        await RefreshAccountInfoAsync(ct);
        return true;
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
        catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", logEx); return null; }
    }

    public Task<bool> ConnectPersonalAsync(CancellationToken ct = default) =>
        ConnectWithOAuthAsync(_personalClientId, ct);

    public async Task<bool> HasStoredTokenAsync()
    {
        LoadStoredCredentials();
        return _token != null && !string.IsNullOrEmpty(_token.RefreshToken);
    }

    public async Task DisconnectAsync()
    {
        _token = null;
        AccountInfo = null;
        var path = TokenFilePath();
        if (File.Exists(path)) File.Delete(path);
    }

    public async Task<bool> PingAsync(CancellationToken ct = default)
    {
        try
        {
            if (!await HasStoredTokenAsync()) return false;
            await EnsureAccessTokenAsync();
            if (_token == null) return false;

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(8));

            var request = new HttpRequestMessage(HttpMethod.Get, $"{GraphBase}/me?$select=id");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token.AccessToken);
            var resp = await _http.SendAsync(request, timeoutCts.Token);
            return resp.IsSuccessStatusCode;
        }
        catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", logEx); return false; }
    }

    public async Task<bool> AuthenticateAsync()
    {
        LoadStoredCredentials();
        if (_token == null) return false;
        await EnsureAccessTokenAsync();
        return _token != null;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        LoadStoredCredentials();
        if (_token == null || string.IsNullOrEmpty(_token.RefreshToken)) return false;
        if (_token.ExpiresAt < DateTimeOffset.UtcNow)
        {
            try { await RefreshAccessTokenAsync(); }
            catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", logEx); return false; }
        }
        return _token != null;
    }

    public async Task SignOutAsync() => await DisconnectAsync();

    // ─── Backup operations ───────────────────────────────────────────────────

    public async Task<CloudBackupResult> UploadBackupAsync(byte[] backupData, string fileName, string? description = null)
    {
        await EnsureAccessTokenAsync();
        if (_token == null) return Fail("Not authenticated with OneDrive.");
        if (backupData.Length > MaxBackupSizeBytes) return Fail("Backup exceeds 100 MB limit.");

        var request = new HttpRequestMessage(HttpMethod.Put, $"{BackupFolderUrl}/{Uri.EscapeDataString(fileName)}:/content");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token!.AccessToken);
        request.Content = new ByteArrayContent(backupData);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(BackupMimeType);

        var resp = await _http.SendAsync(request);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync();
            _logger.LogError("OneDrive upload failed {Status}: {Body}", resp.StatusCode, err);
            return Fail($"Upload failed ({resp.StatusCode}).");
        }

        var body = await resp.Content.ReadAsStringAsync();
        var file = JsonSerializer.Deserialize<DriveItem>(body);
        if (file == null) return Fail("OneDrive returned no file info.");

        return new CloudBackupResult
        {
            Success = true,
            BackupInfo = new CloudBackupInfo
            {
                Id = file.Id,
                FileName = fileName,
                Description = description,
                CreatedAt = file.CreatedDateTime ?? DateTime.UtcNow,
                ModifiedAt = file.LastModifiedDateTime ?? DateTime.UtcNow,
                SizeInBytes = backupData.Length,
                ServiceName = ServiceName,
                Provider = CloudBackupProvider.OneDrive,
            }
        };
    }

    public async Task<CloudBackupResult> DownloadBackupAsync(string backupId)
    {
        await EnsureAccessTokenAsync();
        if (_token == null) return Fail("Not authenticated.");

        var request = new HttpRequestMessage(HttpMethod.Get, $"{GraphBase}/me/drive/items/{backupId}/content");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token!.AccessToken);
        var resp = await _http.SendAsync(request);
        if (!resp.IsSuccessStatusCode) return Fail($"Download failed ({resp.StatusCode}).");

        var data = await resp.Content.ReadAsByteArrayAsync();
        return new CloudBackupResult { Success = true, BackupData = data };
    }

    public async Task<List<CloudBackupInfo>> ListBackupsAsync()
    {
        await EnsureAccessTokenAsync();
        if (_token == null) return [];

        var request = new HttpRequestMessage(HttpMethod.Get, $"{BackupFolderUrl}:/children?$orderby=createdDateTime desc&$top=50");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token!.AccessToken);
        var resp = await _http.SendAsync(request);
        if (!resp.IsSuccessStatusCode) return [];

        var body = await resp.Content.ReadAsStringAsync();
        var list = JsonSerializer.Deserialize<DriveItemList>(body);
        if (list?.Value == null) return [];

        return list.Value
            .Where(f => f.Name.EndsWith(".pwmbackup", StringComparison.OrdinalIgnoreCase))
            .Select(f => new CloudBackupInfo
            {
                Id = f.Id,
                FileName = f.Name,
                CreatedAt = f.CreatedDateTime ?? DateTime.UtcNow,
                ModifiedAt = f.LastModifiedDateTime ?? DateTime.UtcNow,
                SizeInBytes = f.Size,
                ServiceName = ServiceName,
                Provider = CloudBackupProvider.OneDrive,
            }).ToList();
    }

    public async Task<bool> DeleteBackupAsync(string backupId)
    {
        await EnsureAccessTokenAsync();
        if (_token == null) return false;

        var request = new HttpRequestMessage(HttpMethod.Delete, $"{GraphBase}/me/drive/items/{backupId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token!.AccessToken);
        var resp = await _http.SendAsync(request);
        return resp.IsSuccessStatusCode || resp.StatusCode == HttpStatusCode.NoContent;
    }

    public async Task<OneDriveAccountInfo?> GetAccountInfoAsync()
    {
        if (!await IsAuthenticatedAsync()) return null;
        if (AccountInfo == null) await RefreshAccountInfoAsync(CancellationToken.None);
        return AccountInfo;
    }

    public async Task<long> GetAvailableStorageAsync()
    {
        try
        {
            var accountInfo = await GetAccountInfoAsync();
            return accountInfo?.AvailableSpace ?? 0;
        }
        catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", logEx); return 0; }
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task EnsureAccessTokenAsync()
    {
        if (_token == null) return;
        if (_token.ExpiresAt < DateTimeOffset.UtcNow)
            await RefreshAccessTokenAsync();
    }

    private async Task RefreshAccessTokenAsync()
    {
        if (_token == null || string.IsNullOrEmpty(_token.RefreshToken)) return;

        var form = new Dictionary<string, string>
        {
            ["client_id"] = _token.ClientId,
            ["refresh_token"] = _token.RefreshToken,
            ["grant_type"] = "refresh_token",
            ["scope"] = Scope,
        };

        var resp = await _http.PostAsync(TokenEndpoint, new FormUrlEncodedContent(form));
        if (!resp.IsSuccessStatusCode) { _token = null; return; }

        var json = await resp.Content.ReadAsStringAsync();
        var tok = JsonSerializer.Deserialize<OAuthTokenResponse>(json);
        if (tok == null) return;

        _token.AccessToken = tok.AccessToken;
        if (!string.IsNullOrEmpty(tok.RefreshToken)) _token.RefreshToken = tok.RefreshToken;
        _token.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tok.ExpiresIn - 60);
        SaveStoredCredentials();
    }

    private async Task RefreshAccountInfoAsync(CancellationToken ct)
    {
        try
        {
            var meReq = new HttpRequestMessage(HttpMethod.Get, $"{GraphBase}/me");
            meReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token!.AccessToken);
            var meResp = await _http.SendAsync(meReq, ct);

            var quotaReq = new HttpRequestMessage(HttpMethod.Get, $"{GraphBase}/me/drive");
            quotaReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token!.AccessToken);
            var quotaResp = await _http.SendAsync(quotaReq, ct);

            GraphUser? me = meResp.IsSuccessStatusCode
                ? JsonSerializer.Deserialize<GraphUser>(await meResp.Content.ReadAsStringAsync(ct))
                : null;
            GraphDrive? drive = quotaResp.IsSuccessStatusCode
                ? JsonSerializer.Deserialize<GraphDrive>(await quotaResp.Content.ReadAsStringAsync(ct))
                : null;

            AccountInfo = new OneDriveAccountInfo
            {
                DisplayName = me?.DisplayName,
                Email = me?.Mail ?? me?.UserPrincipalName,
                TotalSpace = drive?.Quota?.Total ?? 0,
                UsedSpace = drive?.Quota?.Used ?? 0,
                AvailableSpace = drive?.Quota?.Remaining ?? 0,
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch OneDrive account info.");
        }
    }

    private static CloudBackupResult Fail(string msg) => new() { Success = false, ErrorMessage = msg };

    private static string TokenFilePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            SettingsDir, TokenFile);

    private void LoadStoredCredentials()
    {
        var path = TokenFilePath();
        if (!File.Exists(path)) return;
        try
        {
            var raw = File.ReadAllBytes(path);
            var dec = System.Security.Cryptography.ProtectedData.Unprotect(raw, null, DataProtectionScope.CurrentUser);
            _token = JsonSerializer.Deserialize<StoredToken>(dec);
        }
        catch { _token = null; }
    }

    private void SaveStoredCredentials()
    {
        if (_token == null) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(TokenFilePath())!);
            var json = JsonSerializer.SerializeToUtf8Bytes(_token);
            var enc = System.Security.Cryptography.ProtectedData.Protect(json, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(TokenFilePath(), enc);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not save OneDrive token.");
        }
    }

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

    // ─── JSON models ──────────────────────────────────────────────────────────

    private sealed class OAuthTokenResponse
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; set; } = string.Empty;
        [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
    }

    private sealed class StoredToken
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; set; }
        public string ClientId { get; set; } = string.Empty;
    }

    private sealed class DriveItem
    {
        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("size")] public long Size { get; set; }
        [JsonPropertyName("createdDateTime")] public DateTime? CreatedDateTime { get; set; }
        [JsonPropertyName("lastModifiedDateTime")] public DateTime? LastModifiedDateTime { get; set; }
    }

    private sealed class DriveItemList
    {
        [JsonPropertyName("value")] public List<DriveItem>? Value { get; set; }
    }

    private sealed class GraphUser
    {
        [JsonPropertyName("displayName")] public string? DisplayName { get; set; }
        [JsonPropertyName("mail")] public string? Mail { get; set; }
        [JsonPropertyName("userPrincipalName")] public string? UserPrincipalName { get; set; }
    }

    private sealed class GraphDrive
    {
        [JsonPropertyName("quota")] public GraphQuota? Quota { get; set; }
    }

    private sealed class GraphQuota
    {
        [JsonPropertyName("total")] public long Total { get; set; }
        [JsonPropertyName("used")] public long Used { get; set; }
        [JsonPropertyName("remaining")] public long Remaining { get; set; }
    }
}
