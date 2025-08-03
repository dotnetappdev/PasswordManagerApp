namespace PasswordManager.Services.Interfaces;

/// <summary>
/// Service for handling application startup operations
/// </summary>
public interface IAppStartupService
{
    /// <summary>
    /// Initializes the application during startup
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Initializes the database with the configured provider
    /// </summary>
    Task InitializeDatabaseAsync();

    /// <summary>
    /// Checks if database configuration is complete
    /// </summary>
    Task<bool> IsDatabaseConfiguredAsync();
}
