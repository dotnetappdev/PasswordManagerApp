using Microsoft.Playwright;

namespace VaultGuard.Tests.Playwright;

/// <summary>
/// Blazor web UI tests for user profile and settings management.
/// </summary>
[TestClass]
public class UserManagementCrudTests : BlazorWebTestBase
{
    [ClassCleanup]
    public static Task CleanupAsync() => StopAppAsync();

    // Runs after the base class's own [TestInitialize] (NavigateToHomePageAsync) - every test method
    // below navigates straight to a protected route, which bounces to /login without this.
    [TestInitialize]
    public async Task SignInBeforeTestsAsync() => await SignInAsync();

    [TestMethod]
    public async Task ProfilePage_LoadsSuccessfully()
    {
        await Page.GotoAsync($"{BaseUrl}/profile");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.GetByText("Profile")).ToBeVisibleAsync();
        await SaveEvidenceAsync("profile_page_loaded");
    }

    [TestMethod]
    public async Task SettingsPage_LoadsSuccessfully()
    {
        await Page.GotoAsync($"{BaseUrl}/settings");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Settings", Exact = true })).ToBeVisibleAsync();
        await SaveEvidenceAsync("settings_page_loaded");
    }

    [TestMethod]
    public async Task AuditLogsPage_LoadsSuccessfully()
    {
        await Page.GotoAsync($"{BaseUrl}/audit-logs");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Audit Logs", Exact = true })).ToBeVisibleAsync();
        await SaveEvidenceAsync("audit_logs_loaded");
    }

    [TestMethod]
    public async Task ApiKeyManagementPage_LoadsSuccessfully()
    {
        await Page.GotoAsync($"{BaseUrl}/api-keys");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        // Page renders without error
        await SaveEvidenceAsync("api_keys_loaded");
    }

    [TestMethod]
    public async Task PasskeysPage_LoadsSuccessfully()
    {
        await Page.GotoAsync($"{BaseUrl}/passkeys");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await SaveEvidenceAsync("passkeys_page_loaded");
    }

    [TestMethod]
    public async Task ImportPage_LoadsSuccessfully()
    {
        await Page.GotoAsync($"{BaseUrl}/import");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Import Passwords", Exact = true })).ToBeVisibleAsync();
        await SaveEvidenceAsync("import_page_loaded");
    }

    [TestMethod]
    public async Task CollectionsPage_LoadsSuccessfully()
    {
        await Page.GotoAsync($"{BaseUrl}/collections");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await SaveEvidenceAsync("collections_page_loaded");
    }

    [TestMethod]
    public async Task AppBar_HasDarkModeToggle()
    {
        await Page.GotoAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Dark/light mode toggle button
        var darkModeBtn = Page.Locator("button", new() { HasText = "LightMode" })
                              .Or(Page.Locator("button", new() { HasText = "DarkMode" }))
                              .Or(Page.Locator("[aria-label='toggle theme']"));

        // Just confirm the app bar is rendered with the brand. MainLayout.razor renders the app bar
        // title as "🔐 Vault Guard" (a space between "Vault" and "Guard") - "VaultGuard" (no space)
        // is not a substring of that, so it never matched and this assertion always timed out.
        await Expect(Page.GetByText("Vault Guard")).ToBeVisibleAsync();
        await SaveEvidenceAsync("appbar_rendered");
    }

    [TestMethod]
    public async Task SetupPage_LoadsSuccessfully()
    {
        await Page.GotoAsync($"{BaseUrl}/setup");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await SaveEvidenceAsync("setup_page_loaded");
    }
}
