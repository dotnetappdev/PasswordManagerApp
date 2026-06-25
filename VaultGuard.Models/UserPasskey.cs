using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PasswordManager.Models;

/// <summary>
/// Represents a WebAuthn passkey for a user
/// </summary>
public class UserPasskey
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to ApplicationUser
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// WebAuthn credential ID (base64 encoded)
    /// </summary>
    [Required]
    [MaxLength(1024)]
    public string CredentialId { get; set; } = string.Empty;

    /// <summary>
    /// User-friendly name for the passkey
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// WebAuthn public key (base64 encoded)
    /// </summary>
    [Required]
    [MaxLength(2048)]
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// WebAuthn signature counter
    /// </summary>
    public uint SignatureCounter { get; set; } = 0;

    /// <summary>
    /// Device type/platform where the passkey was created
    /// </summary>
    [MaxLength(50)]
    public string? DeviceType { get; set; }

    /// <summary>
    /// Whether this passkey is backed up/synced across devices
    /// </summary>
    public bool IsBackedUp { get; set; } = false;

    /// <summary>
    /// Whether this passkey requires user verification (PIN/biometrics)
    /// </summary>
    public bool RequiresUserVerification { get; set; } = true;

    /// <summary>
    /// Date when the passkey was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date when the passkey was last used
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Whether the passkey is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether to store this passkey in the encrypted password vault
    /// </summary>
    public bool StoreInVault { get; set; } = true;

    /// <summary>
    /// Encrypted passkey data for vault storage (if StoreInVault is true)
    /// </summary>
    [MaxLength(4096)]
    public string? EncryptedVaultData { get; set; }

    // Navigation property
    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser User { get; set; } = null!;
}