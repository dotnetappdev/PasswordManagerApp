using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VaultGuard.Models;

public class CustomField
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// What is stored in the "Value" column. For secret fields (see <see cref="IsSecret"/>) this is
    /// <see cref="EncryptedPrefix"/> + "cipher|nonce|tag" (AES-256-GCM under the owner's vault key);
    /// for everything else it is the value itself. Code should use <see cref="Value"/>.
    /// </summary>
    [Required]
    [System.Text.Json.Serialization.JsonPropertyOrder(-1)] // deserialize before Value (see Value's setter)
    public string StoredValue { get; set; } = string.Empty;

    private string? _plain;

    /// <summary>
    /// The field's value. Not a database column: for an encrypted secret field it is empty until the item is
    /// opened (decrypted in memory only, so an unrelated save can never write it back in clear). Setting it
    /// stages a new value; secret fields are encrypted again before they are saved. Setting an empty value on
    /// an encrypted field leaves it unchanged (a form that couldn't decrypt it can never wipe it).
    /// </summary>
    [NotMapped]
    public string Value
    {
        get => _plain ?? (IsEncrypted ? string.Empty : StoredValue);
        set
        {
            if (string.IsNullOrEmpty(value) && IsEncrypted) { _plain = null; return; }
            _plain = value ?? string.Empty;
            StoredValue = _plain;
        }
    }

    /// <summary>Marks an encrypted <see cref="StoredValue"/>.</summary>
    public const string EncryptedPrefix = "vgenc1:";

    [NotMapped, System.Text.Json.Serialization.JsonIgnore]
    public bool IsEncrypted => StoredValue.StartsWith(EncryptedPrefix, StringComparison.Ordinal);

    /// <summary>Password and one-time-password fields, anything marked protected, and the TOTP secret.</summary>
    [NotMapped, System.Text.Json.Serialization.JsonIgnore]
    public bool IsSecret => !ReservedNames.Contains(Name) &&
        (Type is CustomFieldType.Password or CustomFieldType.OneTimePassword || IsProtected ||
         string.Equals(Name, "TOTP Secret", StringComparison.OrdinalIgnoreCase));

    // App-internal fields that hold flags or already-encrypted data, never user secrets.
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
        { "VaultGuard.Protected", "vaultguard.password-history", "Brand Icon" };

    /// <summary>Sets the decrypted value in memory without touching the stored column.</summary>
    public void SetDecrypted(string plain) => _plain = plain;

    /// <summary>Replaces the stored column with ciphertext, keeping the in-memory value for display.</summary>
    public void SetSealed(string encryptedStoredValue) { _plain ??= StoredValue; StoredValue = encryptedStoredValue; }

    /// <summary>Drops the in-memory plaintext of an encrypted field.</summary>
    public void ForgetPlaintext() { if (IsEncrypted) _plain = null; }
    
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