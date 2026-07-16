using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VaultGuard.Tests.Playwright;

/// <summary>
/// On-demand utility that regenerates the Blazor web screenshots referenced by the READMEs and docs site.
/// It boots the web app (temp SQLite DB, auto-seeded), captures the pre-auth setup/login screens once
/// (<c>screenshots/blazor/onboarding</c>), then signs in with the seeded account and captures every key
/// page - including all 11 Settings tabs - in BOTH dark and light themes, writing PNGs straight into
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
        ("/profile",                   "profile"),
        // Settings is one page with 11 deep-linkable tabs (/settings?tab=<name>, see Settings.razor) -
        // capture each so the docs site can walk through the full settings surface, not just whichever
        // tab happens to be selected by default.
        ("/settings",                  "settings"), // defaults to the first tab (Security)
        ("/settings?tab=Appearance",   "settings-appearance"),
        ("/settings?tab=Database",     "settings-database"),
        ("/settings?tab=Sync",         "settings-sync"),
        ("/settings?tab=Notifications","settings-notifications"),
        ("/settings?tab=Vaults",       "settings-vaults"),
        ("/settings?tab=Generator",    "settings-generator"),
        ("/settings?tab=Encryption",   "settings-encryption"),
        ("/settings?tab=Shortcuts",    "settings-shortcuts"),
        ("/settings?tab=Maintenance",  "settings-maintenance"),
        ("/settings?tab=About",        "settings-about"),
    };

    // Pre-authentication pages (database setup wizard, sign-in/profile picker) - captured once, before
    // SignInAsync, since there's no theme toggle available yet on these (EmptyLayout, no nav drawer).
    private static readonly (string Route, string Name)[] OnboardingPages =
    {
        ("/setup", "database-setup"),
        ("/login", "login"),
    };

    private static string ScreenshotsRoot => Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "screenshots", "blazor"));

    [TestMethod]
    public async Task CaptureAllScreenshots()
    {
        await EnsureStartedAsync();

        var darkDir = Path.Combine(ScreenshotsRoot, "dark");
        var lightDir = Path.Combine(ScreenshotsRoot, "light");
        var onboardingDir = Path.Combine(ScreenshotsRoot, "onboarding");
        Directory.CreateDirectory(darkDir);
        Directory.CreateDirectory(lightDir);
        Directory.CreateDirectory(onboardingDir);

        var captured = 0;

        foreach (var (route, name) in OnboardingPages)
            captured += await CaptureAsync(onboardingDir, route, name) ? 1 : 0;

        await SignInAsync();

        await SetThemeAsync("Dark");
        foreach (var (route, name) in Pages)
            captured += await CaptureAsync(darkDir, route, name) ? 1 : 0;
        captured += await CaptureHighContrastAsync(darkDir) ? 1 : 0;
        captured += await CaptureActionShotsAsync(darkDir) ? 1 : 0;

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

            // Up to two passes. Creating the master key (first-run) only creates the account - it does
            // NOT authenticate the session, so MainLayout's auth gate immediately bounces the post-create
            // NavigateTo("/home") back to /login. At that point the account exists, so the second pass
            // goes through the normal "pick a profile, enter its master key" flow to actually sign in.
            for (var attempt = 0; attempt < 2; attempt++)
            {
                if (await Page.Locator("#confirmKey").CountAsync() > 0)
                {
                    await Page.FillAsync("#masterKey", MasterKey);
                    await Page.FillAsync("#confirmKey", MasterKey);
                    await Page.ClickAsync("button:has-text('Create Master Key')");
                    await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                    await Task.Delay(2000);
                    continue;
                }

                // Seeded accounts exist: pick whichever profile tile the "Who's unlocking?" picker
                // rendered. All seeded accounts share MasterKey (see IdentityDataSeeder), so which one
                // doesn't matter — this used to hardcode a match on "admin@passwordmanager.local", which
                // silently no-opped (0 tiles matched) whenever a different account rendered first, leaving
                // every subsequent capture stuck on this picker instead of the real page.
                var profileTile = Page.Locator(".profile-tile").First;
                if (await profileTile.CountAsync() > 0)
                {
                    await profileTile.ClickAsync();
                    await Task.Delay(500);
                }

                if (await Page.Locator("#loginKey").CountAsync() > 0)
                {
                    // #loginKey uses a plain @bind (fires on the DOM "change"/blur event, not "input") -
                    // FillAsync alone leaves focus on the field, so the bound C# value never updates and
                    // the Continue button (disabled while loginKey is empty) stays disabled forever.
                    // Tabbing out after filling blurs it, which is what actually commits the value.
                    await Page.FillAsync("#loginKey", MasterKey);
                    await Page.Locator("#loginKey").PressAsync("Tab");
                    await Page.ClickAsync("button:has-text('Continue')");
                    await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                    await Task.Delay(2000);
                }

                if (!Page.Url.Contains("/login", StringComparison.OrdinalIgnoreCase))
                    break;
            }

            if (Page.Url.Contains("/login", StringComparison.OrdinalIgnoreCase))
                TestContext.WriteLine($"WARNING: still on /login after sign-in attempt (url={Page.Url}) - subsequent captures will show the login page.");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Sign-in encountered an issue (continuing best-effort): {ex.Message}");
        }
    }

    // Captures a handful of key interactions (dark theme only, once per run — these are slower and
    // duplicating them per-theme isn't worth the extra CI time) so the gallery shows the app in use,
    // not just static list pages: opening the "Add item" dialog with the password generator expanded,
    // and the "New Vault" / "New Collection" creation dialogs.
    private async Task<bool> CaptureActionShotsAsync(string dir)
    {
        var any = false;
        any |= await CaptureAddItemDialogAsync(dir);
        any |= await CaptureSimpleDialogAsync(dir, "/vaults", "New Vault", "dialog-new-vault");
        any |= await CaptureSimpleDialogAsync(dir, "/collections", "New Collection", "dialog-new-collection");
        return any;
    }

    private async Task<bool> CaptureAddItemDialogAsync(string dir)
    {
        try
        {
            await Page.GotoAsync($"{BaseUrl}/passwords");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(1000);

            await Page.ClickAsync("button[title='Add item']");
            await Task.Delay(800);

            // Expand the inline generator (auto-fills a preview password) so the shot shows the
            // dynamic form engine actually doing something, not just an empty form.
            await Page.Locator(".mud-dialog-content button:has-text('Generate strong password')").ClickAsync();
            await Task.Delay(600);

            var path = Path.Combine(dir, "dialog-add-item.png");
            await Page.ScreenshotAsync(new PageScreenshotOptions { Path = path, FullPage = true });
            TestContext.WriteLine($"Captured {path}");
            return true;
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Failed to capture dialog-add-item: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> CaptureSimpleDialogAsync(string dir, string route, string buttonText, string name)
    {
        try
        {
            await Page.GotoAsync($"{BaseUrl}{route}");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(1000);

            await Page.ClickAsync($"button:has-text('{buttonText}')");
            await Task.Delay(800);

            var path = Path.Combine(dir, $"{name}.png");
            await Page.ScreenshotAsync(new PageScreenshotOptions { Path = path, FullPage = true });
            TestContext.WriteLine($"Captured {path}");
            return true;
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Failed to capture {name}: {ex.Message}");
            return false;
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
