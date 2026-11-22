using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace VaultGuard.Services.BrowserLink;

/// <summary>
/// End-to-end encryption for the browser link (protocol 3). The extension and the app agree on a key once, when
/// the browser is approved, with an ECDH P-256 key exchange (only public keys cross the link); after that every
/// request and response is AES-256-GCM encrypted with that key. The browser helper and the named pipe carry only
/// ciphertext, so passwords are never readable in transit, even by another program watching the local channels.
///
/// Key: HKDF-SHA256(ECDH shared secret, salt = client public key ‖ app public key, info = "VaultGuard browser link v3").
/// Request:  { action, keyId, iv, ct } with AAD "vg3|req|{action}|{keyId}"; the plaintext JSON carries the fields
///           and "ts" (ms since 1970), and is refused if more than two minutes off.
/// Response: { enc: { iv, ct } } with AAD "vg3|resp|{action}|{keyId}|{request iv}", tying it to its request.
/// </summary>
public static class BrowserLinkCrypto
{
    public const string Info = "VaultGuard browser link v3";
    public static readonly TimeSpan MaxClockSkew = TimeSpan.FromMinutes(2);

    public sealed record Exchange(byte[] Key, string AppPublicKey);

    /// <summary>App side of the key exchange, from the extension's raw (uncompressed, 65-byte) P-256 public key.</summary>
    public static Exchange Agree(string clientPublicKeyB64)
    {
        var clientRaw = FromB64(clientPublicKeyB64);
        if (clientRaw.Length != 65 || clientRaw[0] != 0x04) throw new CryptographicException("Invalid browser public key.");
        using var client = ECDiffieHellman.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint { X = clientRaw[1..33], Y = clientRaw[33..65] }
        });
        using var app = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var q = app.ExportParameters(false).Q;
        var appRaw = new byte[65];
        appRaw[0] = 0x04; q.X!.CopyTo(appRaw, 1); q.Y!.CopyTo(appRaw, 33);
        var shared = app.DeriveRawSecretAgreement(client.PublicKey);
        try
        {
            var key = HKDF.DeriveKey(HashAlgorithmName.SHA256, shared, 32, salt: [.. clientRaw, .. appRaw], info: Encoding.UTF8.GetBytes(Info));
            return new Exchange(key, ToB64(appRaw));
        }
        finally { CryptographicOperations.ZeroMemory(shared); }
    }

    /// <summary>Decrypts a request; null if it doesn't authenticate or is stale.</summary>
    public static JsonDocument? OpenRequest(byte[] key, string action, string keyId, string ivB64, string ctB64, DateTimeOffset? now = null)
    {
        try
        {
            var plain = Open(key, FromB64(ivB64), FromB64(ctB64), $"vg3|req|{action}|{keyId}");
            var doc = JsonDocument.Parse(plain);
            if (!doc.RootElement.TryGetProperty("ts", out var ts) || ts.ValueKind != JsonValueKind.Number ||
                Math.Abs(((now ?? DateTimeOffset.UtcNow).ToUnixTimeMilliseconds() - ts.GetInt64())) > MaxClockSkew.TotalMilliseconds)
            {
                doc.Dispose();
                return null;
            }
            return doc;
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException or JsonException or ArgumentException) { return null; }
    }

    public static object SealResponse(byte[] key, string action, string keyId, string requestIvB64, object response)
    {
        var iv = RandomNumberGenerator.GetBytes(12);
        var ct = Seal(key, iv, JsonSerializer.SerializeToUtf8Bytes(response), $"vg3|resp|{action}|{keyId}|{requestIvB64}");
        return new { enc = new { iv = ToB64(iv), ct = ToB64(ct) } };
    }

    // ---- used by tests to act as the extension ----
    public static (string ClientPublicKey, ECDiffieHellman Private) NewClient()
    {
        var c = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var q = c.ExportParameters(false).Q;
        return (ToB64([0x04, .. q.X!, .. q.Y!]), c);
    }

    public static byte[] ClientKey(ECDiffieHellman client, string clientPublicKeyB64, string appPublicKeyB64)
    {
        var appRaw = FromB64(appPublicKeyB64);
        using var app = ECDiffieHellman.Create(new ECParameters { Curve = ECCurve.NamedCurves.nistP256, Q = new ECPoint { X = appRaw[1..33], Y = appRaw[33..65] } });
        var shared = client.DeriveRawSecretAgreement(app.PublicKey);
        return HKDF.DeriveKey(HashAlgorithmName.SHA256, shared, 32, salt: [.. FromB64(clientPublicKeyB64), .. appRaw], info: Encoding.UTF8.GetBytes(Info));
    }

    public static (string Iv, string Ct) SealRequest(byte[] key, string action, string keyId, object fields)
    {
        var iv = RandomNumberGenerator.GetBytes(12);
        return (ToB64(iv), ToB64(Seal(key, iv, JsonSerializer.SerializeToUtf8Bytes(fields), $"vg3|req|{action}|{keyId}")));
    }

    public static JsonDocument OpenResponse(byte[] key, string action, string keyId, string requestIv, string ivB64, string ctB64)
        => JsonDocument.Parse(Open(key, FromB64(ivB64), FromB64(ctB64), $"vg3|resp|{action}|{keyId}|{requestIv}"));

    // ---- AES-256-GCM: ciphertext ‖ 16-byte tag, as WebCrypto produces ----
    private static byte[] Seal(byte[] key, byte[] iv, byte[] plain, string aad)
    {
        var output = new byte[plain.Length + 16];
        using var gcm = new AesGcm(key, 16);
        gcm.Encrypt(iv, plain, output.AsSpan(0, plain.Length), output.AsSpan(plain.Length), Encoding.UTF8.GetBytes(aad));
        return output;
    }

    private static byte[] Open(byte[] key, byte[] iv, byte[] sealedData, string aad)
    {
        if (iv.Length != 12 || sealedData.Length < 16) throw new CryptographicException("Bad envelope.");
        var plain = new byte[sealedData.Length - 16];
        using var gcm = new AesGcm(key, 16);
        gcm.Decrypt(iv, sealedData.AsSpan(0, plain.Length), sealedData.AsSpan(plain.Length), plain, Encoding.UTF8.GetBytes(aad));
        return plain;
    }

    public static string ToB64(byte[] b) => Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    public static byte[] FromB64(string s)
    {
        s = (s ?? "").Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(s.PadRight(s.Length + (4 - s.Length % 4) % 4, '='));
    }
}
