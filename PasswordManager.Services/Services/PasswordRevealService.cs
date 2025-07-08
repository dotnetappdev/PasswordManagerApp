using Microsoft.Extensions.Logging;
using PasswordManager.Services.Interfaces;
using PasswordManager.Crypto.Interfaces;
using PasswordManager.Models;
using System.Text.Json;

namespace PasswordManager.Services.Services;

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

    public bool CanRevealPasswords => _vaultSessionService.IsVaultUnlocked;

    public async Task<string?> RevealPasswordAsync(LoginItem loginItem)
    {
        try
        {
            if (!CanRevealPasswords)
            {
                _logger.LogWarning("Attempted to reveal password when vault is locked");
                return null;
            }

            if (string.IsNullOrEmpty(loginItem.EncryptedPassword))
            {
                return string.Empty;
            }

            var encryptedData = DeserializeEncryptedData(loginItem.EncryptedPassword);
            if (encryptedData == null)
            {
                _logger.LogError("Failed to deserialize encrypted password data for login item {Id}", loginItem.Id);
                return null;
            }

            var decryptedPassword = _vaultSessionService.RevealPassword(encryptedData);
            return decryptedPassword;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revealing password for login item {Id}", loginItem.Id);
            return null;
        }
    }

    public async Task<string?> RevealPasswordAsync(WiFiItem wifiItem)
    {
        try
        {
            if (!CanRevealPasswords)
            {
                _logger.LogWarning("Attempted to reveal WiFi password when vault is locked");
                return null;
            }

            if (string.IsNullOrEmpty(wifiItem.EncryptedPassword))
            {
                return string.Empty;
            }

            var encryptedData = DeserializeEncryptedData(wifiItem.EncryptedPassword);
            if (encryptedData == null)
            {
                _logger.LogError("Failed to deserialize encrypted WiFi password data for item {Id}", wifiItem.Id);
                return null;
            }

            var decryptedPassword = _vaultSessionService.RevealPassword(encryptedData);
            return decryptedPassword;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revealing WiFi password for item {Id}", wifiItem.Id);
            return null;
        }
    }

    public async Task<string?> RevealCardNumberAsync(CreditCardItem creditCardItem)
    {
        try
        {
            if (!CanRevealPasswords)
            {
                _logger.LogWarning("Attempted to reveal card number when vault is locked");
                return null;
            }

            if (string.IsNullOrEmpty(creditCardItem.EncryptedCardNumber))
            {
                return string.Empty;
            }

            var encryptedData = DeserializeEncryptedData(creditCardItem.EncryptedCardNumber);
            if (encryptedData == null)
            {
                _logger.LogError("Failed to deserialize encrypted card number data for item {Id}", creditCardItem.Id);
                return null;
            }

            var decryptedCardNumber = _vaultSessionService.RevealPassword(encryptedData);
            return decryptedCardNumber;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revealing card number for item {Id}", creditCardItem.Id);
            return null;
        }
    }

    public async Task<string?> RevealCvvAsync(CreditCardItem creditCardItem)
    {
        try
        {
            if (!CanRevealPasswords)
            {
                _logger.LogWarning("Attempted to reveal CVV when vault is locked");
                return null;
            }

            if (string.IsNullOrEmpty(creditCardItem.EncryptedCvv))
            {
                return string.Empty;
            }

            var encryptedData = DeserializeEncryptedData(creditCardItem.EncryptedCvv);
            if (encryptedData == null)
            {
                _logger.LogError("Failed to deserialize encrypted CVV data for item {Id}", creditCardItem.Id);
                return null;
            }

            var decryptedCvv = _vaultSessionService.RevealPassword(encryptedData);
            return decryptedCvv;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revealing CVV for item {Id}", creditCardItem.Id);
            return null;
        }
    }

    public async Task<string?> RevealContentAsync(SecureNoteItem secureNoteItem)
    {
        try
        {
            if (!CanRevealPasswords)
            {
                _logger.LogWarning("Attempted to reveal secure note content when vault is locked");
                return null;
            }

            if (string.IsNullOrEmpty(secureNoteItem.EncryptedContent))
            {
                return string.Empty;
            }

            var encryptedData = DeserializeEncryptedData(secureNoteItem.EncryptedContent);
            if (encryptedData == null)
            {
                _logger.LogError("Failed to deserialize encrypted content data for secure note {Id}", secureNoteItem.Id);
                return null;
            }

            var decryptedContent = _vaultSessionService.RevealPassword(encryptedData);
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
