namespace VaultGuard.Models.Licensing;

/// <summary>
/// The plaintext claims that get sealed into a <see cref="SignedLicenseCertificate"/> at activation time.
/// This is what the client actually trusts once verified — never the raw <see cref="LicenseKey.KeyCode"/>,
/// which is only a one-time activation credential.
/// </summary>
public class LicensePayload
{
    public Guid LicenseKeyId { get; set; }
    public string CustomerEmail { get; set; } = "";
    public Guid? TenantId { get; set; }
    public LicensePlan Plan { get; set; }
    public LicenseFeature Features { get; set; }
    public string DeviceId { get; set; } = "";
    public DateTime IssuedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// The encrypted-and-signed certificate a client caches locally after activation (see
/// docs/LICENSING.md). The <see cref="LicensePayload"/> JSON is AES-256-GCM encrypted (so the cached
/// file isn't plainly readable/editable), and the ciphertext+nonce+tag are then signed with an
/// ECDSA P-256 private key that only ever lives on the license server — that signature, verified with
/// the public key embedded in every client, is what actually prevents forged/pirated keys, independent
/// of whether the symmetric encryption key has leaked.
/// </summary>
public class SignedLicenseCertificate
{
    public int FormatVersion { get; set; } = 1;
    public string Algorithm { get; set; } = "AES-256-GCM+ECDSA-P256-SHA256";
    public string CiphertextBase64 { get; set; } = "";
    public string NonceBase64 { get; set; } = "";
    public string TagBase64 { get; set; } = "";
    public string SignatureBase64 { get; set; } = "";
}
