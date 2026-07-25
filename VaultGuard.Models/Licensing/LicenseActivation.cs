using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VaultGuard.Models.Licensing;

/// <summary>
/// One device's activation of a <see cref="LicenseKey"/>. Created when a client (WPF/Blazor) calls
/// POST /api/license/activate; the count of active rows for a key is compared against
/// <see cref="LicenseKey.MaxActivations"/> to enforce seat limits.
/// </summary>
public class LicenseActivation
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid LicenseKeyId { get; set; }
    [ForeignKey(nameof(LicenseKeyId))]
    public virtual LicenseKey LicenseKey { get; set; } = null!;

    /// <summary>Stable per-install device fingerprint the client generates (not a hardware serial —
    /// see docs/LICENSING.md). Used to make re-activation from the same install idempotent.</summary>
    [Required]
    [MaxLength(200)]
    public string DeviceId { get; set; } = "";

    [MaxLength(200)]
    public string? DeviceName { get; set; }

    [MaxLength(50)]
    public string? AppVersion { get; set; }

    /// <summary>"WPF", "Blazor.Web", "Blazor.MAUI", etc.</summary>
    [MaxLength(50)]
    public string? Platform { get; set; }

    public DateTime ActivatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastValidatedAt { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime? DeactivatedAt { get; set; }
}
