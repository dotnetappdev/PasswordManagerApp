using System.ComponentModel.DataAnnotations;

namespace PasswordManager.Models;

public class PasswordItem
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public ItemType Type { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    
    public bool IsFavorite { get; set; }
    
    public bool IsArchived { get; set; }
    
    public bool IsDeleted { get; set; }
    
    public int CategoryId { get; set; } // Foreign key to Category - now required
    
    // Collection relationship
    public int CollectionId { get; set; } // Foreign key to Collection - required

    // Navigation properties
    public LoginItem? LoginItem { get; set; }
    public CreditCardItem? CreditCardItem { get; set; }
    public SecureNoteItem? SecureNoteItem { get; set; }
    public WiFiItem? WiFiItem { get; set; }
    public Category Category { get; set; } = null!; // Required navigation property
    public Collection Collection { get; set; } = null!; // Required navigation property
    public List<Tag> Tags { get; set; } = new();
}
