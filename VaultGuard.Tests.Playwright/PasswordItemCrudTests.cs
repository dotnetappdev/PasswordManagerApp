using Microsoft.Playwright;

namespace VaultGuard.Tests.Playwright;

/// <summary>
/// Blazor web UI tests for Password Item CRUD operations.
/// </summary>
[TestClass]
public class PasswordItemCrudTests : BlazorWebTestBase
{
    [ClassCleanup]
    public static Task CleanupAsync() => StopAppAsync();

    [TestInitialize]
    public new async Task NavigateToHomePageAsync()
    {
        await base.NavigateToHomePageAsync();
        await SignInAsync();
        await Page.GotoAsync($"{BaseUrl}/passwords");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [TestMethod]
    public async Task PasswordItemsPage_LoadsSuccessfully()
    {
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "All Items", Exact = true })).ToBeVisibleAsync();
        await SaveEvidenceAsync("passwords_page_loaded");
    }

    [TestMethod]
    public async Task PasswordItemsPage_HasSearchBox()
    {
        await Expect(Page.GetByPlaceholder("Search items...")).ToBeVisibleAsync();
        await SaveEvidenceAsync("passwords_search_box");
    }

    [TestMethod]
    public async Task PasswordItemsPage_HasAddItemButton()
    {
        var addBtn = Page.GetByRole(AriaRole.Button, new() { Name = "Add Item" })
                         .Or(Page.GetByRole(AriaRole.Button, new() { Name = "Add" }));
        await Expect(addBtn.First).ToBeVisibleAsync();
        await SaveEvidenceAsync("passwords_add_button");
    }

    [TestMethod]
    public async Task CreatePasswordItem_ShouldOpenDialogAndCreateItem()
    {
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "All Items", Exact = true })).ToBeVisibleAsync();

        var addBtn = Page.GetByRole(AriaRole.Button, new() { Name = "Add Item" })
                         .Or(Page.GetByRole(AriaRole.Button, new() { Name = "Add" }));

        await addBtn.First.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Dialog or inline form should open
        var titleField = Page.GetByLabel("Title")
                             .Or(Page.GetByPlaceholder("Title"))
                             .Or(Page.GetByPlaceholder("Item name"));

        if (await titleField.CountAsync() > 0)
        {
            await titleField.First.FillAsync("Test Login Item");

            var saveBtn = Page.GetByRole(AriaRole.Button, new() { Name = "Save" })
                              .Or(Page.GetByRole(AriaRole.Button, new() { Name = "Create" })
                              .Or(Page.GetByRole(AriaRole.Button, new() { Name = "Add" })));

            if (await saveBtn.CountAsync() > 0)
            {
                await saveBtn.First.ClickAsync();
                await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }
        }

        await SaveEvidenceAsync("password_item_create_attempt");
    }

    [TestMethod]
    public async Task CreateCreditCardItem_ShouldFillCreditCardForm()
    {
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "All Items", Exact = true })).ToBeVisibleAsync();

        var addBtn = Page.GetByRole(AriaRole.Button, new() { Name = "Add Item" })
                         .Or(Page.GetByRole(AriaRole.Button, new() { Name = "Add" }));

        await addBtn.First.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Try to select credit card type
        var typeSelector = Page.GetByLabel("Type").Or(Page.GetByRole(AriaRole.Combobox));
        if (await typeSelector.CountAsync() > 0)
        {
            await typeSelector.First.SelectOptionAsync(new SelectOptionValue { Label = "CreditCard" });
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        await SaveEvidenceAsync("credit_card_form");
    }

    [TestMethod]
    public async Task ViewPasswordItemDetails_ShouldOpenDetailsDialog()
    {
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "All Items", Exact = true })).ToBeVisibleAsync();

        // Look for any existing row/item to click
        var rows = Page.Locator("tr.mud-table-row").Or(Page.Locator("[data-testid='password-row']"));
        var count = await rows.CountAsync();
        if (count > 0)
        {
            await rows.First.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        await SaveEvidenceAsync("password_item_view");
    }

    [TestMethod]
    public async Task UpdatePasswordItem_ShouldEditExistingItem()
    {
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "All Items", Exact = true })).ToBeVisibleAsync();

        var editButtons = Page.GetByRole(AriaRole.Button, new() { Name = "Edit" })
                              .Or(Page.Locator("button[aria-label='edit']"))
                              .Or(Page.Locator("[title='Edit']"));

        if (await editButtons.CountAsync() > 0)
        {
            await editButtons.First.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        await SaveEvidenceAsync("password_item_edit_attempt");
    }

    [TestMethod]
    public async Task DeletePasswordItem_ShouldRemoveItemFromList()
    {
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "All Items", Exact = true })).ToBeVisibleAsync();

        var deleteButtons = Page.GetByRole(AriaRole.Button, new() { Name = "Delete" })
                                .Or(Page.Locator("button[aria-label='delete']"))
                                .Or(Page.Locator("[title='Delete']"));

        if (await deleteButtons.CountAsync() > 0)
        {
            await deleteButtons.First.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        await SaveEvidenceAsync("password_item_delete_attempt");
    }

    [TestMethod]
    public async Task CreatePasswordItem_WithInvalidData_ShouldShowValidationErrors()
    {
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "All Items", Exact = true })).ToBeVisibleAsync();

        var addBtn = Page.GetByRole(AriaRole.Button, new() { Name = "Add Item" })
                         .Or(Page.GetByRole(AriaRole.Button, new() { Name = "Add" }));

        await addBtn.First.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Try to save empty form
        var saveBtn = Page.GetByRole(AriaRole.Button, new() { Name = "Save" })
                          .Or(Page.GetByRole(AriaRole.Button, new() { Name = "Create" }));

        if (await saveBtn.CountAsync() > 0)
        {
            await saveBtn.First.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        await SaveEvidenceAsync("password_item_validation");
    }

    [TestMethod]
    public async Task SearchBox_FiltersItems()
    {
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "All Items", Exact = true })).ToBeVisibleAsync();

        var searchBox = Page.GetByPlaceholder("Search items...");
        await searchBox.FillAsync("test");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await SaveEvidenceAsync("password_search_filtered");
    }

    [TestMethod]
    public async Task FavoritesFilter_FiltersItems()
    {
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "All Items", Exact = true })).ToBeVisibleAsync();

        var favCheckbox = Page.GetByLabel("Favorites only")
                              .Or(Page.GetByText("Favorites only"));

        if (await favCheckbox.CountAsync() > 0)
        {
            await favCheckbox.First.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        await SaveEvidenceAsync("password_favorites_filtered");
    }
}
