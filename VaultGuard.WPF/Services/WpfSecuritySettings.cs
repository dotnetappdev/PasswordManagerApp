using VaultGuard.Services.Security;
using VaultGuard.WPF.ViewModels;

namespace VaultGuard.WPF.Services;

/// <summary>The desktop app's lock and clipboard settings, read from %LocalAppData%\VaultGuard\settings.json.</summary>
internal static class WpfSecuritySettings
{
    private static int ReadInt(string key, int fallback, int min, int max)
    {
        var settings = SettingsViewModel.LoadLocalSettings();
        return settings.TryGetValue(key, out var raw) && int.TryParse(raw, out var v)
            ? VaultSecuritySettings.Sanitize(v, min, max, fallback)
            : fallback;
    }

    public static int AutoLockMinutes =>
        ReadInt(VaultSecuritySettings.AutoLockMinutesKey, VaultSecuritySettings.DefaultAutoLockMinutes, 0, 24 * 60);

    public static int ClipboardClearSeconds =>
        ReadInt(VaultSecuritySettings.ClipboardClearSecondsKey, VaultSecuritySettings.DefaultClipboardClearSeconds, 0, 3600);

    public static bool LockOnSystemLock
    {
        get
        {
            var settings = SettingsViewModel.LoadLocalSettings();
            return settings.TryGetValue(VaultSecuritySettings.LockOnSystemLockKey, out var raw) && bool.TryParse(raw, out var b)
                ? b : VaultSecuritySettings.DefaultLockOnSystemLock;
        }
    }
}
