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

    /// <summary>Choices offered in the settings screens (0 = never).</summary>
    public static readonly int[] AutoLockChoices = { 1, 5, 10, 15, 30, 60, 120, 240, 0 };
    public static readonly int[] ClipboardChoices = { 10, 20, 30, 60, 90, 120, 300, 0 };

    public static int AutoLockMinutes(IAppSettingsService settings) =>
        Sanitize(settings.GetInt(AutoLockMinutesKey, DefaultAutoLockMinutes), 0, 24 * 60, DefaultAutoLockMinutes);

    public static int ClipboardClearSeconds(IAppSettingsService settings) =>
        Sanitize(settings.GetInt(ClipboardClearSecondsKey, DefaultClipboardClearSeconds), 0, 3600, DefaultClipboardClearSeconds);

    public static int Sanitize(int value, int min, int max, int fallback) => value < min || value > max ? fallback : value;

    public static string DescribeAutoLock(int minutes) => minutes switch
    {
        0 => "Never",
        1 => "1 minute",
        < 60 => $"{minutes} minutes",
        60 => "1 hour",
        _ => $"{minutes / 60} hours"
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
