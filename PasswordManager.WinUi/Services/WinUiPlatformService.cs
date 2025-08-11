using PasswordManager.Services.Interfaces;

namespace PasswordManager.WinUi.Services;

public class WinUiPlatformService : IPlatformService
{
    public string GetAppDataDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "PasswordManager");
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

    public bool IsWeb()
    {
        return false;
    }

    public async Task<bool> OpenUrlAsync(string url)
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(startInfo);
            return true;
        }
        catch
        {
            return false;
        }
    }
}