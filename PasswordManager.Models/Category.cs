namespace PasswordManager.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; } // Added for mapping
        public string? Icon { get; set; } // Optional: emoji or icon name
        public string? Color { get; set; } // Optional: for UI
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        // User relationship
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
        
        // Collection relationship
        public int? CollectionId { get; set; }
        public Collection? Collection { get; set; }
        
        // Navigation property for related PasswordItems
        public List<PasswordItem> PasswordItems { get; set; } = new();
    }
}
