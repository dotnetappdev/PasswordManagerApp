using System;
using System.Collections.Generic;
namespace PasswordManager.Models
{
    public class Collection
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Icon { get; set; } // Optional: emoji or icon name
        public string? Color { get; set; } // Optional: for UI
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDefault { get; set; } // Mark a collection as the default one
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // User relationship
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        // Parent-child relationship
        public int? ParentCollectionId { get; set; }
        public Collection? ParentCollection { get; set; }
        public List<Collection> Children { get; set; } = new();
        // Navigation properties
        public List<Category> Categories { get; set; } = new();
        public List<PasswordItem> PasswordItems { get; set; } = new();
        public int? ParentId { get => ParentCollectionId; set => ParentCollectionId = value; } // For mapping
        // public Collection? Parent { get => ParentCollection; set => ParentCollection = value; } // For mapping - Commented out to fix EF mapping issue
    }
}
