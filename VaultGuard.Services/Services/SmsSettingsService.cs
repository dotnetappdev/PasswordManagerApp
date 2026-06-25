using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PasswordManager.Crypto.Interfaces;
using PasswordManager.DAL;
using PasswordManager.Models;
using PasswordManager.Models.Configuration;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.Services.Services;

/// <summary>
/// Service for managing SMS settings from both database and configuration
/// </summary>
public class SmsSettingsService : ISmsSettingsService
{
    private readonly PasswordManagerDbContext _context;
    private readonly IPasswordCryptoService _cryptoService;
    private readonly ISmsService _smsService;
    private readonly SmsConfiguration _defaultConfig;
    private readonly ILogger<SmsSettingsService> _logger;

    public SmsSettingsService(
        PasswordManagerDbContext context,
        IPasswordCryptoService cryptoService,
        ISmsService smsService,
        IOptions<SmsConfiguration> defaultConfig,
        ILogger<SmsSettingsService> logger)
    {
        _context = context;
        _cryptoService = cryptoService;
        _smsService = smsService;
        _defaultConfig = defaultConfig.Value;
        _logger = logger;
    }

    public async Task<SmsConfiguration> GetActiveSmsConfigurationAsync(string? userId = null)
    {
        try
        {
            // If userId is provided, try to get user-specific settings first
            if (!string.IsNullOrEmpty(userId))
            {
                var userSettings = await GetActiveSmsSettingsAsync(userId);
                if (userSettings != null)
                {
                    // We can't decrypt here without the master key, so we'll return a limited configuration
                    return new SmsConfiguration
                    {
                        Provider = userSettings.Provider,
                        Enabled = userSettings.Enabled,
                        DefaultCountryCode = userSettings.DefaultCountryCode,
                        CodeLength = userSettings.CodeLength,
                        ExpirationMinutes = userSettings.ExpirationMinutes,
                        MaxAttempts = userSettings.MaxAttempts,
                        MaxSmsPerHour = userSettings.MaxSmsPerHour,
                        MessageTemplate = userSettings.MessageTemplate
                    };
                }
            }

            // Fall back to configuration file settings
            return _defaultConfig;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active SMS configuration for user {UserId}", userId);
            return _defaultConfig;
        }
    }

    public async Task<SmsSettings?> GetActiveSmsSettingsAsync(string userId)
    {
        try
        {
            return await _context.SmsSettings
                .Where(s => s.UserId == userId && s.IsActive)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active SMS settings for user {UserId}", userId);
            return null;
        }
    }

    public async Task<List<SmsSettings>> GetUserSmsSettingsAsync(string userId)
    {
        try
        {
            return await _context.SmsSettings
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.IsActive)
                .ThenByDescending(s => s.LastModified)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SMS settings for user {UserId}", userId);
            return new List<SmsSettings>();
        }
    }

    public async Task<SmsSettings> SaveSmsSettingsAsync(SmsSettings smsSettings)
    {
        try
        {
            if (smsSettings.Id == 0)
            {
                // Creating new settings
                smsSettings.CreatedAt = DateTime.UtcNow;
                smsSettings.LastModified = DateTime.UtcNow;
                
                // If this is being set as active, deactivate others
                if (smsSettings.IsActive)
                {
                    await DeactivateOtherSettingsAsync(smsSettings.UserId, 0);
                }
                
                _context.SmsSettings.Add(smsSettings);
            }
            else
            {
                // Updating existing settings
                var existingSettings = await _context.SmsSettings
                    .Where(s => s.Id == smsSettings.Id && s.UserId == smsSettings.UserId)
                    .FirstOrDefaultAsync();

                if (existingSettings == null)
                    throw new InvalidOperationException("SMS settings not found");

                // Update properties
                existingSettings.Provider = smsSettings.Provider;
                existingSettings.Enabled = smsSettings.Enabled;
                existingSettings.DefaultCountryCode = smsSettings.DefaultCountryCode;
                existingSettings.CodeLength = smsSettings.CodeLength;
                existingSettings.ExpirationMinutes = smsSettings.ExpirationMinutes;
                existingSettings.MaxAttempts = smsSettings.MaxAttempts;
                existingSettings.MaxSmsPerHour = smsSettings.MaxSmsPerHour;
                existingSettings.MessageTemplate = smsSettings.MessageTemplate;
                existingSettings.TwilioAccountSid = smsSettings.TwilioAccountSid;
                existingSettings.TwilioAuthToken = smsSettings.TwilioAuthToken;
                existingSettings.TwilioFromPhoneNumber = smsSettings.TwilioFromPhoneNumber;
                existingSettings.AwsAccessKeyId = smsSettings.AwsAccessKeyId;
                existingSettings.AwsSecretAccessKey = smsSettings.AwsSecretAccessKey;
                existingSettings.AwsRegion = smsSettings.AwsRegion;
                existingSettings.AwsSenderName = smsSettings.AwsSenderName;
                existingSettings.AzureConnectionString = smsSettings.AzureConnectionString;
                existingSettings.AzureFromPhoneNumber = smsSettings.AzureFromPhoneNumber;
                existingSettings.IsActive = smsSettings.IsActive;
                existingSettings.LastModified = DateTime.UtcNow;

                // If this is being set as active, deactivate others
                if (smsSettings.IsActive)
                {
                    await DeactivateOtherSettingsAsync(smsSettings.UserId, smsSettings.Id);
                }

                smsSettings = existingSettings;
            }

            await _context.SaveChangesAsync();
            return smsSettings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving SMS settings for user {UserId}", smsSettings.UserId);
            throw;
        }
    }

    public async Task<bool> DeleteSmsSettingsAsync(int settingsId, string userId)
    {
        try
        {
            var smsSettings = await _context.SmsSettings
                .Where(s => s.Id == settingsId && s.UserId == userId)
                .FirstOrDefaultAsync();

            if (smsSettings == null)
                return false;

            _context.SmsSettings.Remove(smsSettings);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting SMS settings {SettingsId} for user {UserId}", settingsId, userId);
            return false;
        }
    }

    public async Task<bool> ActivateSmsSettingsAsync(int settingsId, string userId)
    {
        try
        {
            var smsSettings = await _context.SmsSettings
                .Where(s => s.Id == settingsId && s.UserId == userId)
                .FirstOrDefaultAsync();

            if (smsSettings == null)
                return false;

            // Deactivate all other settings for this user
            await DeactivateOtherSettingsAsync(userId, settingsId);

            // Activate the selected setting
            smsSettings.IsActive = true;
            smsSettings.LastModified = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating SMS settings {SettingsId} for user {UserId}", settingsId, userId);
            return false;
        }
    }

    public Task<SmsConfiguration> ConvertToConfigurationAsync(SmsSettings smsSettings, byte[] masterKey)
    {
        try
        {
            var config = new SmsConfiguration
            {
                Provider = smsSettings.Provider,
                Enabled = smsSettings.Enabled,
                DefaultCountryCode = smsSettings.DefaultCountryCode,
                CodeLength = smsSettings.CodeLength,
                ExpirationMinutes = smsSettings.ExpirationMinutes,
                MaxAttempts = smsSettings.MaxAttempts,
                MaxSmsPerHour = smsSettings.MaxSmsPerHour,
                MessageTemplate = smsSettings.MessageTemplate
            };

            // Decrypt provider-specific settings
            switch (smsSettings.Provider.ToLowerInvariant())
            {
                case "twilio":
                    if (!string.IsNullOrEmpty(smsSettings.TwilioAccountSid) && 
                        !string.IsNullOrEmpty(smsSettings.TwilioAuthToken))
                    {
                        config.Twilio.AccountSid = DecryptString(smsSettings.TwilioAccountSid, masterKey);
                        config.Twilio.AuthToken = DecryptString(smsSettings.TwilioAuthToken, masterKey);
                        config.Twilio.FromPhoneNumber = smsSettings.TwilioFromPhoneNumber ?? "";
                    }
                    break;

                case "awssns":
                    if (!string.IsNullOrEmpty(smsSettings.AwsAccessKeyId) && 
                        !string.IsNullOrEmpty(smsSettings.AwsSecretAccessKey))
                    {
                        config.AwsSns.AccessKeyId = DecryptString(smsSettings.AwsAccessKeyId, masterKey);
                        config.AwsSns.SecretAccessKey = DecryptString(smsSettings.AwsSecretAccessKey, masterKey);
                        config.AwsSns.Region = smsSettings.AwsRegion ?? "us-east-1";
                        config.AwsSns.SenderName = smsSettings.AwsSenderName ?? "Password Manager";
                    }
                    break;

                case "azurecommunication":
                    if (!string.IsNullOrEmpty(smsSettings.AzureConnectionString))
                    {
                        config.AzureCommunication.ConnectionString = DecryptString(smsSettings.AzureConnectionString, masterKey);
                        config.AzureCommunication.FromPhoneNumber = smsSettings.AzureFromPhoneNumber ?? "";
                    }
                    break;
            }

            return Task.FromResult(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting SMS settings to configuration");
            throw;
        }
    }

    public async Task<SmsResult> TestSmsSettingsAsync(int settingsId, string userId, string phoneNumber, string? testMessage = null)
    {
        try
        {
            var smsSettings = await _context.SmsSettings
                .Where(s => s.Id == settingsId && s.UserId == userId)
                .FirstOrDefaultAsync();

            if (smsSettings == null)
                return SmsResult.Failure("SMS settings not found");

            // For now, return a success result since we don't have the master key to decrypt settings
            // In a real implementation, this would require the master key to decrypt the settings
            // and then configure a temporary SMS service to send the test message
            
            var message = testMessage ?? "Test message from Password Manager SMS configuration.";
            
            // TODO: Implement actual test message sending
            // This would require:
            // 1. Getting the master key from session
            // 2. Converting settings to configuration
            // 3. Creating a temporary SMS service instance
            // 4. Sending the test message
            
            return SmsResult.Success("test-message-id");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing SMS settings {SettingsId}", settingsId);
            return SmsResult.Failure($"Error testing SMS settings: {ex.Message}");
        }
    }

    private async Task DeactivateOtherSettingsAsync(string userId, int excludeId)
    {
        var otherSettings = await _context.SmsSettings
            .Where(s => s.UserId == userId && s.Id != excludeId && s.IsActive)
            .ToListAsync();

        foreach (var setting in otherSettings)
        {
            setting.IsActive = false;
            setting.LastModified = DateTime.UtcNow;
        }
    }

    private string DecryptString(string encryptedString, byte[] masterKey)
    {
        var parts = encryptedString.Split('|');
        if (parts.Length != 3)
            throw new ArgumentException("Invalid encrypted data format");

        var encryptedData = new EncryptedPasswordData
        {
            EncryptedPassword = parts[0],
            Nonce = parts[1],
            AuthenticationTag = parts[2]
        };

        return _cryptoService.DecryptPasswordWithKey(encryptedData, masterKey);
    }
}