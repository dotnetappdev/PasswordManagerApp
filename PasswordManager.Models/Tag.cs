using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;

namespace PasswordManager.Models;

public class Tag
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(7)]
    public string? Color { get; set; } // Hex color code

    [MaxLength(200)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // For mapping
    public bool IsSystemTag { get; set; } // For built-in tags like "favorite", "work", etc.

    // User relationship
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    // Navigation properties
    public List<PasswordItem> PasswordItems { get; set; } = new();
}
