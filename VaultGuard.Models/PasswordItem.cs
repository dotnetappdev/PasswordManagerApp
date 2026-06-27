using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace VaultGuard.Models;

public class PasswordItem : INotifyPropertyChanged
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

    // State flags shown live in list rows — raise change notification so WPF/MAUI
    // DataTriggers (e.g. the favourite star) update the instant the flag flips,
    // without needing a full list refresh. EF Core ignores INotifyPropertyChanged.
    private bool _isFavorite;
    public bool IsFavorite
    {
        get => _isFavorite;
        set => SetField(ref _isFavorite, value);
    }

    private bool _isArchived;
    public bool IsArchived
    {
        get => _isArchived;
        set => SetField(ref _isArchived, value);
    }

    private bool _isDeleted;
    public bool IsDeleted
    {
        get => _isDeleted;
        set => SetField(ref _isDeleted, value);
    }
    
    // User relationship
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    
    public int? CategoryId { get; set; } // Foreign key to Category - now required
    
    // Collection relationship
    public int? CollectionId { get; set; } // Foreign key to Collection - required

    // Vault relationship — column not yet in database; excluded from all SQL until migration is applied
    [NotMapped] public int? VaultId { get; set; }
    [NotMapped] public Vault? Vault { get; set; }

    public string? Website { get; set; }
    // Navigation properties
    public LoginItem? LoginItem { get; set; }
    public CreditCardItem? CreditCardItem { get; set; }
    public SecureNoteItem? SecureNoteItem { get; set; }
    public WiFiItem? WiFiItem { get; set; }
    public PasskeyItem? PasskeyItem { get; set; }

    public DateTime? LastAccessedAt { get; set; }
    public Category Category { get; set; } = null!; // Required navigation property
    public Collection Collection { get; set; } = null!; // Required navigation property
    public List<Tag> Tags { get; set; } = new();
    
    // Computed properties for backward compatibility with UI components
    private LoginItem EnsureLoginItem()
    {
        LoginItem ??= new LoginItem();
        return LoginItem;
    }

    [NotMapped]
    public string? Username 
    { 
        get => LoginItem?.Username; 
        set => EnsureLoginItem().Username = value;
    }
    
    [NotMapped]
    public string? Password 
    { 
        get => LoginItem?.Password; 
        set => EnsureLoginItem().Password = value;
    }
    
    [NotMapped]
    public string? WebsiteUrl 
    { 
        get => LoginItem?.WebsiteUrl ?? LoginItem?.Website ?? Website; 
        set 
        { 
            var loginItem = EnsureLoginItem();
            loginItem.WebsiteUrl = value;
            loginItem.Website = value;
            Website = value;
        } 
    }
    
    // Custom fields navigation property
    public List<CustomField> CustomFields { get; set; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField(ref bool field, bool value, [CallerMemberName] string? propertyName = null)
    {
        if (field == value) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
