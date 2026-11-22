using VaultGuard.Services.BrowserLink;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.App.Services;

/// <summary>
/// Browser link for the MAUI app on Windows: registers the browser helper (shipped in BrowserSupport\ by
/// build/BrowserSupport.targets) with the installed browsers on every start, and answers the extension from the
/// unlocked vault. Same server as the WPF app — see VaultGuard.Services/BrowserLink.
/// </summary>
public static class MauiBrowserLink
{
#if WINDOWS
    private static BrowserLinkServer? _server;
#endif

    public static void Start(IServiceProvider services)
    {
#if WINDOWS
        try
        {
            // The browser runs its own copy (see BrowserHelperStaging), so a running helper never locks the app's files.
            var helper = BrowserHelperStaging.Stage(Path.Combine(AppContext.BaseDirectory, "BrowserSupport", "VaultGuard.BrowserExtension.NativeHost.exe"));
            var settings = services.GetService(typeof(IAppSettingsService)) as IAppSettingsService;
            var extraIds = (settings?.Get("BrowserLink:ExtensionIds") ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var reg = BrowserLinkRegistrar.EnsureRegistered(helper, extraIds);
            if (!reg.Ok) VaultGuard.Services.Logging.AppLogger.Warning($"Browser link not registered: {reg.Error}");

            _server = new BrowserLinkServer(services, new Approver(), "Vault Guard", AppInfo.Current.VersionString);
            _server.Start();
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error("Browser link failed to start", ex); }
#endif
    }

#if WINDOWS
    private sealed class Approver : IBrowserLinkApprover
    {
        public Task<bool> ApproveAsync(string browserDescription, CancellationToken ct)
            => MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var page = Application.Current?.Windows.FirstOrDefault()?.Page;
                if (page == null) return false;
                return await page.DisplayAlert(
                    "Allow this browser to use Vault Guard?",
                    $"{browserDescription} is asking to connect to Vault Guard.\n\nIf you allow it, the Vault Guard extension in that " +
                    "browser can fill your passwords whenever Vault Guard is unlocked. Only allow this if you just clicked \"Connect\" in the extension.",
                    "Allow", "Don't allow");
            });
    }
#endif
}
