using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VaultGuard.Models.Tenancy;

namespace VaultGuard.Models.Licensing;

/// <summary>
/// A CD key issued by a super admin (VaultGuard.Admin) for a customer or tenant. The row here is the
/// server-side source of truth (revocation, activation limits); the value that actually ships to a
/// customer is <see cref="KeyCode"/> — see docs/LICENSING.md for the full activation flow and the
/// encrypt-then-sign format used for the certificate issued at activation time.
/// </summary>
public class LicenseKey
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Human-typeable CD key, e.g. "VG-7K9QP-3M2XR-8HN4T-QW6ZD". Stored as issued (not hashed) so
    /// support/admin can look it up and re-display it; treat the table itself as sensitive.</summary>
    [Required]
    [MaxLength(64)]
    public string KeyCode { get; set; } = "";

    [Required]
    [MaxLength(256)]
    public string CustomerEmail { get; set; } = "";

    [MaxLength(100)]
    public string? CustomerName { get; set; }

    /// <summary>Direct assignment to an existing VaultGuard account, set when a SuperAdmin assigns this
    /// license to a user from VaultGuard.Admin (rather than just issuing a key by email for someone to
    /// activate themselves). Null for a freeform/by-email license with no linked account yet.</summary>
    [MaxLength(450)]
    public string? UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }

    /// <summary>Optional tenant this license belongs to (org-wide license). Null for an individual license.</summary>
    public Guid? TenantId { get; set; }
    [ForeignKey(nameof(TenantId))]
    public virtual Tenant? Tenant { get; set; }

    public LicensePlan Plan { get; set; } = LicensePlan.Pro;

    /// <summary>Features actually granted; defaults to the plan's default set at issue time but can be
    /// customized per-key (e.g. a Pro key with SSO added for one customer).</summary>
    public LicenseFeature Features { get; set; } = LicenseFeature.None;

    /// <summary>How many distinct devices may activate this key concurrently.</summary>
    public int MaxActivations { get; set; } = 1;

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Null = perpetual license.</summary>
    public DateTime? ExpiresAt { get; set; }

    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    [MaxLength(500)]
    public string? RevokedReason { get; set; }

    /// <summary>Admin (Identity) user id that issued this key, for audit purposes.</summary>
    [MaxLength(450)]
    public string? IssuedByAdminUserId { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public virtual ICollection<LicenseActivation> Activations { get; set; } = new List<LicenseActivation>();

    [NotMapped]
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;

    [NotMapped]
    public bool IsValid => !IsRevoked && !IsExpired;
}
