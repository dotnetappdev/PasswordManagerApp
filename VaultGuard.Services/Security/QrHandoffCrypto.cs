using System;
using System.Security.Cryptography;
using System.Text;

namespace VaultGuard.Services.Security;

/// <summary>
/// End-to-end encryption for the QR sign-in "master-key hand-off". The device that DISPLAYS the QR
/// (desktop/WPF/Blazor) creates an ephemeral P-256 key pair and puts its public key in the QR. The
/// device that SCANS it (phone) encrypts the account's master password to that public key; the server
/// only ever relays the ciphertext. The displaying device then decrypts it with its private key and
/// unlocks its own local vault — so this works in local/SQLite mode and the server never sees the
/// plaintext password on this path.
///
/// Scheme: ECDH (nistP256) → shared secret hashed to a 256-bit key (SHA-256) → AES-256-GCM
/// (96-bit random nonce, 128-bit tag). Public keys travel as base64 SubjectPublicKeyInfo.
/// </summary>
public sealed class QrHandoffKeyPair : IDisposable
{
    private readonly ECDiffieHellman _ecdh;

    private QrHandoffKeyPair(ECDiffieHellman ecdh)
    {
        _ecdh = ecdh;
        PublicKeyBase64 = Convert.ToBase64String(_ecdh.ExportSubjectPublicKeyInfo());
    }

    /// <summary>This device's ephemeral public key (base64 SubjectPublicKeyInfo) to embed in the QR.</summary>
    public string PublicKeyBase64 { get; }

    /// <summary>Creates a fresh ephemeral key pair for one QR sign-in session.</summary>
    public static QrHandoffKeyPair Create() =>
        new(ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256));

    /// <summary>
    /// Decrypts a payload produced by <see cref="QrHandoffCrypto.Encrypt"/> using the sender's ephemeral
    /// public key. Returns the recovered plaintext (the master password).
    /// </summary>
    public string Decrypt(string senderPublicKeyBase64, string nonceBase64, string ciphertextBase64)
    {
        var key = DeriveKey(_ecdh, senderPublicKeyBase64);
        var nonce = Convert.FromBase64String(nonceBase64);
        var cipherAndTag = Convert.FromBase64String(ciphertextBase64);

        const int tagSize = 16;
        if (cipherAndTag.Length < tagSize)
            throw new CryptographicException("Ciphertext is too short.");

        var cipher = new byte[cipherAndTag.Length - tagSize];
        var tag = new byte[tagSize];
        Buffer.BlockCopy(cipherAndTag, 0, cipher, 0, cipher.Length);
        Buffer.BlockCopy(cipherAndTag, cipher.Length, tag, 0, tagSize);

        var plaintext = new byte[cipher.Length];
        using var aes = new AesGcm(key, tagSize);
        aes.Decrypt(nonce, cipher, tag, plaintext);
        return Encoding.UTF8.GetString(plaintext);
    }

    internal static byte[] DeriveKey(ECDiffieHellman ownEcdh, string peerPublicKeyBase64)
    {
        using var peer = ECDiffieHellman.Create();
        peer.ImportSubjectPublicKeyInfo(Convert.FromBase64String(peerPublicKeyBase64), out _);
        // 32-byte symmetric key from the ECDH shared secret.
        return ownEcdh.DeriveKeyFromHash(peer.PublicKey, HashAlgorithmName.SHA256);
    }

    public void Dispose() => _ecdh.Dispose();
}

/// <summary>Ciphertext bundle the scanning device sends to the server to relay to the displaying device.</summary>
public sealed record QrHandoffPayload(string EphemeralPublicKey, string Nonce, string Ciphertext);

public static class QrHandoffCrypto
{
    /// <summary>
    /// Encrypts <paramref name="plaintext"/> (the master password) to the displaying device's public key.
    /// Generates a one-shot ephemeral key pair for forward secrecy; the returned public key lets the
    /// recipient derive the same shared secret.
    /// </summary>
    public static QrHandoffPayload Encrypt(string recipientPublicKeyBase64, string plaintext)
    {
        using var ephemeral = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var ephemeralPublicKey = Convert.ToBase64String(ephemeral.ExportSubjectPublicKeyInfo());
        var key = QrHandoffKeyPair.DeriveKey(ephemeral, recipientPublicKeyBase64);

        var nonce = RandomNumberGenerator.GetBytes(12);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[plaintextBytes.Length];
        const int tagSize = 16;
        var tag = new byte[tagSize];

        using (var aes = new AesGcm(key, tagSize))
        {
            aes.Encrypt(nonce, plaintextBytes, cipher, tag);
        }

        var cipherAndTag = new byte[cipher.Length + tagSize];
        Buffer.BlockCopy(cipher, 0, cipherAndTag, 0, cipher.Length);
        Buffer.BlockCopy(tag, 0, cipherAndTag, cipher.Length, tagSize);

        return new QrHandoffPayload(
            ephemeralPublicKey,
            Convert.ToBase64String(nonce),
            Convert.ToBase64String(cipherAndTag));
    }
}
