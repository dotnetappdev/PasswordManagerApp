using System.Windows;
using System.Windows.Controls;
using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Ssh;

namespace VaultGuard.WPF.Services;

/// <summary>
/// Vault Guard as the SSH agent for this Windows user (Settings → SSH agent). Off by default. See
/// VaultGuard.Services/Ssh/SshAgentServer for the protocol, the pipe it uses and what it serves.
/// </summary>
public static class WpfSshAgent
{
    private static SshAgentServer? _server;

    public static bool IsRunning => _server?.PipeName != null;
    public static string? SocketPath => _server?.SocketPath;

    /// <summary>Starts or stops the agent to match the "SshAgent:Enabled" setting.</summary>
    public static async Task ApplyAsync(IServiceProvider services, Func<Window?> owner)
    {
        var enabled = (services.GetService(typeof(IAppSettingsService)) as IAppSettingsService)?.GetBool("SshAgent:Enabled") == true;
        if (enabled && _server == null)
        {
            try
            {
                _server = new SshAgentServer(services, new Approver(owner));
                _server.Start();
                if (_server.PipeName == null) { await _server.DisposeAsync(); _server = null; }
            }
            catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error("SSH agent failed to start", ex); _server = null; }
        }
        else if (!enabled && _server != null)
        {
            await _server.DisposeAsync();
            _server = null;
        }
    }

    public static async Task StopAsync()
    {
        if (_server != null) await _server.DisposeAsync();
        _server = null;
    }

    private sealed class Approver : ISshApprover
    {
        private readonly Func<Window?> _owner;
        public Approver(Func<Window?> owner) => _owner = owner;

        public async Task<bool> ApproveAsync(string keyTitle, CancellationToken ct)
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
                    Title = "Use an SSH key?",
                    Content = new TextBlock
                    {
                        TextWrapping = TextWrapping.Wrap, MaxWidth = 460,
                        Text = $"An app on this computer (for example ssh or git) wants to sign in with your SSH key \"{keyTitle}\".\n\n" +
                               "Allow it only if you just started something that uses SSH. Allowing covers this key for one minute."
                    },
                    PrimaryButtonText = "Allow",
                    CloseButtonText = "Deny",
                    DefaultButton = ModernWpf.Controls.ContentDialogButton.Close
                };
                using var reg = ct.Register(() => app.Dispatcher.Invoke(dialog.Hide));
                try { return await dialog.ShowAsync() == ModernWpf.Controls.ContentDialogResult.Primary; }
                catch (InvalidOperationException) { return false; } // another dialog is open
            }).Task.Unwrap();
        }
    }
}
