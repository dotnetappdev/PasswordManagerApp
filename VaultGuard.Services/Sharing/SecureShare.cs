using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VaultGuard.Services.Sharing;

/// <summary>What is shared: a copy of one item's details, chosen by the person sharing.</summary>
public sealed record ShareContent(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("username")] string? Username,
    [property: JsonPropertyName("password")] string? Password,
    [property: JsonPropertyName("url")] string? Url,
    [property: JsonPropertyName("notes")] string? Notes);

public static class ShareContentFactory
{
    /// <summary>What to share from a decrypted item: title, username, password, website and notes as available.</summary>
    public static ShareContent From(VaultGuard.Models.PasswordItem item) => new(
        item.Title ?? "Shared item",
        item.LoginItem?.Username ?? item.Username ?? item.WiFiItem?.NetworkName,
        item.LoginItem?.Password ?? item.Password ?? item.WiFiItem?.Password,
        item.LoginItem?.WebsiteUrl ?? item.WebsiteUrl ?? item.LoginItem?.Website,
        item.SecureNoteItem?.Content ?? item.LoginItem?.Notes ?? item.Description);
}

/// <summary>The encrypted share as the server stores it. It holds nothing readable.</summary>
public sealed class ShareEnvelope
{
    [JsonPropertyName("v")] public int Version { get; set; } = 1;
    [JsonPropertyName("iv")] public string Iv { get; set; } = "";
    [JsonPropertyName("ct")] public string Ciphertext { get; set; } = "";
    /// <summary>Set when a passphrase is required (PBKDF2-SHA256 salt).</summary>
    [JsonPropertyName("salt")] public string? Salt { get; set; }
    [JsonPropertyName("iter")] public int Iterations { get; set; }
}

/// <summary>Options the person sharing chooses.</summary>
public sealed record ShareOptions(TimeSpan ExpiresIn, int? MaxViews, string? Passphrase);

/// <summary>
/// Secure sharing (like Bitwarden Send): an item is encrypted on the sharer's device with a fresh random key, and
/// only the ciphertext goes to the server. The key travels in the link's #fragment, which browsers never send to
/// servers, so the server (and anyone who reads its storage) can't read what was shared. An optional passphrase,
/// sent separately, is mixed into the key. The viewer page decrypts in the recipient's browser (Web Crypto).
///
/// Key = HKDF-SHA256(link key [‖ PBKDF2-SHA256(passphrase, salt, 600 000)], no salt, info "VaultGuard share v1").
/// Content = AES-256-GCM(key, iv, JSON). Must match the viewer page (SecureShareEndpoints.ViewerHtml).
/// </summary>
public static class SecureShareCrypto
{
    public const string Info = "VaultGuard share v1";
    public const int PassphraseIterations = 600_000;
    public const int MaxCiphertextBytes = 64 * 1024;

    public static (ShareEnvelope Envelope, string LinkKey) Encrypt(ShareContent content, string? passphrase)
    {
        var linkKey = RandomNumberGenerator.GetBytes(32);
        var env = new ShareEnvelope();
        byte[]? salt = null;
        if (!string.IsNullOrEmpty(passphrase))
        {
            salt = RandomNumberGenerator.GetBytes(16);
            env.Salt = B64(salt);
            env.Iterations = PassphraseIterations;
        }
        var key = DeriveKey(linkKey, passphrase, salt, env.Iterations);
        try
        {
            var iv = RandomNumberGenerator.GetBytes(12);
            var plain = JsonSerializer.SerializeToUtf8Bytes(content);
            var sealedData = new byte[plain.Length + 16];
            using (var gcm = new AesGcm(key, 16))
                gcm.Encrypt(iv, plain, sealedData.AsSpan(0, plain.Length), sealedData.AsSpan(plain.Length));
            CryptographicOperations.ZeroMemory(plain);
            env.Iv = B64(iv);
            env.Ciphertext = B64(sealedData);
            return (env, B64(linkKey));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(linkKey);
        }
    }

    /// <summary>Decrypts a share (the viewer does this in the browser; used here by tests).</summary>
    public static ShareContent Decrypt(ShareEnvelope env, string linkKeyB64, string? passphrase)
    {
        var key = DeriveKey(FromB64(linkKeyB64), passphrase, env.Salt == null ? null : FromB64(env.Salt), env.Iterations);
        try
        {
            var sealedData = FromB64(env.Ciphertext);
            var plain = new byte[sealedData.Length - 16];
            using (var gcm = new AesGcm(key, 16))
                gcm.Decrypt(FromB64(env.Iv), sealedData.AsSpan(0, plain.Length), sealedData.AsSpan(plain.Length), plain);
            return JsonSerializer.Deserialize<ShareContent>(plain)!;
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    private static byte[] DeriveKey(byte[] linkKey, string? passphrase, byte[]? salt, int iterations)
    {
        var ikm = linkKey;
        if (salt != null)
        {
            if (string.IsNullOrEmpty(passphrase)) throw new CryptographicException("This share needs its passphrase.");
            var pk = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(passphrase), salt, iterations, HashAlgorithmName.SHA256, 32);
            ikm = [.. linkKey, .. pk];
            CryptographicOperations.ZeroMemory(pk);
        }
        return HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, 32, salt: Array.Empty<byte>(), info: Encoding.UTF8.GetBytes(Info));
    }

    public static string B64(byte[] b) => Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    public static byte[] FromB64(string s)
    {
        s = (s ?? "").Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(s.PadRight(s.Length + (4 - s.Length % 4) % 4, '='));
    }
}

/// <summary>Creates shares on a Vault Guard server (the API, with the app's API key). Used by WPF and MAUI.</summary>
public static class SecureShareClient
{
    public sealed record Result(bool Success, string? Link, DateTime? ExpiresUtc, string? Error);

    public static async Task<Result> CreateAsync(HttpClient http, string serverUrl, string? apiKey, ShareContent content,
        ShareOptions options, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(serverUrl))
            return new Result(false, null, null, "Set up your Vault Guard server first (Settings → Sync), so the recipient can open the link.");
        if (!Uri.TryCreate(serverUrl.TrimEnd('/'), UriKind.Absolute, out var server) || (server.Scheme != "https" && !server.IsLoopback))
            return new Result(false, null, null, "The Vault Guard server address must start with https://.");
        var (env, linkKey) = SecureShareCrypto.Encrypt(content, options.Passphrase);
        using var req = new HttpRequestMessage(HttpMethod.Post, new Uri(server, "/api/shares"))
        {
            Content = JsonContent.Create(new ShareCreateRequest
            {
                Envelope = env, ExpiresInMinutes = (int)Math.Ceiling(options.ExpiresIn.TotalMinutes), MaxViews = options.MaxViews
            })
        };
        if (!string.IsNullOrEmpty(apiKey)) req.Headers.Add("X-API-Key", apiKey);
        try
        {
            using var resp = await http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
                return new Result(false, null, null, resp.StatusCode == System.Net.HttpStatusCode.Unauthorized
                    ? "The server didn't accept this device's API key (Settings → Sync)."
                    : $"The server couldn't create the share ({(int)resp.StatusCode}).");
            var created = await resp.Content.ReadFromJsonAsync<ShareCreateResponse>(cancellationToken: ct);
            if (created?.Id == null) return new Result(false, null, null, "The server sent an unexpected answer.");
            return new Result(true, $"{server.GetLeftPart(UriPartial.Authority)}/s/{created.Id}#{linkKey}", created.ExpiresUtc, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new Result(false, null, null, $"Couldn't reach the Vault Guard server: {ex.Message}");
        }
    }
}

public sealed class ShareCreateRequest
{
    [JsonPropertyName("envelope")] public ShareEnvelope Envelope { get; set; } = new();
    [JsonPropertyName("expiresInMinutes")] public int ExpiresInMinutes { get; set; }
    [JsonPropertyName("maxViews")] public int? MaxViews { get; set; }
}

public sealed class ShareCreateResponse
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("expiresUtc")] public DateTime ExpiresUtc { get; set; }
}
