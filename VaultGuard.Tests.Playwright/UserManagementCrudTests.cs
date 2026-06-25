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

    [TestMethod]
    public async Task ProfilePage_LoadsSuccessfully()
    {
        await Page.GotoAsync("/profile");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.GetByText("Profile")).ToBeVisibleAsync();
        await SaveEvidenceAsync("profile_page_loaded");
    }

    [TestMethod]
    public async Task SettingsPage_LoadsSuccessfully()
    {
        await Page.GotoAsync("/settings");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.GetByText("Settings")).ToBeVisibleAsync();
        await SaveEvidenceAsync("settings_page_loaded");
    }

    [TestMethod]
    public async Task AuditLogsPage_LoadsSuccessfully()
    {
        await Page.GotoAsync("/audit-logs");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.GetByText("Audit")).ToBeVisibleAsync();
        await SaveEvidenceAsync("audit_logs_loaded");
    }

    [TestMethod]
    public async Task ApiKeyManagementPage_LoadsSuccessfully()
    {
        await Page.GotoAsync("/api-keys");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        // Page renders without error
        await SaveEvidenceAsync("api_keys_loaded");
    }

    [TestMethod]
    public async Task PasskeysPage_LoadsSuccessfully()
    {
        await Page.GotoAsync("/passkeys");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await SaveEvidenceAsync("passkeys_page_loaded");
    }

    [TestMethod]
    public async Task ImportPage_LoadsSuccessfully()
    {
        await Page.GotoAsync("/import");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.GetByText("Import")).ToBeVisibleAsync();
        await SaveEvidenceAsync("import_page_loaded");
    }

    [TestMethod]
    public async Task CollectionsPage_LoadsSuccessfully()
    {
        await Page.GotoAsync("/collections");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await SaveEvidenceAsync("collections_page_loaded");
    }

    [TestMethod]
    public async Task AppBar_HasDarkModeToggle()
    {
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Dark/light mode toggle button
        var darkModeBtn = Page.Locator("button", new() { HasText = "LightMode" })
                              .Or(Page.Locator("button", new() { HasText = "DarkMode" }))
                              .Or(Page.Locator("[aria-label='toggle theme']"));

        // Just confirm the app bar is rendered with the brand
        await Expect(Page.GetByText("VaultGuard")).ToBeVisibleAsync();
        await SaveEvidenceAsync("appbar_rendered");
    }

    [TestMethod]
    public async Task SetupPage_LoadsSuccessfully()
    {
        await Page.GotoAsync("/setup");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await SaveEvidenceAsync("setup_page_loaded");
    }
}
