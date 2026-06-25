using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using VaultGuard.Models.DTOs;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.Services.Services;

/// <summary>
/// Google Drive backup service using Drive v3 REST API.
///
/// Stores backups in the hidden "appDataFolder" space — each user's app-specific
/// area in Drive that only this app can read. OAuth2 tokens are stored in
/// the local app-data settings file, encrypted with DPAPI on Windows.
///
/// OAuth flow: installed-app loopback redirect (RFC 8252 / PKCE).
/// </summary>
public class GoogleDriveBackupService : IGoogleDriveBackupService
{
    private readonly ILogger<GoogleDriveBackupService> _logger;
    private readonly HttpClient _http;

    private const string TokenFile = "gdrive_token.json";
    private const string SettingsDir = "VaultGuard";
    private const string BackupMimeType = "application/octet-stream";
    private const string DriveFilesUrl = "https://www.googleapis.com/drive/v3/files";
    private const string DriveUploadUrl = "https://www.googleapis.com/upload/drive/v3/files";
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string AuthEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string Scope = "https://www.googleapis.com/auth/drive.appdata";

    public string ServiceName => "Google Drive";
    public long MaxBackupSizeBytes => 150 * 1024 * 1024; // 150 MB

    private StoredToken? _token;
    private string _clientId = string.Empty;
    private string _clientSecret = string.Empty;

    public GoogleDriveAccountInfo? AccountInfo { get; private set; }

    public GoogleDriveBackupService(ILogger<GoogleDriveBackupService> logger)
    {
        _logger = logger;
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("VaultGuard/1.0");
        LoadStoredCredentials();
    }

    // ─── Auth ───────────────────────────────────────────────────────────────

    public async Task<bool> ConnectWithOAuthAsync(string clientId, string clientSecret, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            return false;

        _clientId = clientId;
        _clientSecret = clientSecret;

        // PKCE
        var verifier = GenerateCodeVerifier();
        var challenge = GenerateCodeChallenge(verifier);

        // Find a free port for the loopback listener
        var port = FindFreePort();
        var redirectUri = $"http://127.0.0.1:{port}/";

        var authUrl = $"{AuthEndpoint}?response_type=code" +
                      $"&client_id={Uri.EscapeDataString(clientId)}" +
                      $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                      $"&scope={Uri.EscapeDataString(Scope)}" +
                      $"&code_challenge={challenge}" +
                      $"&code_challenge_method=S256" +
                      $"&access_type=offline&prompt=consent";

        // Open browser
        Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

        // Wait for callback
        string? code = null;
        using var listener = new HttpListener();
        listener.Prefixes.Add(redirectUri);
        listener.Start();
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromMinutes(5));
            var context = await listener.GetContextAsync().WaitAsync(timeoutCts.Token);
            code = context.Request.QueryString["code"];
            var html = "<html><body><h2>VaultGuard: authorisation complete — you can close this tab.</h2></body></html>";
            var buf = Encoding.UTF8.GetBytes(html);
            context.Response.ContentLength64 = buf.Length;
            await context.Response.OutputStream.WriteAsync(buf, timeoutCts.Token);
            context.Response.Close();
        }
        finally
        {
            listener.Stop();
        }

        if (string.IsNullOrEmpty(code))
            return false;

        // Exchange code → tokens
        var form = new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code",
            ["code_verifier"] = verifier,
        };

        var resp = await _http.PostAsync(TokenEndpoint, new FormUrlEncodedContent(form), ct);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("Token exchange failed: {Status}", resp.StatusCode);
            return false;
        }

        var json = await resp.Content.ReadAsStringAsync(ct);
        var tok = JsonSerializer.Deserialize<OAuthTokenResponse>(json);
        if (tok == null) return false;

        _token = new StoredToken
        {
            AccessToken = tok.AccessToken,
            RefreshToken = tok.RefreshToken ?? string.Empty,
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tok.ExpiresIn - 60),
            ClientId = clientId,
            ClientSecret = clientSecret,
        };

        SaveStoredCredentials();
        await RefreshAccountInfoAsync(ct);
        return true;
    }

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
            catch { return false; }
        }
        return _token != null;
    }

    public async Task SignOutAsync() => await DisconnectAsync();

    // ─── Backup operations ───────────────────────────────────────────────────

    public async Task<CloudBackupResult> UploadBackupAsync(byte[] backupData, string fileName, string? description = null)
    {
        await EnsureAccessTokenAsync();
        if (_token == null) return Fail("Not authenticated with Google Drive.");
        if (backupData.Length > MaxBackupSizeBytes) return Fail("Backup exceeds 150 MB limit.");

        // Multipart upload: metadata + binary
        var metadata = JsonSerializer.Serialize(new
        {
            name = fileName,
            parents = new[] { "appDataFolder" },
            description = description ?? string.Empty,
        });

        var content = new MultipartContent("related");
        var metaPart = new StringContent(metadata, Encoding.UTF8, "application/json");
        content.Add(metaPart);
        content.Add(new ByteArrayContent(backupData) { Headers = { ContentType = new MediaTypeHeaderValue(BackupMimeType) } });

        var request = new HttpRequestMessage(HttpMethod.Post, DriveUploadUrl + "?uploadType=multipart");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token!.AccessToken);
        request.Content = content;

        var resp = await _http.SendAsync(request);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync();
            _logger.LogError("Drive upload failed {Status}: {Body}", resp.StatusCode, err);
            return Fail($"Upload failed ({resp.StatusCode}).");
        }

        var body = await resp.Content.ReadAsStringAsync();
        var file = JsonSerializer.Deserialize<DriveFile>(body);
        if (file == null) return Fail("Drive returned no file info.");

        return new CloudBackupResult
        {
            Success = true,
            BackupInfo = new CloudBackupInfo
            {
                Id = file.Id,
                FileName = fileName,
                Description = description,
                CreatedAt = file.CreatedTime ?? DateTime.UtcNow,
                ModifiedAt = file.ModifiedTime ?? DateTime.UtcNow,
                SizeInBytes = backupData.Length,
                ServiceName = ServiceName,
                Provider = CloudBackupProvider.GoogleDrive,
            }
        };
    }

    public async Task<CloudBackupResult> DownloadBackupAsync(string backupId)
    {
        await EnsureAccessTokenAsync();
        if (_token == null) return Fail("Not authenticated.");

        var request = new HttpRequestMessage(HttpMethod.Get, $"{DriveFilesUrl}/{backupId}?alt=media");
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

        var url = $"{DriveFilesUrl}?spaces=appDataFolder" +
                  $"&fields=files(id,name,description,createdTime,modifiedTime,size)" +
                  $"&orderBy=createdTime+desc&pageSize=50";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token!.AccessToken);
        var resp = await _http.SendAsync(request);
        if (!resp.IsSuccessStatusCode) return [];

        var body = await resp.Content.ReadAsStringAsync();
        var list = JsonSerializer.Deserialize<DriveFileList>(body);
        if (list?.Files == null) return [];

        return list.Files
            .Where(f => f.Name.EndsWith(".pwmbackup", StringComparison.OrdinalIgnoreCase))
            .Select(f => new CloudBackupInfo
            {
                Id = f.Id,
                FileName = f.Name,
                Description = f.Description,
                CreatedAt = f.CreatedTime ?? DateTime.UtcNow,
                ModifiedAt = f.ModifiedTime ?? DateTime.UtcNow,
                SizeInBytes = long.TryParse(f.Size, out var sz) ? sz : 0,
                ServiceName = ServiceName,
                Provider = CloudBackupProvider.GoogleDrive,
            }).ToList();
    }

    public async Task<bool> DeleteBackupAsync(string backupId)
    {
        await EnsureAccessTokenAsync();
        if (_token == null) return false;

        var request = new HttpRequestMessage(HttpMethod.Delete, $"{DriveFilesUrl}/{backupId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token!.AccessToken);
        var resp = await _http.SendAsync(request);
        return resp.IsSuccessStatusCode || resp.StatusCode == HttpStatusCode.NoContent;
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
            ["client_secret"] = _token.ClientSecret,
            ["refresh_token"] = _token.RefreshToken,
            ["grant_type"] = "refresh_token",
        };

        var resp = await _http.PostAsync(TokenEndpoint, new FormUrlEncodedContent(form));
        if (!resp.IsSuccessStatusCode) { _token = null; return; }

        var json = await resp.Content.ReadAsStringAsync();
        var tok = JsonSerializer.Deserialize<OAuthTokenResponse>(json);
        if (tok == null) return;

        _token.AccessToken = tok.AccessToken;
        _token.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tok.ExpiresIn - 60);
        SaveStoredCredentials();
    }

    private async Task RefreshAccountInfoAsync(CancellationToken ct)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get,
                "https://www.googleapis.com/drive/v3/about?fields=user,storageQuota");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token!.AccessToken);
            var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode) return;
            var body = await resp.Content.ReadAsStringAsync(ct);
            var about = JsonSerializer.Deserialize<DriveAbout>(body);
            if (about == null) return;
            AccountInfo = new GoogleDriveAccountInfo
            {
                Email = about.User?.EmailAddress ?? string.Empty,
                DisplayName = about.User?.DisplayName ?? string.Empty,
                StorageUsed = long.TryParse(about.StorageQuota?.Usage, out var u) ? u : 0,
                StorageTotal = long.TryParse(about.StorageQuota?.Limit, out var l) ? l : 0,
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch Google Drive account info.");
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
            var dec = ProtectedData.Unprotect(raw, null, DataProtectionScope.CurrentUser);
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
            var enc = ProtectedData.Protect(json, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(TokenFilePath(), enc);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not save Google Drive token.");
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
        public string ClientSecret { get; set; } = string.Empty;
    }

    private sealed class DriveFile
    {
        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("createdTime")] public DateTime? CreatedTime { get; set; }
        [JsonPropertyName("modifiedTime")] public DateTime? ModifiedTime { get; set; }
        [JsonPropertyName("size")] public string? Size { get; set; }
    }

    private sealed class DriveFileList
    {
        [JsonPropertyName("files")] public List<DriveFile>? Files { get; set; }
    }

    private sealed class DriveAbout
    {
        [JsonPropertyName("user")] public DriveUser? User { get; set; }
        [JsonPropertyName("storageQuota")] public DriveStorageQuota? StorageQuota { get; set; }
    }

    private sealed class DriveUser
    {
        [JsonPropertyName("emailAddress")] public string? EmailAddress { get; set; }
        [JsonPropertyName("displayName")] public string? DisplayName { get; set; }
    }

    private sealed class DriveStorageQuota
    {
        [JsonPropertyName("usage")] public string? Usage { get; set; }
        [JsonPropertyName("limit")] public string? Limit { get; set; }
    }
}
