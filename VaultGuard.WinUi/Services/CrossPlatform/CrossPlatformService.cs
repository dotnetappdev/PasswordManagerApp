using VaultGuard.Services.Interfaces;

namespace VaultGuard.WinUi.Services.CrossPlatform;

/// <summary>
/// Cross-platform implementation of IPlatformService for non-Windows environments.
/// </summary>
public class CrossPlatformService : IPlatformService
{
    public string GetAppDataDirectory()
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var appDir = Path.Combine(baseDir, ".passwordmanager");

        if (!Directory.Exists(appDir))
        {
            Directory.CreateDirectory(appDir);
        }

        return appDir;
    }

    public string GetDocumentsDirectory()
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    public string GetAppVersion()
    {
        return "1.0.0-crossplatform";
    }

    public string GetPlatformName()
    {
        return Environment.OSVersion.Platform.ToString();
    }

    public bool IsRunningOnWindows()
    {
        return Environment.OSVersion.Platform == PlatformID.Win32NT;
    }

    public bool ShouldShowDatabaseSelection()
    {
        return true;
    }

    public string GetDeviceIdentifier()
    {
        var machineName = Environment.MachineName;
        var userName = Environment.UserName;
        return $"{machineName}-{userName}";
    }

    public bool IsMobilePlatform()
    {
        return false;
    }
}
