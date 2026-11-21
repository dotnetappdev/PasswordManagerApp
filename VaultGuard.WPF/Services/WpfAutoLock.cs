using System.Runtime.InteropServices;
using System.Windows.Threading;
using Microsoft.Win32;

namespace VaultGuard.WPF.Services;

/// <summary>
/// Locks the vault after the "Auto-lock after" setting's worth of inactivity, and (optionally) when Windows
/// locks, sleeps or switches user. Idle time is system-wide (GetLastInputInfo), so using the browser extension
/// or another app while Vault Guard sits in the tray still counts as activity — the same rule other desktop
/// password managers use.
/// </summary>
internal static class WpfAutoLock
{
    private static DispatcherTimer? _timer;
    private static Func<bool>? _isUnlocked;
    private static Action<string>? _lock;

    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO { public uint cbSize; public uint dwTime; }

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO info);

    /// <summary>How long since the last keyboard/mouse input anywhere on this Windows session.</summary>
    public static TimeSpan SystemIdleTime()
    {
        var info = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
        if (!GetLastInputInfo(ref info)) return TimeSpan.Zero;
        var idleMs = unchecked((uint)Environment.TickCount - info.dwTime);
        return TimeSpan.FromMilliseconds(idleMs);
    }

    /// <param name="isUnlocked">Whether the vault is currently unlocked.</param>
    /// <param name="lockVault">Locks it; the argument is the reason shown to the user.</param>
    public static void Start(Dispatcher dispatcher, Func<bool> isUnlocked, Action<string> lockVault)
    {
        _isUnlocked = isUnlocked;
        _lock = lockVault;
        _timer = new DispatcherTimer(TimeSpan.FromSeconds(15), DispatcherPriority.Background, (_, _) => Check(), dispatcher);
        _timer.Start();
        SystemEvents.SessionSwitch += OnSessionSwitch;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
    }

    public static void Stop()
    {
        _timer?.Stop();
        SystemEvents.SessionSwitch -= OnSessionSwitch;
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
    }

    private static void Check()
    {
        if (_isUnlocked?.Invoke() != true) return;
        var minutes = WpfSecuritySettings.AutoLockMinutes;
        if (minutes <= 0) return;
        if (SystemIdleTime() >= TimeSpan.FromMinutes(minutes))
            _lock?.Invoke($"Locked after {VaultGuard.Services.Security.VaultSecuritySettings.DescribeAutoLock(minutes)} of inactivity.");
    }

    private static void OnSessionSwitch(object? sender, SessionSwitchEventArgs e)
    {
        if (e.Reason is SessionSwitchReason.SessionLock or SessionSwitchReason.ConsoleDisconnect or SessionSwitchReason.RemoteDisconnect)
            LockForSystem("Locked because Windows was locked.");
    }

    private static void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Suspend) LockForSystem("Locked because the computer went to sleep.");
    }

    private static void LockForSystem(string reason)
    {
        if (!WpfSecuritySettings.LockOnSystemLock) return;
        _timer?.Dispatcher.BeginInvoke(() => { if (_isUnlocked?.Invoke() == true) _lock?.Invoke(reason); });
    }
}
