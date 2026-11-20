using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using VaultGuard.Services.BrowserLink;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.WPF.Services;

/// <summary>
/// Starts the browser link for the WPF app: registers the browser helper with every installed browser (on each
/// start, so updates keep it current) and answers the extension from the unlocked vault. See
/// VaultGuard.Services/BrowserLink.
/// </summary>
public static class WpfBrowserLink
{
    private static BrowserLinkServer? _server;

    public static void Start(IServiceProvider services, Func<Window?> owner)
    {
        try
        {
            var version = services.GetService<UpdateService>()?.GetCurrentVersion() ?? "1.0.0";
            var helper = Path.Combine(AppContext.BaseDirectory, "BrowserSupport", "VaultGuard.BrowserExtension.NativeHost.exe");
            var extraIds = (services.GetService<IAppSettingsService>()?.Get("BrowserLink:ExtensionIds") ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var reg = BrowserLinkRegistrar.EnsureRegistered(helper, extraIds);
            if (!reg.Ok) VaultGuard.Services.Logging.AppLogger.Warning($"Browser link not registered: {reg.Error}");

            _server = new BrowserLinkServer(services, new Approver(owner), "Vault Guard for Windows", version);
            _server.Start();
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error("Browser link failed to start", ex); }
    }

    public static async Task StopAsync()
    {
        if (_server != null) await _server.DisposeAsync();
        _server = null;
    }

    /// <summary>Forgets every approved browser (they must be approved again).</summary>
    public static void DisconnectAllBrowsers() => _server?.Pairings.RevokeAll();

    private sealed class Approver : IBrowserLinkApprover
    {
        private readonly Func<Window?> _owner;
        public Approver(Func<Window?> owner) => _owner = owner;

        public async Task<bool> ApproveAsync(string browserDescription, CancellationToken ct)
        {
            var app = Application.Current;
            if (app == null) return false;
            return await app.Dispatcher.InvokeAsync(async () =>
            {
                var window = _owner();
                if (window != null)
                {
                    if (window.WindowState == WindowState.Minimized) window.WindowState = WindowState.Normal;
                    window.Activate();
                }
                var dialog = new ModernWpf.Controls.ContentDialog
                {
                    Title = "Allow this browser to use Vault Guard?",
                    Content = new TextBlock
                    {
                        TextWrapping = TextWrapping.Wrap, MaxWidth = 460,
                        Text = $"{browserDescription} is asking to connect to Vault Guard.\n\n" +
                               "If you allow it, the Vault Guard extension in that browser can fill your passwords whenever " +
                               "Vault Guard is unlocked. Only allow this if you just clicked \"Connect\" in the extension."
                    },
                    PrimaryButtonText = "Allow",
                    CloseButtonText = "Don't allow",
                    DefaultButton = ModernWpf.Controls.ContentDialogButton.Close
                };
                try { dialog.Style = dialog.TryFindResource("Modern1PasswordDialogStyle") as Style; } catch { }
                using var reg = ct.Register(() => app.Dispatcher.Invoke(dialog.Hide));
                return await Helpers.DialogManager.ShowAsync(dialog) == ModernWpf.Controls.ContentDialogResult.Primary && !ct.IsCancellationRequested;
            }).Task.Unwrap();
        }
    }
}
