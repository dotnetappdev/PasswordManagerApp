using System.ComponentModel.DataAnnotations;

namespace VaultGuard.Models.Licensing;

/// <summary>
/// Single-row, database-persisted licensing configuration — lets a SuperAdmin generate and manage the
/// signing keys from VaultGuard.Admin instead of hand-editing appsettings.json / redeploying. If this
/// row doesn't exist, <c>LicenseController</c> falls back to the "Licensing" appsettings.json section
/// (<see cref="Configuration.LicensingConfiguration"/>), so existing deployments that already configured
/// it via appsettings keep working unchanged. See docs/LICENSING.md.
/// </summary>
public class LicensingSettings
{
    /// <summary>Always 1 — this table only ever holds a single row (upserted in place).</summary>
    [Key]
    public int Id { get; set; } = 1;

    /// <summary>ECDSA P-256 private key (PEM). Only ever read/written server-side — never returned by
    /// any API response, including to SuperAdmin (the settings endpoints report Configured=true/false,
    /// not the key material).</summary>
    public string? SigningPrivateKeyPem { get; set; }

    /// <summary>ECDSA P-256 public key (PEM) — safe to expose; distributed to clients' appsettings.json.</summary>
    public string? SigningPublicKeyPem { get; set; }

    /// <summary>Base64 AES-256 key — ships inside every client to decrypt the certificate payload; see
    /// docs/LICENSING.md for why that's an acceptable, documented trade-off.</summary>
    public string? AesKeyBase64 { get; set; }

    /// <summary>Default number of device activations for newly issued licenses when the admin doesn't
    /// override it (VaultGuard.Admin's "Issue License" dialog pre-fills this).</summary>
    public int DefaultMaxActivations { get; set; } = 1;

    /// <summary>Default plan pre-selected in the "Issue License" dialog.</summary>
    public LicensePlan DefaultPlan { get; set; } = LicensePlan.Pro;

    public DateTime? GeneratedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
