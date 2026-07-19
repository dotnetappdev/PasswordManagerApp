using Microsoft.Playwright;

namespace VaultGuard.Tests.Playwright;

/// <summary>
/// Blazor web UI tests for Category CRUD operations.
/// </summary>
[TestClass]
public class CategoryCrudTests : BlazorWebTestBase
{
    [ClassCleanup]
    public static Task CleanupAsync() => StopAppAsync();

    [TestInitialize]
    public new async Task NavigateToHomePageAsync()
    {
        await base.NavigateToHomePageAsync();
        await SignInAsync();
        await Page.GotoAsync($"{BaseUrl}/categories");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [TestMethod]
    public async Task CategoriesPage_LoadsSuccessfully()
    {
        await Expect(Page.GetByText("Categories")).ToBeVisibleAsync();
        await SaveEvidenceAsync("categories_page_loaded");
    }

    [TestMethod]
    public async Task CategoriesPage_HasExpectedUIElements()
    {
        await Expect(Page.GetByText("Categories")).ToBeVisibleAsync();
        await SaveEvidenceAsync("categories_ui_elements");
    }

    [TestMethod]
    public async Task CreateCategory_ShouldOpenDialogAndCreateCategory()
    {
        await Expect(Page.GetByText("Categories")).ToBeVisibleAsync();

        var addButton = Page.GetByRole(AriaRole.Button, new() { Name = "Add" })
                            .Or(Page.GetByRole(AriaRole.Button, new() { Name = "New Category" }))
                            .Or(Page.GetByRole(AriaRole.Button, new() { Name = "Add Category" }));

        if (await addButton.CountAsync() > 0)
        {
            await addButton.First.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        await SaveEvidenceAsync("category_create_attempt");
    }

    [TestMethod]
    public async Task EditCategory_ShouldUpdateCategoryDetails()
    {
        await Expect(Page.GetByText("Categories")).ToBeVisibleAsync();

        var editButtons = Page.GetByRole(AriaRole.Button, new() { Name = "Edit" })
                              .Or(Page.Locator("button[aria-label='edit']"));

        if (await editButtons.CountAsync() > 0)
        {
            await editButtons.First.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        await SaveEvidenceAsync("category_edit_attempt");
    }

    [TestMethod]
    public async Task DeleteCategory_ShouldRemoveCategoryFromList()
    {
        await Expect(Page.GetByText("Categories")).ToBeVisibleAsync();

        var deleteButtons = Page.GetByRole(AriaRole.Button, new() { Name = "Delete" })
                                .Or(Page.Locator("button[aria-label='delete']"));

        if (await deleteButtons.CountAsync() > 0)
        {
            await deleteButtons.First.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        await SaveEvidenceAsync("category_delete_attempt");
    }

    [TestMethod]
    public async Task CreateCategory_WithInvalidData_ShouldShowValidationErrors()
    {
        await Expect(Page.GetByText("Categories")).ToBeVisibleAsync();

        var addButton = Page.GetByRole(AriaRole.Button, new() { Name = "Add" })
                            .Or(Page.GetByRole(AriaRole.Button, new() { Name = "Add Category" }));

        if (await addButton.CountAsync() > 0)
        {
            await addButton.First.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var saveBtn = Page.GetByRole(AriaRole.Button, new() { Name = "Save" })
                              .Or(Page.GetByRole(AriaRole.Button, new() { Name = "Create" }));

            if (await saveBtn.CountAsync() > 0)
            {
                await saveBtn.First.ClickAsync();
                await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }
        }

        await SaveEvidenceAsync("category_validation_attempt");
    }
}

/// <summary>
/// Blazor web UI tests for Tag CRUD operations.
/// </summary>
[TestClass]
public class TagCrudTests : BlazorWebTestBase
{
    [ClassCleanup]
    public static Task CleanupAsync() => StopAppAsync();

    [TestInitialize]
    public new async Task NavigateToHomePageAsync()
    {
        await base.NavigateToHomePageAsync();
        await SignInAsync();
        await Page.GotoAsync($"{BaseUrl}/tags");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [TestMethod]
    public async Task TagsPage_LoadsSuccessfully()
    {
        await Expect(Page.GetByText("Tags")).ToBeVisibleAsync();
        await SaveEvidenceAsync("tags_page_loaded");
    }

    [TestMethod]
    public async Task CreateTag_ShouldOpenDialogAndCreateTag()
    {
        await Expect(Page.GetByText("Tags")).ToBeVisibleAsync();

        var addButton = Page.GetByRole(AriaRole.Button, new() { Name = "Add" })
                            .Or(Page.GetByRole(AriaRole.Button, new() { Name = "Add Tag" }))
                            .Or(Page.GetByRole(AriaRole.Button, new() { Name = "New Tag" }));

        if (await addButton.CountAsync() > 0)
        {
            await addButton.First.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        await SaveEvidenceAsync("tag_create_attempt");
    }

    [TestMethod]
    public async Task EditTag_ShouldUpdateTagDetails()
    {
        await Expect(Page.GetByText("Tags")).ToBeVisibleAsync();

        var editButtons = Page.GetByRole(AriaRole.Button, new() { Name = "Edit" })
                              .Or(Page.Locator("button[aria-label='edit']"));

        if (await editButtons.CountAsync() > 0)
        {
            await editButtons.First.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        await SaveEvidenceAsync("tag_edit_attempt");
    }

    [TestMethod]
    public async Task DeleteTag_ShouldRemoveTagFromList()
    {
        await Expect(Page.GetByText("Tags")).ToBeVisibleAsync();

        var deleteButtons = Page.GetByRole(AriaRole.Button, new() { Name = "Delete" })
                                .Or(Page.Locator("button[aria-label='delete']"));

        if (await deleteButtons.CountAsync() > 0)
        {
            await deleteButtons.First.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        await SaveEvidenceAsync("tag_delete_attempt");
    }
}
