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
}
