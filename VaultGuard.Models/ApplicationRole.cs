using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace VaultGuard.Models;

/// <summary>
/// Application-specific role class that extends IdentityRole
/// </summary>
public class ApplicationRole : IdentityRole
{
    /// <summary>
    /// Role description for admin purposes
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// When the role was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last time the role was modified
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the role is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Display order for UI purposes
    /// </summary>
    public int DisplayOrder { get; set; } = 0;
}