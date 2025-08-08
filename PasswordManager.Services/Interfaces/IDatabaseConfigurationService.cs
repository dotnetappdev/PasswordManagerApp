using PasswordManager.Models.Configuration;

namespace PasswordManager.Services.Interfaces;

/// <summary>
/// Service for managing database configuration
/// </summary>
public interface IDatabaseConfigurationService
{
    /// <summary>
    /// Gets the current database configuration
    /// </summary>
    Task<DatabaseConfiguration> GetConfigurationAsync();

    /// <summary>
    /// Saves the database configuration
    /// </summary>
    Task SaveConfigurationAsync(DatabaseConfiguration configuration);

    /// <summary>
    /// Checks if this is the first run (no configuration exists)
    /// </summary>
    Task<bool> IsFirstRunAsync();

    /// <summary>
    /// Builds a connection string for the specified database configuration
    /// </summary>
    string BuildConnectionString(DatabaseConfiguration configuration);

    /// <summary>
    /// Builds a connection string for the specified database configuration (async version for providers requiring decryption)
    /// </summary>
    Task<string> BuildConnectionStringAsync(DatabaseConfiguration configuration);

    /// <summary>
    /// Tests the database connection with the given configuration
    /// </summary>
    Task<(bool Success, string ErrorMessage)> TestConnectionAsync(DatabaseConfiguration configuration);

    /// <summary>
    /// Encrypts a database password for secure storage
    /// </summary>
    Task<string> EncryptPasswordAsync(string password);

    /// <summary>
    /// Decrypts a database password for use
    /// </summary>
    Task<string> DecryptPasswordAsync(string encryptedPassword);

    /// <summary>
    /// Gets the platform-specific default configuration
    /// </summary>
    DatabaseConfiguration GetDefaultConfiguration();

    /// <summary>
    /// Determines if the current platform should show the database selection dialog
    /// </summary>
    bool ShouldShowDatabaseSelection();
}