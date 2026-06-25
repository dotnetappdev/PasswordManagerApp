using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PasswordManager.Models;

/// <summary>
/// Represents a one-time passcode for two-factor authentication
/// </summary>
public class OtpCode
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// User ID this OTP belongs to
    /// </summary>
    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// The OTP code (hashed for security)
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string CodeHash { get; set; } = string.Empty;
    
    /// <summary>
    /// Phone number this OTP was sent to
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// When this OTP was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When this OTP expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>
    /// Number of verification attempts made
    /// </summary>
    public int AttemptCount { get; set; } = 0;
    
    /// <summary>
    /// Whether this OTP has been used successfully
    /// </summary>
    public bool IsUsed { get; set; } = false;
    
    /// <summary>
    /// When this OTP was used (if applicable)
    /// </summary>
    public DateTime? UsedAt { get; set; }
    
    /// <summary>
    /// IP address from which the OTP was requested
    /// </summary>
    [MaxLength(45)]
    public string? RequestIpAddress { get; set; }
    
    /// <summary>
    /// User agent string from the OTP request
    /// </summary>
    [MaxLength(500)]
    public string? RequestUserAgent { get; set; }
    
    // Navigation property
    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser User { get; set; } = null!;
}