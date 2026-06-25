using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace VaultGuard.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; } // Added for mapping
        public string? Icon { get; set; } // Optional: emoji or icon name
        public string? Color { get; set; } // Optional: for UI
        public bool IsFavorite { get; set; } // Whether this category appears in navigation
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastModified { get; set; }

        // User relationship
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        // Collection relationship
        public int? CollectionId { get; set; }
        public Collection? Collection { get; set; }

        // Vault relationship — [NotMapped] until migration is applied
        [NotMapped] public int? VaultId { get; set; }
        [NotMapped] public Vault? Vault { get; set; }

        // Navigation property for related PasswordItems
        public List<PasswordItem> PasswordItems { get; set; } = new();
    }
}
