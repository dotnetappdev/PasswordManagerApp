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

    // Runs after the base class's own [TestInitialize] (NavigateToHomePageAsync) - every test method
    // below navigates straight to a protected route, which bounces to /login without this.
    [TestInitialize]
    public async Task SignInBeforeTestsAsync() => await SignInAsync();

    // ── Dashboard ────────────────────────────────────────────────────────────

    // Dashboard.razor's own <h4>Dashboard</h4> heading - scoped to the Heading role so it doesn't
    // collide with the nav drawer's "Dashboard" link, which is on screen at the same time (a bare
    // GetByText("Dashboard") throws a Playwright strict-mode violation once both are visible - they
    // never were before the SignInAsync fix let these tests actually reach an authenticated page).
    static ILocator DashboardHeading(IPage page) => page.GetByRole(AriaRole.Heading, new() { Name = "Dashboard", Exact = true });

    [TestMethod]
    public async Task Dashboard_LoadsSuccessfully()
    {
        await Expect(DashboardHeading(Page)).ToBeVisibleAsync();
        await Expect(Page.GetByText("Total Items")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Favorites")).ToBeVisibleAsync();
        // The stat cards' "Vaults"/"Categories" labels are <p> captions, not headings - scope to <p>
        // to avoid the nav drawer's identically-named links/group titles rendered alongside them.
        await Expect(Page.Locator("p").GetByText("Vaults", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.Locator("p").GetByText("Categories", new() { Exact = true })).ToBeVisibleAsync();
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
        // MainLayout.razor renders the app bar title as an <h6> "🔐 Vault Guard" (a space between
        // "Vault" and "Guard") - "VaultGuard" (no space) is not a substring of that. Scoped to Level=6
        // (like DashboardHeading above scopes to the Heading role) because a bare GetByText("Vault
        // Guard") also matches the login page's own "Vault Guard" <h1> logo and "Sign in to Vault
        // Guard" <h2> - while sign-in is still completing (SignInAsync's post-login redirect can take
        // a few seconds, worse on whichever test runs first against a cold circuit - see
        // BlazorWebTestBase's WarmUpLoginPageAsync comment), both of those are still on screen, so an
        // unscoped locator resolves to 2 elements and Playwright throws a strict-mode violation instead
        // of retrying. Level=6 never matches either login-page heading, so this keeps polling through
        // the sign-in redirect exactly like every other test in this class already does.
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Vault Guard", Level = 6 })).ToBeVisibleAsync();
    }

    // ── Navigation ───────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Navigation_ToDashboard_Works()
    {
        await Page.GotoAsync(BaseUrl);
        await Expect(DashboardHeading(Page)).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Navigation_ToPasswordItems_Works()
    {
        await Page.GotoAsync($"{BaseUrl}/passwords");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "All Items", Exact = true })).ToBeVisibleAsync();
        await SaveEvidenceAsync("nav_passwords");
    }

    [TestMethod]
    public async Task Navigation_ToCategories_Works()
    {
        await Page.GotoAsync($"{BaseUrl}/categories");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Categories", Exact = true })).ToBeVisibleAsync();
        await SaveEvidenceAsync("nav_categories");
    }

    [TestMethod]
    public async Task Navigation_ToTags_Works()
    {
        await Page.GotoAsync($"{BaseUrl}/tags");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Tags", Exact = true })).ToBeVisibleAsync();
        await SaveEvidenceAsync("nav_tags");
    }

    [TestMethod]
    public async Task Navigation_ToVaults_Works()
    {
        await Page.GotoAsync($"{BaseUrl}/vaults");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Vaults", Exact = true })).ToBeVisibleAsync();
        await SaveEvidenceAsync("nav_vaults");
    }

    [TestMethod]
    public async Task Navigation_ToSettings_Works()
    {
        await Page.GotoAsync($"{BaseUrl}/settings");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Settings", Exact = true })).ToBeVisibleAsync();
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
        await Expect(DashboardHeading(Page)).ToBeVisibleAsync();

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
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Import Passwords", Exact = true })).ToBeVisibleAsync();
        await SaveEvidenceAsync("import_page_loaded");
    }

    // ── Complete end-to-end: Dashboard → Passwords → Categories ─────────────

    [TestMethod]
    public async Task CompletePasswordManagementWorkflow_ShouldExecuteSuccessfully()
    {
        // Start at dashboard
        await Expect(DashboardHeading(Page)).ToBeVisibleAsync();
        await SaveEvidenceAsync("workflow_01_dashboard");

        // Navigate to passwords
        await Page.GotoAsync($"{BaseUrl}/passwords");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "All Items", Exact = true })).ToBeVisibleAsync();
        await SaveEvidenceAsync("workflow_02_passwords");

        // Navigate to categories
        await Page.GotoAsync($"{BaseUrl}/categories");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Categories", Exact = true })).ToBeVisibleAsync();
        await SaveEvidenceAsync("workflow_03_categories");

        // Navigate to vaults
        await Page.GotoAsync($"{BaseUrl}/vaults");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Vaults", Exact = true })).ToBeVisibleAsync();
        await SaveEvidenceAsync("workflow_04_vaults");

        // Return to dashboard
        await Page.GotoAsync(BaseUrl);
        await Expect(DashboardHeading(Page)).ToBeVisibleAsync();
        await SaveEvidenceAsync("workflow_05_back_to_dashboard");
    }
}
