using System.ComponentModel.DataAnnotations;

namespace PasswordManager.Models;

/// <summary>
/// Represents a device that has been linked to a user account
/// Similar to WhatsApp's multi-device functionality
/// </summary>
public class Device
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string DeviceName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string DeviceType { get; set; } = string.Empty; // Mobile, Web, Desktop, Tablet
    
    [MaxLength(100)]
    public string? Platform { get; set; } // iOS, Android, Windows, macOS, Linux, Web
    
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    [MaxLength(45)]
    public string? IpAddress { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Unique identifier for the device (can be used for push notifications)
    /// </summary>
    [MaxLength(500)]
    public string? DeviceToken { get; set; }
    
    /// <summary>
    /// Last sync timestamp for this device
    /// </summary>
    public DateTime? LastSyncAt { get; set; }
    
    /// <summary>
    /// Whether this is the primary device (used for sync priority)
    /// </summary>
    public bool IsPrimaryDevice { get; set; } = false;
    
    // Navigation property
    public virtual ApplicationUser? User { get; set; }
}
