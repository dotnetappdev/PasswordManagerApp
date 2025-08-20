using Microsoft.Playwright;

namespace PasswordManager.Tests.Playwright;

/// <summary>
/// Base test class for Playwright UI tests providing common setup and utilities
/// </summary>
[TestClass]
public abstract class PlaywrightTestBase : PageTest
{
    protected static string AppExecutablePath = "";
    protected static bool IsAppRunning = false;

    /// <summary>
    /// Initialize Playwright and application setup
    /// </summary>
    [AssemblyInitialize]
    public static Task AssemblyInitialize(TestContext context)
    {
        // Install playwright browsers if needed
        Microsoft.Playwright.Program.Main(new[] { "install" });
        
        // Set up application path - this would be the built WinUI app
        AppExecutablePath = GetApplicationPath();
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clean up resources
    /// </summary>
    [AssemblyCleanup]
    public static async Task AssemblyCleanup()
    {
        // Cleanup any running instances
        if (IsAppRunning)
        {
            await StopApplication();
        }
    }

    /// <summary>
    /// Set up each test with a new browser context
    /// </summary>
    [TestInitialize]
    public async Task TestInitialize()
    {
        // Configure browser for desktop app testing
        await Context.Tracing.StartAsync(new()
        {
            Title = TestContext.TestName,
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });
    }

    /// <summary>
    /// Clean up after each test and save artifacts
    /// </summary>
    [TestCleanup]
    public async Task TestCleanup()
    {
        // Save trace for debugging
        var tracePath = Path.Combine(TestContext.TestResultsDirectory ?? "TestResults", 
            $"{TestContext.TestName}.zip");
        await Context.Tracing.StopAsync(new() { Path = tracePath });

        // Take final screenshot if test failed
        if (TestContext.CurrentTestOutcome == UnitTestOutcome.Failed)
        {
            var screenshotPath = Path.Combine(TestContext.TestResultsDirectory ?? "TestResults",
                $"{TestContext.TestName}_failure.png");
            await Page.ScreenshotAsync(new() { Path = screenshotPath, FullPage = true });
        }
    }

    /// <summary>
    /// Get the path to the WinUI application executable
    /// </summary>
    private static string GetApplicationPath()
    {
        // In a real scenario, this would point to the built WinUI executable
        // For development/CI, this might be in bin/Debug or bin/Release
        var baseDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        var appPath = Path.Combine(baseDir!, "..", "..", "..", "..", "PasswordManager.WinUi", 
            "bin", "Debug", "net9.0-windows10.0.19041.0", "win-x64", "PasswordManager.WinUi.exe");
        
        return Path.GetFullPath(appPath);
    }

    /// <summary>
    /// Start the WinUI application for testing
    /// </summary>
    protected Task<IPage> StartApplication()
    {
        if (!File.Exists(AppExecutablePath))
        {
            throw new FileNotFoundException($"Application not found at: {AppExecutablePath}. Ensure the WinUI app is built.");
        }

        // For WinUI apps, we might need to use Windows Application Driver or similar
        // This is a placeholder for the actual application launch logic
        
        // Note: In a real implementation, you would either:
        // 1. Use Windows Application Driver (WinAppDriver) for WinUI testing
        // 2. Use Playwright with a web version of the app
        // 3. Use specialized WinUI testing frameworks
        
        IsAppRunning = true;
        return Task.FromResult(Page);
    }

    /// <summary>
    /// Stop the running application
    /// </summary>
    protected static async Task StopApplication()
    {
        // Implementation would depend on how the app was started
        IsAppRunning = false;
        await Task.CompletedTask;
    }

    /// <summary>
    /// Take a screenshot with a descriptive name
    /// </summary>
    protected async Task TakeScreenshot(string name)
    {
        var screenshotPath = Path.Combine(TestContext.TestResultsDirectory ?? "Screenshots",
            $"{TestContext.TestName}_{name}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
        
        Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath)!);
        await Page.ScreenshotAsync(new() { Path = screenshotPath, FullPage = true });
        
        TestContext.WriteLine($"Screenshot saved: {screenshotPath}");
    }

    /// <summary>
    /// Wait for element to be visible and take screenshot
    /// </summary>
    protected async Task WaitAndScreenshot(string selector, string screenshotName)
    {
        await Page.WaitForSelectorAsync(selector, new() { State = WaitForSelectorState.Visible });
        await TakeScreenshot(screenshotName);
    }
}