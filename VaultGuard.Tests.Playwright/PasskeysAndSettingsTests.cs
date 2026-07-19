using Microsoft.Playwright;

namespace VaultGuard.Tests.Playwright;

/// <summary>
/// UI tests for the Blazor web app's Passkeys page and the deep-linkable Settings tabs
/// (Settings?tab=Name) plus the Vaults/Settings nav groups.
/// </summary>
[TestClass]
public class PasskeysAndSettingsTests : BlazorWebTestBase
{
    [ClassCleanup]
    public static Task CleanupAsync() => StopAppAsync();

    // ── Passkeys page ────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Passkeys_Page_Loads()
    {
        await Page.GotoAsync($"{BaseUrl}/passkeys");
        await Expect(Page.GetByText("Passkeys").First).ToBeVisibleAsync();
        await SaveEvidenceAsync("passkeys_page");
    }

    [TestMethod]
    public async Task Passkeys_Page_ShowsWebsitePasskeysSection()
    {
        await Page.GotoAsync($"{BaseUrl}/passkeys");
        // The password-manager-style section that lists ItemType.Passkey vault items.
        await Expect(Page.GetByText("Passkeys saved for your websites")).ToBeVisibleAsync();
        await SaveEvidenceAsync("passkeys_website_section");
    }

    [TestMethod]
    public async Task Passkeys_Page_HasRegisterAction()
    {
        await Page.GotoAsync($"{BaseUrl}/passkeys");
        // Either the empty-state CTA or the header button is present.
        var register = Page.GetByRole(AriaRole.Button, new() { Name = "Register" });
        await Expect(register.First).ToBeVisibleAsync();
    }

    // ── Settings deep-linkable tabs ──────────────────────────────────────────

    [DataTestMethod]
    [DataRow("Security")]
    [DataRow("Appearance")]
    [DataRow("Database")]
    [DataRow("Vaults")]
    [DataRow("Generator")]
    public async Task Settings_DeepLink_ActivatesRequestedTab(string tab)
    {
        await Page.GotoAsync($"{BaseUrl}/settings?tab={tab}");
        // MudBlazor marks the selected tab header with .mud-tab-active; deep-linking must select it.
        await Expect(Page.Locator(".mud-tab-active")).ToContainTextAsync(tab);
        await SaveEvidenceAsync($"settings_tab_{tab.ToLowerInvariant()}");
    }

    // ── Nav groups (Vaults / Settings expandable submenus) ───────────────────

    [TestMethod]
    public async Task Nav_SettingsGroup_ExpandsToTabLinks()
    {
        await Page.GotoAsync(BaseUrl);
        // The "Settings" nav group header expands to per-tab sub-links.
        await Page.GetByText("Settings", new() { Exact = true }).First.ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "All Settings" })).ToBeVisibleAsync();
        await SaveEvidenceAsync("nav_settings_group");
    }

    [TestMethod]
    public async Task Nav_VaultsGroup_ExpandsToAllVaultsLink()
    {
        await Page.GotoAsync(BaseUrl);
        await Page.GetByText("Vaults", new() { Exact = true }).First.ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "All Vaults" })).ToBeVisibleAsync();
        await SaveEvidenceAsync("nav_vaults_group");
    }
}
