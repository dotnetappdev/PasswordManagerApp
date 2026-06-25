using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;

namespace VaultGuard.WPF.Services;

public record UpdateInfo(
    string TagName,
    string Version,
    string ReleaseNotes,
    string HtmlUrl,
    string? InstallerDownloadUrl,
    string? ZipDownloadUrl,
    long? InstallerSize,
    IReadOnlyList<string>? Features = null,
    bool RequiresRestart = false,
    string? AppName = null);

public class UpdateService
{
    private const string ApiUrl = "https://api.github.com/repos/dotnetappdev/VaultGuardApp/releases/latest";
    private const string ReleasesUrl = "https://github.com/dotnetappdev/VaultGuardApp/releases";

    /// <summary>Default JSON version manifest if none is configured in settings/appsettings.</summary>
    public const string DefaultManifestUrl =
        "https://raw.githubusercontent.com/dotnetappdev/VaultGuardApp/main/version.json";

    private readonly HttpClient _http;

    public UpdateService(IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient("GitHub");
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("VaultGuard-WPF/1.0");
    }

    public string GetCurrentVersion()
    {
        var v = Assembly.GetExecutingAssembly().GetName().Version;
        return v is null ? "1.0.0" : $"{v.Major}.{v.Minor}.{v.Build}";
    }

    public async Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken ct = default)
    {
        try
        {
            var release = await _http.GetFromJsonAsync<GitHubRelease>(ApiUrl, ct);
            if (release is null) return null;

            var remoteVersion = release.TagName.TrimStart('v');
            if (!IsNewer(remoteVersion, GetCurrentVersion())) return null;

            // Prefer the .exe installer asset; fall back to zip
            var exeAsset = release.Assets.FirstOrDefault(a =>
                a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
                a.Name.Contains("Setup", StringComparison.OrdinalIgnoreCase));
            var zipAsset = release.Assets.FirstOrDefault(a =>
                a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));

            return new UpdateInfo(
                TagName: release.TagName,
                Version: remoteVersion,
                ReleaseNotes: release.Body ?? string.Empty,
                HtmlUrl: release.HtmlUrl,
                InstallerDownloadUrl: exeAsset?.BrowserDownloadUrl,
                ZipDownloadUrl: zipAsset?.BrowserDownloadUrl,
                InstallerSize: exeAsset?.Size ?? zipAsset?.Size);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            return null;
        }
    }

    /// <summary>
    /// Checks a configurable JSON version manifest (app name, version, feature list, requiresRestart,
    /// download URL). Falls back to the GitHub releases API when no manifest URL is supplied. Returns
    /// null when already up to date or unreachable.
    /// </summary>
    public async Task<UpdateInfo?> CheckManifestAsync(string? manifestUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(manifestUrl))
            return await CheckForUpdateAsync(ct);

        try
        {
            var m = await _http.GetFromJsonAsync<UpdateManifest>(manifestUrl, ct);
            if (m is null || string.IsNullOrWhiteSpace(m.Version)) return null;

            var remoteVersion = m.Version.TrimStart('v');
            if (!IsNewer(remoteVersion, GetCurrentVersion())) return null;

            return new UpdateInfo(
                TagName: m.Version,
                Version: remoteVersion,
                ReleaseNotes: m.Notes ?? string.Empty,
                HtmlUrl: m.NotesUrl ?? ReleasesUrl,
                InstallerDownloadUrl: m.DownloadUrl,
                ZipDownloadUrl: null,
                InstallerSize: m.InstallerSize,
                Features: m.Features,
                RequiresRestart: m.RequiresRestart,
                AppName: m.AppName);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                      or OperationCanceledException or System.Text.Json.JsonException)
        {
            return null;
        }
    }

    public async Task DownloadInstallerAsync(
        string downloadUrl,
        string savePath,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        using var response = await _http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength ?? -1L;
        await using var src = await response.Content.ReadAsStreamAsync(ct);
        await using var dst = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        var buffer = new byte[81920];
        long downloaded = 0;
        int read;

        while ((read = await src.ReadAsync(buffer, ct)) > 0)
        {
            await dst.WriteAsync(buffer.AsMemory(0, read), ct);
            downloaded += read;
            if (total > 0)
                progress?.Report((int)(downloaded * 100 / total));
        }

        progress?.Report(100);
    }

    public void LaunchInstallerAndExit(string installerPath)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = installerPath,
            UseShellExecute = true
        });
        System.Windows.Application.Current.Shutdown();
    }

    public void OpenReleasesPage()
    {
        Process.Start(new ProcessStartInfo { FileName = ReleasesUrl, UseShellExecute = true });
    }

    private static bool IsNewer(string remote, string current)
    {
        if (Version.TryParse(remote, out var r) && Version.TryParse(current, out var c))
            return r > c;
        return false;
    }

    /// <summary>Schema of the JSON version manifest hosted on GitHub (raw file).</summary>
    private sealed class UpdateManifest
    {
        [JsonPropertyName("appName")] public string? AppName { get; set; }
        [JsonPropertyName("version")] public string Version { get; set; } = string.Empty;
        [JsonPropertyName("releaseDate")] public string? ReleaseDate { get; set; }
        [JsonPropertyName("features")] public List<string>? Features { get; set; }
        [JsonPropertyName("requiresRestart")] public bool RequiresRestart { get; set; }
        [JsonPropertyName("mandatory")] public bool Mandatory { get; set; }
        [JsonPropertyName("downloadUrl")] public string? DownloadUrl { get; set; }
        [JsonPropertyName("installerSize")] public long? InstallerSize { get; set; }
        [JsonPropertyName("notes")] public string? Notes { get; set; }
        [JsonPropertyName("notesUrl")] public string? NotesUrl { get; set; }
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("assets")]
        public List<GitHubAsset> Assets { get; set; } = new();
    }

    private sealed class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }
}
