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

    /// <summary>The stored choice: minutes, 0 = never, or "Same as Windows" (-1).</summary>
    public static int StoredAutoLockMinutes =>
        ReadInt(VaultSecuritySettings.AutoLockMinutesKey, VaultSecuritySettings.DefaultAutoLockMinutes, VaultSecuritySettings.SameAsWindows, 24 * 60);

    /// <summary>What auto-lock actually does on this PC: the choice, Windows' own lock timeout for
    /// "Same as Windows", and any organization policy.</summary>
    public static AutoLockResolution AutoLock => Resolve(StoredAutoLockMinutes);

    public static AutoLockResolution Resolve(int storedMinutes) =>
        VaultSecuritySettings.ResolveAutoLock(storedMinutes,
            storedMinutes == VaultSecuritySettings.SameAsWindows ? WindowsLockPolicy.Read() : WindowsLockInfo.None,
            VaultGuardPolicy.Read());

    /// <summary>Seconds before copied values are wiped (0 = never), capped by organization policy.</summary>
    public static int ClipboardClearSeconds => VaultSecuritySettings.ResolveClipboard(
        ReadInt(VaultSecuritySettings.ClipboardClearSecondsKey, VaultSecuritySettings.DefaultClipboardClearSeconds, 0, 3600),
        VaultGuardPolicy.Read()).Seconds;

    public static bool LockOnSystemLock
    {
        get
        {
            var settings = SettingsViewModel.LoadLocalSettings();
            var stored = settings.TryGetValue(VaultSecuritySettings.LockOnSystemLockKey, out var raw) && bool.TryParse(raw, out var b)
                ? b : VaultSecuritySettings.DefaultLockOnSystemLock;
            return VaultSecuritySettings.ResolveLockOnSystemLock(stored, VaultGuardPolicy.Read());
        }
    }
}
