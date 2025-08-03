using PasswordManager.Crypto.Interfaces;

namespace PasswordManager.Services.Interfaces;

/// <summary>
/// Interface for vault session management
/// Handles session creation, user identification, and session clearing
/// </summary>
public interface IVaultSessionService
{
    /// <summary>
    /// Initialize a session for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="masterKey">The master key derived from the user's password</param>
    /// <returns>A session ID</returns>
    string InitializeSession(string userId, byte[] masterKey);

    /// <summary>
    /// Get the user ID associated with a session
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <returns>The user ID or null if the session is invalid</returns>
    string? GetSessionUserId(string sessionId);

    /// <summary>
    /// Clear a session
    /// </summary>
    /// <param name="sessionId">The session ID to clear</param>
    void ClearSession(string sessionId);

    /// <summary>
    /// Encrypt password using the session's master key
    /// </summary>
    /// <param name="password">Password to encrypt</param>
    /// <param name="sessionId">Session ID</param>
    /// <returns>Encrypted password data</returns>
    string EncryptPassword(string password, string sessionId);

    /// <summary>
    /// Decrypt password using the session's master key
    /// </summary>
    /// <param name="encryptedPassword">Encrypted password</param>
    /// <param name="sessionId">Session ID</param>
    /// <returns>Decrypted password</returns>
    string DecryptPassword(string encryptedPassword, string sessionId);

    // Vault lock/unlock methods
    bool IsVaultUnlocked(string sessionId);
    void LockVault(string sessionId);
    bool UnlockVault(string sessionId, byte[] masterKey);

    /// <summary>
    /// Get the master key for a session (for advanced operations)
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>Master key or null if session is invalid or locked</returns>
    byte[]? GetMasterKey(string sessionId);
}
