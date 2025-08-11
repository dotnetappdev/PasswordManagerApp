using PasswordManager.Services.Interfaces;

namespace PasswordManager.WinUi.Services;

public class WinUiPlatformService : IPlatformService
{
    public string GetAppDataDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appDir = Path.Combine(localAppData, "PasswordManager");
        
        // Ensure directory exists
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error opening URL: {ex.Message}");
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error sharing text: {ex.Message}");
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving file: {ex.Message}");
            return Task.FromResult(false);
        }
    }
}