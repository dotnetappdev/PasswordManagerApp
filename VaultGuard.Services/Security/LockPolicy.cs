using System.Runtime.InteropServices;

namespace VaultGuard.Services.Security;

/// <summary>How long Windows itself waits before locking this PC, and why.</summary>
/// <param name="Timeout">Null when Windows isn't set to lock on its own after a period of inactivity.</param>
/// <param name="Source">Human-readable reason, e.g. "your organization's Windows policy".</param>
public sealed record WindowsLockInfo(TimeSpan? Timeout, string Source)
{
    public static readonly WindowsLockInfo None = new(null, "");
}

/// <summary>
/// Reads the inactivity lock Windows enforces, so Vault Guard can follow it ("Same as Windows"):
/// <list type="bullet">
/// <item>Group Policy "Interactive logon: Machine inactivity limit"
/// (HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System\InactivityTimeoutSecs);</item>
/// <item>a password-protected screen saver ("On resume, display logon screen") and its wait time — the effective
/// values, including any Group Policy override, as Windows reports them through SystemParametersInfo.</item>
/// </list>
/// When both apply, the shorter wins (that's when Windows actually locks). Locks Windows performs for other
/// reasons (Win+L, sleep, "require sign-in after display off", Dynamic Lock) are caught separately by the
/// lock-when-Windows-locks event.
/// </summary>
public static class WindowsLockPolicy
{
    private const uint SPI_GETSCREENSAVETIMEOUT = 0x000E;
    private const uint SPI_GETSCREENSAVEACTIVE = 0x0010;
    private const uint SPI_GETSCREENSAVESECURE = 0x0076;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint action, uint param, out int value, uint winIni);

    public static WindowsLockInfo Read()
    {
        if (!OperatingSystem.IsWindows()) return WindowsLockInfo.None;
        try
        {
            TimeSpan? policy = null;
            using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
            {
                if (key?.GetValue("InactivityTimeoutSecs") is int secs && secs > 0) policy = TimeSpan.FromSeconds(secs);
            }

            TimeSpan? screenSaver = null;
            if (SystemParametersInfo(SPI_GETSCREENSAVEACTIVE, 0, out var active, 0) && active != 0
                && SystemParametersInfo(SPI_GETSCREENSAVESECURE, 0, out var secure, 0) && secure != 0
                && SystemParametersInfo(SPI_GETSCREENSAVETIMEOUT, 0, out var wait, 0) && wait > 0)
                screenSaver = TimeSpan.FromSeconds(wait);

            return Combine(policy, screenSaver);
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Warning("Couldn't read the Windows lock settings", ex);
            return WindowsLockInfo.None;
        }
    }

    /// <summary>Which of the two Windows locks happens first (pure, for tests).</summary>
    public static WindowsLockInfo Combine(TimeSpan? machineInactivityLimit, TimeSpan? secureScreenSaver)
    {
        if (machineInactivityLimit is { } p && (secureScreenSaver is not { } s0 || p <= s0))
            return new(p, "your organization's Windows inactivity policy");
        if (secureScreenSaver is { } s)
            return new(s, "the Windows screen saver lock");
        return WindowsLockInfo.None;
    }
}

/// <summary>
/// Settings an administrator enforces for Vault Guard through Group Policy (see docs/POLICIES.md and the ADMX
/// template in deploy/policies). Machine policy (HKLM) wins over user policy (HKCU):
/// SOFTWARE\Policies\VaultGuard — AutoLockMaxMinutes, ClipboardClearMaxSeconds (DWORD, 1+ = the longest
/// allowed; "never" is then not allowed) and LockOnWindowsLock (DWORD 1 = always lock when Windows locks).
/// </summary>
public sealed record VaultGuardPolicy(int? AutoLockMaxMinutes, int? ClipboardClearMaxSeconds, bool ForceLockOnWindowsLock)
{
    public static readonly VaultGuardPolicy None = new(null, null, false);

    public const string RegistryPath = @"SOFTWARE\Policies\VaultGuard";

    public bool IsEmpty => AutoLockMaxMinutes == null && ClipboardClearMaxSeconds == null && !ForceLockOnWindowsLock;

    public static VaultGuardPolicy Read()
    {
        if (!OperatingSystem.IsWindows()) return None;
        try
        {
            using var hklm = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(RegistryPath);
            using var hkcu = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RegistryPath);
            int? Dword(string name, int max)
            {
                var v = hklm?.GetValue(name) ?? hkcu?.GetValue(name);
                return v is int i && i > 0 ? Math.Min(i, max) : null;
            }
            var force = (hklm?.GetValue("LockOnWindowsLock") ?? hkcu?.GetValue("LockOnWindowsLock")) is int f && f != 0;
            return new VaultGuardPolicy(Dword("AutoLockMaxMinutes", 24 * 60), Dword("ClipboardClearMaxSeconds", 3600), force);
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Warning("Couldn't read Vault Guard policy", ex);
            return None;
        }
    }
}

/// <summary>The auto-lock that actually applies, and a sentence explaining it for the settings screen.</summary>
public sealed record AutoLockResolution(TimeSpan? Timeout, string Explanation, bool LimitedByPolicy);

/// <summary>The clipboard wipe that actually applies (0 = never).</summary>
public sealed record ClipboardResolution(int Seconds, string Explanation, bool LimitedByPolicy);
