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
        
        // Navigation properties
        public List<Category> Categories { get; set; } = new();
        public List<PasswordItem> PasswordItems { get; set; } = new();
    }
}
