namespace PasswordManager.Tests.Playwright;

/// <summary>
/// UI tests for user management CRUD operations using Microsoft Playwright.
/// Tests the admin user management forms and functionality.
/// </summary>
[TestClass]
public class UserManagementCrudTests : PlaywrightTestBase
{
    /// <summary>
    /// Test creating a new user through the admin interface
    /// </summary>
    [TestMethod]
    public async Task CreateUser_ShouldOpenDialogAndCreateUser()
    {
        // Arrange
        var page = await StartApplication();
        await TakeScreenshot("user_mgmt_app_startup");

        try
        {
            // Act - Navigate to admin user management
            await page.ClickAsync("[data-testid='admin-nav-link']");
            await WaitAndScreenshot("[data-testid='admin-page']", "admin_page_opened");

            await page.ClickAsync("[data-testid='user-management-link']");
            await WaitAndScreenshot("[data-testid='user-management-page']", "user_management_page");

            await page.ClickAsync("[data-testid='add-user-button']");
            await WaitAndScreenshot("[data-testid='create-user-dialog']", "create_user_dialog_opened");

            // Fill user creation form
            await page.FillAsync("[data-testid='email-textbox']", "newuser@example.com");
            await page.FillAsync("[data-testid='password-textbox']", "SecurePassword123!");
            await page.FillAsync("[data-testid='confirm-password-textbox']", "SecurePassword123!");
            await page.FillAsync("[data-testid='first-name-textbox']", "John");
            await page.FillAsync("[data-testid='last-name-textbox']", "Doe");
            await page.FillAsync("[data-testid='password-hint-textbox']", "My secure password");
            
            await TakeScreenshot("create_user_form_filled");

            // Submit the form
            await page.ClickAsync("[data-testid='create-user-button']");
            
            // Assert - Verify user was created
            await WaitAndScreenshot("[data-testid='users-list']", "users_list_updated");
            
            var userExists = await page.IsVisibleAsync("text=newuser@example.com");
            Assert.IsTrue(userExists, "Created user should appear in the users list");

            await TakeScreenshot("create_user_test_completed");
        }
        catch (Exception ex)
        {
            await TakeScreenshot("create_user_test_failed");
            throw new AssertFailedException($"User creation test failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Test editing an existing user
    /// </summary>
    [TestMethod]
    public async Task EditUser_ShouldUpdateUserDetails()
    {
        // Arrange
        var page = await StartApplication();
        await TakeScreenshot("edit_user_startup");

        try
        {
            // Navigate to user management
            await page.ClickAsync("[data-testid='admin-nav-link']");
            await page.ClickAsync("[data-testid='user-management-link']");
            await WaitAndScreenshot("[data-testid='user-management-page']", "user_management_for_edit");

            // Edit first user
            await page.ClickAsync("[data-testid='user-row']:first-child [data-testid='edit-user-button']");
            await WaitAndScreenshot("[data-testid='edit-user-dialog']", "edit_user_dialog_opened");

            // Modify user details
            await page.FillAsync("[data-testid='first-name-textbox']", "Jane");
            await page.FillAsync("[data-testid='last-name-textbox']", "Smith");
            await page.FillAsync("[data-testid='password-hint-textbox']", "Updated password hint");
            
            await TakeScreenshot("edit_user_form_modified");

            // Save changes
            await page.ClickAsync("[data-testid='save-user-button']");
            
            // Assert - Verify user was updated
            await WaitAndScreenshot("[data-testid='users-list']", "users_list_after_edit");
            
            var updatedUserExists = await page.IsVisibleAsync("text=Jane Smith");
            Assert.IsTrue(updatedUserExists, "Updated user name should appear in the list");

            await TakeScreenshot("edit_user_test_completed");
        }
        catch (Exception ex)
        {
            await TakeScreenshot("edit_user_test_failed");
            throw new AssertFailedException($"User edit test failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Test changing user password
    /// </summary>
    [TestMethod]
    public async Task ChangeUserPassword_ShouldUpdatePassword()
    {
        // Arrange
        var page = await StartApplication();
        await TakeScreenshot("change_password_startup");

        try
        {
            // Navigate to user management
            await page.ClickAsync("[data-testid='admin-nav-link']");
            await page.ClickAsync("[data-testid='user-management-link']");
            await WaitAndScreenshot("[data-testid='user-management-page']", "user_management_for_password");

            // Open change password dialog
            await page.ClickAsync("[data-testid='user-row']:first-child [data-testid='change-password-button']");
            await WaitAndScreenshot("[data-testid='change-password-dialog']", "change_password_dialog_opened");

            // Fill password change form
            await page.FillAsync("[data-testid='new-password-textbox']", "NewSecurePassword123!");
            await page.FillAsync("[data-testid='confirm-new-password-textbox']", "NewSecurePassword123!");
            
            await TakeScreenshot("change_password_form_filled");

            // Submit password change
            await page.ClickAsync("[data-testid='change-password-submit-button']");
            
            // Assert - Verify success message
            await WaitAndScreenshot("[data-testid='success-message']", "password_change_success");
            
            var successVisible = await page.IsVisibleAsync("[data-testid='success-message']");
            Assert.IsTrue(successVisible, "Success message should be displayed after password change");

            await TakeScreenshot("change_password_test_completed");
        }
        catch (Exception ex)
        {
            await TakeScreenshot("change_password_test_failed");
            throw new AssertFailedException($"Change password test failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Test deactivating a user
    /// </summary>
    [TestMethod]
    public async Task DeactivateUser_ShouldDisableUserAccount()
    {
        // Arrange
        var page = await StartApplication();
        await TakeScreenshot("deactivate_user_startup");

        try
        {
            // Navigate to user management
            await page.ClickAsync("[data-testid='admin-nav-link']");
            await page.ClickAsync("[data-testid='user-management-link']");
            await WaitAndScreenshot("[data-testid='user-management-page']", "user_management_for_deactivate");

            // Deactivate user
            await page.ClickAsync("[data-testid='user-row']:first-child [data-testid='deactivate-user-button']");
            await WaitAndScreenshot("[data-testid='deactivate-confirmation']", "deactivate_confirmation_dialog");

            await page.ClickAsync("[data-testid='confirm-deactivate-button']");
            
            // Assert - Verify user status changed
            await WaitAndScreenshot("[data-testid='users-list']", "users_list_after_deactivate");
            
            var deactivatedStatus = await page.IsVisibleAsync("[data-testid='user-row']:first-child text=Inactive");
            Assert.IsTrue(deactivatedStatus, "User should show as inactive after deactivation");

            await TakeScreenshot("deactivate_user_test_completed");
        }
        catch (Exception ex)
        {
            await TakeScreenshot("deactivate_user_test_failed");
            throw new AssertFailedException($"Deactivate user test failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Test deleting a user permanently
    /// </summary>
    [TestMethod]
    public async Task DeleteUser_ShouldRemoveUserFromSystem()
    {
        // Arrange
        var page = await StartApplication();
        await TakeScreenshot("delete_user_startup");

        try
        {
            // Navigate to user management
            await page.ClickAsync("[data-testid='admin-nav-link']");
            await page.ClickAsync("[data-testid='user-management-link']");
            await WaitAndScreenshot("[data-testid='user-management-page']", "user_management_for_delete");

            // Get initial user count
            var initialCount = await page.Locator("[data-testid='user-row']").CountAsync();

            // Delete user
            await page.ClickAsync("[data-testid='user-row']:last-child [data-testid='delete-user-button']");
            await WaitAndScreenshot("[data-testid='delete-user-confirmation']", "delete_user_confirmation");

            await page.ClickAsync("[data-testid='confirm-delete-user-button']");
            
            // Assert - Verify user was removed
            await WaitAndScreenshot("[data-testid='users-list']", "users_list_after_delete");
            
            var finalCount = await page.Locator("[data-testid='user-row']").CountAsync();
            Assert.AreEqual(initialCount - 1, finalCount, "User count should decrease by 1 after deletion");

            await TakeScreenshot("delete_user_test_completed");
        }
        catch (Exception ex)
        {
            await TakeScreenshot("delete_user_test_failed");
            throw new AssertFailedException($"Delete user test failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Test user creation form validation
    /// </summary>
    [TestMethod]
    public async Task CreateUser_WithInvalidData_ShouldShowValidationErrors()
    {
        // Arrange
        var page = await StartApplication();
        await TakeScreenshot("user_validation_startup");

        try
        {
            // Navigate to create user dialog
            await page.ClickAsync("[data-testid='admin-nav-link']");
            await page.ClickAsync("[data-testid='user-management-link']");
            await page.ClickAsync("[data-testid='add-user-button']");
            await WaitAndScreenshot("[data-testid='create-user-dialog']", "user_validation_dialog");

            // Test email validation
            await page.FillAsync("[data-testid='email-textbox']", "invalid-email");
            await page.ClickAsync("[data-testid='password-textbox']"); // Trigger validation
            
            await TakeScreenshot("email_validation_error");
            var emailError = await page.IsVisibleAsync("[data-testid='email-validation-error']");
            Assert.IsTrue(emailError, "Email validation error should be displayed");

            // Test password mismatch
            await page.FillAsync("[data-testid='email-textbox']", "valid@example.com");
            await page.FillAsync("[data-testid='password-textbox']", "Password123!");
            await page.FillAsync("[data-testid='confirm-password-textbox']", "DifferentPassword123!");
            
            await TakeScreenshot("password_mismatch_error");
            var passwordError = await page.IsVisibleAsync("[data-testid='password-mismatch-error']");
            Assert.IsTrue(passwordError, "Password mismatch error should be displayed");

            // Test required fields
            await page.ClickAsync("[data-testid='create-user-button']");
            
            await TakeScreenshot("required_fields_validation");
            var requiredFieldsError = await page.IsVisibleAsync("[data-testid='validation-summary']");
            Assert.IsTrue(requiredFieldsError, "Required fields validation should be displayed");

            // Cancel dialog
            await page.ClickAsync("[data-testid='cancel-user-button']");
            await TakeScreenshot("user_validation_test_completed");
        }
        catch (Exception ex)
        {
            await TakeScreenshot("user_validation_test_failed");
            throw new AssertFailedException($"User validation test failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Test user search and filtering functionality
    /// </summary>
    [TestMethod]
    public async Task SearchUsers_ShouldFilterUsersList()
    {
        // Arrange
        var page = await StartApplication();
        await TakeScreenshot("user_search_startup");

        try
        {
            // Navigate to user management
            await page.ClickAsync("[data-testid='admin-nav-link']");
            await page.ClickAsync("[data-testid='user-management-link']");
            await WaitAndScreenshot("[data-testid='user-management-page']", "user_management_for_search");

            // Get initial user count
            var initialCount = await page.Locator("[data-testid='user-row']").CountAsync();

            // Test search functionality
            await page.FillAsync("[data-testid='user-search-textbox']", "admin");
            await page.PressAsync("[data-testid='user-search-textbox']", "Enter");
            
            await TakeScreenshot("user_search_results");

            // Verify filtered results
            var filteredCount = await page.Locator("[data-testid='user-row']").CountAsync();
            Assert.IsTrue(filteredCount <= initialCount, "Filtered results should be less than or equal to initial count");

            // Clear search
            await page.FillAsync("[data-testid='user-search-textbox']", "");
            await page.PressAsync("[data-testid='user-search-textbox']", "Enter");
            
            await TakeScreenshot("user_search_cleared");

            // Verify all users are shown again
            var clearedCount = await page.Locator("[data-testid='user-row']").CountAsync();
            Assert.AreEqual(initialCount, clearedCount, "All users should be visible after clearing search");

            await TakeScreenshot("user_search_test_completed");
        }
        catch (Exception ex)
        {
            await TakeScreenshot("user_search_test_failed");
            throw new AssertFailedException($"User search test failed: {ex.Message}", ex);
        }
    }
}