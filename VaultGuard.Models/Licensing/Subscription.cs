using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VaultGuard.Models.Tenancy;

namespace VaultGuard.Models.Licensing;

public enum SubscriptionStatus
{
    Trialing = 0,
    Active = 1,
    PastDue = 2,
    Cancelled = 3,
    Expired = 4
}

/// <summary>
/// Billing/plan state for either an individual user or a whole tenant. This is deliberately payment-
/// provider agnostic (<see cref="ExternalProviderRef"/> is a free-text slot for a Stripe/Paddle
/// subscription id) — wiring an actual payment provider is a follow-up, not part of this pass.
/// </summary>
public class Subscription
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Set for an individual (non-tenant) subscriber.</summary>
    [MaxLength(450)]
    public string? UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }

    /// <summary>Set for an org-wide subscription covering every user in the tenant.</summary>
    public Guid? TenantId { get; set; }
    [ForeignKey(nameof(TenantId))]
    public virtual Tenant? Tenant { get; set; }

    public LicensePlan Plan { get; set; } = LicensePlan.Free;
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    public int SeatCount { get; set; } = 1;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CurrentPeriodEnd { get; set; }
    public DateTime? CancelledAt { get; set; }

    [MaxLength(200)]
    public string? ExternalProviderRef { get; set; }

    /// <summary>The CD key this subscription was activated with, if any (self-service subscriptions
    /// created directly by an admin may have no associated license key).</summary>
    public Guid? LicenseKeyId { get; set; }
    [ForeignKey(nameof(LicenseKeyId))]
    public virtual LicenseKey? LicenseKey { get; set; }

    [NotMapped]
    public bool IsActive => Status is SubscriptionStatus.Active or SubscriptionStatus.Trialing;
}
