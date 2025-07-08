using PasswordManager.API.Interfaces;
using System.Collections.Concurrent;

namespace PasswordManager.API.Services;

public class VaultSessionService : IVaultSessionService
{
    private readonly ConcurrentDictionary<string, (string userId, byte[] masterKey)> _sessions = new();
    private readonly ILogger<VaultSessionService> _logger;

    public VaultSessionService(ILogger<VaultSessionService> logger)
    {
        _logger = logger;
    }

    public string InitializeSession(string userId, byte[] masterKey)
    {
        var sessionId = Guid.NewGuid().ToString();
        _sessions[sessionId] = (userId, masterKey);
        _logger.LogInformation("Session initialized for user {UserId}", userId);
        return sessionId;
    }

    public string? GetSessionUserId(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            return session.userId;
        }
        return null;
    }

    public void ClearSession(string sessionId)
    {
        if (_sessions.TryRemove(sessionId, out var session))
        {
            // Securely clear the master key from memory
            Array.Clear(session.masterKey, 0, session.masterKey.Length);
            _logger.LogInformation("Session cleared for user {UserId}", session.userId);
        }
    }
}
