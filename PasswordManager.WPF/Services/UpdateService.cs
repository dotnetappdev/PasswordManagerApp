using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;

namespace PasswordManager.WPF.Services;

public record UpdateInfo(
    string TagName,
    string Version,
    string ReleaseNotes,
    string HtmlUrl,
    string? InstallerDownloadUrl,
    string? ZipDownloadUrl,
    long? InstallerSize);

public class UpdateService
{
    private const string ApiUrl = "https://api.github.com/repos/dotnetappdev/PasswordManagerApp/releases/latest";
    private const string ReleasesUrl = "https://github.com/dotnetappdev/PasswordManagerApp/releases";

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
