using System.Diagnostics;
using System.Net.Http;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;

namespace VaultGuard.Tests.Playwright;

[TestClass]
public abstract class BlazorWebTestBase : PageTest
{
    private static Process? _appProcess;
    private static string? _baseUrl;
    private static string? _tempDbPath;

    /// <summary>Base URL of the app under test (set once the app has started).</summary>
    protected static string BaseUrl => _baseUrl ?? throw new InvalidOperationException("App not started yet.");

    /// <summary>Ensures the web app is running; usable from tests that navigate before capture.</summary>
    protected static Task EnsureStartedAsync() => EnsureAppStartedAsync();
    private static readonly SemaphoreSlim AppStartLock = new(1, 1);
    private static readonly string WebProjectPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..",
        "VaultGuard.Web",
        "VaultGuard.Web.csproj"));

    private static async Task EnsureAppStartedAsync()
    {
        // Show a browser window when running locally, but let callers force headless (e.g. CI /
        // screenshot capture) by setting HEADED=0 beforehand.
        if (Environment.GetEnvironmentVariable("HEADED") is null)
            Environment.SetEnvironmentVariable("HEADED", "1");

        if (!string.IsNullOrWhiteSpace(_baseUrl))
            return;

        await AppStartLock.WaitAsync();
        try
        {
            if (!string.IsNullOrWhiteSpace(_baseUrl))
                return;

            Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });

            // Allow override from CI
            _baseUrl = Environment.GetEnvironmentVariable("PLAYWRIGHT_BASE_URL");
            if (!string.IsNullOrWhiteSpace(_baseUrl))
                return;

            _baseUrl = "http://127.0.0.1:5099";

            // Use a temp SQLite database for test isolation
            _tempDbPath = Path.Combine(Path.GetTempPath(), $"pm-playwright-{Guid.NewGuid():N}.db");

            // Pre-build the web project so startup is fast
            var buildProcess = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{WebProjectPath}\" -c Debug --no-restore -v q",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            })!;
            await buildProcess.WaitForExitAsync();

            _appProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"run --project \"{WebProjectPath}\" --no-launch-profile --no-build --urls {_baseUrl}",
                    WorkingDirectory = Path.GetDirectoryName(WebProjectPath)!,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };

            // Point the app at a temp SQLite db and disable seed/first-run prompts
            _appProcess.StartInfo.Environment["DatabaseProvider"] = "Sqlite";
            _appProcess.StartInfo.Environment["ConnectionStrings__SqliteConnection"] = $"Data Source={_tempDbPath}";
            _appProcess.StartInfo.Environment["ConnectionStrings__DefaultConnection"] = $"Data Source={_tempDbPath}";
            _appProcess.StartInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";

            _appProcess.Start();

            await WaitForAppAsync(_baseUrl, _appProcess);
        }
        finally
        {
            AppStartLock.Release();
        }
    }

    // Run browser in headed mode so tests are visible on screen.
    // Set HEADED=1 environment variable (picked up by Playwright MSTest) or rely on
    // the PLAYWRIGHT_HEADED env var. We also set it explicitly here so tests are
    // always visible when run locally without extra env config.
    public override BrowserNewContextOptions ContextOptions() => new()
    {
        IgnoreHTTPSErrors = true,
        ViewportSize = new ViewportSize { Width = 1440, Height = 900 }
    };

    [TestInitialize]
    public async Task NavigateToHomePageAsync()
    {
        await EnsureAppStartedAsync();
        await Page.GotoAsync(_baseUrl ?? throw new InvalidOperationException("Base URL not initialized."));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public static Task StopAppAsync()
    {
        try
        {
            if (_appProcess is { HasExited: false })
            {
                _appProcess.Kill(entireProcessTree: true);
                _appProcess.WaitForExit(5000);
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Best-effort app stop failed: {ex.Message}"); }

        if (_tempDbPath is not null && File.Exists(_tempDbPath))
        {
            try { File.Delete(_tempDbPath); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Failed to delete temp db: {ex.Message}"); }
        }

        // Reset so a later test class re-launches the app instead of pointing at a dead process.
        _appProcess = null;
        _baseUrl = null;
        _tempDbPath = null;

        return Task.CompletedTask;
    }

    protected async Task SaveEvidenceAsync(string name)
    {
        var dir = TestContext.TestResultsDirectory ?? Path.Combine(AppContext.BaseDirectory, "TestResults");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{name}.png");
        await Page.ScreenshotAsync(new PageScreenshotOptions { Path = path, FullPage = true });
        TestContext.WriteLine($"Screenshot: {path}");
    }

    private static async Task WaitForAppAsync(string baseUrl, Process process)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };

        for (var attempt = 0; attempt < 90; attempt++)
        {
            if (process.HasExited)
            {
                var stdOut = await process.StandardOutput.ReadToEndAsync();
                var stdErr = await process.StandardError.ReadToEndAsync();
                throw new InvalidOperationException(
                    $"VaultGuard.Web exited before tests started.{Environment.NewLine}{stdOut}{Environment.NewLine}{stdErr}");
            }

            try
            {
                using var response = await client.GetAsync(baseUrl);
                if ((int)response.StatusCode < 500)
                    return;
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"App not ready yet: {ex.Message}"); }

            await Task.Delay(1000);
        }

        throw new TimeoutException($"Timed out waiting for VaultGuard.Web at {baseUrl}.");
    }
}
