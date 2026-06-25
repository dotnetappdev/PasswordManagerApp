using VaultGuard.Services.Interfaces;
using System.IO;
using System.Windows;

namespace VaultGuard.WPF.Services;

public class WpfPlatformService : IPlatformService
{
    public string GetAppDataDirectory()
    {
        // For WPF apps, use the traditional LocalApplicationData folder
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appDir = Path.Combine(localAppData, "VaultGuard");

        // Create directory if it doesn't exist
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
        return "WPF";
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
        // WPF is a desktop platform, so show database selection
        return true;
    }

    public string GetDeviceIdentifier()
    {
        return $"{Environment.MachineName}-{Environment.OSVersion.Platform}-WPF";
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
            // For WPF, copy to clipboard
            await Task.Run(() =>
            {
                Clipboard.SetText(text);
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
        try
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            if (allowedExtensions != null && allowedExtensions.Length > 0)
            {
                dialog.Filter = $"Files|{string.Join(";", allowedExtensions.Select(e => "*" + e))}";
            }
            
            var result = dialog.ShowDialog();
            return Task.FromResult(result == true ? dialog.FileName : null);
        }
        catch
        {
            return Task.FromResult<string?>(null);
        }
    }

    public Task<string?> ShowFolderPickerAsync()
    {
        try
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            var result = dialog.ShowDialog();
            return Task.FromResult(result == System.Windows.Forms.DialogResult.OK ? dialog.SelectedPath : null);
        }
        catch
        {
            return Task.FromResult<string?>(null);
        }
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