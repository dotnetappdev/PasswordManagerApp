using System.Security.Cryptography;
using System.Text;
using VaultGuard.Crypto.Interfaces;

namespace VaultGuard.Crypto.Services;

/// <summary>
/// See <see cref="ILicenseCryptoService"/> and docs/LICENSING.md for the full design. Summary: CD keys
/// are opaque, checksummed random tokens used once to activate a device; what actually gets trusted
/// afterward is a <c>SignedLicensePackage</c> — the license claims (customer, plan, features, expiry)
/// AES-256-GCM encrypted, then ECDSA-P256 signed over (ciphertext‖nonce‖tag). The signing private key
/// never leaves the license server, so encryption-key extraction from a decompiled client does not, by
/// itself, let anyone mint valid licenses — only forging a valid signature would, and that requires the
/// private key.
/// </summary>
public class LicenseCryptoService : ILicenseCryptoService
{
    private const int AesKeyLength = 32;
    private const int NonceLength = 12;
    private const int AuthTagLength = 16;
    private const int CdKeyBodyBytes = 10; // 80 bits of entropy
    private const int CdKeyGroupSize = 5;

    public (string PublicKeyPem, string PrivateKeyPem) GenerateSigningKeyPair()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var publicPem = ecdsa.ExportSubjectPublicKeyInfoPem();
        var privatePem = ecdsa.ExportECPrivateKeyPem();
        return (publicPem, privatePem);
    }

    public SignedLicensePackage Seal(string json, byte[] aesKey, string privateKeyPem)
    {
        if (string.IsNullOrEmpty(json)) throw new ArgumentException("Payload cannot be empty", nameof(json));
        if (aesKey == null || aesKey.Length != AesKeyLength)
            throw new ArgumentException($"AES key must be exactly {AesKeyLength} bytes", nameof(aesKey));

        var plaintext = Encoding.UTF8.GetBytes(json);
        var nonce = RandomNumberGenerator.GetBytes(NonceLength);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[AuthTagLength];

        using (var aesGcm = new AesGcm(aesKey, AuthTagLength))
        {
            aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);
        }

        using var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(privateKeyPem);
        var signature = ecdsa.SignData(CombineForSigning(ciphertext, nonce, tag), HashAlgorithmName.SHA256);

        return new SignedLicensePackage
        {
            CiphertextBase64 = Convert.ToBase64String(ciphertext),
            NonceBase64 = Convert.ToBase64String(nonce),
            TagBase64 = Convert.ToBase64String(tag),
            SignatureBase64 = Convert.ToBase64String(signature)
        };
    }

    public string Open(SignedLicensePackage package, byte[] aesKey, string publicKeyPem)
    {
        ArgumentNullException.ThrowIfNull(package);
        if (aesKey == null || aesKey.Length != AesKeyLength)
            throw new ArgumentException($"AES key must be exactly {AesKeyLength} bytes", nameof(aesKey));

        var ciphertext = Convert.FromBase64String(package.CiphertextBase64);
        var nonce = Convert.FromBase64String(package.NonceBase64);
        var tag = Convert.FromBase64String(package.TagBase64);
        var signature = Convert.FromBase64String(package.SignatureBase64);

        using var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(publicKeyPem);
        if (!ecdsa.VerifyData(CombineForSigning(ciphertext, nonce, tag), signature, HashAlgorithmName.SHA256))
            throw new CryptographicException("License signature verification failed — certificate is forged, tampered, or was issued by a different signing key.");

        var plaintext = new byte[ciphertext.Length];
        using (var aesGcm = new AesGcm(aesKey, AuthTagLength))
        {
            // Throws CryptographicException on tag mismatch (tampered ciphertext).
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
        }

        return Encoding.UTF8.GetString(plaintext);
    }

    public string GenerateCdKey(string prefix = "VG")
    {
        var body = RandomNumberGenerator.GetBytes(CdKeyBodyBytes);
        var bodyEncoded = Crockford32.Encode(body);
        var checksum = Crc16Ccitt(body);
        var checksumEncoded = Crockford32.Encode(new[] { (byte)(checksum >> 8), (byte)checksum });

        var full = bodyEncoded + checksumEncoded;
        var grouped = string.Join("-", Chunk(full, CdKeyGroupSize));
        return $"{prefix}-{grouped}";
    }

    public bool TryNormalizeCdKey(string input, out string normalized)
    {
        normalized = "";
        if (string.IsNullOrWhiteSpace(input)) return false;

        var upper = Crockford32.Normalize(input);

        // Strip a leading product prefix (letters before the first run of digits/checksum body) —
        // callers may or may not include it; we only care about the 20-character payload.
        string prefix = "VG";
        var body = upper;
        if (upper.StartsWith("VG", StringComparison.Ordinal))
            body = upper[2..];

        if (body.Length != 20) return false;

        var bodyPart = body[..16];
        var checksumPart = body[16..];

        if (!Crockford32.TryDecode(bodyPart, out var bodyBytes) || bodyBytes.Length != CdKeyBodyBytes)
            return false;
        if (!Crockford32.TryDecode(checksumPart, out var checksumBytes) || checksumBytes.Length != 2)
            return false;

        var expectedChecksum = Crc16Ccitt(bodyBytes);
        var actualChecksum = (ushort)((checksumBytes[0] << 8) | checksumBytes[1]);
        if (expectedChecksum != actualChecksum) return false;

        var grouped = string.Join("-", Chunk(body, CdKeyGroupSize));
        normalized = $"{prefix}-{grouped}";
        return true;
    }

    private static byte[] CombineForSigning(byte[] ciphertext, byte[] nonce, byte[] tag)
    {
        var combined = new byte[ciphertext.Length + nonce.Length + tag.Length];
        Buffer.BlockCopy(ciphertext, 0, combined, 0, ciphertext.Length);
        Buffer.BlockCopy(nonce, 0, combined, ciphertext.Length, nonce.Length);
        Buffer.BlockCopy(tag, 0, combined, ciphertext.Length + nonce.Length, tag.Length);
        return combined;
    }

    private static IEnumerable<string> Chunk(string value, int size)
    {
        for (int i = 0; i < value.Length; i += size)
            yield return value.Substring(i, Math.Min(size, value.Length - i));
    }

    /// <summary>CRC-16/CCITT-FALSE (poly 0x1021, init 0xFFFF) — plenty for catching fat-finger typos in a
    /// short CD key; not a security control (the ECDSA signature on the activation certificate is).</summary>
    private static ushort Crc16Ccitt(byte[] data)
    {
        ushort crc = 0xFFFF;
        foreach (var b in data)
        {
            crc ^= (ushort)(b << 8);
            for (int i = 0; i < 8; i++)
            {
                crc = (crc & 0x8000) != 0
                    ? (ushort)((crc << 1) ^ 0x1021)
                    : (ushort)(crc << 1);
            }
        }
        return crc;
    }
}
