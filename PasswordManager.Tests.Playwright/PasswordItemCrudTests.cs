namespace PasswordManager.Tests.Playwright;

/// <summary>
/// UI tests for CRUD operations on password items using Microsoft Playwright.
/// Tests the AddPasswordDialog and related CRUD functionality.
/// </summary>
[TestClass]
public class PasswordItemCrudTests : PlaywrightTestBase
{
    /// <summary>
    /// Test creating a new password item through the AddPasswordDialog
    /// </summary>
    [TestMethod]
    public async Task CreatePasswordItem_ShouldOpenDialogAndCreateItem()
    {
        // Arrange
        var page = await StartApplication();
        await TakeScreenshot("app_startup");

        // Note: These selectors would need to be updated based on actual WinUI automation IDs
        // For WinUI apps, you typically use AccessibilityId or other automation properties
        
        try
        {
            // Act - Navigate to add password
            await page.ClickAsync("[data-testid='add-password-button']");
            await WaitAndScreenshot("[data-testid='add-password-dialog']", "add_password_dialog_opened");

            // Fill in the form fields
            await page.FillAsync("[data-testid='title-textbox']", "Test Website Login");
            await page.SelectOptionAsync("[data-testid='category-combobox']", "Personal");
            await page.SelectOptionAsync("[data-testid='type-combobox']", "Login");
            
            await TakeScreenshot("form_filled_basic_info");

            // Fill login specific fields
            await page.FillAsync("[data-testid='username-textbox']", "testuser@example.com");
            await page.FillAsync("[data-testid='password-textbox']", "SecurePassword123!");
            await page.FillAsync("[data-testid='website-textbox']", "https://test.example.com");
            
            await TakeScreenshot("form_filled_complete");

            // Submit the form
            await page.ClickAsync("[data-testid='save-button']");
            
            // Assert - Verify item was created
            await WaitAndScreenshot("[data-testid='password-items-list']", "password_items_list_updated");
            
            // Verify the new item appears in the list
            var itemExists = await page.IsVisibleAsync("text=Test Website Login");
            Assert.IsTrue(itemExists, "Created password item should appear in the list");

            await TakeScreenshot("test_completed_successfully");
        }
        catch (Exception ex)
        {
            await TakeScreenshot("test_failed_exception");
            throw new AssertFailedException($"Test failed with exception: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Test creating a credit card item
    /// </summary>
    [TestMethod]
    public async Task CreateCreditCardItem_ShouldFillCreditCardForm()
    {
        // Arrange
        var page = await StartApplication();
        await TakeScreenshot("app_startup_creditcard");

        try
        {
            // Act - Navigate to add password and select credit card type
            await page.ClickAsync("[data-testid='add-password-button']");
            await WaitAndScreenshot("[data-testid='add-password-dialog']", "add_item_dialog_opened");

            await page.FillAsync("[data-testid='title-textbox']", "My Credit Card");
            await page.SelectOptionAsync("[data-testid='category-combobox']", "Financial");
            await page.SelectOptionAsync("[data-testid='type-combobox']", "Credit Card");
            
            await TakeScreenshot("creditcard_type_selected");

            // Fill credit card specific fields
            await page.FillAsync("[data-testid='cardholder-name-textbox']", "John Doe");
            await page.FillAsync("[data-testid='card-number-textbox']", "1234567890123456");
            await page.FillAsync("[data-testid='expiry-date-textbox']", "12/28");
            await page.FillAsync("[data-testid='cvv-textbox']", "123");
            await page.FillAsync("[data-testid='bank-website-textbox']", "https://bank.example.com");
            
            await TakeScreenshot("creditcard_form_completed");

            // Submit the form
            await page.ClickAsync("[data-testid='save-button']");
            
            // Assert - Verify item was created
            await WaitAndScreenshot("[data-testid='password-items-list']", "creditcard_items_list_updated");
            
            var itemExists = await page.IsVisibleAsync("text=My Credit Card");
            Assert.IsTrue(itemExists, "Created credit card item should appear in the list");

            await TakeScreenshot("creditcard_test_completed");
        }
        catch (Exception ex)
        {
            await TakeScreenshot("creditcard_test_failed");
            throw new AssertFailedException($"Credit card test failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Test reading/viewing password item details
    /// </summary>
    [TestMethod]
    public async Task ViewPasswordItemDetails_ShouldOpenDetailsDialog()
    {
        // Arrange
        var page = await StartApplication();
        await TakeScreenshot("app_startup_view");

        try
        {
            // Act - Click on existing item to view details
            await page.ClickAsync("[data-testid='password-item']:first-child");
            await WaitAndScreenshot("[data-testid='password-details-dialog']", "password_details_opened");

            // Verify details are displayed
            var titleVisible = await page.IsVisibleAsync("[data-testid='item-title']");
            var usernameVisible = await page.IsVisibleAsync("[data-testid='item-username']");
            var websiteVisible = await page.IsVisibleAsync("[data-testid='item-website']");

            Assert.IsTrue(titleVisible, "Item title should be visible in details");
            Assert.IsTrue(usernameVisible, "Username should be visible in details");
            Assert.IsTrue(websiteVisible, "Website should be visible in details");

            await TakeScreenshot("password_details_verified");

            // Test copy functionality
            await page.ClickAsync("[data-testid='copy-username-button']");
            await TakeScreenshot("username_copied");

            // Close dialog
            await page.ClickAsync("[data-testid='close-details-button']");
            await TakeScreenshot("details_dialog_closed");
        }
        catch (Exception ex)
        {
            await TakeScreenshot("view_test_failed");
            throw new AssertFailedException($"View details test failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Test updating an existing password item
    /// </summary>
    [TestMethod]
    public async Task UpdatePasswordItem_ShouldEditExistingItem()
    {
        // Arrange
        var page = await StartApplication();
        await TakeScreenshot("app_startup_edit");

        try
        {
            // Act - Right-click on item to open context menu and select edit
            await page.ClickAsync("[data-testid='password-item']:first-child", new() { Button = MouseButton.Right });
            await WaitAndScreenshot("[data-testid='context-menu']", "context_menu_opened");

            await page.ClickAsync("[data-testid='edit-item-menu']");
            await WaitAndScreenshot("[data-testid='edit-password-dialog']", "edit_dialog_opened");

            // Modify some fields
            await page.FillAsync("[data-testid='title-textbox']", "Updated Website Login");
            await page.FillAsync("[data-testid='username-textbox']", "updated_user@example.com");
            
            await TakeScreenshot("edit_form_modified");

            // Save changes
            await page.ClickAsync("[data-testid='save-button']");
            
            // Assert - Verify changes were saved
            await WaitAndScreenshot("[data-testid='password-items-list']", "updated_items_list");
            
            var updatedItemExists = await page.IsVisibleAsync("text=Updated Website Login");
            Assert.IsTrue(updatedItemExists, "Updated item should appear in the list with new title");

            await TakeScreenshot("update_test_completed");
        }
        catch (Exception ex)
        {
            await TakeScreenshot("update_test_failed");
            throw new AssertFailedException($"Update test failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Test deleting a password item
    /// </summary>
    [TestMethod]
    public async Task DeletePasswordItem_ShouldRemoveItemFromList()
    {
        // Arrange
        var page = await StartApplication();
        await TakeScreenshot("app_startup_delete");

        try
        {
            // Get initial count of items
            var initialCount = await page.Locator("[data-testid='password-item']").CountAsync();
            
            // Act - Right-click on item and select delete
            await page.ClickAsync("[data-testid='password-item']:first-child", new() { Button = MouseButton.Right });
            await WaitAndScreenshot("[data-testid='context-menu']", "delete_context_menu");

            await page.ClickAsync("[data-testid='delete-item-menu']");
            await WaitAndScreenshot("[data-testid='delete-confirmation-dialog']", "delete_confirmation");

            // Confirm deletion
            await page.ClickAsync("[data-testid='confirm-delete-button']");
            
            // Assert - Verify item was deleted
            await WaitAndScreenshot("[data-testid='password-items-list']", "items_list_after_delete");
            
            var finalCount = await page.Locator("[data-testid='password-item']").CountAsync();
            Assert.AreEqual(initialCount - 1, finalCount, "Item count should decrease by 1 after deletion");

            await TakeScreenshot("delete_test_completed");
        }
        catch (Exception ex)
        {
            await TakeScreenshot("delete_test_failed");
            throw new AssertFailedException($"Delete test failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Test form validation in AddPasswordDialog
    /// </summary>
    [TestMethod]
    public async Task CreatePasswordItem_WithInvalidData_ShouldShowValidationErrors()
    {
        // Arrange
        var page = await StartApplication();
        await TakeScreenshot("app_startup_validation");

        try
        {
            // Act - Open dialog and try to save without required fields
            await page.ClickAsync("[data-testid='add-password-button']");
            await WaitAndScreenshot("[data-testid='add-password-dialog']", "validation_dialog_opened");

            // Try to save without filling required fields
            await page.ClickAsync("[data-testid='save-button']");
            
            await TakeScreenshot("validation_errors_shown");

            // Assert - Verify validation errors are displayed
            var titleError = await page.IsVisibleAsync("[data-testid='title-validation-error']");
            var categoryError = await page.IsVisibleAsync("[data-testid='category-validation-error']");
            
            Assert.IsTrue(titleError || categoryError, "Validation errors should be shown for required fields");

            // Fill required fields and verify errors disappear
            await page.FillAsync("[data-testid='title-textbox']", "Valid Title");
            await page.SelectOptionAsync("[data-testid='category-combobox']", "Personal");
            
            await TakeScreenshot("validation_errors_cleared");

            // Cancel dialog
            await page.ClickAsync("[data-testid='cancel-button']");
            await TakeScreenshot("validation_test_completed");
        }
        catch (Exception ex)
        {
            await TakeScreenshot("validation_test_failed");
            throw new AssertFailedException($"Validation test failed: {ex.Message}", ex);
        }
    }
}