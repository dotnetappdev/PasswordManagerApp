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

            // Up to four passes (raised from two - see the bounce-back note below). Creating the master
            // key (first-run) only creates the account - it does NOT authenticate the session, so
            // MainLayout's auth gate immediately bounces the post-create NavigateTo("/home") back to
            // /login. At that point the account exists, so the next pass goes through the normal "pick
            // a profile, enter its master key" flow to actually sign in.
            for (var attempt = 0; attempt < 4; attempt++)
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

                    // Confirmed in CI: WaitForLoginRedirectAsync above can resolve cleanly (the URL
                    // genuinely leaves /login) and yet this loop's own end-of-attempt check below still
                    // finds it back on /login - MainLayout's auth gate (EnforceAuthAsync) bounces straight
                    // back if the server-side "is the vault actually unlocked" check hasn't caught up with
                    // the client-side redirect yet. Give that second bounce a moment to happen (or not)
                    // before this attempt's own check decides whether sign-in actually stuck.
                    if (!Page.Url.Contains("/login", StringComparison.OrdinalIgnoreCase))
                    {
                        await Task.Delay(1000);
                        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                    }
                }

                if (!Page.Url.Contains("/login", StringComparison.OrdinalIgnoreCase))
                    break;

                TestContext.WriteLine($"SignInAsync attempt {attempt + 1}/4: still on /login (url={Page.Url}) after the bounce-back settle wait - retrying.");
            }

            if (Page.Url.Contains("/login", StringComparison.OrdinalIgnoreCase))
                TestContext.WriteLine($"WARNING: still on /login after sign-in attempt (url={Page.Url}) - subsequent test steps will see the login page instead of the real one.");
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
