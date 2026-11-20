using VaultGuard.Models;

namespace VaultGuard.Models.DTOs;

public class PasswordItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ItemType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModified { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsArchived { get; set; }
    public bool IsDeleted { get; set; }
    public string? UserId { get; set; }
    public int? CategoryId { get; set; }
    public int? CollectionId { get; set; }
    public CategoryDto? Category { get; set; }
    public CollectionDto? Collection { get; set; }
    public LoginItemDto? LoginItem { get; set; }
    public CreditCardItemDto? CreditCardItem { get; set; }
    public SecureNoteItemDto? SecureNoteItem { get; set; }
    public WiFiItemDto? WiFiItem { get; set; }
    public List<TagDto> Tags { get; set; } = new();
}

public class CreatePasswordItemDto
{
    /// <summary>
    /// Required whenever the request carries a secret (password, card number, CVV, PIN, Wi-Fi password,
    /// note content…). The server uses it only to derive the vault key and encrypt those values before they
    /// are stored, then discards it; requests with secrets but no valid master password are rejected rather
    /// than stored unencrypted. Never returned.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("masterPassword")]
    public string? MasterPassword { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ItemType Type { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsArchived { get; set; } // Added to fix missing property error
    public int? CategoryId { get; set; }
    public int? CollectionId { get; set; }
    public CreateLoginItemDto? LoginItem { get; set; }
    public CreateCreditCardItemDto? CreditCardItem { get; set; }
    public CreateSecureNoteItemDto? SecureNoteItem { get; set; }
    public CreateWiFiItemDto? WiFiItem { get; set; }
    public List<int> TagIds { get; set; } = new();

}

public class UpdatePasswordItemDto
{
    /// <summary>
    /// Required whenever the request carries a secret (password, card number, CVV, PIN, Wi-Fi password,
    /// note content…). The server uses it only to derive the vault key and encrypt those values before they
    /// are stored, then discards it; requests with secrets but no valid master password are rejected rather
    /// than stored unencrypted. Never returned.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("masterPassword")]
    public string? MasterPassword { get; set; }

    public string? Title { get; set; }
    public string? Description { get; set; }
    public bool? IsFavorite { get; set; }
    public int? CategoryId { get; set; }
    public int? CollectionId { get; set; }
    public bool IsArchived { get; set; }

    public UpdateLoginItemDto? LoginItem { get; set; }
    public UpdateCreditCardItemDto? CreditCardItem { get; set; }
    public UpdateSecureNoteItemDto? SecureNoteItem { get; set; }
    public UpdateWiFiItemDto? WiFiItem { get; set; }
    public List<int> TagIds { get; set; } = new();
}
