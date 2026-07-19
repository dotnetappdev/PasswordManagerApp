using Allure.NUnit;
using Allure.NUnit.Attributes;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using VaultGuard.DAL;
using VaultGuard.DAL.Seed;
using VaultGuard.Models;
using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Services;

namespace VaultGuard.BackEnd.Tests.Services;

[TestFixture]
[AllureNUnit]
[AllureEpic("Vault Data Management")]
[AllureFeature("Categories & Collections")]
[AllureParentSuite("Vault Data Management")]
[AllureSuite("Categories & Collections")]
[AllureStory("Categories Service")]
[AllureSubSuite("Categories Service")]
public class CategoryServiceTests
{
    private DbContextOptions<VaultGuardDbContext> _options = null!;
    private VaultGuardDbContext _context = null!;
    private CategoryService _service = null!;
    private Mock<IAuthService> _authMock = null!;
    private const string UserId = "test-user-id-12345";

    [AllureBefore("Create a fresh EF Core in-memory database context")]
    [SetUp]
    public void Setup()
    {
        _options = new DbContextOptionsBuilder<VaultGuardDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new VaultGuardDbContext(_options);

        _authMock = new Mock<IAuthService>();
        _service  = new CategoryService(_context, _authMock.Object);
    }

    [AllureAfter("Drop and dispose the in-memory database context")]
    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    // ── This test proves WHY the dropdown shows more than 6 items ──────────

    [Test]
    public async Task GetAllAsync_AfterSeeder_ReturnsTwentyFourRows()
    {
        // The seeder inserts 24 category rows (the full 1Password-style list).
        // GetAllAsync returns ALL of them with no filtering, so the dropdown
        // always shows 24 items — not the 6 shown in the navigation sidebar.
        TestDataSeeder.SeedCategories(_context, UserId);

        var result = await _service.GetAllAsync();

        Assert.That(result.Count, Is.EqualTo(24),
            "Seeder inserts 24 rows; GetAllAsync returns all of them — " +
            "this is why the dropdown shows far more than 6 categories.");
    }

    [Test]
    public async Task GetAllAsync_WhenSeederRunTwice_ReturnsDuplicates()
    {
        // If the seeder guard (Any() check) fails — e.g. two startup paths
        // both call SeedCategories before the first SaveChanges commits —
        // the same 24 names are inserted twice, giving 48 rows (all unique IDs).
        // The UI then shows every name twice.
        _context.Categories.AddRange(BuildDuplicateCategories(UserId));
        await _context.SaveChangesAsync();

        var result = await _service.GetAllAsync();
        var duplicateNames = result
            .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.That(duplicateNames, Is.Not.Empty,
            "When the seeder runs twice, GetAllAsync returns duplicate names. " +
            "The dialog must deduplicate before populating the ComboBox.");
    }

    [Test]
    public async Task GetAllAsync_WithDeduplication_StillExceedsSixItems()
    {
        // Even after deduplicating by name, 24 unique categories remain.
        // The dialog needs a fixed, curated list — not a DB query — for the
        // type selector that mirrors the 6 navigation sidebar items.
        TestDataSeeder.SeedCategories(_context, UserId);

        var all = await _service.GetAllAsync();
        var deduped = all
            .GroupBy(c => c.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        Assert.That(deduped.Count, Is.GreaterThan(6),
            "Deduplication alone still leaves 24 distinct categories. " +
            "The Add-Password dialog must use a hardcoded 6-item type list, " +
            "not GetAllAsync(), for its Category dropdown.");
    }

    [Test]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        var result = await _service.GetAllAsync();
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task CreateAsync_NewCategory_IsPersisted()
    {
        _authMock.Setup(a => a.CurrentUser)
                 .Returns(new ApplicationUser { Id = UserId });

        var cat = new Category { Name = "Banking", Icon = "🏦", Color = "#10b981", UserId = UserId };

        var created = await _service.CreateAsync(cat);

        Assert.That(created.Id, Is.GreaterThan(0));
        Assert.That(await _context.Categories.CountAsync(), Is.EqualTo(1));
    }

    // ── Helper ──────────────────────────────────────────────────────────────

    private static List<Category> BuildDuplicateCategories(string userId)
    {
        // Simulate seeder running twice: same names, different IDs (auto-assigned)
        var names = new[] { "Login", "Secure Note", "Credit Card", "Identity", "Password" };
        var list  = new List<Category>();
        foreach (var pass in Enumerable.Range(0, 2))
            foreach (var name in names)
                list.Add(new Category { Name = name, Icon = "📁", Color = "#fff", UserId = userId });
        return list;
    }
}
