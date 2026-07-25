namespace VaultGuard.Crypto.Interfaces;

/// <summary>
/// An AES-256-GCM encrypted, then ECDSA-signed blob. See docs/LICENSING.md for why both layers exist:
/// encryption keeps the cached certificate from being casually read/edited, while the signature — over
/// the ciphertext, nonce and tag together — is the actual anti-forgery guarantee, since the ECDSA
/// private key never leaves the license-issuing server (VaultGuard.Admin/API), unlike the AES key which
/// necessarily ships inside every client.
/// </summary>
public class SignedLicensePackage
{
    public string CiphertextBase64 { get; set; } = "";
    public string NonceBase64 { get; set; } = "";
    public string TagBase64 { get; set; } = "";
    public string SignatureBase64 { get; set; } = "";
}

/// <summary>
/// Cryptography for the CD-key / license-certificate system. Kept separate from
/// <see cref="ICryptographyService"/> (which is about vault-data encryption) since callers, key
/// material, and lifetime are entirely different.
/// </summary>
public interface ILicenseCryptoService
{
    /// <summary>Generates a new ECDSA P-256 signing key pair. Run once by an operator when standing up
    /// VaultGuard.Admin; the private key PEM is stored only in the admin/API's configuration
    /// (Licensing:SigningPrivateKeyPem), the public key PEM is embedded in every client.</summary>
    (string PublicKeyPem, string PrivateKeyPem) GenerateSigningKeyPair();

    /// <summary>Encrypts <paramref name="json"/> with AES-256-GCM under <paramref name="aesKey"/>, then
    /// signs (ciphertext || nonce || tag) with the ECDSA private key.</summary>
    SignedLicensePackage Seal(string json, byte[] aesKey, string privateKeyPem);

    /// <summary>Verifies the ECDSA signature, then decrypts. Throws <see cref="System.Security.Cryptography.CryptographicException"/>
    /// if the signature or GCM authentication tag don't check out (tampered or forged package).</summary>
    string Open(SignedLicensePackage package, byte[] aesKey, string publicKeyPem);

    /// <summary>Generates a random human-typeable CD key, e.g. "VG-7K9QP-3M2XR-8HN4T-QW6ZD" (Crockford
    /// Base32, grouped, with a trailing checksum character per group so obvious typos are caught client-side
    /// before ever calling the API).</summary>
    string GenerateCdKey(string prefix = "VG");

    /// <summary>Normalizes user input (case/dash-insensitive) and validates the checksum. Returns false for
    /// malformed input or a failed checksum — does NOT check the database (revocation/expiry/activation
    /// count are server-side checks in LicenseController).</summary>
    bool TryNormalizeCdKey(string input, out string normalized);
}
