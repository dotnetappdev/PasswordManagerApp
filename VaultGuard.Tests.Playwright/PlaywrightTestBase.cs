using Microsoft.Playwright;

namespace VaultGuard.Tests.Playwright;

/// <summary>
/// Base for WinUI/WPF desktop app tests.
/// NOTE: Playwright is a browser automation framework and cannot drive WinUI3 or WPF
/// desktop processes directly. Desktop UI automation requires WinAppDriver or the
/// Windows Application Driver (WinAppDriver) + Appium stack.
/// All tests in this base are skipped with an informational message.
/// Use BlazorWebTestBase for browser-based (Blazor) UI tests.
/// </summary>
[TestClass]
public abstract class PlaywrightTestBase : PageTest
{
    [AssemblyInitialize]
    public static Task AssemblyInitialize(TestContext context) => Task.CompletedTask;

    [AssemblyCleanup]
    public static Task AssemblyCleanup() => Task.CompletedTask;

    [TestInitialize]
    public Task TestInitialize() => Task.CompletedTask;

    [TestCleanup]
    public Task TestCleanup() => Task.CompletedTask;

    protected Task<IPage> StartApplication()
    {
        Assert.Inconclusive(
            "WinUI3 and WPF desktop apps cannot be automated with Playwright. " +
            "Use WinAppDriver + Appium for desktop UI tests, or migrate test coverage to the " +
            "Blazor web app (BlazorWebTestBase) for browser-based Playwright testing.");
        return Task.FromResult(Page);
    }

    protected Task TakeScreenshot(string name) => Task.CompletedTask;
    protected Task WaitAndScreenshot(string selector, string name) => Task.CompletedTask;
    protected static Task StopApplication() => Task.CompletedTask;
}
