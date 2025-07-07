using PasswordManager.Crypto.Interfaces;

namespace PasswordManager.Crypto.Interfaces;

/// <summary>
/// Interface for vault session management following Bitwarden's approach
/// Handles the complete flow: master password input → key derivation → vault unlocking → password operations
/// </summary>
public interface IVaultSessionService : IDisposable
{
    /// <summary>
    /// Step 1: Master Password Input + Key Derivation
    /// Unlocks the vault by verifying master password and caching the derived key
    /// This is the expensive operation (600,000 PBKDF2 iterations) done once per session
    /// </summary>
    /// <param name="masterPassword">User's master password</param>
    /// <param name="userSalt">User-specific salt</param>
    /// <param name="storedHash">Stored authentication hash</param>
    /// <param name="iterations">PBKDF2 iterations used for the stored hash</param>
    /// <returns>True if vault was successfully unlocked</returns>
    bool UnlockVault(string masterPassword, byte[] userSalt, string storedHash, int iterations = 600000);

    /// <summary>
    /// Step 2: Encrypt Vault Data
    /// Encrypts password data using the cached master key (fast operation)
    /// </summary>
    /// <param name="password">Password to encrypt</param>
    /// <returns>Encrypted password data</returns>
    EncryptedPasswordData? EncryptPassword(string password);

    /// <summary>
    /// Step 3: Decrypt Vault Data
    /// Decrypts password data using the cached master key (fast operation)
    /// </summary>
    /// <param name="encryptedPasswordData">Encrypted password data</param>
    /// <returns>Decrypted password</returns>
    string? DecryptPassword(EncryptedPasswordData encryptedPasswordData);

    /// <summary>
    /// Step 4: Reveal Password
    /// Returns the password for display (instantaneous - may use cached decrypted data)
    /// This simulates the "reveal" button behavior in password managers
    /// </summary>
    /// <param name="encryptedPasswordData">Encrypted password data</param>
    /// <returns>Plain text password for display</returns>
    string? RevealPassword(EncryptedPasswordData encryptedPasswordData);

    /// <summary>
    /// Locks the vault by clearing the cached master key from memory
    /// </summary>
    void LockVault();

    /// <summary>
    /// Checks if the vault is currently unlocked (master key is available)
    /// </summary>
    bool IsVaultUnlocked { get; }
}
