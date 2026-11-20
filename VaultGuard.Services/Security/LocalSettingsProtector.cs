using System.Security.Cryptography;
using System.Text;

namespace VaultGuard.Services.Security;

/// <summary>
/// Protects the secrets kept in the machine-local settings file (%LocalAppData%\VaultGuard\settings.json):
/// API key/secret, NAS/FTP passwords, Google Drive client secret, 1Password Connect token, database password
/// and connection string. These used to be written in plain text. On Windows they are encrypted with DPAPI
/// for the current Windows user ("dpapi:" + Base64); elsewhere (a Linux web server) DPAPI is unavailable and
/// values are left as they are. Plain values written by older versions are still read, and re-encrypted on save.
/// </summary>
public static class LocalSettingsProtector
{
    private const string Prefix = "dpapi:";
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("VaultGuard.LocalSettings.v1");

    private static readonly HashSet<string> SecretKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "ApiKey", "ApiClientSecret", "FtpPassword", "NasPassword", "GDriveClientSecret",
        "OnePasswordConnectToken", "dbpassword", "DatabaseConnectionString",
    };

    public static bool IsSecret(string key) => SecretKeys.Contains(key);

    public static bool IsProtected(string? stored) => stored?.StartsWith(Prefix, StringComparison.Ordinal) == true;

    /// <summary>Value as it should be written to disk.</summary>
    public static string Protect(string key, string? value)
    {
        value ??= string.Empty;
        if (!IsSecret(key) || value.Length == 0 || IsProtected(value) || !OperatingSystem.IsWindows()) return value;
        var bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(value), Entropy, DataProtectionScope.CurrentUser);
        return Prefix + Convert.ToBase64String(bytes);
    }

    /// <summary>Value as the app should see it. Unreadable protected values (another Windows user/machine) read as empty.</summary>
    public static string Unprotect(string key, string? stored)
    {
        stored ??= string.Empty;
        if (!IsProtected(stored)) return stored;
        if (!OperatingSystem.IsWindows()) return string.Empty;
        try
        {
            var bytes = ProtectedData.Unprotect(Convert.FromBase64String(stored.Substring(Prefix.Length)), Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Warning($"Couldn't decrypt local setting {key}", ex);
            return string.Empty;
        }
    }

    /// <summary>Encrypts any secret still stored in plain text. Returns true if anything changed.</summary>
    public static bool ProtectAll(IDictionary<string, string> stored)
    {
        if (!OperatingSystem.IsWindows()) return false;
        var changed = false;
        foreach (var key in stored.Keys.ToList())
        {
            if (!IsSecret(key) || IsProtected(stored[key]) || string.IsNullOrEmpty(stored[key])) continue;
            stored[key] = Protect(key, stored[key]);
            changed = true;
        }
        return changed;
    }
}
