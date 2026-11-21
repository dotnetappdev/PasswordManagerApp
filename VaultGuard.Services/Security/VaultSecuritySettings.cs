using VaultGuard.Services.Interfaces;

namespace VaultGuard.Services.Security;

/// <summary>
/// The "lock and clipboard" settings every app shares: how long the vault may sit idle before it locks, and how
/// long a copied secret stays on the clipboard. Stored under the same keys in each app's settings file
/// (WPF: %LocalAppData%\VaultGuard\settings.json; Web/MAUI: <see cref="IAppSettingsService"/>).
/// </summary>
public static class VaultSecuritySettings
{
    /// <summary>Minutes of inactivity before the vault locks. 0 = never. (Key kept from the original setting.)</summary>
    public const string AutoLockMinutesKey = "SessionTimeout";
    public const int DefaultAutoLockMinutes = 15;

    /// <summary>Seconds before a copied secret is wiped from the clipboard. 0 = never.</summary>
    public const string ClipboardClearSecondsKey = "ClipboardClearSeconds";
    public const int DefaultClipboardClearSeconds = 60;

    /// <summary>Desktop: also lock when Windows locks, sleeps or the user switches account.</summary>
    public const string LockOnSystemLockKey = "LockOnSystemLock";
    public const bool DefaultLockOnSystemLock = true;

    /// <summary>Auto-lock value meaning "follow the lock timeout Windows enforces" (Windows apps only).</summary>
    public const int SameAsWindows = -1;

    /// <summary>Choices offered in the settings screens (0 = never).</summary>
    public static readonly int[] AutoLockChoices = { 1, 5, 10, 15, 30, 60, 120, 240, 0 };

    /// <summary>The auto-lock choices to show: "Same as Windows" first on Windows, and none longer than an
    /// organization's maximum (which also removes "Never").</summary>
    public static int[] AutoLockChoicesFor(bool windows, VaultGuardPolicy? policy = null)
    {
        var max = policy?.AutoLockMaxMinutes;
        var list = AutoLockChoices.Where(m => max is not { } mx || (m > 0 && m <= mx)).ToList();
        if (max is { } cap && !list.Contains(cap)) { list.Add(cap); list.Sort(); }
        if (windows) list.Insert(0, SameAsWindows);
        return list.ToArray();
    }

    public static int[] ClipboardChoicesFor(VaultGuardPolicy? policy = null)
    {
        var max = policy?.ClipboardClearMaxSeconds;
        var list = ClipboardChoices.Where(s => max is not { } mx || (s > 0 && s <= mx)).ToList();
        if (max is { } cap && !list.Contains(cap)) { list.Add(cap); list.Sort(); }
        return list.ToArray();
    }

    /// <summary>
    /// What auto-lock really does, given the user's choice, Windows' own lock timeout (for "Same as Windows") and
    /// any organization policy. Pure, so it's unit-tested; hosts pass <see cref="WindowsLockPolicy.Read"/> and
    /// <see cref="VaultGuardPolicy.Read"/>.
    /// </summary>
    public static AutoLockResolution ResolveAutoLock(int storedMinutes, WindowsLockInfo windows, VaultGuardPolicy policy)
    {
        TimeSpan? timeout;
        string why;
        if (storedMinutes == SameAsWindows)
        {
            if (windows.Timeout is { } w)
            {
                timeout = w;
                why = $"Locks after {DescribeSpan(w)} of inactivity, like Windows ({windows.Source}).";
            }
            else
            {
                timeout = TimeSpan.FromMinutes(DefaultAutoLockMinutes);
                why = $"Windows isn't set to lock after a period of inactivity, so Vault Guard locks after {DescribeAutoLock(DefaultAutoLockMinutes)}. It still locks whenever Windows locks.";
            }
        }
        else
        {
            timeout = storedMinutes <= 0 ? null : TimeSpan.FromMinutes(storedMinutes);
            why = timeout is { } t ? $"Locks after {DescribeSpan(t)} of inactivity." : "Doesn't lock on its own (it still locks whenever Windows locks, if that's on).";
        }

        if (policy.AutoLockMaxMinutes is { } max && (timeout == null || timeout > TimeSpan.FromMinutes(max)))
        {
            timeout = TimeSpan.FromMinutes(max);
            return new(timeout, $"Your organization requires Vault Guard to lock after at most {DescribeAutoLock(max)} of inactivity.", true);
        }
        return new(timeout, why, false);
    }

    public static ClipboardResolution ResolveClipboard(int storedSeconds, VaultGuardPolicy policy)
    {
        if (policy.ClipboardClearMaxSeconds is { } max && (storedSeconds <= 0 || storedSeconds > max))
            return new(max, $"Your organization requires copied values to be cleared within {DescribeClipboard(max)}.", true);
        return new(storedSeconds, storedSeconds > 0 ? $"Copied values are cleared after {DescribeClipboard(storedSeconds)}." : "Copied values stay on the clipboard.", false);
    }

    public static bool ResolveLockOnSystemLock(bool stored, VaultGuardPolicy policy) => stored || policy.ForceLockOnWindowsLock;

    private static string DescribeSpan(TimeSpan t) =>
        t.TotalSeconds < 60 ? $"{(int)t.TotalSeconds} seconds"
        : t.TotalMinutes % 1 == 0 ? DescribeAutoLock((int)t.TotalMinutes)
        : $"{t.TotalMinutes:0.#} minutes";
    public static readonly int[] ClipboardChoices = { 10, 20, 30, 60, 90, 120, 300, 0 };

    /// <summary>The stored choice: minutes, 0 = never, or <see cref="SameAsWindows"/>.</summary>
    public static int AutoLockMinutes(IAppSettingsService settings) =>
        Sanitize(settings.GetInt(AutoLockMinutesKey, DefaultAutoLockMinutes), SameAsWindows, 24 * 60, DefaultAutoLockMinutes);

    /// <summary>The auto-lock in force for this device (reads Windows and policy only when it matters).</summary>
    public static AutoLockResolution EffectiveAutoLock(IAppSettingsService settings) =>
        ResolveAutoLock(AutoLockMinutes(settings),
            AutoLockMinutes(settings) == SameAsWindows ? WindowsLockPolicy.Read() : WindowsLockInfo.None,
            VaultGuardPolicy.Read());

    public static ClipboardResolution EffectiveClipboard(IAppSettingsService settings) =>
        ResolveClipboard(ClipboardClearSeconds(settings), VaultGuardPolicy.Read());

    public static int ClipboardClearSeconds(IAppSettingsService settings) =>
        Sanitize(settings.GetInt(ClipboardClearSecondsKey, DefaultClipboardClearSeconds), 0, 3600, DefaultClipboardClearSeconds);

    public static int Sanitize(int value, int min, int max, int fallback) => value < min || value > max ? fallback : value;

    public static string DescribeAutoLock(int minutes) => minutes switch
    {
        SameAsWindows => "Same as Windows",
        0 => "Never",
        1 => "1 minute",
        < 60 => $"{minutes} minutes",
        60 => "1 hour",
        _ when minutes % 60 == 0 => $"{minutes / 60} hours",
        _ => $"{minutes} minutes"
    };

    public static string DescribeClipboard(int seconds) => seconds switch
    {
        0 => "Never",
        < 60 => $"{seconds} seconds",
        60 => "1 minute",
        _ when seconds % 60 == 0 => $"{seconds / 60} minutes",
        _ => $"{seconds} seconds"
    };
}
