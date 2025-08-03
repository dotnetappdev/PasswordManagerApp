using PasswordManager.Models;

namespace PasswordManager.Services.Interfaces;

/// <summary>
/// Service for managing unified configuration settings with grouping support
/// </summary>
public interface IConfigurationSettingService
{
    /// <summary>
    /// Get a configuration value by group, type, and key
    /// </summary>
    /// <param name="groupKey">Configuration group (e.g., DatabaseConfig, UserSettings)</param>
    /// <param name="configType">Configuration type (e.g., SQLite, SqlServer)</param>
    /// <param name="key">Setting key</param>
    /// <param name="userId">User ID for user-specific settings (null for system settings)</param>
    /// <returns>Configuration value or null if not found</returns>
    Task<string?> GetConfigurationValueAsync(string groupKey, string configType, string key, string? userId = null);

    /// <summary>
    /// Set a configuration value
    /// </summary>
    /// <param name="groupKey">Configuration group</param>
    /// <param name="configType">Configuration type</param>
    /// <param name="key">Setting key</param>
    /// <param name="value">Setting value</param>
    /// <param name="userId">User ID for user-specific settings (null for system settings)</param>
    /// <param name="isEncrypted">Whether the value should be encrypted</param>
    /// <param name="isSystemLevel">Whether this is a system-level setting</param>
    Task SetConfigurationValueAsync(string groupKey, string configType, string key, string value, 
        string? userId = null, bool isEncrypted = false, bool isSystemLevel = false);

    /// <summary>
    /// Get all configuration settings for a specific group and type
    /// </summary>
    /// <param name="groupKey">Configuration group</param>
    /// <param name="configType">Configuration type</param>
    /// <param name="userId">User ID for user-specific settings (null for system settings)</param>
    /// <returns>Dictionary of key-value pairs</returns>
    Task<Dictionary<string, string>> GetConfigurationGroupAsync(string groupKey, string configType, string? userId = null);

    /// <summary>
    /// Set multiple configuration values for a group and type
    /// </summary>
    /// <param name="groupKey">Configuration group</param>
    /// <param name="configType">Configuration type</param>
    /// <param name="configurations">Dictionary of key-value pairs to set</param>
    /// <param name="userId">User ID for user-specific settings (null for system settings)</param>
    /// <param name="encryptedKeys">List of keys that should be encrypted</param>
    /// <param name="isSystemLevel">Whether these are system-level settings</param>
    Task SetConfigurationGroupAsync(string groupKey, string configType, Dictionary<string, string> configurations, 
        string? userId = null, List<string>? encryptedKeys = null, bool isSystemLevel = false);

    /// <summary>
    /// Delete a configuration setting
    /// </summary>
    /// <param name="groupKey">Configuration group</param>
    /// <param name="configType">Configuration type</param>
    /// <param name="key">Setting key</param>
    /// <param name="userId">User ID for user-specific settings (null for system settings)</param>
    Task DeleteConfigurationAsync(string groupKey, string configType, string key, string? userId = null);

    /// <summary>
    /// Delete all configuration settings for a group and type
    /// </summary>
    /// <param name="groupKey">Configuration group</param>
    /// <param name="configType">Configuration type</param>
    /// <param name="userId">User ID for user-specific settings (null for system settings)</param>
    Task DeleteConfigurationGroupAsync(string groupKey, string configType, string? userId = null);

    /// <summary>
    /// Get all configuration groups for a user
    /// </summary>
    /// <param name="userId">User ID (null for system settings)</param>
    /// <returns>List of group keys</returns>
    Task<List<string>> GetConfigurationGroupsAsync(string? userId = null);

    /// <summary>
    /// Get all configuration types within a group
    /// </summary>
    /// <param name="groupKey">Configuration group</param>
    /// <param name="userId">User ID (null for system settings)</param>
    /// <returns>List of configuration types</returns>
    Task<List<string>> GetConfigurationTypesAsync(string groupKey, string? userId = null);

    /// <summary>
    /// Migrate existing database configuration to unified table
    /// </summary>
    /// <param name="databaseConfig">Existing database configuration</param>
    /// <param name="userId">User ID</param>
    Task MigrateDatabaseConfigurationAsync(Configuration.DatabaseConfiguration databaseConfig, string? userId = null);

    /// <summary>
    /// Load database configuration from unified table
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Database configuration object</returns>
    Task<Configuration.DatabaseConfiguration> LoadDatabaseConfigurationAsync(string? userId = null);
}