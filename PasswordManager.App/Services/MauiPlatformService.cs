using PasswordManager.Services.Interfaces;

namespace PasswordManager.App.Services;

/// <summary>
/// MAUI-specific implementation of platform service
/// </summary>
public class MauiPlatformService : IPlatformService
{
    public string GetPlatformName()
    {
        return DeviceInfo.Platform.ToString();
    }

    public bool ShouldShowDatabaseSelection()
    {
        try
        {
            // Show database selection only on Windows and macOS
            return DeviceInfo.Platform == DevicePlatform.WinUI || 
                   DeviceInfo.Platform == DevicePlatform.MacCatalyst;
        }
        catch (Exception)
        {
            // If DeviceInfo is not available during startup, default to false
            return false;
        }
    }

    public string GetAppDataDirectory()
    {
        return Path.Combine(FileSystem.AppDataDirectory, "PasswordManager");
    }

    public string GetDeviceIdentifier()
    {
        return $"{DeviceInfo.Model}-{DeviceInfo.Platform}-{AppInfo.Name}";
    }

    public bool IsMobilePlatform()
    {
        return DeviceInfo.Platform == DevicePlatform.Android || 
               DeviceInfo.Platform == DevicePlatform.iOS;
    }
}