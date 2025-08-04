using Microsoft.Playwright;
using Xunit;
using System.IO;

namespace PasswordManager.Tests.UI;

/// <summary>
/// Real UI automation demonstration using Playwright to show how comprehensive UI testing would work.
/// This demonstrates the approach for testing the Password Manager web application with screenshots.
/// </summary>
public class PlaywrightUIDemo : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage? _page;
    private readonly string _screenshotsDirectory;

    public PlaywrightUIDemo()
    {
        // Create screenshots directory
        var testDataDirectory = Path.Combine(Path.GetTempPath(), "PasswordManagerUIDemo", Guid.NewGuid().ToString());
        _screenshotsDirectory = Path.Combine(testDataDirectory, "screenshots");
        Directory.CreateDirectory(_screenshotsDirectory);
    }

    public async Task InitializeAsync()
    {
        // Install and setup Playwright browsers
        Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
        
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false, // Show browser for demonstration
            SlowMo = 1000 // Slow down for visibility
        });
        
        _page = await _browser.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        await _page?.CloseAsync();
        await _browser?.CloseAsync();
        _playwright?.Dispose();
    }

    [Fact]
    public async Task DemonstrateUITestingCapabilities_WithScreenshots()
    {
        Assert.NotNull(_page);
        
        // Demo 1: Navigate to a sample page and take screenshot
        await _page.GotoAsync("https://example.com");
        await TakeScreenshotAsync("01-navigation-demo", "Navigation to example.com");
        
        // Demo 2: Show form interaction capabilities
        await _page.GotoAsync("data:text/html,<html><head><title>Password Manager UI Demo</title></head><body><h1>Password Manager UI Test Demo</h1><form><label>Master Password:</label><input type='password' id='password' placeholder='Enter master password'><br><br><label>Website:</label><input type='text' id='website' placeholder='Website URL'><br><br><label>Username:</label><input type='text' id='username' placeholder='Username'><br><br><button type='submit'>Save Password</button></form></body></html>");
        await TakeScreenshotAsync("02-demo-form", "Sample password manager form");
        
        // Demo 3: Fill form fields to show interaction
        await _page.FillAsync("#password", "DemoMasterPassword123!");
        await _page.FillAsync("#website", "https://example.com");
        await _page.FillAsync("#username", "demouser");
        await TakeScreenshotAsync("03-form-filled", "Form filled with demo data");
        
        // Demo 4: Show mobile viewport testing
        await _page.SetViewportSizeAsync(375, 667); // iPhone size
        await TakeScreenshotAsync("04-mobile-view", "Mobile viewport demonstration");
        
        // Demo 5: Reset to desktop view
        await _page.SetViewportSizeAsync(1280, 720);
        await TakeScreenshotAsync("05-desktop-view", "Desktop viewport demonstration");
        
        // Verify screenshots were created
        var screenshotFiles = Directory.GetFiles(_screenshotsDirectory, "*.png");
        Assert.True(screenshotFiles.Length >= 5, "All demonstration screenshots should be created");
        
        // Log results
        Console.WriteLine($"UI Testing Demo Complete!");
        Console.WriteLine($"Screenshots saved to: {_screenshotsDirectory}");
        Console.WriteLine($"Total screenshots: {screenshotFiles.Length}");
        
        foreach (var file in screenshotFiles)
        {
            Console.WriteLine($"- {Path.GetFileName(file)}");
        }
    }

    private async Task TakeScreenshotAsync(string filename, string description)
    {
        var screenshotPath = Path.Combine(_screenshotsDirectory, $"{filename}.png");
        await _page!.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = screenshotPath,
            FullPage = true
        });
        
        Console.WriteLine($"📸 Screenshot: {filename} - {description}");
        Console.WriteLine($"   Saved to: {screenshotPath}");
    }
}