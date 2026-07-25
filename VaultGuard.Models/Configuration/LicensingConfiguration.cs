namespace VaultGuard.Models.Configuration;

/// <summary>
/// Binds the "Licensing" configuration section. See docs/LICENSING.md for how these values are
/// generated and why the private key must never leave the API/VaultGuard.Admin deployment.
/// </summary>
public class LicensingConfiguration
{
    public const string SectionName = "Licensing";

    /// <summary>ECDSA P-256 private key (PEM). API/VaultGuard.Admin only — signs license certificates at
    /// activation time. Never ship this to WPF/Blazor/mobile clients.</summary>
    public string? SigningPrivateKeyPem { get; set; }

    /// <summary>ECDSA P-256 public key (PEM), matching <see cref="SigningPrivateKeyPem"/>. Safe to embed
    /// in every client — it can only verify signatures, not create them.</summary>
    public string? SigningPublicKeyPem { get; set; }

    /// <summary>Base64 AES-256 key used to encrypt the license certificate payload. Ships inside every
    /// client (it must, to decrypt/read the cached certificate) — see docs/LICENSING.md for why this is
    /// safe despite that: it only obscures the cached file, it is not what stops forged keys.</summary>
    public string? AesKeyBase64 { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(SigningPrivateKeyPem) &&
        !string.IsNullOrWhiteSpace(SigningPublicKeyPem) &&
        !string.IsNullOrWhiteSpace(AesKeyBase64);
}
