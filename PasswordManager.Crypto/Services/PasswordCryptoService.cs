using System.Text;
using PasswordManager.Crypto.Interfaces;

namespace PasswordManager.Crypto.Services;

/// <summary>
/// Implementation of password-specific cryptographic operations following Bitwarden's approach
/// </summary>
public class PasswordCryptoService : IPasswordCryptoService
{
    private readonly ICryptographyService _cryptographyService;
    private const int MasterKeyIterations = 600000; // OWASP 2024 recommendation for PBKDF2 iterations
    private const int AuthHashIterations = 600000; // OWASP 2024 recommendation for PBKDF2 iterations
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
    /// Following Bitwarden's approach: PBKDF2(masterKey + masterPassword, 1 iteration)
    /// This maintains OWASP security while following industry standard pattern
    /// </summary>
    public string CreateMasterPasswordHash(string masterPassword, byte[] userSalt, int iterations = AuthHashIterations)
    {
        if (string.IsNullOrEmpty(masterPassword))
            throw new ArgumentException("Master password cannot be null or empty", nameof(masterPassword));
        
        if (userSalt == null || userSalt.Length == 0)
            throw new ArgumentException("User salt cannot be null or empty", nameof(userSalt));

        // Step 1: Derive the master key using OWASP recommended 600,000 iterations
        var masterKey = _cryptographyService.DeriveKey(masterPassword, userSalt, MasterKeyIterations, MasterKeyLength);

        try
        {
            // Step 2: Create authentication hash following Bitwarden's approach
            // Hash the master key with the master password using minimal iterations for performance
            // This creates a one-way hash that can't be reversed to get the encryption key
            var masterKeyBytes = Encoding.UTF8.GetBytes(Convert.ToBase64String(masterKey));
            var masterPasswordBytes = Encoding.UTF8.GetBytes(masterPassword);
            
            // Combine master key + master password as salt for authentication hash
            var authSalt = new byte[masterKeyBytes.Length + masterPasswordBytes.Length];
            Buffer.BlockCopy(masterKeyBytes, 0, authSalt, 0, masterKeyBytes.Length);
            Buffer.BlockCopy(masterPasswordBytes, 0, authSalt, masterKeyBytes.Length, masterPasswordBytes.Length);
            
            try
            {
                // Use single iteration for authentication hash (Bitwarden approach)
                // The security comes from the 600,000 iterations used to derive the master key
                return _cryptographyService.HashPassword(Convert.ToBase64String(masterKey), authSalt, 1);
            }
            finally
            {
                // Clear sensitive data from memory
                Array.Clear(authSalt, 0, authSalt.Length);
                Array.Clear(masterKeyBytes, 0, masterKeyBytes.Length);
                Array.Clear(masterPasswordBytes, 0, masterPasswordBytes.Length);
            }
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

    /// <summary>
    /// Derives the master key from master password and user salt
    /// This key should be cached in memory during the user session for performance
    /// Following Bitwarden's approach: derive once, cache for session, use for all operations
    /// </summary>
    public byte[] DeriveMasterKey(string masterPassword, byte[] userSalt)
    {
        if (string.IsNullOrEmpty(masterPassword))
            throw new ArgumentException("Master password cannot be null or empty", nameof(masterPassword));
        
        if (userSalt == null || userSalt.Length == 0)
            throw new ArgumentException("User salt cannot be null or empty", nameof(userSalt));

        // Derive master key using OWASP recommended 600,000 iterations
        // This is the expensive operation that should be done once per session
        return _cryptographyService.DeriveKey(masterPassword, userSalt, MasterKeyIterations, MasterKeyLength);
    }

    /// <summary>
    /// Encrypts a password using a pre-derived master key
    /// This version is more efficient for multiple operations as it skips key derivation
    /// </summary>
    public EncryptedPasswordData EncryptPasswordWithKey(string password, byte[] masterKey)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));
        
        if (masterKey == null || masterKey.Length != MasterKeyLength)
            throw new ArgumentException($"Master key must be exactly {MasterKeyLength} bytes", nameof(masterKey));

        // Encrypt the password using AES-256-GCM with the provided master key
        var encryptedData = _cryptographyService.EncryptAes256Gcm(password, masterKey);

        return new EncryptedPasswordData
        {
            EncryptedPassword = Convert.ToBase64String(encryptedData.Ciphertext),
            Nonce = Convert.ToBase64String(encryptedData.Nonce),
            AuthenticationTag = Convert.ToBase64String(encryptedData.AuthenticationTag)
        };
    }

    /// <summary>
    /// Decrypts a password using a pre-derived master key
    /// This version is more efficient for multiple operations as it skips key derivation
    /// </summary>
    public string DecryptPasswordWithKey(EncryptedPasswordData encryptedPasswordData, byte[] masterKey)
    {
        if (encryptedPasswordData == null)
            throw new ArgumentNullException(nameof(encryptedPasswordData));
        
        if (masterKey == null || masterKey.Length != MasterKeyLength)
            throw new ArgumentException($"Master key must be exactly {MasterKeyLength} bytes", nameof(masterKey));

        // Reconstruct EncryptedData object
        var encryptedData = new EncryptedData
        {
            Ciphertext = Convert.FromBase64String(encryptedPasswordData.EncryptedPassword),
            Nonce = Convert.FromBase64String(encryptedPasswordData.Nonce),
            AuthenticationTag = Convert.FromBase64String(encryptedPasswordData.AuthenticationTag)
        };

        // Decrypt the password using AES-256-GCM with the provided master key
        return _cryptographyService.DecryptAes256Gcm(encryptedData, masterKey);
    }
}
