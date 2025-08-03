using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PasswordManager.Models;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.Services.Helpers;

/// <summary>
/// Utility class for migrating configurations to the unified configuration system
/// </summary>
public class ConfigurationMigrationHelper
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConfigurationMigrationHelper> _logger;

    public ConfigurationMigrationHelper(IServiceProvider serviceProvider, ILogger<ConfigurationMigrationHelper> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Migrates all existing configurations to unified storage
    /// </summary>
    /// <param name="userId">User ID for user-specific settings (null for system settings)</param>
    /// <returns>Migration result with statistics</returns>
    public async Task<ConfigurationMigrationResult> MigrateAllConfigurationsAsync(string? userId = null)
    {
        var result = new ConfigurationMigrationResult();

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var configService = scope.ServiceProvider.GetService<IConfigurationSettingService>();
            var dbConfigService = scope.ServiceProvider.GetService<IDatabaseConfigurationService>();

            if (configService == null || dbConfigService == null)
            {
                result.Success = false;
                result.ErrorMessage = "Required services not available";
                return result;
            }

            // Migrate database configuration
            try
            {
                var dbConfig = await dbConfigService.GetConfigurationAsync();
                await configService.MigrateDatabaseConfigurationAsync(dbConfig, userId);
                result.DatabaseConfigMigrated = true;
                _logger.LogInformation("Database configuration migrated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to migrate database configuration");
                result.Errors.Add($"Database config migration failed: {ex.Message}");
            }

            // You can add more configuration types here as needed
            // For example: user preferences, app settings, etc.

            result.Success = result.DatabaseConfigMigrated && result.Errors.Count == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Configuration migration failed");
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Creates sample user settings in unified configuration
    /// </summary>
    /// <param name="userId">User ID</param>
    public async Task CreateSampleUserSettingsAsync(string userId)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var configService = scope.ServiceProvider.GetService<IConfigurationSettingService>();
            if (configService == null) return;

            var userSettings = new Dictionary<string, string>
            {
                [ConfigurationKeys.Theme] = "Dark",
                [ConfigurationKeys.Language] = "en-US",
                [ConfigurationKeys.AutoSync] = "true"
            };

            await configService.SetConfigurationGroupAsync(
                ConfigurationGroups.UserSettings,
                "General",
                userSettings,
                userId,
                isSystemLevel: false);

            _logger.LogInformation("Sample user settings created for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create sample user settings");
        }
    }

    /// <summary>
    /// Creates sample app settings in unified configuration
    /// </summary>
    public async Task CreateSampleAppSettingsAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var configService = scope.ServiceProvider.GetService<IConfigurationSettingService>();
            if (configService == null) return;

            var appSettings = new Dictionary<string, string>
            {
                ["DefaultSessionTimeout"] = "30",
                ["MaxLoginAttempts"] = "5",
                ["EnableAuditLogging"] = "true",
                ["BackupRetentionDays"] = "30"
            };

            await configService.SetConfigurationGroupAsync(
                ConfigurationGroups.AppSettings,
                "General",
                appSettings,
                userId: null, // System-level settings
                isSystemLevel: true);

            _logger.LogInformation("Sample app settings created");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create sample app settings");
        }
    }

    /// <summary>
    /// Validates unified configuration system
    /// </summary>
    /// <param name="userId">User ID for testing user-specific settings</param>
    /// <returns>Validation result</returns>
    public async Task<ConfigurationValidationResult> ValidateUnifiedConfigurationAsync(string? userId = null)
    {
        var result = new ConfigurationValidationResult();

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var configService = scope.ServiceProvider.GetService<IConfigurationSettingService>();
            if (configService == null)
            {
                result.Success = false;
                result.ErrorMessage = "Configuration service not available";
                return result;
            }

            // Test basic operations
            var testKey = "TestKey";
            var testValue = "TestValue";

            // Set a test value
            await configService.SetConfigurationValueAsync(
                ConfigurationGroups.AppSettings, 
                "Test", 
                testKey, 
                testValue, 
                userId);

            // Get the test value
            var retrievedValue = await configService.GetConfigurationValueAsync(
                ConfigurationGroups.AppSettings, 
                "Test", 
                testKey, 
                userId);

            if (retrievedValue == testValue)
            {
                result.BasicOperationsWork = true;
            }
            else
            {
                result.Errors.Add("Basic set/get operations failed");
            }

            // Test group operations
            var testGroup = new Dictionary<string, string>
            {
                ["Key1"] = "Value1",
                ["Key2"] = "Value2"
            };

            await configService.SetConfigurationGroupAsync(
                ConfigurationGroups.AppSettings,
                "TestGroup",
                testGroup,
                userId);

            var retrievedGroup = await configService.GetConfigurationGroupAsync(
                ConfigurationGroups.AppSettings,
                "TestGroup",
                userId);

            if (retrievedGroup.Count == testGroup.Count && 
                retrievedGroup.All(kvp => testGroup.ContainsKey(kvp.Key) && testGroup[kvp.Key] == kvp.Value))
            {
                result.GroupOperationsWork = true;
            }
            else
            {
                result.Errors.Add("Group operations failed");
            }

            // Clean up test data
            await configService.DeleteConfigurationGroupAsync(ConfigurationGroups.AppSettings, "Test", userId);
            await configService.DeleteConfigurationGroupAsync(ConfigurationGroups.AppSettings, "TestGroup", userId);

            result.Success = result.BasicOperationsWork && result.GroupOperationsWork && result.Errors.Count == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Configuration validation failed");
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }
}

/// <summary>
/// Result of configuration migration operation
/// </summary>
public class ConfigurationMigrationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public bool DatabaseConfigMigrated { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Result of configuration validation operation
/// </summary>
public class ConfigurationValidationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public bool BasicOperationsWork { get; set; }
    public bool GroupOperationsWork { get; set; }
    public List<string> Errors { get; set; } = new();
}