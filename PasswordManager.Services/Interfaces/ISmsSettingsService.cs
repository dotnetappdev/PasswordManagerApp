using PasswordManager.Models;
using PasswordManager.Models.Configuration;

namespace PasswordManager.Services.Interfaces;

/// <summary>
/// Interface for managing SMS settings from both database and configuration
/// </summary>
public interface ISmsSettingsService
{
    /// <summary>
    /// Get the active SMS configuration for a user
    /// </summary>
    /// <param name="userId">User ID (optional - if null, uses global settings)</param>
    /// <returns>SMS configuration</returns>
    Task<SmsConfiguration> GetActiveSmsConfigurationAsync(string? userId = null);

    /// <summary>
    /// Get SMS settings from database for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Active SMS settings or null</returns>
    Task<SmsSettings?> GetActiveSmsSettingsAsync(string userId);

    /// <summary>
    /// Get all SMS settings for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of SMS settings</returns>
    Task<List<SmsSettings>> GetUserSmsSettingsAsync(string userId);

    /// <summary>
    /// Create or update SMS settings for a user
    /// </summary>
    /// <param name="smsSettings">SMS settings to save</param>
    /// <returns>Saved SMS settings</returns>
    Task<SmsSettings> SaveSmsSettingsAsync(SmsSettings smsSettings);

    /// <summary>
    /// Delete SMS settings
    /// </summary>
    /// <param name="settingsId">Settings ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteSmsSettingsAsync(int settingsId, string userId);

    /// <summary>
    /// Activate specific SMS settings for a user
    /// </summary>
    /// <param name="settingsId">Settings ID to activate</param>
    /// <param name="userId">User ID</param>
    /// <returns>True if activated successfully</returns>
    Task<bool> ActivateSmsSettingsAsync(int settingsId, string userId);

    /// <summary>
    /// Convert database SMS settings to configuration format
    /// </summary>
    /// <param name="smsSettings">Database SMS settings</param>
    /// <param name="masterKey">Master key for decryption</param>
    /// <returns>SMS configuration</returns>
    Task<SmsConfiguration> ConvertToConfigurationAsync(SmsSettings smsSettings, byte[] masterKey);

    /// <summary>
    /// Test SMS settings by sending a test message
    /// </summary>
    /// <param name="settingsId">Settings ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="phoneNumber">Phone number to send test to</param>
    /// <param name="testMessage">Test message content</param>
    /// <returns>Test result</returns>
    Task<SmsResult> TestSmsSettingsAsync(int settingsId, string userId, string phoneNumber, string? testMessage = null);
}