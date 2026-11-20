using Microsoft.Extensions.Logging;
using VaultGuard.Services.Interfaces;
using VaultGuard.Crypto.Interfaces;
using VaultGuard.Models;
using System.Text.Json;

namespace VaultGuard.Services.Services;

/// <summary>
/// Service for revealing encrypted passwords stored in password items
/// Uses VaultSessionService for secure password decryption
/// </summary>
public class PasswordRevealService : IPasswordRevealService
{
    private readonly IVaultSessionService _vaultSessionService;
    private readonly ILogger<PasswordRevealService> _logger;

    public PasswordRevealService(
        IVaultSessionService vaultSessionService,
        ILogger<PasswordRevealService> logger)
    {
        _vaultSessionService = vaultSessionService;
        _logger = logger;
    }

    public bool CanRevealPasswords(string sessionId) => _vaultSessionService.IsVaultUnlocked(sessionId);

    private string ResolveSessionId(string sessionId)
    {
        if (_vaultSessionService.IsVaultUnlocked(sessionId)) return sessionId;
        return _vaultSessionService.GetActiveSessionId() ?? sessionId;
    }

    public async Task<string?> RevealPasswordAsync(LoginItem loginItem, string sessionId)
    {
        var sid = ResolveSessionId(sessionId);
        try
        {
            if (!CanRevealPasswords(sid))
            {
                _logger.LogWarning("Attempted to reveal password when vault is locked");
                return null;
            }

            if (string.IsNullOrEmpty(loginItem.EncryptedPassword))
            {
                return string.Empty;
            }

            var payload = ToCipherPayload(loginItem.EncryptedPassword, loginItem.PasswordNonce, loginItem.PasswordAuthTag);
            if (payload == null)
            {
                // Legacy rows (e.g. the demo-data seeder, which runs without a vault session) store the
                // password as-is in EncryptedPassword with no nonce/tag — there's nothing to decrypt.
                return loginItem.EncryptedPassword;
            }

            return _vaultSessionService.DecryptPassword(payload, sid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revealing password for login item {Id}", loginItem.Id);
            return null;
        }
    }

    /// <summary>
    /// Login passwords are stored in one of two encrypted layouts: the pipe-joined
    /// "cipher|nonce|tag" string written by imports and <see cref="TryEncryptLoginPassword"/>, or the
    /// ciphertext alone in EncryptedPassword with the nonce/tag in their own columns (written by
    /// PasswordEncryptionService). Returns the pipe-joined form for either, or null when the value
    /// carries no nonce/tag at all (legacy plaintext).
    /// </summary>
    internal static string? ToCipherPayload(string encrypted, string? nonce, string? tag)
    {
        if (encrypted.Split('|').Length == 3) return encrypted;
        if (!string.IsNullOrEmpty(nonce) && !string.IsNullOrEmpty(tag)) return $"{encrypted}|{nonce}|{tag}";
        return null;
    }

    public bool TryEncryptLoginPassword(LoginItem loginItem, string sessionId)
    {
        if (string.IsNullOrEmpty(loginItem.Password)) return false;
        // Deliberately no ResolveSessionId fallback: on the multi-user web host "any active session"
        // could be another user's, and encrypting with their key would make the password unreadable.
        var sid = sessionId;
        if (!CanRevealPasswords(sid))
        {
            _logger.LogWarning("Cannot encrypt password: vault is locked");
            return false;
        }

        try
        {
            loginItem.EncryptedPassword = _vaultSessionService.EncryptPassword(loginItem.Password, sid);
            // The pipe-joined value is self-contained; clear the split-layout columns so a stale
            // nonce/tag from an older save can't be paired with the new ciphertext.
            loginItem.PasswordNonce = null;
            loginItem.PasswordAuthTag = null;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting password for login item {Id}", loginItem.Id);
            return false;
        }
    }

    public async Task<string?> RevealTotpSecretAsync(LoginItem loginItem, string sessionId)
    {
        var sid = ResolveSessionId(sessionId);
        try
        {
            if (!CanRevealPasswords(sid))
            {
                _logger.LogWarning("Attempted to reveal TOTP secret when vault is locked");
                return null;
            }

            if (string.IsNullOrEmpty(loginItem.EncryptedTotpSecret))
                return string.Empty;

            // Same pipe-delimited (cipher|nonce|tag) format used for the password.
            var parts = loginItem.EncryptedTotpSecret.Split('|');
            if (parts.Length != 3)
            {
                _logger.LogError("Invalid encrypted TOTP secret format for login item {Id}", loginItem.Id);
                return null;
            }

            return _vaultSessionService.DecryptPassword(
                string.Join("|", parts[0], parts[1], parts[2]), sid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revealing TOTP secret for login item {Id}", loginItem.Id);
            return null;
        }
    }

    public async Task<string?> RevealPasswordAsync(WiFiItem wifiItem, string sessionId)
    {
        var sid = ResolveSessionId(sessionId);
        try
        {
            if (!CanRevealPasswords(sid))
            {
                _logger.LogWarning("Attempted to reveal WiFi password when vault is locked");
                return null;
            }

            if (string.IsNullOrEmpty(wifiItem.Password))
            {
                return string.Empty;
            }

            var parts = wifiItem.Password.Split('|');
            if (parts.Length != 3)
            {
                _logger.LogError("Invalid encrypted WiFi password format for item {Id}", wifiItem.Id);
                return null;
            }
            var encryptedData = new EncryptedPasswordData
            {
                EncryptedPassword = parts[0],
                Nonce = parts[1],
                AuthenticationTag = parts[2]
            };

            var decryptedPassword = _vaultSessionService.DecryptPassword(
                string.Join("|", encryptedData.EncryptedPassword, encryptedData.Nonce, encryptedData.AuthenticationTag),
                sid);
            return decryptedPassword;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revealing WiFi password for item {Id}", wifiItem.Id);
            return null;
        }
    }

    public async Task<string?> RevealCardNumberAsync(CreditCardItem creditCardItem, string sessionId)
    {
        var sid = ResolveSessionId(sessionId);
        try
        {
            if (!CanRevealPasswords(sid))
            {
                _logger.LogWarning("Attempted to reveal card number when vault is locked");
                return null;
            }

            if (string.IsNullOrEmpty(creditCardItem.EncryptedCardNumber) ||
                string.IsNullOrEmpty(creditCardItem.CardNumberNonce) ||
                string.IsNullOrEmpty(creditCardItem.CardNumberAuthTag))
            {
                return string.Empty;
            }

            var encryptedData = new EncryptedPasswordData
            {
                EncryptedPassword = creditCardItem.EncryptedCardNumber,
                Nonce = creditCardItem.CardNumberNonce,
                AuthenticationTag = creditCardItem.CardNumberAuthTag
            };

            var decryptedCardNumber = _vaultSessionService.DecryptPassword(
                string.Join("|", encryptedData.EncryptedPassword, encryptedData.Nonce, encryptedData.AuthenticationTag),
                sid);
            return decryptedCardNumber;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revealing card number for item {Id}", creditCardItem.Id);
            return null;
        }
    }

    public async Task<string?> RevealCvvAsync(CreditCardItem creditCardItem, string sessionId)
    {
        var sid = ResolveSessionId(sessionId);
        try
        {
            if (!CanRevealPasswords(sid))
            {
                _logger.LogWarning("Attempted to reveal CVV when vault is locked");
                return null;
            }

            if (string.IsNullOrEmpty(creditCardItem.EncryptedCvv) ||
                string.IsNullOrEmpty(creditCardItem.CvvNonce) ||
                string.IsNullOrEmpty(creditCardItem.CvvAuthTag))
            {
                return string.Empty;
            }

            var encryptedData = new EncryptedPasswordData
            {
                EncryptedPassword = creditCardItem.EncryptedCvv,
                Nonce = creditCardItem.CvvNonce,
                AuthenticationTag = creditCardItem.CvvAuthTag
            };

            var decryptedCvv = _vaultSessionService.DecryptPassword(
                string.Join("|", encryptedData.EncryptedPassword, encryptedData.Nonce, encryptedData.AuthenticationTag),
                sid);
            return decryptedCvv;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revealing CVV for item {Id}", creditCardItem.Id);
            return null;
        }
    }

    public async Task<string?> RevealContentAsync(SecureNoteItem secureNoteItem, string sessionId)
    {
        var sid = ResolveSessionId(sessionId);
        try
        {
            if (!CanRevealPasswords(sid))
            {
                _logger.LogWarning("Attempted to reveal secure note content when vault is locked");
                return null;
            }

            if (string.IsNullOrEmpty(secureNoteItem.EncryptedContent) ||
                string.IsNullOrEmpty(secureNoteItem.ContentNonce) ||
                string.IsNullOrEmpty(secureNoteItem.ContentAuthTag))
            {
                return string.Empty;
            }

            var encryptedData = new EncryptedPasswordData
            {
                EncryptedPassword = secureNoteItem.EncryptedContent,
                Nonce = secureNoteItem.ContentNonce,
                AuthenticationTag = secureNoteItem.ContentAuthTag
            };

            var decryptedContent = _vaultSessionService.DecryptPassword(
                string.Join("|", encryptedData.EncryptedPassword, encryptedData.Nonce, encryptedData.AuthenticationTag),
                sid);
            return decryptedContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revealing content for secure note {Id}", secureNoteItem.Id);
            return null;
        }
    }

    /// <summary>
    /// Deserializes encrypted data from JSON string
    /// </summary>
    private EncryptedPasswordData? DeserializeEncryptedData(string encryptedDataJson)
    {
        try
        {
            return JsonSerializer.Deserialize<EncryptedPasswordData>(encryptedDataJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize encrypted data: {Data}", encryptedDataJson);
            return null;
        }
    }
}
