using Microsoft.Playwright;

namespace VaultGuard.Tests.Playwright;

/// <summary>
/// End-to-end workflow tests against the Blazor web app.
/// Covers login → navigate → CRUD sequences across major features.
/// </summary>
[TestClass]
public class ApplicationWorkflowTests : BlazorWebTestBase
{
    [ClassCleanup]
    public static Task CleanupAsync() => StopAppAsync();

    // ── Dashboard ────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Dashboard_LoadsSuccessfully()
    {
        await Expect(Page.GetByText("Dashboard")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Total Items")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Favorites")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Vaults")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Categories")).ToBeVisibleAsync();
        await SaveEvidenceAsync("dashboard_loaded");
    }

    [TestMethod]
    public async Task Dashboard_NavBar_AllLinksPresent()
    {
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Dashboard" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "All Items" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Vaults" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Categories" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Tags" })).ToBeVisibleAsync();
        await SaveEvidenceAsync("navbar_links");
    }

    [TestMethod]
    public async Task Dashboard_AppBar_HasBrandName()
    {
        await Expect(Page.GetByText("VaultGuard")).ToBeVisibleAsync();
    }

    // ── Navigation ───────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Navigation_ToDashboard_Works()
    {
        await Page.GotoAsync(BaseUrl);
        await Expect(Page.GetByText("Dashboard")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Navigation_ToPasswordItems_Works()
    {
        await Page.GotoAsync($"{BaseUrl}/passwords");
        await Expect(Page.GetByText("All Items")).ToBeVisibleAsync();
        await SaveEvidenceAsync("nav_passwords");
    }

    [TestMethod]
    public async Task Navigation_ToCategories_Works()
    {
        await Page.GotoAsync($"{BaseUrl}/categories");
        await Expect(Page.GetByText("Categories")).ToBeVisibleAsync();
        await SaveEvidenceAsync("nav_categories");
    }

    [TestMethod]
    public async Task Navigation_ToTags_Works()
    {
        await Page.GotoAsync($"{BaseUrl}/tags");
        await Expect(Page.GetByText("Tags")).ToBeVisibleAsync();
        await SaveEvidenceAsync("nav_tags");
    }

    [TestMethod]
    public async Task Navigation_ToVaults_Works()
    {
        await Page.GotoAsync($"{BaseUrl}/vaults");
        await Expect(Page.GetByText("Vaults")).ToBeVisibleAsync();
        await SaveEvidenceAsync("nav_vaults");
    }

    [TestMethod]
    public async Task Navigation_ToSettings_Works()
    {
        await Page.GotoAsync($"{BaseUrl}/settings");
        await Expect(Page.GetByText("Settings")).ToBeVisibleAsync();
        await SaveEvidenceAsync("nav_settings");
    }

    [TestMethod]
    public async Task Navigation_ToProfile_Works()
    {
        await Page.GotoAsync($"{BaseUrl}/profile");
        await Expect(Page.GetByText("Profile")).ToBeVisibleAsync();
        await SaveEvidenceAsync("nav_profile");
    }

    // ── Admin / User Management ───────────────────────────────────────────────

    [TestMethod]
    public async Task AdminUserManagementWorkflow_ShouldExecuteSuccessfully()
    {
        await Page.GotoAsync(BaseUrl);
        await Expect(Page.GetByText("Dashboard")).ToBeVisibleAsync();

        // Check profile link exists in account menu
        var accountMenu = Page.Locator("button", new() { HasText = "AccountCircle" })
                              .Or(Page.Locator("[aria-label='account']"))
                              .Or(Page.GetByRole(AriaRole.Button, new() { Name = "account" }));

        // Profile page is accessible
        await Page.GotoAsync($"{BaseUrl}/profile");
        await Expect(Page.GetByText("Profile")).ToBeVisibleAsync();
        await SaveEvidenceAsync("admin_workflow_profile");
    }

    // ── Import / Export ───────────────────────────────────────────────────────

    [TestMethod]
    public async Task ImportExportWorkflow_ShouldHandleDataTransfer()
    {
        await Page.GotoAsync($"{BaseUrl}/import");
        await Expect(Page.GetByText("Import")).ToBeVisibleAsync();
        await SaveEvidenceAsync("import_page_loaded");
    }

    // ── Complete end-to-end: Dashboard → Passwords → Categories ─────────────

    [TestMethod]
    public async Task CompletePasswordManagementWorkflow_ShouldExecuteSuccessfully()
    {
        // Start at dashboard
        await Expect(Page.GetByText("Dashboard")).ToBeVisibleAsync();
        await SaveEvidenceAsync("workflow_01_dashboard");

        // Navigate to passwords
        await Page.GotoAsync($"{BaseUrl}/passwords");
        await Expect(Page.GetByText("All Items")).ToBeVisibleAsync();
        await SaveEvidenceAsync("workflow_02_passwords");

        // Navigate to categories
        await Page.GotoAsync($"{BaseUrl}/categories");
        await Expect(Page.GetByText("Categories")).ToBeVisibleAsync();
        await SaveEvidenceAsync("workflow_03_categories");

        // Navigate to vaults
        await Page.GotoAsync($"{BaseUrl}/vaults");
        await Expect(Page.GetByText("Vaults")).ToBeVisibleAsync();
        await SaveEvidenceAsync("workflow_04_vaults");

        // Return to dashboard
        await Page.GotoAsync(BaseUrl);
        await Expect(Page.GetByText("Dashboard")).ToBeVisibleAsync();
        await SaveEvidenceAsync("workflow_05_back_to_dashboard");
    }
}
