using PasswordManager.Crypto.Interfaces;

namespace PasswordManager.API.Interfaces;

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
}
