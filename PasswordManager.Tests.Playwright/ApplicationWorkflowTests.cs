namespace PasswordManager.Tests.Playwright;

/// <summary>
/// Integration tests that demonstrate complete CRUD workflows in the PasswordManager WinUI application.
/// These tests showcase the full user journey through various application features.
/// </summary>
[TestClass]
public class ApplicationWorkflowTests : PlaywrightTestBase
{
    /// <summary>
    /// Complete workflow test: Login → Create Category → Create Password Item → Edit → Delete
    /// </summary>
    [TestMethod]
    public async Task CompletePasswordManagementWorkflow_ShouldExecuteSuccessfully()
    {
        // Arrange
        var page = await StartApplication();
        await TakeScreenshot("workflow_app_startup");

        try
        {
            // Step 1: Application Login/Authentication
            await WaitAndScreenshot("[data-testid='login-page']", "01_login_page_displayed");
            
            await page.FillAsync("[data-testid='master-password-textbox']", "TestMasterPassword123!");
            await TakeScreenshot("02_master_password_entered");
            
            await page.ClickAsync("[data-testid='login-button']");
            await WaitAndScreenshot("[data-testid='main-dashboard']", "03_dashboard_loaded");

            // Step 2: Create a new category for organization
            await page.ClickAsync("[data-testid='categories-nav-link']");
            await WaitAndScreenshot("[data-testid='categories-page']", "04_categories_page_opened");

            await page.ClickAsync("[data-testid='add-category-button']");
            await WaitAndScreenshot("[data-testid='category-dialog']", "05_category_dialog_opened");

            await page.FillAsync("[data-testid='category-name-textbox']", "Work Accounts");
            await page.FillAsync("[data-testid='category-description-textbox']", "Professional work-related accounts");
            await page.SelectOptionAsync("[data-testid='category-color-picker']", "#2196F3");
            await page.FillAsync("[data-testid='category-icon-textbox']", "💼");
            await TakeScreenshot("06_category_form_completed");

            await page.ClickAsync("[data-testid='save-category-button']");
            await WaitAndScreenshot("[data-testid='categories-list']", "07_category_created_successfully");

            // Step 3: Create a new password item using the created category
            await page.ClickAsync("[data-testid='password-items-nav-link']");
            await WaitAndScreenshot("[data-testid='password-items-page']", "08_password_items_page");

            await page.ClickAsync("[data-testid='add-password-button']");
            await WaitAndScreenshot("[data-testid='add-password-dialog']", "09_add_password_dialog");

            // Fill basic information
            await page.FillAsync("[data-testid='title-textbox']", "Company Email Account");
            await page.SelectOptionAsync("[data-testid='category-combobox']", "Work Accounts");
            await page.SelectOptionAsync("[data-testid='type-combobox']", "Login");
            await TakeScreenshot("10_basic_info_filled");

            // Fill login specific details
            await page.FillAsync("[data-testid='username-textbox']", "john.doe@company.com");
            await page.FillAsync("[data-testid='password-textbox']", "CompanyPassword2024!");
            await page.FillAsync("[data-testid='website-textbox']", "https://mail.company.com");
            await page.FillAsync("[data-testid='notes-textbox']", "Corporate email account for daily work");
            await TakeScreenshot("11_login_details_completed");

            await page.ClickAsync("[data-testid='save-button']");
            await WaitAndScreenshot("[data-testid='password-items-list']", "12_password_item_created");

            // Step 4: Create a tag and associate it with the item
            await page.ClickAsync("[data-testid='tags-nav-link']");
            await WaitAndScreenshot("[data-testid='tags-page']", "13_tags_page_opened");

            await page.ClickAsync("[data-testid='add-tag-button']");
            await WaitAndScreenshot("[data-testid='tag-dialog']", "14_tag_dialog_opened");

            await page.FillAsync("[data-testid='tag-name-textbox']", "High Priority");
            await page.FillAsync("[data-testid='tag-description-textbox']", "Critical accounts requiring frequent access");
            await page.SelectOptionAsync("[data-testid='tag-color-picker']", "#FF5722");
            await TakeScreenshot("15_tag_form_completed");

            await page.ClickAsync("[data-testid='save-tag-button']");
            await WaitAndScreenshot("[data-testid='tags-list']", "16_tag_created_successfully");

            // Step 5: Edit the password item to add the tag
            await page.ClickAsync("[data-testid='password-items-nav-link']");
            await page.ClickAsync("[data-testid='password-item']:has-text('Company Email Account')");
            await WaitAndScreenshot("[data-testid='password-details-dialog']", "17_password_details_opened");

            await page.ClickAsync("[data-testid='edit-item-button']");
            await WaitAndScreenshot("[data-testid='edit-password-dialog']", "18_edit_dialog_opened");

            // Add tag to the item
            await page.ClickAsync("[data-testid='tag-high-priority-checkbox']");
            await page.FillAsync("[data-testid='notes-textbox']", "Corporate email account for daily work - Updated with high priority tag");
            await TakeScreenshot("19_item_updated_with_tag");

            await page.ClickAsync("[data-testid='save-button']");
            await WaitAndScreenshot("[data-testid='password-items-list']", "20_updated_item_in_list");

            // Step 6: Test search and filtering functionality
            await page.FillAsync("[data-testid='search-textbox']", "Company");
            await TakeScreenshot("21_search_results_filtered");

            var searchResults = await page.Locator("[data-testid='password-item']").CountAsync();
            Assert.IsTrue(searchResults >= 1, "Search should return at least the created item");

            await page.FillAsync("[data-testid='search-textbox']", "");
            await TakeScreenshot("22_search_cleared");

            // Step 7: Test password revelation and copy functionality
            await page.ClickAsync("[data-testid='password-item']:has-text('Company Email Account')");
            await WaitAndScreenshot("[data-testid='password-details-dialog']", "23_password_details_for_copy");

            await page.ClickAsync("[data-testid='reveal-password-button']");
            await TakeScreenshot("24_password_revealed");

            await page.ClickAsync("[data-testid='copy-username-button']");
            await TakeScreenshot("25_username_copied");

            await page.ClickAsync("[data-testid='copy-password-button']");
            await TakeScreenshot("26_password_copied");

            await page.ClickAsync("[data-testid='close-details-button']");

            // Step 8: Create a different type of item (Credit Card)
            await page.ClickAsync("[data-testid='add-password-button']");
            await page.FillAsync("[data-testid='title-textbox']", "Corporate Credit Card");
            await page.SelectOptionAsync("[data-testid='category-combobox']", "Work Accounts");
            await page.SelectOptionAsync("[data-testid='type-combobox']", "Credit Card");
            await TakeScreenshot("27_credit_card_type_selected");

            // Fill credit card details
            await page.FillAsync("[data-testid='cardholder-name-textbox']", "John Doe");
            await page.FillAsync("[data-testid='card-number-textbox']", "4532123456789012");
            await page.FillAsync("[data-testid='expiry-date-textbox']", "12/27");
            await page.FillAsync("[data-testid='cvv-textbox']", "123");
            await page.FillAsync("[data-testid='bank-website-textbox']", "https://business.bank.com");
            await TakeScreenshot("28_credit_card_details_filled");

            await page.ClickAsync("[data-testid='save-button']");
            await WaitAndScreenshot("[data-testid='password-items-list']", "29_credit_card_created");

            // Step 9: Test item deletion
            await page.ClickAsync("[data-testid='password-item']:has-text('Corporate Credit Card')", new() { Button = MouseButton.Right });
            await WaitAndScreenshot("[data-testid='context-menu']", "30_delete_context_menu");

            await page.ClickAsync("[data-testid='delete-item-menu']");
            await WaitAndScreenshot("[data-testid='delete-confirmation-dialog']", "31_delete_confirmation");

            await page.ClickAsync("[data-testid='confirm-delete-button']");
            await WaitAndScreenshot("[data-testid='password-items-list']", "32_item_deleted");

            // Step 10: Verify final state and export functionality
            await page.ClickAsync("[data-testid='settings-nav-link']");
            await WaitAndScreenshot("[data-testid='settings-page']", "33_settings_page_opened");

            await page.ClickAsync("[data-testid='export-data-button']");
            await WaitAndScreenshot("[data-testid='export-dialog']", "34_export_dialog_opened");

            await page.SelectOptionAsync("[data-testid='export-format-combobox']", "JSON");
            await page.ClickAsync("[data-testid='export-button']");
            await TakeScreenshot("35_data_exported");

            await TakeScreenshot("36_workflow_completed_successfully");

            // Assert final state
            await page.ClickAsync("[data-testid='password-items-nav-link']");
            var finalItems = await page.Locator("[data-testid='password-item']").CountAsync();
            Assert.IsTrue(finalItems >= 1, "At least one password item should remain after the workflow");

            TestContext.WriteLine("Complete workflow test executed successfully with comprehensive screenshot documentation");
        }
        catch (Exception ex)
        {
            await TakeScreenshot("workflow_test_failed_exception");
            throw new AssertFailedException($"Complete workflow test failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Admin workflow test: User management operations
    /// </summary>
    [TestMethod]
    public async Task AdminUserManagementWorkflow_ShouldExecuteSuccessfully()
    {
        // Arrange
        var page = await StartApplication();
        await TakeScreenshot("admin_workflow_startup");

        try
        {
            // Login as admin
            await page.FillAsync("[data-testid='master-password-textbox']", "AdminPassword123!");
            await page.ClickAsync("[data-testid='login-button']");
            await WaitAndScreenshot("[data-testid='main-dashboard']", "admin_dashboard_loaded");

            // Navigate to admin section
            await page.ClickAsync("[data-testid='admin-nav-link']");
            await WaitAndScreenshot("[data-testid='admin-page']", "admin_section_opened");

            // User Management workflow
            await page.ClickAsync("[data-testid='user-management-link']");
            await WaitAndScreenshot("[data-testid='user-management-page']", "user_management_opened");

            // Create new user
            await page.ClickAsync("[data-testid='add-user-button']");
            await page.FillAsync("[data-testid='email-textbox']", "newemployee@company.com");
            await page.FillAsync("[data-testid='password-textbox']", "EmployeePass123!");
            await page.FillAsync("[data-testid='confirm-password-textbox']", "EmployeePass123!");
            await page.FillAsync("[data-testid='first-name-textbox']", "New");
            await page.FillAsync("[data-testid='last-name-textbox']", "Employee");
            await TakeScreenshot("new_user_form_completed");

            await page.ClickAsync("[data-testid='create-user-button']");
            await WaitAndScreenshot("[data-testid='users-list']", "new_user_created");

            // Edit user
            await page.ClickAsync("[data-testid='user-row']:has-text('newemployee@company.com') [data-testid='edit-user-button']");
            await page.FillAsync("[data-testid='last-name-textbox']", "Smith");
            await page.ClickAsync("[data-testid='save-user-button']");
            await TakeScreenshot("user_updated");

            // Change user password
            await page.ClickAsync("[data-testid='user-row']:has-text('New Smith') [data-testid='change-password-button']");
            await page.FillAsync("[data-testid='new-password-textbox']", "NewEmployeePass123!");
            await page.FillAsync("[data-testid='confirm-new-password-textbox']", "NewEmployeePass123!");
            await page.ClickAsync("[data-testid='change-password-submit-button']");
            await TakeScreenshot("user_password_changed");

            await TakeScreenshot("admin_workflow_completed");

            TestContext.WriteLine("Admin workflow test executed successfully");
        }
        catch (Exception ex)
        {
            await TakeScreenshot("admin_workflow_failed");
            throw new AssertFailedException($"Admin workflow test failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Import/Export workflow test
    /// </summary>
    [TestMethod]
    public async Task ImportExportWorkflow_ShouldHandleDataTransfer()
    {
        // Arrange
        var page = await StartApplication();
        await TakeScreenshot("import_export_startup");

        try
        {
            // Login
            await page.FillAsync("[data-testid='master-password-textbox']", "TestPassword123!");
            await page.ClickAsync("[data-testid='login-button']");
            await WaitAndScreenshot("[data-testid='main-dashboard']", "logged_in_for_import_export");

            // Test Export functionality
            await page.ClickAsync("[data-testid='settings-nav-link']");
            await page.ClickAsync("[data-testid='export-data-button']");
            await WaitAndScreenshot("[data-testid='export-dialog']", "export_dialog_displayed");

            await page.SelectOptionAsync("[data-testid='export-format-combobox']", "CSV");
            await page.ClickAsync("[data-testid='select-export-location-button']");
            await TakeScreenshot("export_location_selected");

            await page.ClickAsync("[data-testid='export-button']");
            await WaitAndScreenshot("[data-testid='export-success-message']", "export_completed");

            // Test Import functionality
            await page.ClickAsync("[data-testid='import-data-button']");
            await WaitAndScreenshot("[data-testid='import-dialog']", "import_dialog_displayed");

            await page.SelectOptionAsync("[data-testid='import-format-combobox']", "1Password");
            await page.ClickAsync("[data-testid='select-import-file-button']");
            await TakeScreenshot("import_file_selection");

            // Note: File selection would be handled differently in a real test
            await page.ClickAsync("[data-testid='preview-import-button']");
            await WaitAndScreenshot("[data-testid='import-preview']", "import_preview_displayed");

            await page.ClickAsync("[data-testid='confirm-import-button']");
            await WaitAndScreenshot("[data-testid='import-success-message']", "import_completed");

            await TakeScreenshot("import_export_workflow_completed");

            TestContext.WriteLine("Import/Export workflow test executed successfully");
        }
        catch (Exception ex)
        {
            await TakeScreenshot("import_export_workflow_failed");
            throw new AssertFailedException($"Import/Export workflow test failed: {ex.Message}", ex);
        }
    }
}