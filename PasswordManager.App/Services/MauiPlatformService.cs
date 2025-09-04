using System;
using System.IO;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;
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
        try
        {
            // Store app data under the platform app data directory to ensure persistence
            return Path.Combine(FileSystem.AppDataDirectory, "PasswordManager");
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
                return Path.Combine(FileSystem.AppDataDirectory, "PasswordManager");
            }

            return Path.Combine(docs, "PasswordManager");
        }
        catch
        {
            // Ensure we always return a usable path
            return Path.Combine(FileSystem.AppDataDirectory, "PasswordManager");
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