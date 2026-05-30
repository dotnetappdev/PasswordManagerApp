using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;

namespace PasswordManager.Tests.Playwright;

[TestClass]
public abstract class BlazorWebTestBase : PageTest
{
    private static Process? _appProcess;
    private static string? _baseUrl;
    private static string? _tempHomeDirectory;
    private static readonly SemaphoreSlim AppStartLock = new(1, 1);
    private static readonly string WebProjectPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..",
        "PasswordManager.Web",
        "PasswordManager.Web.csproj"));

    private static async Task EnsureAppStartedAsync()
    {
        if (!string.IsNullOrWhiteSpace(_baseUrl))
        {
            return;
        }

        await AppStartLock.WaitAsync();
        try
        {
            if (!string.IsNullOrWhiteSpace(_baseUrl))
            {
                return;
            }

            Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });

            _baseUrl = Environment.GetEnvironmentVariable("PLAYWRIGHT_BASE_URL");
            if (!string.IsNullOrWhiteSpace(_baseUrl))
            {
                return;
            }

            _baseUrl = "http://127.0.0.1:5099";
            _tempHomeDirectory = Path.Combine(Path.GetTempPath(), $"passwordmanager-playwright-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempHomeDirectory);

            var appDataDirectory = Path.Combine(_tempHomeDirectory, ".local", "share", "PasswordManager");
            Directory.CreateDirectory(appDataDirectory);

            var databasePath = Path.Combine(_tempHomeDirectory, "passwordmanager.playwright.db");
            var configPath = Path.Combine(appDataDirectory, "appsettings.json");
            var config = new
            {
                provider = 0,
                authenticationMode = 0,
                isFirstRun = false,
                sqlite = new
                {
                    databasePath
                }
            };

            await File.WriteAllTextAsync(configPath, JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true
            }));

            _appProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"run --project \"{WebProjectPath}\" --no-launch-profile --urls {_baseUrl}",
                    WorkingDirectory = Path.GetDirectoryName(WebProjectPath)!,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };

            _appProcess.StartInfo.Environment["HOME"] = _tempHomeDirectory;
            _appProcess.StartInfo.Environment["XDG_DATA_HOME"] = Path.Combine(_tempHomeDirectory, ".local", "share");
            _appProcess.Start();

            await WaitForAppAsync(_baseUrl, _appProcess);
        }
        finally
        {
            AppStartLock.Release();
        }
    }

    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
            ViewportSize = new ViewportSize
            {
                Width = 1440,
                Height = 900
            }
        };
    }

    [TestInitialize]
    public async Task NavigateToHomePageAsync()
    {
        await EnsureAppStartedAsync();
        await Page.GotoAsync(_baseUrl ?? throw new InvalidOperationException("Playwright base URL was not initialized."));
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
        catch
        {
            // Best effort cleanup
        }

        return Task.CompletedTask;
    }

    protected async Task SaveEvidenceAsync(string name)
    {
        var resultsDirectory = TestContext.TestResultsDirectory ?? Path.Combine(AppContext.BaseDirectory, "TestResults");
        Directory.CreateDirectory(resultsDirectory);

        var screenshotPath = Path.Combine(resultsDirectory, $"{name}.png");
        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = screenshotPath,
            FullPage = true
        });

        TestContext.WriteLine($"Saved screenshot: {screenshotPath}");
    }

    private static async Task WaitForAppAsync(string baseUrl, Process process)
    {
        using var client = new HttpClient();

        for (var attempt = 0; attempt < 60; attempt++)
        {
            if (process.HasExited)
            {
                var stdOut = await process.StandardOutput.ReadToEndAsync();
                var stdErr = await process.StandardError.ReadToEndAsync();
                throw new InvalidOperationException($"PasswordManager.Web exited before tests started.{Environment.NewLine}{stdOut}{Environment.NewLine}{stdErr}");
            }

            try
            {
                using var response = await client.GetAsync(baseUrl);
                if ((int)response.StatusCode < 500)
                {
                    return;
                }
            }
            catch
            {
                // Retry until the app is ready.
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException($"Timed out waiting for PasswordManager.Web to start at {baseUrl}.");
    }
}
