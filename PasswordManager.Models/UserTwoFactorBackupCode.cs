using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PasswordManager.Models;

/// <summary>
/// Represents a backup code for two-factor authentication recovery
/// </summary>
public class UserTwoFactorBackupCode
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to ApplicationUser
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Hashed backup code (for security)
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string CodeHash { get; set; } = string.Empty;

    /// <summary>
    /// Salt used for hashing the backup code
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string CodeSalt { get; set; } = string.Empty;

    /// <summary>
    /// Whether this backup code has been used
    /// </summary>
    public bool IsUsed { get; set; } = false;

    /// <summary>
    /// Date when the backup code was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date when the backup code was used (if used)
    /// </summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// IP address from which the backup code was used (if used)
    /// </summary>
    [MaxLength(45)] // IPv6 max length
    public string? UsedFromIp { get; set; }

    // Navigation property
    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser User { get; set; } = null!;
}