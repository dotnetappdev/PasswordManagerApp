using PasswordManager.Services.Interfaces;

namespace PasswordManager.WinUi.Services;

public class WinUiPlatformService : IPlatformService
{
    /// <summary>
    /// Determines if the app is running as a packaged MSIX app.
    /// </summary>
    private static bool IsPackaged()
    {
        try
        {
            // If we can access Package.Current without exception, we're packaged
            var package = Windows.ApplicationModel.Package.Current;
            return package != null;
        }
        catch
        {
            // Exception thrown when accessing Package.Current means we're unpackaged
            return false;
        }
    }

    public string GetAppDataDirectory()
    {
        string appDir;

        if (IsPackaged())
        {
            // For packaged apps (MSIX), use Windows.Storage.ApplicationData.Current.LocalFolder
            // This is the correct location for sandboxed apps
            appDir = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
        }
        else
        {
            // For unpackaged apps, use the traditional LocalApplicationData folder
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            appDir = Path.Combine(localAppData, "PasswordManager");

            // Create directory for unpackaged apps (packaged apps have LocalFolder pre-created)
            try
            {
                if (!Directory.Exists(appDir))
                {
                    Directory.CreateDirectory(appDir);
                }
            }
            catch (Exception)
            {
                // Directory creation failed, will return path anyway
            }
        }

        return appDir;
    }

    public string GetDocumentsDirectory()
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    public string GetDownloadsDirectory()
    {
        // Windows doesn't have a built-in Downloads special folder in older .NET versions
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, "Downloads");
    }

    public string GetTempDirectory()
    {
        return Path.GetTempPath();
    }

    public string GetPlatformName()
    {
        return "WinUI";
    }

    public bool IsDesktop()
    {
        return true;
    }

    public bool IsMobile()
    {
        return false;
    }

    public bool IsMobilePlatform()
    {
        return false;
    }

    public bool IsWeb()
    {
        return false;
    }

    public bool ShouldShowDatabaseSelection()
    {
        // WinUI is a desktop platform, so show database selection
        return true;
    }

    public string GetDeviceIdentifier()
    {
        return $"{Environment.MachineName}-{Environment.OSVersion.Platform}-WinUI";
    }

    public async Task<bool> OpenUrlAsync(string url)
    {
        try
        {
            await Task.Run(() =>
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(startInfo);
            });
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> ShareTextAsync(string title, string text)
    {
        try
        {
            // For WinUI, we could use the Windows Share contract
            // For now, use Windows.ApplicationModel.DataTransfer.Clipboard as a fallback
            await Task.Run(() =>
            {
                var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                dataPackage.SetText(text);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
            });
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public Task<string?> ShowFilePickerAsync(string[] allowedExtensions)
    {
        // This would require WinUI-specific file picker implementation
        // For now, return a simulated path
        return Task.FromResult<string?>("C:\\Demo\\selected_file.csv");
    }

    public Task<string?> ShowFolderPickerAsync()
    {
        // This would require WinUI-specific folder picker implementation
        return Task.FromResult<string?>("C:\\Demo\\selected_folder");
    }

    public Task<bool> SaveFileAsync(string filename, byte[] data)
    {
        try
        {
            var downloadsPath = Path.Combine(GetDownloadsDirectory(), filename);
            File.WriteAllBytes(downloadsPath, data);
            return Task.FromResult(true);
        }
        catch (Exception)
        {
            return Task.FromResult(false);
        }
    }
}