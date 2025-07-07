using PasswordManager.Crypto.Interfaces;

namespace PasswordManager.Crypto.Services;

/// <summary>
/// Session management service following Bitwarden's approach
/// Handles master key derivation, caching, and vault operations
/// </summary>
public class VaultSessionService : IVaultSessionService
{
    private readonly IPasswordCryptoService _passwordCryptoService;
    private byte[]? _cachedMasterKey;
    private readonly object _lockObject = new object();
    private bool _disposed = false;

    public VaultSessionService(IPasswordCryptoService passwordCryptoService)
    {
        _passwordCryptoService = passwordCryptoService ?? throw new ArgumentNullException(nameof(passwordCryptoService));
    }

    /// <summary>
    /// Step 1: Master Password Input + Key Derivation
    /// Derives and caches the master key for the session (expensive operation - done once)
    /// </summary>
    public bool UnlockVault(string masterPassword, byte[] userSalt, string storedHash, int iterations = 600000)
    {
        if (string.IsNullOrEmpty(masterPassword) || userSalt == null || string.IsNullOrEmpty(storedHash))
            return false;

        try
        {
            // Verify the master password first
            if (!_passwordCryptoService.VerifyMasterPassword(masterPassword, storedHash, userSalt, iterations))
                return false;

            lock (_lockObject)
            {
                // Clear any existing cached key
                ClearCachedMasterKey();

                // Derive and cache the master key for this session
                // This is the expensive PBKDF2 operation with 600,000 iterations
                _cachedMasterKey = _passwordCryptoService.DeriveMasterKey(masterPassword, userSalt);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Step 2: Encrypt Vault Data
    /// Uses the cached master key to encrypt password data
    /// </summary>
    public EncryptedPasswordData? EncryptPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return null;

        lock (_lockObject)
        {
            if (_cachedMasterKey == null)
                throw new InvalidOperationException("Vault is locked. Call UnlockVault first.");

            return _passwordCryptoService.EncryptPasswordWithKey(password, _cachedMasterKey);
        }
    }

    /// <summary>
    /// Step 3: Decrypt Vault Data 
    /// Uses the cached master key to decrypt password data (fast operation)
    /// </summary>
    public string? DecryptPassword(EncryptedPasswordData encryptedPasswordData)
    {
        if (encryptedPasswordData == null)
            return null;

        lock (_lockObject)
        {
            if (_cachedMasterKey == null)
                throw new InvalidOperationException("Vault is locked. Call UnlockVault first.");

            return _passwordCryptoService.DecryptPasswordWithKey(encryptedPasswordData, _cachedMasterKey);
        }
    }

    /// <summary>
    /// Step 4: Reveal Password
    /// Simply returns the already decrypted password (instantaneous - already in memory)
    /// This simulates the "reveal" button behavior in password managers
    /// </summary>
    public string? RevealPassword(EncryptedPasswordData encryptedPasswordData)
    {
        // In a real implementation, you might cache decrypted passwords in memory
        // For security, we decrypt on-demand, but this could be optimized
        return DecryptPassword(encryptedPasswordData);
    }

    /// <summary>
    /// Locks the vault by clearing the cached master key from memory
    /// Following security best practices for memory cleanup
    /// </summary>
    public void LockVault()
    {
        lock (_lockObject)
        {
            ClearCachedMasterKey();
        }
    }

    /// <summary>
    /// Checks if the vault is currently unlocked (master key is cached)
    /// </summary>
    public bool IsVaultUnlocked
    {
        get
        {
            lock (_lockObject)
            {
                return _cachedMasterKey != null;
            }
        }
    }

    /// <summary>
    /// Securely clears the cached master key from memory
    /// </summary>
    private void ClearCachedMasterKey()
    {
        if (_cachedMasterKey != null)
        {
            Array.Clear(_cachedMasterKey, 0, _cachedMasterKey.Length);
            _cachedMasterKey = null;
        }
    }

    /// <summary>
    /// Disposes the service and clears sensitive data from memory
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            lock (_lockObject)
            {
                ClearCachedMasterKey();
            }
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Finalizer to ensure sensitive data is cleared even if Dispose is not called
    /// </summary>
    ~VaultSessionService()
    {
        Dispose();
    }
}
