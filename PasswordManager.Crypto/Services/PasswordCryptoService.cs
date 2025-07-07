using System.Text;
using PasswordManager.Crypto.Interfaces;

namespace PasswordManager.Crypto.Services;

/// <summary>
/// Implementation of password-specific cryptographic operations following Bitwarden's approach
/// </summary>
public class PasswordCryptoService : IPasswordCryptoService
{
    private readonly ICryptographyService _cryptographyService;
    private const int MasterKeyIterations = 100000; // For deriving encryption key from master password
    private const int AuthHashIterations = 100000; // For master password authentication hash
    private const int UserSaltLength = 32;
    private const int MasterKeyLength = 32; // 256 bits for AES

    public PasswordCryptoService(ICryptographyService cryptographyService)
    {
        _cryptographyService = cryptographyService ?? throw new ArgumentNullException(nameof(cryptographyService));
    }

    /// <summary>
    /// Encrypts a password using the master key derived from user's master password
    /// Similar to Bitwarden's approach: master password + salt -> master key -> encrypt password
    /// </summary>
    public EncryptedPasswordData EncryptPassword(string password, string masterPassword, byte[] userSalt)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));
        
        if (string.IsNullOrEmpty(masterPassword))
            throw new ArgumentException("Master password cannot be null or empty", nameof(masterPassword));
        
        if (userSalt == null || userSalt.Length == 0)
            throw new ArgumentException("User salt cannot be null or empty", nameof(userSalt));

        // Derive master key from master password and user salt
        var masterKey = _cryptographyService.DeriveKey(masterPassword, userSalt, MasterKeyIterations, MasterKeyLength);

        try
        {
            // Encrypt the password using AES-256-GCM
            var encryptedData = _cryptographyService.EncryptAes256Gcm(password, masterKey);

            return new EncryptedPasswordData
            {
                EncryptedPassword = Convert.ToBase64String(encryptedData.Ciphertext),
                Nonce = Convert.ToBase64String(encryptedData.Nonce),
                AuthenticationTag = Convert.ToBase64String(encryptedData.AuthenticationTag)
            };
        }
        finally
        {
            // Clear master key from memory
            Array.Clear(masterKey, 0, masterKey.Length);
        }
    }

    /// <summary>
    /// Decrypts a password using the master key derived from user's master password
    /// </summary>
    public string DecryptPassword(EncryptedPasswordData encryptedPasswordData, string masterPassword, byte[] userSalt)
    {
        if (encryptedPasswordData == null)
            throw new ArgumentNullException(nameof(encryptedPasswordData));
        
        if (string.IsNullOrEmpty(masterPassword))
            throw new ArgumentException("Master password cannot be null or empty", nameof(masterPassword));
        
        if (userSalt == null || userSalt.Length == 0)
            throw new ArgumentException("User salt cannot be null or empty", nameof(userSalt));

        // Derive master key from master password and user salt
        var masterKey = _cryptographyService.DeriveKey(masterPassword, userSalt, MasterKeyIterations, MasterKeyLength);

        try
        {
            // Reconstruct EncryptedData object
            var encryptedData = new EncryptedData
            {
                Ciphertext = Convert.FromBase64String(encryptedPasswordData.EncryptedPassword),
                Nonce = Convert.FromBase64String(encryptedPasswordData.Nonce),
                AuthenticationTag = Convert.FromBase64String(encryptedPasswordData.AuthenticationTag)
            };

            // Decrypt the password using AES-256-GCM
            return _cryptographyService.DecryptAes256Gcm(encryptedData, masterKey);
        }
        finally
        {
            // Clear master key from memory
            Array.Clear(masterKey, 0, masterKey.Length);
        }
    }

    /// <summary>
    /// Creates a master password hash for authentication (stored in database)
    /// This is separate from the encryption key - it's only used for verifying the master password
    /// </summary>
    public string CreateMasterPasswordHash(string masterPassword, byte[] userSalt, int iterations = AuthHashIterations)
    {
        if (string.IsNullOrEmpty(masterPassword))
            throw new ArgumentException("Master password cannot be null or empty", nameof(masterPassword));
        
        if (userSalt == null || userSalt.Length == 0)
            throw new ArgumentException("User salt cannot be null or empty", nameof(userSalt));

        // First derive the master key (this is what we would use for encryption)
        var masterKey = _cryptographyService.DeriveKey(masterPassword, userSalt, MasterKeyIterations, MasterKeyLength);

        try
        {
            // Then hash the master key again for authentication
            // This creates a one-way hash that can't be reversed to get the encryption key
            return _cryptographyService.HashPassword(Convert.ToBase64String(masterKey), userSalt, iterations);
        }
        finally
        {
            // Clear master key from memory
            Array.Clear(masterKey, 0, masterKey.Length);
        }
    }

    /// <summary>
    /// Verifies a master password against stored hash
    /// </summary>
    public bool VerifyMasterPassword(string masterPassword, string storedHash, byte[] userSalt, int iterations = AuthHashIterations)
    {
        if (string.IsNullOrEmpty(masterPassword) || string.IsNullOrEmpty(storedHash))
            return false;

        if (userSalt == null || userSalt.Length == 0)
            return false;

        try
        {
            var computedHash = CreateMasterPasswordHash(masterPassword, userSalt, iterations);
            return computedHash.Equals(storedHash, StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Generates a new user salt
    /// </summary>
    public byte[] GenerateUserSalt()
    {
        return _cryptographyService.GenerateSalt(UserSaltLength);
    }
}
