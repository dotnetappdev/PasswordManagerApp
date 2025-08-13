using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
    
    // User relationship
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    
    public int? CategoryId { get; set; } // Foreign key to Category - now required
    
    // Collection relationship
    public int? CollectionId { get; set; } // Foreign key to Collection - required

    public string? Website { get; set; }
    // Navigation properties
    public LoginItem? LoginItem { get; set; }
    public CreditCardItem? CreditCardItem { get; set; }
    public SecureNoteItem? SecureNoteItem { get; set; }

    public DateTime? LastAccessedAt { get; set; }
    public WiFiItem? WiFiItem { get; set; }
    public Category Category { get; set; } = null!; // Required navigation property
    public Collection Collection { get; set; } = null!; // Required navigation property
    public List<Tag> Tags { get; set; } = new();
    
    // Computed properties for backward compatibility with UI components
    [NotMapped]
    public string? Username 
    { 
        get => LoginItem?.Username; 
        set 
        { 
            if (LoginItem != null) 
                LoginItem.Username = value; 
        } 
    }
    
    [NotMapped]
    public string? Password 
    { 
        get => LoginItem?.Password; 
        set 
        { 
            if (LoginItem != null) 
                LoginItem.Password = value; 
        } 
    }
    
    [NotMapped]
    public string? WebsiteUrl 
    { 
        get => LoginItem?.WebsiteUrl; 
        set 
        { 
            if (LoginItem != null) 
                LoginItem.WebsiteUrl = value; 
        } 
    }
}
