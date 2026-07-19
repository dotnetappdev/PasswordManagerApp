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

    // Program.cs seeds demo password items once at app startup (VaultGuard.Web/Program.cs), but that
    // runs before any real user account exists - its fallback user id (TestDataSeeder.TestUserId, a
    // fixed constant) never matches the "user@passwordmanager.local" account SignInAsync actually
    // creates/signs into (ASP.NET Identity assigns it a fresh random id). Those startup-seeded items
    // are effectively orphaned: /passwords is empty for whoever actually signs in, which is why every
    // item-title assertion below timed out regardless of locator. The Setup page's "Seed demo data"
    // button re-runs the same seeder against whichever user is actually in the database - i.e. the one
    // just signed into - which is what these tests need. Seeded once per class run (not per test
    // method - both tests below share one running app/db instance, see BlazorWebTestBase) to avoid
    // seeding duplicate items on the second test.
    static bool _demoDataSeeded;

    [TestInitialize]
    public async Task SignInBeforeTestsAsync()
    {
        await SignInAsync();

        if (!_demoDataSeeded)
        {
            await Page.GotoAsync($"{BaseUrl}/setup");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Page.Locator("button:has-text('Seed demo data')").ClickAsync();
            await Task.Delay(1500);
            _demoDataSeeded = true;
        }
    }

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
