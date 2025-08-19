namespace PasswordManager.Tests.Playwright;

/// <summary>
/// UI tests for CRUD operations on categories using Microsoft Playwright.
/// Tests the CategoryDialog and related functionality.
/// </summary>
[TestClass]
public class CategoryCrudTests : PlaywrightTestBase
{
    /// <summary>
    /// Test creating a new category
    /// </summary>
    [TestMethod]
    public async Task CreateCategory_ShouldOpenDialogAndCreateCategory()
    {
        // Arrange
        var page = await StartApplication();
        await TakeScreenshot("category_app_startup");

        try
        {
            // Act - Navigate to categories page and add new category
            await page.ClickAsync("[data-testid='categories-nav-link']");
            await WaitAndScreenshot("[data-testid='categories-page']", "categories_page_opened");

            await page.ClickAsync("[data-testid='add-category-button']");
            await WaitAndScreenshot("[data-testid='category-dialog']", "category_dialog_opened");

            // Fill category form
            await page.FillAsync("[data-testid='category-name-textbox']", "Test Category");
            await page.FillAsync("[data-testid='category-description-textbox']", "Category for testing purposes");
            await page.SelectOptionAsync("[data-testid='category-color-picker']", "#FF5722");
            await page.FillAsync("[data-testid='category-icon-textbox']", "🔧");
            
            await TakeScreenshot("category_form_filled");

            // Save category
            await page.ClickAsync("[data-testid='save-category-button']");
            
            // Assert - Verify category was created
            await WaitAndScreenshot("[data-testid='categories-list']", "categories_list_updated");
            
            var categoryExists = await page.IsVisibleAsync("text=Test Category");
            Assert.IsTrue(categoryExists, "Created category should appear in the list");

            await TakeScreenshot("category_create_test_completed");
        }
        catch (Exception ex)
        {
            await TakeScreenshot("category_create_test_failed");
            throw new AssertFailedException($"Category creation test failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Test editing an existing category
    /// </summary>
    [TestMethod]
    public async Task EditCategory_ShouldUpdateCategoryDetails()
    {
        // Arrange
        var page = await StartApplication();
        await TakeScreenshot("category_edit_startup");

        try
        {
            // Navigate to categories page
            await page.ClickAsync("[data-testid='categories-nav-link']");
            await WaitAndScreenshot("[data-testid='categories-page']", "categories_page_for_edit");

            // Edit first category
            await page.ClickAsync("[data-testid='category-item']:first-child [data-testid='edit-category-button']");
            await WaitAndScreenshot("[data-testid='edit-category-dialog']", "edit_category_dialog_opened");

            // Modify category details
            await page.FillAsync("[data-testid='category-name-textbox']", "Updated Test Category");
            await page.FillAsync("[data-testid='category-description-textbox']", "Updated description for testing");
            await page.SelectOptionAsync("[data-testid='category-color-picker']", "#4CAF50");
            
            await TakeScreenshot("category_edit_form_modified");

            // Save changes
            await page.ClickAsync("[data-testid='save-category-button']");
            
            // Assert - Verify category was updated
            await WaitAndScreenshot("[data-testid='categories-list']", "categories_list_after_edit");
            
            var updatedCategoryExists = await page.IsVisibleAsync("text=Updated Test Category");
            Assert.IsTrue(updatedCategoryExists, "Updated category should appear in the list");

            await TakeScreenshot("category_edit_test_completed");
        }
        catch (Exception ex)
        {
            await TakeScreenshot("category_edit_test_failed");
            throw new AssertFailedException($"Category edit test failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Test deleting a category
    /// </summary>
    [TestMethod]
    public async Task DeleteCategory_ShouldRemoveCategoryFromList()
    {
        // Arrange
        var page = await StartApplication();
        await TakeScreenshot("category_delete_startup");

        try
        {
            // Navigate to categories page
            await page.ClickAsync("[data-testid='categories-nav-link']");
            await WaitAndScreenshot("[data-testid='categories-page']", "categories_page_for_delete");

            // Get initial count
            var initialCount = await page.Locator("[data-testid='category-item']").CountAsync();

            // Delete category
            await page.ClickAsync("[data-testid='category-item']:first-child [data-testid='delete-category-button']");
            await WaitAndScreenshot("[data-testid='delete-category-confirmation']", "delete_category_confirmation");

            await page.ClickAsync("[data-testid='confirm-delete-category-button']");
            
            // Assert - Verify category was deleted
            await WaitAndScreenshot("[data-testid='categories-list']", "categories_list_after_delete");
            
            var finalCount = await page.Locator("[data-testid='category-item']").CountAsync();
            Assert.AreEqual(initialCount - 1, finalCount, "Category count should decrease by 1 after deletion");

            await TakeScreenshot("category_delete_test_completed");
        }
        catch (Exception ex)
        {
            await TakeScreenshot("category_delete_test_failed");
            throw new AssertFailedException($"Category delete test failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Test category form validation
    /// </summary>
    [TestMethod]
    public async Task CreateCategory_WithInvalidData_ShouldShowValidationErrors()
    {
        // Arrange
        var page = await StartApplication();
        await TakeScreenshot("category_validation_startup");

        try
        {
            // Navigate to categories page
            await page.ClickAsync("[data-testid='categories-nav-link']");
            await page.ClickAsync("[data-testid='add-category-button']");
            await WaitAndScreenshot("[data-testid='category-dialog']", "category_validation_dialog");

            // Try to save without required fields
            await page.ClickAsync("[data-testid='save-category-button']");
            
            await TakeScreenshot("category_validation_errors_shown");

            // Assert - Verify validation errors
            var nameError = await page.IsVisibleAsync("[data-testid='category-name-validation-error']");
            Assert.IsTrue(nameError, "Name validation error should be displayed");

            // Fill required field and verify error disappears
            await page.FillAsync("[data-testid='category-name-textbox']", "Valid Category Name");
            
            await TakeScreenshot("category_validation_errors_cleared");

            // Cancel dialog
            await page.ClickAsync("[data-testid='cancel-category-button']");
            await TakeScreenshot("category_validation_test_completed");
        }
        catch (Exception ex)
        {
            await TakeScreenshot("category_validation_test_failed");
            throw new AssertFailedException($"Category validation test failed: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// UI tests for CRUD operations on tags using Microsoft Playwright.
/// Tests the TagDialog and related functionality.
/// </summary>
[TestClass]
public class TagCrudTests : PlaywrightTestBase
{
    /// <summary>
    /// Test creating a new tag
    /// </summary>
    [TestMethod]
    public async Task CreateTag_ShouldOpenDialogAndCreateTag()
    {
        // Arrange
        var page = await StartApplication();
        await TakeScreenshot("tag_app_startup");

        try
        {
            // Act - Navigate to tags management and add new tag
            await page.ClickAsync("[data-testid='tags-nav-link']");
            await WaitAndScreenshot("[data-testid='tags-page']", "tags_page_opened");

            await page.ClickAsync("[data-testid='add-tag-button']");
            await WaitAndScreenshot("[data-testid='tag-dialog']", "tag_dialog_opened");

            // Fill tag form
            await page.FillAsync("[data-testid='tag-name-textbox']", "Important");
            await page.FillAsync("[data-testid='tag-description-textbox']", "Tag for important items");
            await page.SelectOptionAsync("[data-testid='tag-color-picker']", "#FF9800");
            
            await TakeScreenshot("tag_form_filled");

            // Save tag
            await page.ClickAsync("[data-testid='save-tag-button']");
            
            // Assert - Verify tag was created
            await WaitAndScreenshot("[data-testid='tags-list']", "tags_list_updated");
            
            var tagExists = await page.IsVisibleAsync("text=Important");
            Assert.IsTrue(tagExists, "Created tag should appear in the list");

            await TakeScreenshot("tag_create_test_completed");
        }
        catch (Exception ex)
        {
            await TakeScreenshot("tag_create_test_failed");
            throw new AssertFailedException($"Tag creation test failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Test editing an existing tag
    /// </summary>
    [TestMethod]
    public async Task EditTag_ShouldUpdateTagDetails()
    {
        // Arrange
        var page = await StartApplication();
        await TakeScreenshot("tag_edit_startup");

        try
        {
            // Navigate to tags page
            await page.ClickAsync("[data-testid='tags-nav-link']");
            await WaitAndScreenshot("[data-testid='tags-page']", "tags_page_for_edit");

            // Edit first tag
            await page.ClickAsync("[data-testid='tag-item']:first-child [data-testid='edit-tag-button']");
            await WaitAndScreenshot("[data-testid='edit-tag-dialog']", "edit_tag_dialog_opened");

            // Modify tag details
            await page.FillAsync("[data-testid='tag-name-textbox']", "Very Important");
            await page.FillAsync("[data-testid='tag-description-textbox']", "Updated tag description");
            await page.SelectOptionAsync("[data-testid='tag-color-picker']", "#F44336");
            
            await TakeScreenshot("tag_edit_form_modified");

            // Save changes
            await page.ClickAsync("[data-testid='save-tag-button']");
            
            // Assert - Verify tag was updated
            await WaitAndScreenshot("[data-testid='tags-list']", "tags_list_after_edit");
            
            var updatedTagExists = await page.IsVisibleAsync("text=Very Important");
            Assert.IsTrue(updatedTagExists, "Updated tag should appear in the list");

            await TakeScreenshot("tag_edit_test_completed");
        }
        catch (Exception ex)
        {
            await TakeScreenshot("tag_edit_test_failed");
            throw new AssertFailedException($"Tag edit test failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Test deleting a tag
    /// </summary>
    [TestMethod]
    public async Task DeleteTag_ShouldRemoveTagFromList()
    {
        // Arrange
        var page = await StartApplication();
        await TakeScreenshot("tag_delete_startup");

        try
        {
            // Navigate to tags page
            await page.ClickAsync("[data-testid='tags-nav-link']");
            await WaitAndScreenshot("[data-testid='tags-page']", "tags_page_for_delete");

            // Get initial count
            var initialCount = await page.Locator("[data-testid='tag-item']").CountAsync();

            // Delete tag
            await page.ClickAsync("[data-testid='tag-item']:first-child [data-testid='delete-tag-button']");
            await WaitAndScreenshot("[data-testid='delete-tag-confirmation']", "delete_tag_confirmation");

            await page.ClickAsync("[data-testid='confirm-delete-tag-button']");
            
            // Assert - Verify tag was deleted
            await WaitAndScreenshot("[data-testid='tags-list']", "tags_list_after_delete");
            
            var finalCount = await page.Locator("[data-testid='tag-item']").CountAsync();
            Assert.AreEqual(initialCount - 1, finalCount, "Tag count should decrease by 1 after deletion");

            await TakeScreenshot("tag_delete_test_completed");
        }
        catch (Exception ex)
        {
            await TakeScreenshot("tag_delete_test_failed");
            throw new AssertFailedException($"Tag delete test failed: {ex.Message}", ex);
        }
    }
}