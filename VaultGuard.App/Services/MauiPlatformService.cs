using System;
using System.IO;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.App.Services;

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
            // Mobile platforms should always use the default local setup flow.
            if (DeviceInfo.Platform == DevicePlatform.iOS || DeviceInfo.Platform == DevicePlatform.Android)
            {
                return false;
            }

            // Show database selection only on desktop platforms.
            return DeviceInfo.Platform == DevicePlatform.WinUI || DeviceInfo.Platform == DevicePlatform.MacCatalyst;
        }
        catch (Exception)
        {
            // If DeviceInfo is not available during startup, default to false
            return false;
        }
    }

    public string GetAppDataDirectory()
    {
        try
        {
            // On Windows, use the same path as WinUI app for database sharing
            if (DeviceInfo.Platform == DevicePlatform.WinUI)
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var appDir = Path.Combine(localAppData, "VaultGuard");
                
                // Ensure directory exists
                if (!Directory.Exists(appDir))
                {
                    Directory.CreateDirectory(appDir);
                }
                
                return appDir;
            }
            
            // For other platforms, use the platform-specific app data directory
            return Path.Combine(FileSystem.AppDataDirectory, "VaultGuard");
        }
        catch
        {
            // Fallback to a documents path if FileSystem is unavailable
            return GetDocumentsDirectory();
        }
    }

    public string GetDocumentsDirectory()
    {
        try
        {
            // Use the user's Documents folder as a fallback/persistent location
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (string.IsNullOrEmpty(docs))
            {
                // Last-resort: use AppData
                return Path.Combine(FileSystem.AppDataDirectory, "VaultGuard");
            }

            return Path.Combine(docs, "VaultGuard");
        }
        catch
        {
            // Ensure we always return a usable path
            return Path.Combine(FileSystem.AppDataDirectory, "VaultGuard");
        }
    }

    public string GetDeviceIdentifier()
    {
        try
        {
            return $"{DeviceInfo.Model}-{DeviceInfo.Platform}-{AppInfo.Name}";
        }
        catch
        {
            return "unknown-device";
        }
    }

    public bool IsMobilePlatform()
    {
        return DeviceInfo.Platform == DevicePlatform.Android ||
               DeviceInfo.Platform == DevicePlatform.iOS;
    }
}