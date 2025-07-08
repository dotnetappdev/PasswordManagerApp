using PasswordManager.Crypto.Interfaces;

namespace PasswordManager.Services.Interfaces;

/// <summary>
/// Service interface for handling password item encryption and decryption using session-based vault operations
/// </summary>
public interface IPasswordEncryptionService
{
    /// <summary>
    /// Encrypts a login item's sensitive data using the session master key
    /// </summary>
    /// <param name="loginItem">Login item to encrypt</param>
    /// <param name="sessionId">Session ID to identify the user's vault session</param>
    Task EncryptLoginItemAsync(Models.LoginItem loginItem, string sessionId);

    /// <summary>
    /// Decrypts a login item's sensitive data using the session master key
    /// </summary>
    /// <param name="loginItem">Login item to decrypt</param>
    /// <param name="sessionId">Session ID to identify the user's vault session</param>
    /// <returns>Decrypted login item data</returns>
    Task<DecryptedLoginItem> DecryptLoginItemAsync(Models.LoginItem loginItem, string sessionId);

    /// <summary>
    /// Encrypts a specific field using the session master key
    /// </summary>
    /// <param name="value">Value to encrypt</param>
    /// <param name="sessionId">Session ID to identify the user's vault session</param>
    /// <returns>Encrypted field data</returns>
    Task<EncryptedPasswordData> EncryptFieldAsync(string value, string sessionId);

    /// <summary>
    /// Decrypts a specific field using the session master key
    /// </summary>
    /// <param name="encryptedData">Encrypted field data</param>
    /// <param name="sessionId">Session ID to identify the user's vault session</param>
    /// <returns>Decrypted value</returns>
    Task<string> DecryptFieldAsync(EncryptedPasswordData encryptedData, string sessionId);

    // Legacy methods for backward compatibility (deprecated)
    /// <summary>
    /// Encrypts a login item's sensitive data using the user's master password (legacy method)
    /// </summary>
    [Obsolete("Use EncryptLoginItemAsync(loginItem, sessionId) instead")]
    Task EncryptLoginItemAsync(Models.LoginItem loginItem, string masterPassword, byte[] userSalt);

    /// <summary>
    /// Decrypts a login item's sensitive data using the user's master password (legacy method)
    /// </summary>
    [Obsolete("Use DecryptLoginItemAsync(loginItem, sessionId) instead")]
    Task<DecryptedLoginItem> DecryptLoginItemAsync(Models.LoginItem loginItem, string masterPassword, byte[] userSalt);

    /// <summary>
    /// Encrypts a specific field (legacy method)
    /// </summary>
    [Obsolete("Use EncryptFieldAsync(value, sessionId) instead")]
    Task<EncryptedPasswordData> EncryptFieldAsync(string value, string masterPassword, byte[] userSalt);

    /// <summary>
    /// Decrypts a specific field (legacy method)
    /// </summary>
    [Obsolete("Use DecryptFieldAsync(encryptedData, sessionId) instead")]
    Task<string> DecryptFieldAsync(EncryptedPasswordData encryptedData, string masterPassword, byte[] userSalt);
}

/// <summary>
/// Represents decrypted login item data for UI display
/// </summary>
public class DecryptedLoginItem
{
    public int Id { get; set; }
    public int PasswordItemId { get; set; }
    public string? Website { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? TotpSecret { get; set; }
    public string? TwoFactorType { get; set; }
    public string? SecurityQuestion1 { get; set; }
    public string? SecurityAnswer1 { get; set; }
    public string? SecurityQuestion2 { get; set; }
    public string? SecurityAnswer2 { get; set; }
    public string? SecurityQuestion3 { get; set; }
    public string? SecurityAnswer3 { get; set; }
    public string? RecoveryEmail { get; set; }
    public string? RecoveryPhone { get; set; }
    public string? LoginUrl { get; set; }
    public string? SupportUrl { get; set; }
    public string? AdminConsoleUrl { get; set; }
    public DateTime? PasswordLastChanged { get; set; }
    public bool RequiresPasswordChange { get; set; }
    public DateTime? LastUsed { get; set; }
    public int UsageCount { get; set; }
    public string? CompanyName { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public string? Notes { get; set; }
}
