using VaultGuard.Models;

namespace VaultGuard.Services.Interfaces;

/// <summary>
/// Service for revealing encrypted passwords stored in password items
/// </summary>
public interface IPasswordRevealService
{
    /// <summary>
    /// Reveals the password for a login item
    /// </summary>
    /// <param name="loginItem">The login item containing encrypted password</param>
    /// <param name="sessionId">The session ID for decryption</param>
    /// <returns>The decrypted password or null if decryption fails</returns>
    Task<string?> RevealPasswordAsync(LoginItem loginItem, string sessionId);

    /// <summary>
    /// Encrypts <see cref="LoginItem.Password"/> (a non-persisted, plaintext-only property) into
    /// <see cref="LoginItem.EncryptedPassword"/> so it is actually saved. Call before creating or
    /// updating an item from a form. Returns false (leaving the stored password unchanged) when the
    /// password is empty or the vault is locked.
    /// </summary>
    bool TryEncryptLoginPassword(LoginItem loginItem, string sessionId);

    /// <summary>
    /// Encrypts every plaintext secret on the item (login, card, Wi-Fi, note, passkey) with the caller's
    /// own unlocked session before it is saved. Returns false if that session isn't unlocked.
    /// Empty plaintext leaves the stored value unchanged. See VaultGuard.Services.Security.VaultItemSecrets.
    /// </summary>
    bool TrySealItem(PasswordItem item, string sessionId);

    /// <summary>
    /// Decrypts the item's stored secrets into its (non-persisted) plaintext properties. Returns false if
    /// the vault is locked or any field failed to decrypt.
    /// </summary>
    bool OpenItem(PasswordItem item, string sessionId);

    /// <summary>
    /// Decrypts any vault-encrypted field stored either pipe-joined ("cipher|nonce|tag") or split
    /// across ciphertext/nonce/tag columns. Returns null when locked or on failure.
    /// </summary>
    string? RevealField(string? encrypted, string? nonce, string? tag, string sessionId);

    /// <summary>
    /// Encrypts a value with the caller's own vault session into the pipe-joined format, or returns
    /// null if that session isn't unlocked. Never falls back to another session.
    /// </summary>
    string? ProtectField(string plaintext, string sessionId);

    /// <summary>
    /// Reveals the authenticator (TOTP) secret for a login item. This is where TOTP secrets
    /// imported from other managers (1Password, etc.) are stored — encrypted at rest in
    /// <see cref="LoginItem.EncryptedTotpSecret"/> — so the verification-code UI can read them.
    /// </summary>
    Task<string?> RevealTotpSecretAsync(LoginItem loginItem, string sessionId);

    /// <summary>
    /// Reveals the password for a WiFi item
    /// </summary>
    /// <param name="wifiItem">The WiFi item containing encrypted password</param>
    /// <param name="sessionId">The session ID for decryption</param>
    /// <returns>The decrypted password or null if decryption fails</returns>
    Task<string?> RevealPasswordAsync(WiFiItem wifiItem, string sessionId);

    /// <summary>
    /// Reveals the card number for a credit card item
    /// </summary>
    /// <param name="creditCardItem">The credit card item containing encrypted card number</param>
    /// <param name="sessionId">The session ID for decryption</param>
    /// <returns>The decrypted card number or null if decryption fails</returns>
    Task<string?> RevealCardNumberAsync(CreditCardItem creditCardItem, string sessionId);

    /// <summary>
    /// Reveals the CVV for a credit card item
    /// </summary>
    /// <param name="creditCardItem">The credit card item containing encrypted CVV</param>
    /// <param name="sessionId">The session ID for decryption</param>
    /// <returns>The decrypted CVV or null if decryption fails</returns>
    Task<string?> RevealCvvAsync(CreditCardItem creditCardItem, string sessionId);

    /// <summary>
    /// Reveals the content for a secure note item
    /// </summary>
    /// <param name="secureNoteItem">The secure note item containing encrypted content</param>
    /// <param name="sessionId">The session ID for decryption</param>
    /// <returns>The decrypted content or null if decryption fails</returns>
    Task<string?> RevealContentAsync(SecureNoteItem secureNoteItem, string sessionId);

    /// <summary>
    /// Checks if the vault is unlocked and passwords can be revealed
    /// </summary>
    /// <param name="sessionId">The session ID to check</param>
    /// <returns>True if passwords can be revealed</returns>
    bool CanRevealPasswords(string sessionId);
}
