namespace PasswordManager.Crypto.Interfaces;

/// <summary>
/// Interface for password-specific cryptographic operations
/// </summary>
public interface IPasswordCryptoService
{
    /// <summary>
    /// Encrypts a password using the master key derived from user's master password
    /// </summary>
    /// <param name="password">Password to encrypt</param>
    /// <param name="masterPassword">User's master password</param>
    /// <param name="userSalt">User-specific salt</param>
    /// <returns>Encrypted password data</returns>
    EncryptedPasswordData EncryptPassword(string password, string masterPassword, byte[] userSalt);

    /// <summary>
    /// Decrypts a password using the master key derived from user's master password
    /// </summary>
    /// <param name="encryptedPasswordData">Encrypted password data</param>
    /// <param name="masterPassword">User's master password</param>
    /// <param name="userSalt">User-specific salt</param>
    /// <returns>Decrypted password</returns>
    string DecryptPassword(EncryptedPasswordData encryptedPasswordData, string masterPassword, byte[] userSalt);

    /// <summary>
    /// Creates a master key hash for authentication (stored in database)
    /// </summary>
    /// <param name="masterPassword">User's master password</param>
    /// <param name="userSalt">User-specific salt</param>
    /// <param name="iterations">PBKDF2 iterations for the authentication hash</param>
    /// <returns>Hash suitable for storage and authentication</returns>
    string CreateMasterPasswordHash(string masterPassword, byte[] userSalt, int iterations = 100000);

    /// <summary>
    /// Verifies a master password against stored hash
    /// </summary>
    /// <param name="masterPassword">Password to verify</param>
    /// <param name="storedHash">Stored master password hash</param>
    /// <param name="userSalt">User-specific salt</param>
    /// <param name="iterations">PBKDF2 iterations used for the hash</param>
    /// <returns>True if password is correct</returns>
    bool VerifyMasterPassword(string masterPassword, string storedHash, byte[] userSalt, int iterations = 100000);

    /// <summary>
    /// Generates a new user salt
    /// </summary>
    /// <returns>New cryptographically secure salt</returns>
    byte[] GenerateUserSalt();
}

/// <summary>
/// Represents encrypted password data with all components needed for decryption
/// </summary>
public class EncryptedPasswordData
{
    public string EncryptedPassword { get; set; } = string.Empty;
    public string Nonce { get; set; } = string.Empty;
    public string AuthenticationTag { get; set; } = string.Empty;
}
