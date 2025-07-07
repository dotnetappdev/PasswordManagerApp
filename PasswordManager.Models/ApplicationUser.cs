using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace PasswordManager.Models;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Cryptographic properties for secure password storage
    /// <summary>
    /// User-specific salt for key derivation (Base64 encoded)
    /// </summary>
    [MaxLength(200)]
    public string? UserSalt { get; set; }
    
    /// <summary>
    /// Hashed master password for authentication (cannot be used for decryption)
    /// </summary>
    [MaxLength(500)]
    public string? MasterPasswordHash { get; set; }
    
    /// <summary>
    /// PBKDF2 iterations used for master password hashing
    /// </summary>
    public int MasterPasswordIterations { get; set; } = 600000;
    
    // Navigation properties for user's data
    public virtual ICollection<PasswordItem> PasswordItems { get; set; } = new List<PasswordItem>();
    public virtual ICollection<Collection> Collections { get; set; } = new List<Collection>();
}
