using System.Security.Cryptography;
using System.Text;
using VaultGuard.Crypto.Interfaces;

namespace VaultGuard.Crypto.Services;

/// <summary>
/// Implementation of cryptographic operations using PBKDF2 and AES-256-GCM
/// </summary>
public class CryptographyService : ICryptographyService
{
    private const int DefaultSaltLength = 32;
    private const int AesKeyLength = 32; // 256 bits
    private const int NonceLength = 12; // 96 bits for GCM
    private const int AuthTagLength = 16; // 128 bits for GCM

    // Recommended Argon2id parameters for new vaults (OWASP: >=19 MiB, >=2 iters). 64 MiB is a strong
    // desktop/server default; tune down for constrained mobile targets if needed.
    public const int Argon2DefaultMemoryKib = 65536;      // 64 MiB
    public const int Argon2DefaultIterations = 3;
    public const int Argon2DefaultParallelism = 4;

    /// <summary>
    /// Derives a key from a password using PBKDF2 with specified salt and iterations
    /// </summary>
    public byte[] DeriveKey(string password, byte[] salt, int iterations, int keyLength)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));
        
        if (salt == null || salt.Length == 0)
            throw new ArgumentException("Salt cannot be null or empty", nameof(salt));
        
        if (iterations < 1)
            throw new ArgumentException("Iterations must be greater than 0", nameof(iterations));
        
        if (keyLength < 1)
            throw new ArgumentException("Key length must be greater than 0", nameof(keyLength));

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(keyLength);
    }

    /// <summary>
    /// Derives a key using Argon2id (memory-hard). See <see cref="ICryptographyService.DeriveKeyArgon2id"/>.
    /// </summary>
    public byte[] DeriveKeyArgon2id(string password, byte[] salt, int memorySizeKib, int iterations, int degreeOfParallelism, int keyLength)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));
        if (salt == null || salt.Length == 0)
            throw new ArgumentException("Salt cannot be null or empty", nameof(salt));
        if (memorySizeKib < 8192)
            throw new ArgumentException("Argon2id memory must be at least 8192 KiB", nameof(memorySizeKib));
        if (iterations < 1)
            throw new ArgumentException("Iterations must be greater than 0", nameof(iterations));
        if (degreeOfParallelism < 1)
            throw new ArgumentException("Degree of parallelism must be greater than 0", nameof(degreeOfParallelism));
        if (keyLength < 1)
            throw new ArgumentException("Key length must be greater than 0", nameof(keyLength));

        var passwordBytes = Encoding.UTF8.GetBytes(password);
        try
        {
            using var argon2 = new Konscious.Security.Cryptography.Argon2id(passwordBytes)
            {
                Salt = salt,
                MemorySize = memorySizeKib,
                Iterations = iterations,
                DegreeOfParallelism = degreeOfParallelism,
            };
            return argon2.GetBytes(keyLength);
        }
        finally
        {
            Array.Clear(passwordBytes, 0, passwordBytes.Length);
        }
    }

    /// <summary>
    /// Derives an independent purpose-specific sub-key via HKDF-SHA256. See
    /// <see cref="ICryptographyService.DeriveSubKey"/>.
    /// </summary>
    public byte[] DeriveSubKey(byte[] masterKey, string purpose, int keyLength = 32)
    {
        if (masterKey == null || masterKey.Length == 0)
            throw new ArgumentException("Master key cannot be null or empty", nameof(masterKey));
        if (string.IsNullOrEmpty(purpose))
            throw new ArgumentException("Purpose cannot be null or empty", nameof(purpose));
        if (keyLength < 1)
            throw new ArgumentException("Key length must be greater than 0", nameof(keyLength));

        // HKDF-Expand with a domain-separation label as the 'info' parameter yields keys that are
        // cryptographically independent per purpose.
        return HKDF.DeriveKey(
            HashAlgorithmName.SHA256,
            ikm: masterKey,
            outputLength: keyLength,
            salt: null,
            info: Encoding.UTF8.GetBytes($"VaultGuard|{purpose}"));
    }

    /// <summary>
    /// Generates a cryptographically secure random salt
    /// </summary>
    public byte[] GenerateSalt(int length = DefaultSaltLength)
    {
        if (length < 1)
            throw new ArgumentException("Salt length must be greater than 0", nameof(length));

        var salt = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        return salt;
    }

    /// <summary>
    /// Encrypts data using AES-256-GCM with provided key
    /// </summary>
    public EncryptedData EncryptAes256Gcm(string plaintext, byte[] key)
    {
        if (string.IsNullOrEmpty(plaintext))
            throw new ArgumentException("Plaintext cannot be null or empty", nameof(plaintext));
        
        if (key == null || key.Length != AesKeyLength)
            throw new ArgumentException($"Key must be exactly {AesKeyLength} bytes", nameof(key));

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = new byte[NonceLength];
        var ciphertext = new byte[plaintextBytes.Length];
        var authTag = new byte[AuthTagLength];

        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(nonce);

        using var aes = new AesGcm(key, AuthTagLength);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, authTag);

        return new EncryptedData
        {
            Ciphertext = ciphertext,
            Nonce = nonce,
            AuthenticationTag = authTag
        };
    }

    /// <summary>
    /// Decrypts data using AES-256-GCM with provided key
    /// </summary>
    public string DecryptAes256Gcm(EncryptedData encryptedData, byte[] key)
    {
        if (encryptedData == null)
            throw new ArgumentNullException(nameof(encryptedData));
        
        if (key == null || key.Length != AesKeyLength)
            throw new ArgumentException($"Key must be exactly {AesKeyLength} bytes", nameof(key));

        if (encryptedData.Nonce == null || encryptedData.Nonce.Length != NonceLength)
            throw new ArgumentException($"Nonce must be exactly {NonceLength} bytes");

        if (encryptedData.AuthenticationTag == null || encryptedData.AuthenticationTag.Length != AuthTagLength)
            throw new ArgumentException($"Authentication tag must be exactly {AuthTagLength} bytes");

        var plaintext = new byte[encryptedData.Ciphertext.Length];

        using var aes = new AesGcm(key, AuthTagLength);
        aes.Decrypt(encryptedData.Nonce, encryptedData.Ciphertext, encryptedData.AuthenticationTag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }

    /// <summary>
    /// Hashes a password using PBKDF2 for secure storage
    /// </summary>
    public string HashPassword(string password, byte[] salt, int iterations)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));
        
        if (salt == null || salt.Length == 0)
            throw new ArgumentException("Salt cannot be null or empty", nameof(salt));
        
        if (iterations < 1)
            throw new ArgumentException("Iterations must be greater than 0", nameof(iterations));

        var hash = DeriveKey(password, salt, iterations, AesKeyLength);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Verifies a password against its hash
    /// </summary>
    public bool VerifyPassword(string password, string hash, byte[] salt, int iterations)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
            return false;

        try
        {
            var computedHash = HashPassword(password, salt, iterations);
            // Constant-time comparison to avoid leaking hash bytes via timing.
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedHash), Encoding.UTF8.GetBytes(hash));
        }
        catch
        {
            return false;
        }
    }
}
