namespace PasswordManager.Services.Interfaces;

/// <summary>
/// Service for platform detection and platform-specific operations
/// </summary>
public interface IPlatformService
{
    /// <summary>
    /// Gets the current platform name
    /// </summary>
    string GetPlatformName();

    /// <summary>
    /// Determines if the current platform should show the database selection dialog
    /// </summary>
    bool ShouldShowDatabaseSelection();

    /// <summary>
    /// Gets the application data directory path
    /// </summary>
    string GetAppDataDirectory();

    /// <summary>
    /// Gets the documents directory path
    /// </summary>
    string GetDocumentsDirectory();

    /// <summary>
    /// Gets platform-specific device information for encryption key generation
    /// </summary>
    string GetDeviceIdentifier();

    /// <summary>
    /// Determines if the current platform is mobile
    /// </summary>
    bool IsMobilePlatform();
}