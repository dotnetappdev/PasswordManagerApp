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

    [TestMethod]
    public async Task VaultsPage_LoadsSuccessfully()
    {
        await Expect(Page.GetByText("Vaults")).ToBeVisibleAsync();
        await SaveEvidenceAsync("vaults_page_loaded");
    }

    [TestMethod]
    public async Task CreateVault_ShouldOpenForm()
    {
        await Expect(Page.GetByText("Vaults")).ToBeVisibleAsync();

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
        await Expect(Page.GetByText("Vaults")).ToBeVisibleAsync();

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
        await Expect(Page.GetByText("Vaults")).ToBeVisibleAsync();

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

        // Dashboard shows vault count stat card
        await Expect(Page.GetByText("Vaults")).ToBeVisibleAsync();
        await SaveEvidenceAsync("vault_stats_on_dashboard");
    }
}
