using PasswordManager.Models;

namespace PasswordManager.Services.Interfaces;

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
