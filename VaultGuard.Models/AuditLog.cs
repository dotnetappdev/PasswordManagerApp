using System.ComponentModel.DataAnnotations;

namespace PasswordManager.Models;

/// <summary>
/// Audit log entry for tracking all changes in the system
/// </summary>
public class AuditLog
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty; // Create, Update, Delete, Login, Logout, etc.
    
    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty; // PasswordItem, Collection, Category, Device, etc.
    
    [MaxLength(100)]
    public string? EntityId { get; set; }
    
    [MaxLength(200)]
    public string? EntityName { get; set; } // Human-readable name of the entity
    
    /// <summary>
    /// JSON representation of changes made (before/after for updates)
    /// </summary>
    public string? Changes { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [MaxLength(45)]
    public string? IpAddress { get; set; }
    
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    [MaxLength(100)]
    public string? DeviceId { get; set; }
    
    [MaxLength(50)]
    public string? DeviceName { get; set; }
    
    /// <summary>
    /// Whether the action was successful
    /// </summary>
    public bool Success { get; set; } = true;
    
    /// <summary>
    /// Error message if action failed
    /// </summary>
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }
    
    // Navigation property
    public virtual ApplicationUser? User { get; set; }
}
