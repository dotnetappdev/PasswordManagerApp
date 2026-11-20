using System.Text.Json;

namespace VaultGuard.Services.BrowserLink;

/// <summary>
/// Registers the browser extension's native-messaging host with every supported browser, for the current
/// user only (no admin rights). The desktop app calls this on every start, the way 1Password keeps its
/// browser integration current: after an update, a move or a reinstall the registration points at the new
/// helper automatically, instead of relying on a one-off admin installer script.
///
/// Windows: manifests under %LocalAppData%\VaultGuard\BrowserLink plus HKCU registry keys for Chrome, Edge,
/// Brave, Chromium, Vivaldi and Firefox. macOS/Linux: the browsers' per-user NativeMessagingHosts folders.
/// </summary>
public static class BrowserLinkRegistrar
{
    public const string HostName = "com.passwordmanager.native_host";

    /// <summary>
    /// The extension's stable id — derived from the public "key" in VaultGuard.BrowserExtension/manifest.json,
    /// so an unpacked/side-loaded copy always has this id. Store-published ids can be added via settings.
    /// </summary>
    public const string ChromiumExtensionId = "niapolneagndnohemlljncmndahkoncp";

    /// <summary>The Firefox add-on id (browser_specific_settings.gecko.id in the manifest).</summary>
    public const string FirefoxExtensionId = "vaultguard@dotnetappdevni.com";

    public sealed record Result(bool Ok, string HostPath, IReadOnlyList<string> Browsers, string? Error = null);

    private static readonly (string Name, string RegistryKey, string MacDir, string LinuxDir)[] Chromium =
    {
        ("Chrome",   @"Software\Google\Chrome\NativeMessagingHosts",         "Google/Chrome",                   "google-chrome"),
        ("Edge",     @"Software\Microsoft\Edge\NativeMessagingHosts",        "Microsoft Edge",                  "microsoft-edge"),
        ("Brave",    @"Software\BraveSoftware\Brave-Browser\NativeMessagingHosts", "BraveSoftware/Brave-Browser", "BraveSoftware/Brave-Browser"),
        ("Chromium", @"Software\Chromium\NativeMessagingHosts",              "Chromium",                        "chromium"),
        ("Vivaldi",  @"Software\Vivaldi\NativeMessagingHosts",               "Vivaldi",                         "vivaldi"),
    };

    /// <param name="hostPath">Full path to the native-messaging host executable shipped with the app.</param>
    /// <param name="extraExtensionIds">Additional Chromium extension ids (e.g. Chrome Web Store / Edge Add-ons builds).</param>
    public static Result EnsureRegistered(string hostPath, IEnumerable<string>? extraExtensionIds = null, string? homeOverride = null)
    {
        try
        {
            if (!File.Exists(hostPath))
                return new Result(false, hostPath, Array.Empty<string>(), "The browser helper isn't installed next to the app.");

            var ids = new[] { ChromiumExtensionId }.Concat(extraExtensionIds ?? Enumerable.Empty<string>())
                .Select(i => i.Trim()).Where(i => i.Length == 32 && i.All(c => c is >= 'a' and <= 'p')).Distinct().ToArray();

            var chromiumManifest = new
            {
                name = HostName,
                description = "Vault Guard browser link",
                path = hostPath,
                type = "stdio",
                allowed_origins = ids.Select(i => $"chrome-extension://{i}/").ToArray()
            };
            var firefoxManifest = new
            {
                name = HostName,
                description = "Vault Guard browser link",
                path = hostPath,
                type = "stdio",
                allowed_extensions = new[] { FirefoxExtensionId }
            };

            var done = new List<string>();
            if (OperatingSystem.IsWindows())
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VaultGuard", "BrowserLink");
                var chromiumFile = WriteIfChanged(Path.Combine(dir, HostName + ".json"), chromiumManifest);
                var firefoxFile = WriteIfChanged(Path.Combine(dir, HostName + ".firefox.json"), firefoxManifest);
                foreach (var b in Chromium) { SetRegistryDefault(b.RegistryKey + "\\" + HostName, chromiumFile); done.Add(b.Name); }
                SetRegistryDefault(@"Software\Mozilla\NativeMessagingHosts\" + HostName, firefoxFile);
                done.Add("Firefox");
            }
            else
            {
                var home = homeOverride ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var mac = OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst();
                foreach (var b in Chromium)
                {
                    var dir = mac ? Path.Combine(home, "Library", "Application Support", b.MacDir, "NativeMessagingHosts")
                                  : Path.Combine(home, ".config", b.LinuxDir, "NativeMessagingHosts");
                    WriteIfChanged(Path.Combine(dir, HostName + ".json"), chromiumManifest);
                    done.Add(b.Name);
                }
                var ffDir = mac ? Path.Combine(home, "Library", "Application Support", "Mozilla", "NativeMessagingHosts")
                                : Path.Combine(home, ".mozilla", "native-messaging-hosts");
                WriteIfChanged(Path.Combine(ffDir, HostName + ".json"), firefoxManifest);
                done.Add("Firefox");
            }
            return new Result(true, hostPath, done);
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Warning("Browser link registration failed", ex);
            return new Result(false, hostPath, Array.Empty<string>(), ex.Message);
        }
    }

    private static string WriteIfChanged(string path, object manifest)
    {
        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (!File.Exists(path) || File.ReadAllText(path) != json) File.WriteAllText(path, json);
        return path;
    }

    private static void SetRegistryDefault(string subKey, string value)
    {
        if (!OperatingSystem.IsWindows()) return;
        using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(subKey);
        if (key.GetValue(null) as string != value) key.SetValue(null, value);
    }
}
