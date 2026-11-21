using System.Windows;
using VaultGuard.WPF.Dialogs;
using VaultGuard.WPF.ViewModels;

namespace VaultGuard.WPF.Services;

/// <summary>
/// Owns the Quick Access shortcut: registers it at startup (and again when Settings changes it), and opens one
/// <see cref="QuickAccessWindow"/> at a time.
/// </summary>
internal static class QuickAccess
{
    public const string SettingKey = "QuickAccessHotkey";
    public const string Default = "Ctrl+Shift+Space";
    public const string Off = "Off";

    private static GlobalHotkey? _hotkey;
    private static QuickAccessWindow? _open;
    private static IServiceProvider? _services;
    private static Func<MainWindow?>? _main;
    private static Action? _showMain;

    /// <summary>The shortcut from settings (or "Off").</summary>
    public static string Current
    {
        get
        {
            var settings = SettingsViewModel.LoadLocalSettings();
            return settings.TryGetValue(SettingKey, out var v) && (v == Off || GlobalHotkey.Choices.Any(c => c.Name == v)) ? v : Default;
        }
    }

    /// <returns>An error message when the shortcut is taken by another app.</returns>
    public static string? Start(IServiceProvider services, Func<MainWindow?> mainWindow, Action showMainWindow)
    {
        _services = services;
        _main = mainWindow;
        _showMain = showMainWindow;
        _hotkey ??= new GlobalHotkey(Show);
        return Apply();
    }

    /// <summary>(Re)registers the shortcut from settings. Returns an error message if Windows refused it.</summary>
    public static string? Apply()
    {
        if (_hotkey == null) return null;
        var name = Current;
        if (name == Off) { _hotkey.Unregister(); return null; }
        var choice = GlobalHotkey.Choices.First(c => c.Name == name);
        if (_hotkey.Register(choice.Mods, choice.Key)) return null;
        var msg = $"{name} is already used by another app, so Quick Access can't use it. Choose another shortcut in Settings → Security.";
        VaultGuard.Services.Logging.AppLogger.Warning(msg);
        return msg;
    }

    public static void Show()
    {
        if (_services == null) return;
        if (_open != null) { _open.Activate(); return; }
        _open = new QuickAccessWindow(_services,
            isUnlocked: () => _main?.Invoke()?.IsVaultUnlocked == true,
            openInApp: id =>
            {
                _showMain?.Invoke();
                if (id is { } itemId) _main?.Invoke()?.OpenItem(itemId);
            });
        _open.Closed += (_, _) => _open = null;
        _open.Show();
        _open.Activate();
    }

    public static void Stop()
    {
        _hotkey?.Dispose();
        _hotkey = null;
    }
}
