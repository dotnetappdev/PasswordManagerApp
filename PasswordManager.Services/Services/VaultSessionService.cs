using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PasswordManager.Crypto.Interfaces;
using PasswordManager.Services.Interfaces;
using System.Collections.Concurrent;

namespace PasswordManager.Services.Services;

/// <summary>
/// Service for managing vault sessions in the API
/// </summary>
public class VaultSessionService : IVaultSessionService
{
    private readonly ConcurrentDictionary<string, (string userId, byte[]? masterKey, bool unlocked)> _sessions = new();
    private readonly IPasswordCryptoService _passwordCryptoService;
    private readonly ILogger<VaultSessionService> _logger;

    public VaultSessionService(
        IPasswordCryptoService passwordCryptoService,
        ILogger<VaultSessionService> logger)
    {
        _passwordCryptoService = passwordCryptoService ?? throw new ArgumentNullException(nameof(passwordCryptoService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string InitializeSession(string userId, byte[] masterKey)
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        if (masterKey == null || masterKey.Length == 0)
            throw new ArgumentException("Master key cannot be null or empty", nameof(masterKey));
        var sessionId = Guid.NewGuid().ToString();
        _sessions[sessionId] = (userId, masterKey, true);
        _logger.LogInformation("Session initialized for user {UserId}", userId);
        return sessionId;
    }

    public string? GetSessionUserId(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            return null;
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            return session.userId;
        }
        return null;
    }

    public void ClearSession(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            return;
        if (_sessions.TryRemove(sessionId, out var session))
        {
            if (session.masterKey != null)
                Array.Clear(session.masterKey, 0, session.masterKey.Length);
            _logger.LogInformation("Session cleared for user {UserId}", session.userId);
        }
    }

    public string EncryptPassword(string password, string sessionId)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));
        if (string.IsNullOrEmpty(sessionId))
            throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));
        if (!_sessions.TryGetValue(sessionId, out var session))
            throw new InvalidOperationException("Invalid or expired session");
        if (!session.unlocked || session.masterKey == null)
            throw new InvalidOperationException("Vault is locked");
        var encryptedData = _passwordCryptoService.EncryptPasswordWithKey(password, session.masterKey);
        return $"{encryptedData.EncryptedPassword}|{encryptedData.Nonce}|{encryptedData.AuthenticationTag}";
    }

    public string DecryptPassword(string encryptedPassword, string sessionId)
    {
        if (string.IsNullOrEmpty(encryptedPassword))
            throw new ArgumentException("Encrypted password cannot be null or empty", nameof(encryptedPassword));
        if (string.IsNullOrEmpty(sessionId))
            throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));
        if (!_sessions.TryGetValue(sessionId, out var session))
            throw new InvalidOperationException("Invalid or expired session");
        if (!session.unlocked || session.masterKey == null)
            throw new InvalidOperationException("Vault is locked");
        var parts = encryptedPassword.Split('|');
        if (parts.Length != 3)
            throw new FormatException("Invalid encrypted password format");
        var encryptedData = new EncryptedPasswordData
        {
            EncryptedPassword = parts[0],
            Nonce = parts[1],
            AuthenticationTag = parts[2]
        };
        return _passwordCryptoService.DecryptPasswordWithKey(encryptedData, session.masterKey);
    }

    public bool IsVaultUnlocked(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            return false;
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            return session.unlocked;
        }
        return false;
    }

    public void LockVault(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            return;
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            _sessions[sessionId] = (session.userId, null, false);
            _logger.LogInformation("Vault locked for session {SessionId}", sessionId);
        }
    }

    public bool UnlockVault(string sessionId, byte[] masterKey)
    {
        if (string.IsNullOrEmpty(sessionId) || masterKey == null || masterKey.Length == 0)
            return false;
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            _sessions[sessionId] = (session.userId, masterKey, true);
            _logger.LogInformation("Vault unlocked for session {SessionId}", sessionId);
            return true;
        }
        return false;
    }

    public byte[]? GetMasterKey(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            return null;
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            return session.unlocked ? session.masterKey : null;
        }
        return null;
    }
}
