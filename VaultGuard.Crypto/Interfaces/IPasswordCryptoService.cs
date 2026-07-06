namespace VaultGuard.Crypto.Interfaces;

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
    string CreateMasterPasswordHash(string masterPassword, byte[] userSalt, int iterations = 600000);

    /// <summary>
    /// Creates an authentication hash using the memory-hard Argon2id KDF, encoded in a self-describing
    /// PHC string (<c>$argon2id$v=19$m=..,t=..,p=..$salt$hash</c>). <see cref="VerifyMasterPassword"/>
    /// auto-detects this format, so it interoperates with existing PBKDF2 hashes and works across every
    /// client that shares this service (WPF, Web, API, MAUI) with no per-client changes.
    /// </summary>
    string CreateArgon2idMasterPasswordHash(string masterPassword, byte[] userSalt,
        int memoryKib = 65536, int iterations = 3, int parallelism = 4);

    /// <summary>
    /// Creates an authentication hash using pre-derived master key and master password
    /// </summary>
    /// <param name="masterKey">Pre-derived master key</param>
    /// <param name="masterPassword">User's master password</param>
    /// <returns>Authentication hash for user verification</returns>
    string CreateAuthHash(byte[] masterKey, string masterPassword);

    /// <summary>
    /// Verifies a master password against stored hash
    /// </summary>
    /// <param name="masterPassword">Password to verify</param>
    /// <param name="storedHash">Stored master password hash</param>
    /// <param name="userSalt">User-specific salt</param>
    /// <param name="iterations">PBKDF2 iterations used for the hash</param>
    /// <returns>True if password is correct</returns>
    bool VerifyMasterPassword(string masterPassword, string storedHash, byte[] userSalt, int iterations = 600000);

    /// <summary>
    /// Generates a new user salt
    /// </summary>
    /// <returns>New cryptographically secure salt</returns>
    byte[] GenerateUserSalt();

    /// <summary>
    /// Derives the master key from master password and user salt
    /// This key should be cached in memory during the user session for performance
    /// Following Bitwarden's approach: derive once, cache for session, use for all operations
    /// </summary>
    /// <param name="masterPassword">User's master password</param>
    /// <param name="userSalt">User-specific salt</param>
    /// <returns>Derived master key (should be securely cached for session)</returns>
    byte[] DeriveMasterKey(string masterPassword, byte[] userSalt);

    /// <summary>
    /// Encrypts a password using a pre-derived master key
    /// This version is more efficient for multiple operations as it skips key derivation
    /// </summary>
    /// <param name="password">Password to encrypt</param>
    /// <param name="masterKey">Pre-derived master key</param>
    /// <returns>Encrypted password data</returns>
    EncryptedPasswordData EncryptPasswordWithKey(string password, byte[] masterKey);

    /// <summary>
    /// Decrypts a password using a pre-derived master key
    /// This version is more efficient for multiple operations as it skips key derivation
    /// </summary>
    /// <param name="encryptedPasswordData">Encrypted password data</param>
    /// <param name="masterKey">Pre-derived master key</param>
    /// <returns>Decrypted password</returns>
    string DecryptPasswordWithKey(EncryptedPasswordData encryptedPasswordData, byte[] masterKey);

    /// <summary>
    /// Creates a master key identifier for user lookup during master key login
    /// This creates a searchable hash that allows finding users by their master key
    /// </summary>
    /// <param name="masterPassword">User's master password</param>
    /// <param name="userSalt">User-specific salt</param>
    /// <returns>Identifier hash for database lookup</returns>
    string CreateMasterKeyIdentifier(string masterPassword, byte[] userSalt);

    /// <summary>
    /// Verifies if a master password matches a stored master key identifier
    /// </summary>
    /// <param name="masterPassword">Password to verify</param>
    /// <param name="userSalt">User-specific salt</param>
    /// <param name="storedIdentifier">Stored master key identifier</param>
    /// <returns>True if master password matches the identifier</returns>
    bool VerifyMasterKeyIdentifier(string masterPassword, byte[] userSalt, string storedIdentifier);
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
