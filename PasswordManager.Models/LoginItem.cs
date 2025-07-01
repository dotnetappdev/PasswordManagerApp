using System.ComponentModel.DataAnnotations;

namespace PasswordManager.Models;

public class LoginItem
{
    public int Id { get; set; }
    public int PasswordItemId { get; set; }
    
    [MaxLength(200)]
    public string? Website { get; set; }
    
    [MaxLength(100)]
    public string? Username { get; set; }
    
    [MaxLength(500)]
    public string? Password { get; set; }
    
    [MaxLength(200)]
    public string? Email { get; set; }
    
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
    
    // Two-Factor Authentication
    [MaxLength(500)]
    public string? TotpSecret { get; set; }
    
    [MaxLength(200)]
    public string? TwoFactorType { get; set; } // SMS, Authenticator App, Email, etc.
    
    // Security Questions
    [MaxLength(200)]
    public string? SecurityQuestion1 { get; set; }
    
    [MaxLength(300)]
    public string? SecurityAnswer1 { get; set; }
    
    [MaxLength(200)]
    public string? SecurityQuestion2 { get; set; }
    
    [MaxLength(300)]
    public string? SecurityAnswer2 { get; set; }
    
    [MaxLength(200)]
    public string? SecurityQuestion3 { get; set; }
    
    [MaxLength(300)]
    public string? SecurityAnswer3 { get; set; }
    
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
    
    [MaxLength(2000)]
    public string? Notes { get; set; }
    
    // Navigation property
    public PasswordItem PasswordItem { get; set; } = null!;
}
