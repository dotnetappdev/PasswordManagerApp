using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PasswordManager.Models;

/// <summary>
/// Represents a parent-child relationship between users
/// </summary>
public class UserRelationship
{
    /// <summary>
    /// Primary key
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Parent user ID
    /// </summary>
    [Required]
    [MaxLength(450)]
    public string ParentUserId { get; set; } = string.Empty;

    /// <summary>
    /// Child user ID
    /// </summary>
    [Required]
    [MaxLength(450)]
    public string ChildUserId { get; set; } = string.Empty;

    /// <summary>
    /// Type of relationship
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string RelationshipType { get; set; } = UserRelationshipTypes.ParentChild;

    /// <summary>
    /// When the relationship was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who created this relationship
    /// </summary>
    [MaxLength(450)]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Whether the relationship is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional notes about the relationship
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// When the relationship was last modified
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to parent user
    /// </summary>
    [ForeignKey(nameof(ParentUserId))]
    public virtual ApplicationUser? ParentUser { get; set; }

    /// <summary>
    /// Navigation property to child user
    /// </summary>
    [ForeignKey(nameof(ChildUserId))]
    public virtual ApplicationUser? ChildUser { get; set; }
}

/// <summary>
/// Types of user relationships
/// </summary>
public static class UserRelationshipTypes
{
    /// <summary>
    /// Parent-child relationship
    /// </summary>
    public const string ParentChild = "ParentChild";

    /// <summary>
    /// Guardian-ward relationship
    /// </summary>
    public const string GuardianWard = "GuardianWard";

    /// <summary>
    /// Manager-subordinate relationship (for organizational use)
    /// </summary>
    public const string ManagerSubordinate = "ManagerSubordinate";

    /// <summary>
    /// Get all available relationship types
    /// </summary>
    public static readonly string[] AllTypes = { ParentChild, GuardianWard, ManagerSubordinate };
}

/// <summary>
/// Configuration for child user permissions
/// </summary>
public class ChildPermissionConfig
{
    /// <summary>
    /// Primary key
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Child user ID
    /// </summary>
    [Required]
    [MaxLength(450)]
    public string ChildUserId { get; set; } = string.Empty;

    /// <summary>
    /// Parent user ID who manages these permissions
    /// </summary>
    [Required]
    [MaxLength(450)]
    public string ParentUserId { get; set; } = string.Empty;

    /// <summary>
    /// Whether child can view passwords (read-only)
    /// </summary>
    public bool CanViewPasswords { get; set; } = true;

    /// <summary>
    /// Whether child can create new passwords
    /// </summary>
    public bool CanCreatePasswords { get; set; } = false;

    /// <summary>
    /// Whether child can edit passwords
    /// </summary>
    public bool CanEditPasswords { get; set; } = false;

    /// <summary>
    /// Whether child can delete passwords
    /// </summary>
    public bool CanDeletePasswords { get; set; } = false;

    /// <summary>
    /// Whether child can reveal/copy sensitive password data
    /// </summary>
    public bool CanRevealPasswords { get; set; } = false;

    /// <summary>
    /// Whether child can share passwords with others
    /// </summary>
    public bool CanSharePasswords { get; set; } = false;

    /// <summary>
    /// Whether child can export data
    /// </summary>
    public bool CanExportData { get; set; } = false;

    /// <summary>
    /// Whether child can create collections
    /// </summary>
    public bool CanCreateCollections { get; set; } = false;

    /// <summary>
    /// Whether child can manage their own collections
    /// </summary>
    public bool CanManageOwnCollections { get; set; } = true;

    /// <summary>
    /// Maximum number of password items child can create (0 = no limit)
    /// </summary>
    public int MaxPasswordItems { get; set; } = 0;

    /// <summary>
    /// Time restrictions - start time (24-hour format, e.g., "08:00")
    /// </summary>
    [MaxLength(5)]
    public string? AccessStartTime { get; set; }

    /// <summary>
    /// Time restrictions - end time (24-hour format, e.g., "20:00")
    /// </summary>
    [MaxLength(5)]
    public string? AccessEndTime { get; set; }

    /// <summary>
    /// Days of week when access is allowed (comma-separated, e.g., "1,2,3,4,5" for Mon-Fri)
    /// </summary>
    [MaxLength(20)]
    public string? AllowedDaysOfWeek { get; set; }

    /// <summary>
    /// Whether to require parent approval for password changes
    /// </summary>
    public bool RequireParentApproval { get; set; } = true;

    /// <summary>
    /// Whether to log all child activities
    /// </summary>
    public bool LogActivities { get; set; } = true;

    /// <summary>
    /// When this configuration was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this configuration was last modified
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to child user
    /// </summary>
    [ForeignKey(nameof(ChildUserId))]
    public virtual ApplicationUser? ChildUser { get; set; }

    /// <summary>
    /// Navigation property to parent user
    /// </summary>
    [ForeignKey(nameof(ParentUserId))]
    public virtual ApplicationUser? ParentUser { get; set; }
}