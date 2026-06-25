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

    [TestMethod]
    public async Task HomePage_ShowsSeededDemoContent()
    {
        await Expect(Page.GetByText("Chase Bank", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByText("Personal Gmail", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByText("Banking", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByPlaceholder("Search your vault...")).ToBeVisibleAsync();

        await SaveEvidenceAsync("seeded-demo-home");
    }

    [TestMethod]
    public async Task Search_CanFindSeededItems()
    {
        await Page.GetByPlaceholder("Search your vault...").FillAsync("Netflix");

        await Expect(Page.GetByText("Netflix", new() { Exact = true })).ToBeVisibleAsync();
        await SaveEvidenceAsync("seeded-demo-search");
    }
}
