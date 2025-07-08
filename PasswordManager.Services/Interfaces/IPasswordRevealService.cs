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
    /// <returns>The decrypted password or null if decryption fails</returns>
    Task<string?> RevealPasswordAsync(LoginItem loginItem);

    /// <summary>
    /// Reveals the password for a WiFi item
    /// </summary>
    /// <param name="wifiItem">The WiFi item containing encrypted password</param>
    /// <returns>The decrypted password or null if decryption fails</returns>
    Task<string?> RevealPasswordAsync(WiFiItem wifiItem);

    /// <summary>
    /// Reveals the card number for a credit card item
    /// </summary>
    /// <param name="creditCardItem">The credit card item containing encrypted card number</param>
    /// <returns>The decrypted card number or null if decryption fails</returns>
    Task<string?> RevealCardNumberAsync(CreditCardItem creditCardItem);

    /// <summary>
    /// Reveals the CVV for a credit card item
    /// </summary>
    /// <param name="creditCardItem">The credit card item containing encrypted CVV</param>
    /// <returns>The decrypted CVV or null if decryption fails</returns>
    Task<string?> RevealCvvAsync(CreditCardItem creditCardItem);

    /// <summary>
    /// Reveals the content for a secure note item
    /// </summary>
    /// <param name="secureNoteItem">The secure note item containing encrypted content</param>
    /// <returns>The decrypted content or null if decryption fails</returns>
    Task<string?> RevealContentAsync(SecureNoteItem secureNoteItem);

    /// <summary>
    /// Checks if the vault is unlocked and passwords can be revealed
    /// </summary>
    bool CanRevealPasswords { get; }
}
