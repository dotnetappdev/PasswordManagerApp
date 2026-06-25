using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VaultGuard.Models;

public class CustomField
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public string Value { get; set; } = string.Empty;
    
    [Required]
    public CustomFieldType Type { get; set; } = CustomFieldType.Text;
    
    public bool IsRequired { get; set; } = false;
    
    public bool IsProtected { get; set; } = false;
    
    public int DisplayOrder { get; set; } = 0;
    
    // Foreign key to PasswordItem
    public int PasswordItemId { get; set; }
    
    // Navigation property
    public PasswordItem PasswordItem { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}

public enum CustomFieldType
{
    Text = 1,
    Password = 2,
    Date = 3,
    Number = 4,
    Email = 5,
    Url = 6,
    TextArea = 7,
    Phone = 8,
    File = 9,
    Toggle = 10,          // Yes / No radio button pair
    Address = 11,         // Multi-line address block
    OneTimePassword = 12, // TOTP / authenticator code
    SignInWith = 13,      // SSO provider (e.g. "Sign in with Google")
}