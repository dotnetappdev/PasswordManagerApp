using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PasswordManager.Models;

/// <summary>
/// Represents a vault (collection of categories and password items) like in 1Password
/// </summary>
public class Vault
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Indicates if this is the default vault for the user (typically "Personal")
    /// </summary>
    public bool IsDefault { get; set; }

    [MaxLength(100)]
    public string? Icon { get; set; } // Optional: emoji or icon name

    [MaxLength(7)]
    public string? Color { get; set; } // Optional: hex color for UI

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // User relationship
    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    // Navigation properties
    public List<Category> Categories { get; set; } = new();
    public List<PasswordItem> PasswordItems { get; set; } = new();
}
