using PasswordManager.Services.Interfaces;

namespace PasswordManager.Services.Services;

/// <summary>
/// Default implementation of platform service for non-MAUI environments
/// </summary>
public class DefaultPlatformService : IPlatformService
{
    public string GetPlatformName()
    {
        return Environment.OSVersion.Platform.ToString();
    }

    public bool ShouldShowDatabaseSelection()
    {
        // For non-MAUI environments, show database selection by default
        return true;
    }

    public string GetAppDataDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PasswordManager");
    }

    public string GetDeviceIdentifier()
    {
        return $"{Environment.MachineName}-{Environment.OSVersion}";
    }

    public bool IsMobilePlatform()
    {
        return false;
    }
}