using System.Diagnostics;
using System.Net.Http;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;

namespace VaultGuard.Tests.Playwright;

[TestClass]
public abstract class BlazorWebTestBase : PageTest
{
    static BlazorWebTestBase()
    {
        // Every test class in this assembly shares ONE running VaultGuard.Web instance and ONE SQLite
        // db for the whole run (see EnsureAppStartedAsync below) - once SeededDemoDataTests seeds real
        // demo data (categories/collections/tags/~50 password items) for the shared signed-in account,
        // every page rendered by any class that runs afterward has meaningfully more content (nav
        // sidebar vault list, item counts, etc.) and Blazor Server round-trips get measurably slower on
        // top of an already-loaded CI runner. Playwright's default 5s assertion timeout was fine
        // against a near-empty database but started flaking on genuinely slower (not broken) renders
        // once real content existed - bumped process-wide so it isn't tuned to whichever class happens
        // to run first.
        Assertions.SetDefaultExpectTimeout(15000);
    }

    private static Process? _appProcess;
    private static string? _baseUrl;
    private static string? _tempDbPath;

    // Bounded tail of the shared VaultGuard.Web process's console output (stdout+stderr interleaved),
    // kept for failure diagnostics - see the comment on BeginOutputReadLine/BeginErrorReadLine below
    // for why this needs to be drained continuously rather than read on demand.
    private const int MaxAppLogTailLines = 400;
    private static readonly object AppLogLock = new();
    private static readonly List<string> AppLogTail = new();

    private static void AppendAppLogLine(string? line)
    {
        if (line is null) return;
        lock (AppLogLock)
        {
            AppLogTail.Add(line);
            if (AppLogTail.Count > MaxAppLogTailLines)
                AppLogTail.RemoveAt(0);
        }
    }

    /// <summary>Snapshot of the shared app process's most recent console output, for failure diagnostics.</summary>
    protected static string GetAppLogTail()
    {
        lock (AppLogLock)
            return string.Join(Environment.NewLine, AppLogTail);
    }

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

            // Pre-build the web project so startup is fast. Shared compilation (the persistent
            // VBCSCompiler/MSBuild server processes dotnet reuses across invocations to save startup
            // time) has a real failure mode: if a prior server process in this environment gets into a
            // bad state, a later build's client can block forever waiting on it - no output, no error,
            // just an indefinite hang with no way to tell it apart from a slow build. Since this is a
            // one-off test-infra build (not a hot loop where server reuse's speedup matters), disable
            // it entirely and add a hard timeout as a second line of defense so a hung build can never
            // block the whole test run - only degrade it to "used whatever was already built."
            var buildProcess = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{WebProjectPath}\" -c Debug --no-restore -v q -p:UseSharedCompilation=false",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                Environment = { ["MSBUILDDISABLENODEREUSE"] = "1" }
            })!;
            var buildExited = buildProcess.WaitForExitAsync();
            var winner = await Task.WhenAny(buildExited, Task.Delay(TimeSpan.FromMinutes(2)));
            if (winner != buildExited)
            {
                Debug.WriteLine("Pre-build of VaultGuard.Web timed out after 2 minutes (likely a wedged " +
                    "build-server process) - killing it and continuing with whatever was already built.");
                try { buildProcess.Kill(entireProcessTree: true); } catch (Exception ex) { Debug.WriteLine($"Failed to kill hung build process: {ex.Message}"); }
            }

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

            // RedirectStandardOutput/Error means the OS gives this process's stdout/stderr a small
            // fixed-size pipe buffer (~64KB on Linux) instead of a real console - nothing drains it
            // unless we read it here. Development-environment logging (EF Core "Executed DbCommand"
            // at Information level logs the full SQL text of every query) is verbose enough that,
            // across dozens of shared tests' worth of page loads before this process is torn down,
            // the buffer can fill completely. Once it does, the app's next Console write BLOCKS the
            // thread doing it - which can stall in-flight requests indefinitely and has no relation
            // to any client-side (Playwright) timeout, however generous. Wiring these up (and calling
            // BeginOutputReadLine/BeginErrorReadLine below) keeps the pipe permanently drained so the
            // app process can never block on writing to it. Keep only a bounded tail for diagnostics -
            // this is a whole app's console output across the entire shared test run.
            _appProcess.OutputDataReceived += (_, e) => AppendAppLogLine(e.Data);
            _appProcess.ErrorDataReceived += (_, e) => AppendAppLogLine(e.Data);

            _appProcess.Start();
            _appProcess.BeginOutputReadLine();
            _appProcess.BeginErrorReadLine();

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
        // Without this, the PNG sits on disk but is never attached to the test result - AddResultFile
        // is what makes it show up as evidence alongside the test outcome (trx attachment / VSTest
        // "Attachments" list), not just a stray file a CI artifact upload happens to sweep up.
        TestContext.AddResultFile(path);
        TestContext.WriteLine($"Screenshot: {path}");
    }

    /// <summary>
    /// Wraps a "page loaded" Expect(...).ToBeVisibleAsync() so a failure logs the actual page state
    /// (URL, title, a body-text snippet) plus a screenshot before rethrowing - a plain
    /// LocatorAssertions timeout only says "not found within Xms", which can't distinguish a slow
    /// render from a silent redirect (e.g. bounced back to /login) or a genuinely broken page. Use
    /// this for the top assertion in a test instead of a bare Expect(...) when the failure mode is
    /// still being diagnosed.
    /// </summary>
    protected async Task ExpectVisibleWithDiagnosticsAsync(ILocator locator, string evidenceName)
    {
        try
        {
            await Expect(locator).ToBeVisibleAsync();
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"[{evidenceName}] FAILED - Page.Url={Page.Url}");
            try { TestContext.WriteLine($"[{evidenceName}] Page title={await Page.TitleAsync()}"); }
            catch (Exception titleEx) { TestContext.WriteLine($"[{evidenceName}] Could not read title: {titleEx.Message}"); }
            try
            {
                var bodyText = await Page.Locator("body").InnerTextAsync();
                var snippet = bodyText.Length > 400 ? bodyText[..400] : bodyText;
                TestContext.WriteLine($"[{evidenceName}] Body text snippet: {snippet.ReplaceLineEndings(" | ")}");
            }
            catch (Exception bodyEx) { TestContext.WriteLine($"[{evidenceName}] Could not read body text: {bodyEx.Message}"); }
            try { await SaveEvidenceAsync($"{evidenceName}_FAILURE"); }
            catch (Exception evEx) { TestContext.WriteLine($"[{evidenceName}] Could not save failure screenshot: {evEx.Message}"); }
            // Tail of the shared app process's own console output as of this failure - e.g. an EF Core
            // exception, an unhandled exception in a Razor component, or (previously) evidence of the
            // process stalling on a full stdout pipe (see EnsureAppStartedAsync) would show up here even
            // though the browser side never sees more than a generic redirect/timeout.
            TestContext.WriteLine($"[{evidenceName}] --- VaultGuard.Web console tail ---{Environment.NewLine}{GetAppLogTail()}");
            TestContext.WriteLine($"[{evidenceName}] Original exception: {ex.Message}");
            throw;
        }
    }

    // The seeded default accounts' master key (see IdentityDataSeeder.CommonMasterKey).
    protected const string SeededMasterKey = "7hm3Z!Csu:Y64nm";

    /// <summary>
    /// Signs in with a seeded account, creating the default accounts first if this is a fresh database.
    /// Every UI test other than the pre-auth onboarding captures in ScreenshotCaptureTests needs this
    /// before navigating to any real page - without it, protected routes bounce back to /login and the
    /// test just times out waiting for content that's never going to render.
    /// </summary>
    protected async Task SignInAsync()
    {
        try
        {
            await Page.GotoAsync($"{BaseUrl}/login");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(1500);

            // Make sure the default accounts exist (button added to the login screen), best-effort.
            var createBtn = Page.Locator("button:has-text('Create default accounts')");
            if (await createBtn.CountAsync() > 0 && await createBtn.First.IsVisibleAsync())
            {
                await createBtn.First.ClickAsync();
                await Task.Delay(2500);
            }

            // Up to two passes. Creating the master key (first-run) only creates the account - it does
            // NOT authenticate the session, so MainLayout's auth gate immediately bounces the post-create
            // NavigateTo("/home") back to /login. At that point the account exists, so the second pass
            // goes through the normal "pick a profile, enter its master key" flow to actually sign in.
            for (var attempt = 0; attempt < 2; attempt++)
            {
                if (await Page.Locator("#confirmKey").CountAsync() > 0)
                {
                    await Page.FillAsync("#masterKey", SeededMasterKey);
                    await Page.FillAsync("#confirmKey", SeededMasterKey);
                    await Page.ClickAsync("button:has-text('Create Master Key')");
                    await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                    await WaitForLoginRedirectAsync();
                    continue;
                }

                // Seeded accounts exist: pick whichever profile tile the "Who's unlocking?" picker
                // rendered. All seeded accounts share SeededMasterKey (see IdentityDataSeeder).
                var profileTile = Page.Locator(".profile-tile").First;
                if (await profileTile.CountAsync() > 0)
                {
                    await profileTile.ClickAsync();
                    await Page.Locator("#loginKey").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
                }

                if (await Page.Locator("#loginKey").CountAsync() > 0)
                {
                    // #loginKey uses a plain @bind (fires on the DOM "change"/blur event, not "input") -
                    // FillAsync alone leaves focus on the field, so the bound C# value never updates and
                    // the Continue button (disabled while loginKey is empty) stays disabled forever.
                    // Tabbing out after filling blurs it, which is what actually commits the value.
                    await Page.FillAsync("#loginKey", SeededMasterKey);
                    await Page.Locator("#loginKey").PressAsync("Tab");
                    await Page.ClickAsync("button:has-text('Continue')");
                    await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                    await WaitForLoginRedirectAsync();
                }

                if (!Page.Url.Contains("/login", StringComparison.OrdinalIgnoreCase))
                    break;
            }

            if (Page.Url.Contains("/login", StringComparison.OrdinalIgnoreCase))
            {
                TestContext.WriteLine($"WARNING: still on /login after sign-in attempt (url={Page.Url}) - subsequent test steps will see the login page instead of the real one.");
                TestContext.WriteLine($"--- VaultGuard.Web console tail at sign-in failure ---{Environment.NewLine}{GetAppLogTail()}");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Sign-in encountered an issue (continuing best-effort): {ex.Message}");
        }
    }

    /// <summary>
    /// Waits for the post-login redirect away from /login, instead of a fixed delay. Master-key
    /// verification runs an intentionally slow KDF server-side (see PasswordCryptoService) - a fixed
    /// delay that "usually" covers it can silently strand the test on /login once the self-hosted
    /// dev server is under more load (e.g. late in a long shared test run with many prior requests
    /// already processed), which is exactly what a fixed delay can't adapt to. Swallows its own
    /// timeout - the caller's retry loop / final /login check already handles "still stuck" - this
    /// only replaces the blind wait with a real one when the redirect happens sooner.
    /// </summary>
    private async Task WaitForLoginRedirectAsync()
    {
        try
        {
            await Page.WaitForURLAsync(url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
                new PageWaitForURLOptions { Timeout = 15000 });
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"WaitForLoginRedirectAsync: no redirect away from /login within 15s (url={Page.Url}): {ex.Message}");
        }
    }

    private static async Task WaitForAppAsync(string baseUrl, Process process)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };

        for (var attempt = 0; attempt < 90; attempt++)
        {
            if (process.HasExited)
            {
                // Stdout/stderr are drained continuously via BeginOutputReadLine/BeginErrorReadLine
                // (see EnsureAppStartedAsync) into AppLogTail - reading process.StandardOutput/Error
                // directly here would race the async line reader against this synchronous read on the
                // same underlying stream.
                throw new InvalidOperationException(
                    $"VaultGuard.Web exited before tests started.{Environment.NewLine}{GetAppLogTail()}");
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
