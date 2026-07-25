using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VaultGuard.Models.Licensing;

namespace VaultGuard.Models.Tenancy;

public enum TenantStatus
{
    PendingSetup = 0,
    Active = 1,
    Suspended = 2,
    Cancelled = 3
}

/// <summary>
/// A customer organization. Every VaultGuard install is single-tenant by default (TenantId is null on
/// ApplicationUser); creating a Tenant here is what turns on multi-tenant behavior for its users —
/// see docs/ADMIN_MULTITENANCY.md for how <see cref="Slug"/> / <see cref="CustomDomain"/> get resolved
/// to a tenant by TenantResolutionMiddleware in VaultGuard.API and VaultGuard.Web.
/// </summary>
public class Tenant
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = "";

    /// <summary>Subdomain slug, e.g. "acme" for acme.vaultguardapp.com. Lowercase, DNS-label safe.</summary>
    [Required]
    [MaxLength(63)]
    public string Slug { get; set; } = "";

    /// <summary>Customer-owned vanity domain, e.g. "vaultguard.acme.com". Set by an admin after the
    /// customer points a CNAME at the VaultGuard host (see docs/ADMIN_MULTITENANCY.md); requests to that
    /// Host are matched here at runtime once <see cref="CustomDomainVerified"/> is true.</summary>
    [MaxLength(253)]
    public string? CustomDomain { get; set; }

    public bool CustomDomainVerified { get; set; }

    /// <summary>Random token the customer must publish as a TXT record (_vaultguard-verify.&lt;domain&gt;)
    /// before the custom domain is activated — proves domain ownership without any manual out-of-band step.</summary>
    [MaxLength(64)]
    public string? DomainVerificationToken { get; set; }

    public TenantStatus Status { get; set; } = TenantStatus.PendingSetup;

    public Guid? SubscriptionId { get; set; }
    [ForeignKey(nameof(SubscriptionId))]
    public virtual Subscription? Subscription { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    public virtual ICollection<LicenseKey> LicenseKeys { get; set; } = new List<LicenseKey>();
}
