using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VaultGuard.Tests.Playwright;

/// <summary>
/// On-demand utility that regenerates the Blazor web screenshots referenced by the READMEs.
/// It boots the web app (temp SQLite DB, auto-seeded), signs in with the seeded account, then captures
/// every key page in BOTH dark and light themes, writing PNGs straight into
/// <c>screenshots/blazor/dark</c> and <c>screenshots/blazor/light</c> at the repo root.
///
/// Run it explicitly (it needs a browser + display / headless Chromium):
///   dotnet test VaultGuard.Tests.Playwright --filter FullyQualifiedName~ScreenshotCaptureTests
///
/// It is intentionally lenient — each page is captured best-effort so one flaky page doesn't abort the
/// whole run — but it fails if it couldn't capture anything (so a broken login/boot is visible).
/// </summary>
[TestClass]
public class ScreenshotCaptureTests : BlazorWebTestBase
{
    // The seeded default account's master key (see IdentityDataSeeder.CommonMasterKey).
    private const string MasterKey = "CommonMaster123!";

    // route -> output file name (matches the paths the READMEs reference).
    private static readonly (string Route, string Name)[] Pages =
    {
        ("/",           "dashboard"),
        ("/passwords",  "all-items"),
        ("/vaults",     "vaults"),
        ("/collections","collections"),
        ("/categories", "categories"),
        ("/tags",       "tags"),
        ("/security",   "security"),
        ("/passkeys",   "passkeys"),
        ("/api-keys",   "api-keys"),
        ("/import",     "import"),
        ("/audit-logs", "audit-logs"),
        ("/settings",   "settings"),
    };

    private static string ScreenshotsRoot => Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "screenshots", "blazor"));

    [TestMethod]
    public async Task CaptureAllScreenshots()
    {
        await EnsureStartedAsync();

        var darkDir = Path.Combine(ScreenshotsRoot, "dark");
        var lightDir = Path.Combine(ScreenshotsRoot, "light");
        Directory.CreateDirectory(darkDir);
        Directory.CreateDirectory(lightDir);

        await SignInAsync();

        var captured = 0;

        await SetThemeAsync("Dark Mode");
        foreach (var (route, name) in Pages)
            captured += await CaptureAsync(darkDir, route, name) ? 1 : 0;

        await SetThemeAsync("Light Mode");
        foreach (var (route, name) in Pages)
            captured += await CaptureAsync(lightDir, route, name) ? 1 : 0;

        TestContext.WriteLine($"Captured {captured} screenshot(s) into {ScreenshotsRoot}");
        Assert.IsTrue(captured > 0, "No screenshots were captured — check that the app booted and sign-in succeeded.");
    }

    private async Task SignInAsync()
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

            if (await Page.Locator("#confirmKey").CountAsync() > 0)
            {
                // First-run: create the master key.
                await Page.FillAsync("#masterKey", MasterKey);
                await Page.FillAsync("#confirmKey", MasterKey);
                await Page.ClickAsync("button:has-text('Create Master Key')");
            }
            else if (await Page.Locator("#loginKey").CountAsync() > 0)
            {
                // Returning: sign in with the master key.
                await Page.FillAsync("#loginKey", MasterKey);
                await Page.ClickAsync("button:has-text('Continue')");
            }

            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(2500);
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Sign-in encountered an issue (continuing best-effort): {ex.Message}");
        }
    }

    // Sets the theme deterministically via Settings → Appearance so full-page reloads honour it
    // (ThemeService persists the choice to the shared settings.json).
    private async Task SetThemeAsync(string radioLabel)
    {
        try
        {
            await Page.GotoAsync($"{BaseUrl}/settings");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(1500);

            var appearanceTab = Page.Locator("text=Appearance");
            if (await appearanceTab.CountAsync() > 0)
            {
                await appearanceTab.First.ClickAsync();
                await Task.Delay(600);
            }

            var radio = Page.Locator($"text={radioLabel}");
            if (await radio.CountAsync() > 0)
            {
                await radio.First.ClickAsync();
                await Task.Delay(1200);
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Setting theme '{radioLabel}' failed (continuing): {ex.Message}");
        }
    }

    private async Task<bool> CaptureAsync(string dir, string route, string name)
    {
        try
        {
            await Page.GotoAsync($"{BaseUrl}{route}");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(1200); // let MudBlazor render + theme settle
            var path = Path.Combine(dir, $"{name}.png");
            await Page.ScreenshotAsync(new PageScreenshotOptions { Path = path, FullPage = true });
            TestContext.WriteLine($"Captured {path}");
            return true;
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Failed to capture {name} ({route}): {ex.Message}");
            return false;
        }
    }
}
