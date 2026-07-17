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

        await SetThemeAsync("Dark Mode");
        foreach (var (route, name) in Pages)
            captured += await CaptureAsync(darkDir, route, name) ? 1 : 0;
        captured += await CaptureActionShotsAsync(darkDir) ? 1 : 0;
        captured += await CaptureApiKeyGenerationAsync(darkDir) ? 1 : 0;

        await SetThemeAsync("Light Mode");
        foreach (var (route, name) in Pages)
            captured += await CaptureAsync(lightDir, route, name) ? 1 : 0;

        // High Contrast is a fourth, standalone Theme option (not an overlay on light/dark - see the
        // Theme MudRadioGroup in Settings.razor), so there's only one meaningful capture of it. Save it
        // into both theme directories since the README/docs site galleries link both paths.
        await SetThemeAsync("High Contrast");
        var highContrastCaptured = await CaptureAsync(darkDir, "/", "high-contrast");
        if (highContrastCaptured)
            File.Copy(Path.Combine(darkDir, "high-contrast.png"), Path.Combine(lightDir, "high-contrast.png"), overwrite: true);
        captured += highContrastCaptured ? 1 : 0;

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

    // Captures the "Create API Key" flow on /api-keys: filling in a name and the one-time
    // "API Key Created Successfully" reveal dialog that shows the generated key.
    private async Task<bool> CaptureApiKeyGenerationAsync(string dir)
    {
        try
        {
            await Page.GotoAsync($"{BaseUrl}/api-keys");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(1000);

            // Like #loginKey (see SignInAsync), this MudTextField's @bind-Value commits on blur/change,
            // not "input" - FillAsync alone leaves focus in the field, so the bound name stays empty and
            // the Create button (disabled while the name is empty) never enables. Tab out to commit it.
            var nameField = Page.GetByLabel("API Key Name");
            await nameField.FillAsync("Pixel 8");
            await nameField.PressAsync("Tab");
            await Task.Delay(500);
            await Page.ClickAsync("button:has-text('Create API Key')");
            await Task.Delay(1200);

            var path = Path.Combine(dir, "dialog-api-key-generated.png");
            await Page.ScreenshotAsync(new PageScreenshotOptions { Path = path, FullPage = true });
            TestContext.WriteLine($"Captured {path}");
            return true;
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Failed to capture dialog-api-key-generated: {ex.Message}");
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

    // Sets the theme via Settings → Appearance → Theme - a MudRadioGroup with four options ("Dark
    // Mode", "Light Mode", "High Contrast", "System Default"), NOT a dropdown - so full-page reloads
    // honour it (ThemeService persists the choice to the shared settings.json). Pass the exact radio
    // label text (e.g. "Dark Mode", "Light Mode", "High Contrast").
    private async Task SetThemeAsync(string radioLabel)
    {
        try
        {
            await Page.GotoAsync($"{BaseUrl}/settings?tab=Appearance");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(1000);

            var radio = Page.Locator("label.mud-radio", new PageLocatorOptions { HasTextString = radioLabel }).First;
            await radio.ClickAsync();
            await Task.Delay(1200);
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
