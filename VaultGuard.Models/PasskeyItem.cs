using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PasswordManager.Models;

public class PasskeyItem
{
    public int Id { get; set; }
    public int PasswordItemId { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    // User relationship
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    // Passkey specific fields
    [MaxLength(200)]
    public string? Website { get; set; }

    [MaxLength(200)]
    public string? WebsiteUrl { get; set; }

    [MaxLength(100)]
    public string? Username { get; set; }

    [MaxLength(200)]
    public string? DisplayName { get; set; }

    // WebAuthn Credential ID (encrypted for vault storage)
    [MaxLength(1000)]
    public string? EncryptedCredentialId { get; set; }

    [MaxLength(200)]
    public string? CredentialIdNonce { get; set; }

    [MaxLength(200)]
    public string? CredentialIdAuthTag { get; set; }

    // Device/Platform Information
    [MaxLength(100)]
    public string? DeviceType { get; set; }

    [MaxLength(100)]
    public string? PlatformName { get; set; }

    // Passkey Metadata
    public bool IsBackedUp { get; set; } = false;
    public bool RequiresUserVerification { get; set; } = true;
    public DateTime? LastUsedAt { get; set; }
    public int UsageCount { get; set; } = 0;

    // Additional Security Fields
    [MaxLength(4000)]
    public string? EncryptedNotes { get; set; }

    [MaxLength(200)]
    public string? NotesNonce { get; set; }

    [MaxLength(200)]
    public string? NotesAuthTag { get; set; }

    // Temporary properties for display (not stored in database)
    [NotMapped]
    public string? CredentialId { get; set; }

    [NotMapped]
    public string? Notes { get; set; }

    // Navigation property
    public PasswordItem PasswordItem { get; set; } = null!;
}