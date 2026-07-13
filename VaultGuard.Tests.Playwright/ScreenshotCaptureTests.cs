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
    // The seeded default accounts' master key (see IdentityDataSeeder.CommonMasterKey).
    private const string MasterKey = "7hm3Z!Csu:Y64nm";

    // route -> output file name (matches the paths the READMEs reference).
    private static readonly (string Route, string Name)[] Pages =
    {
        ("/",                          "dashboard"),
        ("/passwords",                 "all-items"),
        ("/vaults",                    "vaults"),
        ("/collections",               "collections"),
        ("/categories",                "categories"),
        ("/tags",                      "tags"),
        ("/passwords?status=archived", "archive"),
        ("/passwords?status=deleted",  "recently-deleted"),
        ("/security",                  "security"),
        ("/passkeys",                  "passkeys"),
        ("/api-keys",                  "api-keys"),
        ("/import",                    "import"),
        ("/audit-logs",                "audit-logs"),
        ("/settings",                  "settings"),
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

        await SetThemeAsync("Dark");
        foreach (var (route, name) in Pages)
            captured += await CaptureAsync(darkDir, route, name) ? 1 : 0;
        captured += await CaptureHighContrastAsync(darkDir) ? 1 : 0;

        await SetThemeAsync("Light");
        foreach (var (route, name) in Pages)
            captured += await CaptureAsync(lightDir, route, name) ? 1 : 0;
        captured += await CaptureHighContrastAsync(lightDir) ? 1 : 0;

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
                // First-run, no seeded accounts: create the master key.
                await Page.FillAsync("#masterKey", MasterKey);
                await Page.FillAsync("#confirmKey", MasterKey);
                await Page.ClickAsync("button:has-text('Create Master Key')");
            }
            else
            {
                // Seeded accounts exist: pick the admin profile tile from the "Who's unlocking?" picker.
                var profileTile = Page.Locator(".profile-tile", new PageLocatorOptions { HasTextString = "admin@passwordmanager.local" });
                if (await profileTile.CountAsync() > 0)
                {
                    await profileTile.First.ClickAsync();
                    await Task.Delay(500);
                }

                if (await Page.Locator("#loginKey").CountAsync() > 0)
                {
                    await Page.FillAsync("#loginKey", MasterKey);
                    await Page.ClickAsync("button:has-text('Continue')");
                }
            }

            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(2500);
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Sign-in encountered an issue (continuing best-effort): {ex.Message}");
        }
    }

    // Sets the theme via Settings → Appearance → Theme (a MudSelect with Light/Dark/System options)
    // so full-page reloads honour it (ThemeService persists the choice to the shared settings.json).
    private async Task SetThemeAsync(string mode)
    {
        try
        {
            await Page.GotoAsync($"{BaseUrl}/settings");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(1000);

            // Open the "Theme" MudSelect dropdown.
            var select = Page.Locator(".mud-input-control", new PageLocatorOptions { HasTextString = "Theme" }).First;
            await select.ClickAsync();
            await Task.Delay(400);

            // Pick the matching option from the popover list (last opened popover, exact text).
            var option = Page.Locator(".mud-list-item", new PageLocatorOptions { HasTextString = mode }).Last;
            await option.ClickAsync();
            await Task.Delay(1200);
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Setting theme '{mode}' failed (continuing): {ex.Message}");
        }
    }

    // Toggles Accessibility → High contrast on, captures the dashboard, then toggles it back off
    // so it doesn't leak into subsequent captures.
    private async Task<bool> CaptureHighContrastAsync(string dir)
    {
        try
        {
            await Page.GotoAsync($"{BaseUrl}/settings");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(1000);

            var toggle = Page.Locator(".mud-switch", new PageLocatorOptions { HasTextString = "High contrast" }).First;
            await toggle.ClickAsync();
            await Task.Delay(800);

            await Page.GotoAsync($"{BaseUrl}/");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(1200);
            var path = Path.Combine(dir, "high-contrast.png");
            await Page.ScreenshotAsync(new PageScreenshotOptions { Path = path, FullPage = true });
            TestContext.WriteLine($"Captured {path}");

            // Reset so it doesn't affect the next theme pass.
            await Page.GotoAsync($"{BaseUrl}/settings");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(1000);
            var toggleOff = Page.Locator(".mud-switch", new PageLocatorOptions { HasTextString = "High contrast" }).First;
            await toggleOff.ClickAsync();
            await Task.Delay(600);

            return true;
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Failed to capture high-contrast: {ex.Message}");
            return false;
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
