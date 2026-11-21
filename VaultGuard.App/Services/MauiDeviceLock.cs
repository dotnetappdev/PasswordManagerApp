using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Security;

namespace VaultGuard.App.Services;

/// <summary>
/// Locks Vault Guard when the device locks, if "Lock when this device locks" is on (it is by default, and an
/// organization can require it):
/// <list type="bullet">
/// <item>Windows: when Windows is locked (Win+L, Ctrl+Alt+Del → Lock, screen saver lock), the PC sleeps, or the
/// user switches account / disconnects a remote session.</item>
/// <item>Android / iOS: when the app goes to the background or the screen turns off.</item>
/// </list>
/// The vault key is wiped immediately, here, so it's gone even if the UI is suspended; the vault page then shows
/// the sign-in screen ("locked because this device was locked").
/// </summary>
public static class MauiDeviceLock
{
    private static IServiceProvider? _services;

    public static void Start(IServiceProvider services)
    {
        _services = services;
#if WINDOWS
        Microsoft.Win32.SystemEvents.SessionSwitch += (_, e) =>
        {
            if (e.Reason is Microsoft.Win32.SessionSwitchReason.SessionLock
                or Microsoft.Win32.SessionSwitchReason.ConsoleDisconnect
                or Microsoft.Win32.SessionSwitchReason.RemoteDisconnect)
                Lock();
        };
        Microsoft.Win32.SystemEvents.PowerModeChanged += (_, e) =>
        {
            if (e.Mode == Microsoft.Win32.PowerModes.Suspend) Lock();
        };
#endif
    }

    /// <summary>Phones: called by the app window when it stops being visible (background / screen off).</summary>
    public static void OnAppBackgrounded()
    {
#if ANDROID || IOS
        Lock();
#endif
    }

    public static bool Enabled
    {
        get
        {
            var settings = _services?.GetService(typeof(IAppSettingsService)) as IAppSettingsService;
            var stored = settings?.GetBool(VaultSecuritySettings.LockOnSystemLockKey, VaultSecuritySettings.DefaultLockOnSystemLock)
                         ?? VaultSecuritySettings.DefaultLockOnSystemLock;
            return VaultSecuritySettings.ResolveLockOnSystemLock(stored, VaultGuardPolicy.Read());
        }
    }

    private static void Lock()
    {
        if (_services == null || !Enabled) return;
        try
        {
            // Wipe the key now (no UI or JavaScript needed), then let the UI show the sign-in page.
            var sessions = _services.GetService(typeof(IVaultSessionService)) as IVaultSessionService;
            var active = sessions?.GetActiveSessionId();
            if (sessions != null && !string.IsNullOrEmpty(active)) sessions.ClearSession(active);
            (_services.GetService(typeof(VaultLockSignal)) as VaultLockSignal)?.RequestLock(VaultLockSignal.DeviceLocked);
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Couldn't lock when the device locked", ex); }
    }
}
