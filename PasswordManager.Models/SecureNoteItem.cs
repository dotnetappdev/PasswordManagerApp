using System.ComponentModel.DataAnnotations;

namespace PasswordManager.Models;

public class SecureNoteItem
{
    public int Id { get; set; }
    public int PasswordItemId { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    // User relationship
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    // Title
    [MaxLength(100)]
    public string? Title { get; set; }
    
    [MaxLength(10000)]
    public string? Content { get; set; }
    
    public bool IsMarkdown { get; set; }
    
    public bool IsRichText { get; set; }
    
    // File Attachments (if supported)
    [MaxLength(500)]
    public string? AttachmentPaths { get; set; } // JSON array of file paths
    
    // Categories for organization
    [MaxLength(50)]
    public string? Category { get; set; }
    
    // Template type for common note formats
    [MaxLength(50)]
    public string? TemplateType { get; set; } // Personal, Business, Legal, Medical, etc.
    
    // Security level
    public bool IsHighSecurity { get; set; }
    
    // Expiration (for time-sensitive notes)
    public DateTime? ExpiresAt { get; set; }
    
    // Sharing
    public bool IsShared { get; set; }
    
    [MaxLength(500)]
    public string? SharedWith { get; set; } // JSON array of email addresses
    
    // Version control
    public int Version { get; set; } = 1;
    
    [MaxLength(200)]
    public string? LastEditedBy { get; set; }
    
    // Usage Tracking
    public DateTime? LastUsed { get; set; }
    
    public int UsageCount { get; set; }
    
    // Navigation property
    public PasswordItem PasswordItem { get; set; } = null!;
}
