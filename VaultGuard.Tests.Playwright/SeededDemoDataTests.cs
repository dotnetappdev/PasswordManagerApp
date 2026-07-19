using Microsoft.Playwright;

namespace VaultGuard.Tests.Playwright;

[TestClass]
public class SeededDemoDataTests : BlazorWebTestBase
{
    [ClassCleanup]
    public static Task CleanupAsync()
    {
        return StopAppAsync();
    }

    // Runs after the base class's own [TestInitialize] (NavigateToHomePageAsync) - both tests below
    // expect authenticated dashboard content, which never renders without this.
    [TestInitialize]
    public async Task SignInBeforeTestsAsync() => await SignInAsync();

    [TestMethod]
    public async Task HomePage_ShowsSeededDemoContent()
    {
        // The Dashboard's "Recent Items" widget only shows the 8 most-recently-modified items, and
        // has no search box at all - "Chase Bank" (the exact title never existed; the seeded login is
        // "Chase Bank Online") and "Search your vault..." (the real placeholder on /passwords is
        // "Search items...") were never actually on this page. /passwords lists every seeded item
        // unconditionally, so check the real seeded titles and the real search box there instead.
        await Page.GotoAsync($"{BaseUrl}/passwords");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByText("Chase Bank Online", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByText("Personal Gmail", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByPlaceholder("Search items...")).ToBeVisibleAsync();

        await SaveEvidenceAsync("seeded-demo-home");
    }

    [TestMethod]
    public async Task Search_CanFindSeededItems()
    {
        await Page.GotoAsync($"{BaseUrl}/passwords");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.GetByPlaceholder("Search items...").FillAsync("Netflix");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByText("Netflix", new() { Exact = true })).ToBeVisibleAsync();
        await SaveEvidenceAsync("seeded-demo-search");
    }
}
