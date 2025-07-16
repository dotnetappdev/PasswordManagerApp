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

    public bool CanRevealPasswords => _vaultSessionService.IsVaultUnlocked();

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

            // Parse encrypted data from pipe-delimited string
            var parts = loginItem.EncryptedPassword.Split('|');
            if (parts.Length != 3)
            {
                _logger.LogError("Invalid encrypted password format for login item {Id}", loginItem.Id);
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
                loginItem.UserId);
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
                wifiItem.UserId);
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
                creditCardItem.UserId);
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
                creditCardItem.UserId);
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
                secureNoteItem.UserId);
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
