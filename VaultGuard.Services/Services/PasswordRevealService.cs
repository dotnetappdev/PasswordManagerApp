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
        => VaultGuard.Services.Security.VaultItemSecrets.ToPayload(encrypted, nonce, tag);

    public string? RevealField(string? encrypted, string? nonce, string? tag, string sessionId)
    {
        if (string.IsNullOrEmpty(encrypted)) return string.Empty;
        var sid = ResolveSessionId(sessionId);
        if (!CanRevealPasswords(sid)) return null;
        var payload = ToCipherPayload(encrypted, nonce, tag);
        if (payload == null) return null;
        try { return _vaultSessionService.DecryptPassword(payload, sid); }
        catch (Exception ex) { _logger.LogError(ex, "Error decrypting field"); return null; }
    }

    public string? ProtectField(string plaintext, string sessionId)
    {
        if (string.IsNullOrEmpty(plaintext) || !CanRevealPasswords(sessionId)) return null;
        try { return _vaultSessionService.EncryptPassword(plaintext, sessionId); }
        catch (Exception ex) { _logger.LogError(ex, "Error encrypting field"); return null; }
    }

    public bool TryEncryptLoginPassword(LoginItem loginItem, string sessionId)
        => !string.IsNullOrEmpty(loginItem.Password)
           && TrySealItem(new PasswordItem { LoginItem = loginItem }, sessionId);

    public bool TrySealItem(PasswordItem item, string sessionId)
    {
        // Deliberately no ResolveSessionId fallback: on the multi-user web host "any active session"
        // could be another user's, and encrypting with their key would make the data unreadable.
        var cipher = VaultGuard.Services.Security.VaultCipher.ForSession(_vaultSessionService, sessionId);
        if (cipher == null)
        {
            _logger.LogWarning("Cannot encrypt item secrets: vault is locked");
            return false;
        }
        try
        {
            cipher.Seal(item);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting secrets for item {Id}", item.Id);
            return false;
        }
    }

    public bool OpenItem(PasswordItem item, string sessionId)
    {
        var cipher = VaultGuard.Services.Security.VaultCipher.ForSession(_vaultSessionService, ResolveSessionId(sessionId));
        if (cipher == null) return false;
        var failed = cipher.Open(item);
        if (failed.Count > 0)
            _logger.LogWarning("Could not decrypt {Count} field(s) of item {Id}: {Fields}", failed.Count, item.Id, string.Join(", ", failed));
        return failed.Count == 0;
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

            // Same storage layouts as the password: pipe-joined, or split across TotpNonce/TotpAuthTag.
            var payload = ToCipherPayload(loginItem.EncryptedTotpSecret, loginItem.TotpNonce, loginItem.TotpAuthTag);
            if (payload == null)
            {
                _logger.LogError("Invalid encrypted TOTP secret format for login item {Id}", loginItem.Id);
                return null;
            }

            return _vaultSessionService.DecryptPassword(payload, sid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revealing TOTP secret for login item {Id}", loginItem.Id);
            return null;
        }
    }

    // Field reveals for the other item types. Each returns the value if the item is already open, otherwise
    // decrypts the stored ciphertext (split columns, or the legacy "cipher|nonce|tag" form) — never ciphertext,
    // and null when the vault is locked.
    public Task<string?> RevealPasswordAsync(WiFiItem wifiItem, string sessionId) =>
        Task.FromResult(Reveal(wifiItem.Password, wifiItem.EncryptedPassword, wifiItem.PasswordNonce, wifiItem.PasswordAuthTag, sessionId));

    public Task<string?> RevealCardNumberAsync(CreditCardItem creditCardItem, string sessionId) =>
        Task.FromResult(Reveal(creditCardItem.CardNumber, creditCardItem.EncryptedCardNumber, creditCardItem.CardNumberNonce, creditCardItem.CardNumberAuthTag, sessionId));

    public Task<string?> RevealCvvAsync(CreditCardItem creditCardItem, string sessionId) =>
        Task.FromResult(Reveal(creditCardItem.CVV, creditCardItem.EncryptedCvv, creditCardItem.CvvNonce, creditCardItem.CvvAuthTag, sessionId));

    public Task<string?> RevealContentAsync(SecureNoteItem secureNoteItem, string sessionId) =>
        Task.FromResult(Reveal(secureNoteItem.Content, secureNoteItem.EncryptedContent, secureNoteItem.ContentNonce, secureNoteItem.ContentAuthTag, sessionId));

    private string? Reveal(string? opened, string? encrypted, string? nonce, string? tag, string sessionId)
    {
        if (string.IsNullOrEmpty(encrypted)) return opened ?? string.Empty;
        if (!CanRevealPasswords(ResolveSessionId(sessionId)))
        {
            _logger.LogWarning("Attempted to reveal a field while the vault is locked");
            return null;
        }
        return !string.IsNullOrEmpty(opened) ? opened : RevealField(encrypted, nonce, tag, sessionId);
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
