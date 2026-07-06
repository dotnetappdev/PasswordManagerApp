namespace VaultGuard.Crypto.Interfaces;

/// <summary>
/// Interface for cryptographic operations including PBKDF2 key derivation and AES-256-GCM encryption
/// </summary>
public interface ICryptographyService
{
    /// <summary>
    /// Derives a key from a password using PBKDF2 with specified salt and iterations
    /// </summary>
    /// <param name="password">The password to derive key from</param>
    /// <param name="salt">The salt bytes</param>
    /// <param name="iterations">Number of PBKDF2 iterations (OWASP 2024 recommendation: 600,000+)</param>
    /// <param name="keyLength">Length of derived key in bytes</param>
    /// <returns>Derived key bytes</returns>
    byte[] DeriveKey(string password, byte[] salt, int iterations, int keyLength);

    /// <summary>
    /// Derives a key from a password using Argon2id — a memory-hard KDF that resists GPU/ASIC cracking
    /// far better than PBKDF2. Recommended for new vaults.
    /// </summary>
    /// <param name="password">The password to derive the key from.</param>
    /// <param name="salt">Random per-user salt (16+ bytes).</param>
    /// <param name="memorySizeKib">Memory cost in KiB (e.g. 65536 = 64 MB; OWASP min 19 MB).</param>
    /// <param name="iterations">Time cost / passes over memory (OWASP min 2).</param>
    /// <param name="degreeOfParallelism">Number of lanes/threads.</param>
    /// <param name="keyLength">Length of the derived key in bytes.</param>
    byte[] DeriveKeyArgon2id(string password, byte[] salt, int memorySizeKib, int iterations, int degreeOfParallelism, int keyLength);

    /// <summary>
    /// Derives a purpose-specific sub-key from a high-entropy master key using HKDF-SHA256, so a single
    /// master key yields cryptographically independent keys (encryption, authentication, backup, …).
    /// A leak of one sub-key does not expose the others (cryptographic key separation).
    /// </summary>
    /// <param name="masterKey">High-entropy input key material (e.g. the derived master key).</param>
    /// <param name="purpose">Domain-separation label, e.g. "encryption" or "backup".</param>
    /// <param name="keyLength">Length of the derived sub-key in bytes.</param>
    byte[] DeriveSubKey(byte[] masterKey, string purpose, int keyLength = 32);

    /// <summary>
    /// Generates a cryptographically secure random salt
    /// </summary>
    /// <param name="length">Length of salt in bytes</param>
    /// <returns>Random salt bytes</returns>
    byte[] GenerateSalt(int length = 32);

    /// <summary>
    /// Encrypts data using AES-256-GCM with provided key
    /// </summary>
    /// <param name="plaintext">Data to encrypt</param>
    /// <param name="key">256-bit encryption key</param>
    /// <returns>Encrypted data with nonce and authentication tag</returns>
    EncryptedData EncryptAes256Gcm(string plaintext, byte[] key);

    /// <summary>
    /// Decrypts data using AES-256-GCM with provided key
    /// </summary>
    /// <param name="encryptedData">Encrypted data with nonce and authentication tag</param>
    /// <param name="key">256-bit decryption key</param>
    /// <returns>Decrypted plaintext</returns>
    string DecryptAes256Gcm(EncryptedData encryptedData, byte[] key);

    /// <summary>
    /// Hashes a password using PBKDF2 for secure storage
    /// </summary>
    /// <param name="password">Password to hash</param>
    /// <param name="salt">Salt for hashing</param>
    /// <param name="iterations">Number of PBKDF2 iterations</param>
    /// <returns>Hashed password</returns>
    string HashPassword(string password, byte[] salt, int iterations);

    /// <summary>
    /// Verifies a password against its hash
    /// </summary>
    /// <param name="password">Password to verify</param>
    /// <param name="hash">Stored password hash</param>
    /// <param name="salt">Salt used for hashing</param>
    /// <param name="iterations">Number of PBKDF2 iterations used</param>
    /// <returns>True if password matches hash</returns>
    bool VerifyPassword(string password, string hash, byte[] salt, int iterations);
}

/// <summary>
/// Represents encrypted data with all necessary components for decryption
/// </summary>
public class EncryptedData
{
    public byte[] Ciphertext { get; set; } = Array.Empty<byte>();
    public byte[] Nonce { get; set; } = Array.Empty<byte>();
    public byte[] AuthenticationTag { get; set; } = Array.Empty<byte>();
}
