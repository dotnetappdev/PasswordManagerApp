using Microsoft.Playwright;
using NUnit.Framework;

namespace PasswordManager.Tests.Playwright;

/// <summary>
/// Tests for validating user input validation on login and registration forms
/// </summary>
[TestFixture]
public class ValidationTests : PlaywrightTestBase
{
    [Test]
    [Category("Validation")]
    public async Task LoginPage_RejectsEmptyPassword()
    {
        await Page.GotoAsync(BaseUrl + "/login");
        
        // Wait for the login page to load
        await Page.WaitForSelectorAsync("input[type='password']", new() { Timeout = 5000 });
        
        // Try to submit with empty password (if button is enabled)
        var continueButton = Page.Locator("button:has-text('Continue'), button:has-text('Unlock')");
        if (await continueButton.IsEnabledAsync())
        {
            await continueButton.ClickAsync();
            
            // Should see an error message
            var errorMessage = await Page.Locator(".error-message").IsVisibleAsync();
            Assert.That(errorMessage, Is.True, "Error message should be visible for empty password");
        }
    }
    
    [Test]
    [Category("Validation")]
    public async Task LoginPage_RejectsSingleCharacterPassword()
    {
        await Page.GotoAsync(BaseUrl + "/login");
        
        // Wait for the password field
        await Page.WaitForSelectorAsync("input[type='password']", new() { Timeout = 5000 });
        
        // Enter single character password
        await Page.FillAsync("input[type='password']", "a");
        
        // Try to submit
        var continueButton = Page.Locator("button:has-text('Continue'), button:has-text('Unlock')");
        if (await continueButton.IsEnabledAsync())
        {
            await continueButton.ClickAsync();
            
            // Should see an error message about minimum length
            var errorText = await Page.Locator(".error-message").TextContentAsync();
            Assert.That(errorText, Does.Contain("at least").Or.Contain("characters"), 
                "Error message should mention minimum character requirement");
        }
    }
    
    [Test]
    [Category("Validation")]
    public async Task MasterKeySetup_RequiresComplexity()
    {
        // This test assumes first-time setup scenario
        await Page.GotoAsync(BaseUrl + "/login");
        
        // Wait for the page to load
        await Task.Delay(1000);
        
        // Check if we're on the master key setup page
        var setupHeading = await Page.Locator("h2:has-text('Create Master Key')").IsVisibleAsync();
        
        if (setupHeading)
        {
            // Try weak password
            await Page.FillAsync("input#masterKey", "password");
            await Page.FillAsync("input#confirmKey", "password");
            
            // Try to submit
            await Page.ClickAsync("button:has-text('Create Master Key')");
            
            // Should see error about password requirements
            await Task.Delay(500);
            var errorVisible = await Page.Locator(".error-message, .strength-text:has-text('Weak')").IsVisibleAsync();
            Assert.That(errorVisible, Is.True, "Should show password strength feedback or error");
        }
    }
    
    [Test]
    [Category("Validation")]
    public async Task MasterKeySetup_ShowsPasswordStrengthIndicator()
    {
        await Page.GotoAsync(BaseUrl + "/login");
        
        // Wait for the page to load
        await Task.Delay(1000);
        
        // Check if we're on the master key setup page
        var setupHeading = await Page.Locator("h2:has-text('Create Master Key')").IsVisibleAsync();
        
        if (setupHeading)
        {
            // Enter password to trigger validation
            await Page.FillAsync("input#masterKey", "Test123");
            
            // Wait a bit for strength indicator to update
            await Task.Delay(300);
            
            // Should see password strength indicator
            var strengthIndicator = await Page.Locator(".password-strength, .strength-text").IsVisibleAsync();
            Assert.That(strengthIndicator, Is.True, "Password strength indicator should be visible");
        }
    }
}
