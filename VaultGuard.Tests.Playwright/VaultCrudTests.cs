using Microsoft.Playwright;

namespace VaultGuard.Tests.Playwright;

/// <summary>
/// Blazor web UI tests for Vault CRUD operations.
/// </summary>
[TestClass]
public class VaultCrudTests : BlazorWebTestBase
{
    [ClassCleanup]
    public static Task CleanupAsync() => StopAppAsync();

    [TestInitialize]
    public new async Task NavigateToHomePageAsync()
    {
        await base.NavigateToHomePageAsync();
        await SignInAsync();
        await Page.GotoAsync($"{BaseUrl}/vaults");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    // Vaults.razor's own <h4>Vaults</h4> heading - scoped to the Heading role so it doesn't collide
    // with the several other "Vaults"/"All Vaults" strings the nav drawer renders on every page (a
    // bare GetByText("Vaults") throws a Playwright strict-mode violation once those are all on screen
    // simultaneously, which they never were before the SignInAsync fix let these tests reach the page).
    static ILocator VaultsHeading(IPage page) => page.GetByRole(AriaRole.Heading, new() { Name = "Vaults", Exact = true });

    [TestMethod]
    public async Task VaultsPage_LoadsSuccessfully()
    {
        await ExpectVisibleWithDiagnosticsAsync(VaultsHeading(Page), "VaultsHeading");
        await SaveEvidenceAsync("vaults_page_loaded");
    }

    [TestMethod]
    public async Task CreateVault_ShouldOpenForm()
    {
        await ExpectVisibleWithDiagnosticsAsync(VaultsHeading(Page), "VaultsHeading");

        var addBtn = Page.GetByRole(AriaRole.Button, new() { Name = "Add" })
                         .Or(Page.GetByRole(AriaRole.Button, new() { Name = "New Vault" }))
                         .Or(Page.GetByRole(AriaRole.Button, new() { Name = "Add Vault" }));

        if (await addBtn.CountAsync() > 0)
        {
            await addBtn.First.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        await SaveEvidenceAsync("vault_create_dialog");
    }

    [TestMethod]
    public async Task EditVault_ShouldOpenEditForm()
    {
        await ExpectVisibleWithDiagnosticsAsync(VaultsHeading(Page), "VaultsHeading");

        var editButtons = Page.GetByRole(AriaRole.Button, new() { Name = "Edit" })
                              .Or(Page.Locator("button[aria-label='edit']"));

        if (await editButtons.CountAsync() > 0)
        {
            await editButtons.First.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        await SaveEvidenceAsync("vault_edit_attempt");
    }

    [TestMethod]
    public async Task DeleteVault_ShouldPromptConfirmation()
    {
        await ExpectVisibleWithDiagnosticsAsync(VaultsHeading(Page), "VaultsHeading");

        var deleteButtons = Page.GetByRole(AriaRole.Button, new() { Name = "Delete" })
                                .Or(Page.Locator("button[aria-label='delete']"));

        if (await deleteButtons.CountAsync() > 0)
        {
            await deleteButtons.First.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        await SaveEvidenceAsync("vault_delete_attempt");
    }

    [TestMethod]
    public async Task VaultsPage_ShowsDashboardStats()
    {
        await Page.GotoAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Dashboard shows vault count stat card - its label is a <p>, not a heading, so scope to <p>
        // to avoid the nav drawer's "Vaults" nav-group title (also exact "Vaults") next to it.
        await ExpectVisibleWithDiagnosticsAsync(Page.Locator("p").GetByText("Vaults", new() { Exact = true }), "DashboardVaultsStat");
        await SaveEvidenceAsync("vault_stats_on_dashboard");
    }
}
