using PasswordManager.Models.DTOs;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PasswordManager.Models;

public class LoginItem
{
    public int Id { get; set; }
    public int PasswordItemId { get; set; }
    
    [MaxLength(200)]
    public string? Website { get; set; }
    
    [MaxLength(100)]
    public string? Username { get; set; }
    
    // Encrypted password storage (Base64 encoded ciphertext)
    [MaxLength(1000)]
    public string? EncryptedPassword { get; set; }
    
    // Nonce for AES-GCM encryption (Base64 encoded)
    [MaxLength(200)]
    public string? PasswordNonce { get; set; }
    
    // Authentication tag for AES-GCM (Base64 encoded)
    [MaxLength(200)]
    public string? PasswordAuthTag { get; set; }
    
    [MaxLength(200)]
    public string? Email { get; set; }
    
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
    
    // Two-Factor Authentication (also encrypted)
    [MaxLength(1000)]
    public string? EncryptedTotpSecret { get; set; }
    
    [MaxLength(200)]
    public string? TotpNonce { get; set; }
    
    [MaxLength(200)]
    public string? TotpAuthTag { get; set; }
    
    [MaxLength(200)]
    public string? TwoFactorType { get; set; } // SMS, Authenticator App, Email, etc.
    
    // Security Questions (encrypted)
    [MaxLength(200)]
    public string? SecurityQuestion1 { get; set; }
    
    [MaxLength(1000)]
    public string? EncryptedSecurityAnswer1 { get; set; }
    
    [MaxLength(200)]
    public string? SecurityAnswer1Nonce { get; set; }
    
    [MaxLength(200)]
    public string? SecurityAnswer1AuthTag { get; set; }
    
    [MaxLength(200)]
    public string? SecurityQuestion2 { get; set; }
    
    [MaxLength(1000)]
    public string? EncryptedSecurityAnswer2 { get; set; }
    
    [MaxLength(200)]
    public string? SecurityAnswer2Nonce { get; set; }
    
    [MaxLength(200)]
    public string? SecurityAnswer2AuthTag { get; set; }
    
    [MaxLength(200)]
    public string? SecurityQuestion3 { get; set; }
    
    [MaxLength(1000)]
    public string? EncryptedSecurityAnswer3 { get; set; }
    
    [MaxLength(200)]
    public string? SecurityAnswer3Nonce { get; set; }
    
    [MaxLength(200)]
    public string? SecurityAnswer3AuthTag { get; set; }
    
    // Alternative Contacts
    [MaxLength(200)]
    public string? RecoveryEmail { get; set; }
    
    [MaxLength(20)]
    public string? RecoveryPhone { get; set; }
    
    // Additional URLs
    [MaxLength(200)]
    public string? LoginUrl { get; set; }
    
    [MaxLength(200)]
    public string? SupportUrl { get; set; }
    
    [MaxLength(200)]
    public string? AdminConsoleUrl { get; set; }
    
    // Password History
    public DateTime? PasswordLastChanged { get; set; }
    
    public bool RequiresPasswordChange { get; set; }
    
    // Usage Tracking
    public DateTime? LastUsed { get; set; }
    
    public int UsageCount { get; set; }
    
    // Company/Organization
    [MaxLength(100)]
    public string? CompanyName { get; set; }
    
    [MaxLength(100)]
    public string? Department { get; set; }
    
    [MaxLength(100)]
    public string? JobTitle { get; set; }
    
    // Notes (encrypted)
    [MaxLength(4000)]
    public string? EncryptedNotes { get; set; }
    
    [MaxLength(200)]
    public string? NotesNonce { get; set; }
    
    [MaxLength(200)]
    public string? NotesAuthTag { get; set; }
    
    // Temporary properties for import/export (not stored in database)
    [NotMapped]
    public string? Password { get; set; }
    
    [NotMapped]
    public string? TotpSecret { get; set; }
    
    [NotMapped]
    public string? SecurityAnswer1 { get; set; }
    
    [NotMapped]
    public string? SecurityAnswer2 { get; set; }
    
    [NotMapped]
    public string? SecurityAnswer3 { get; set; }
    
    [NotMapped]
    public string? Notes { get; set; }
    
    // Navigation property
    public PasswordItem PasswordItem { get; set; } = null!;

    public static implicit operator LoginItem(CreateLoginItemDto v)
    {
        throw new NotImplementedException();
    }
}
