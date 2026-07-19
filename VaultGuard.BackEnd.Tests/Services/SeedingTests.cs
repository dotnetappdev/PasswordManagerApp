using Allure.NUnit;
using Allure.NUnit.Attributes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using VaultGuard.DAL;
using VaultGuard.DAL.Seed;
using VaultGuard.Models;

namespace VaultGuard.BackEnd.Tests.Services;

/// <summary>
/// Seeding tests backed by a real SQLite in-memory connection so that FK
/// constraints, cascade rules, and NOT-NULL violations behave identically
/// to the production database.  EF Core InMemory silently ignores all of
/// these, which is why failures only appeared at runtime.
/// </summary>
[TestFixture]
[AllureNUnit]
[AllureEpic("Platform & Infrastructure")]
[AllureFeature("Data Seeding")]
public class SeedingTests
{
    private SqliteConnection  _connection = null!;
    private VaultGuardDbContext _db = null!;
    private const string UserId = "seed-test-user-001";

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static VaultGuardDbContext BuildContext(SqliteConnection conn)
    {
        var opts = new DbContextOptionsBuilder<VaultGuardDbContext>()
            .UseSqlite(conn)
            .Options;
        return new VaultGuardDbContext(opts);
    }

    private void EnsureUser()
    {
        if (!_db.Users.Any(u => u.Id == UserId))
        {
            _db.Users.Add(new ApplicationUser
            {
                Id            = UserId,
                UserName      = "seed@test.local",
                Email         = "seed@test.local",
                SecurityStamp = Guid.NewGuid().ToString(),
                IsActive      = true
            });
            _db.SaveChanges();
        }
    }

    // ── Setup / Teardown ────────────────────────────────────────────────────

    [SetUp]
    public void Setup()
    {
        // Keep the same connection open for the lifetime of the test so the
        // in-memory SQLite database is not dropped between DbContext uses.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _db = BuildContext(_connection);
        _db.Database.EnsureCreated();   // creates schema from current EF model
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    // ── 1. Schema sanity ────────────────────────────────────────────────────

    [Test]
    public void Schema_PasswordItemsTable_DoesNotHaveVaultIdColumn()
    {
        // [NotMapped] on PasswordItem.VaultId means EF Core must not create
        // the column.  If the column is present we have a model mismatch that
        // will cause "no column named VaultId" at runtime.
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "PRAGMA table_info(PasswordItems);";
        using var reader = cmd.ExecuteReader();

        var columns = new List<string>();
        while (reader.Read())
            columns.Add(reader.GetString(1)); // column 1 = name

        Assert.That(columns, Does.Not.Contain("VaultId"),
            "VaultId is [NotMapped] — EF Core must not create the column. " +
            "If it appears here, the model and DbContext are still referencing it.");

        Assert.That(columns, Does.Contain("Title"),
            "PasswordItems table should have a Title column.");
    }

    [Test]
    public void Schema_CategoriesTable_DoesNotHaveVaultIdColumn()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "PRAGMA table_info(Categories);";
        using var reader = cmd.ExecuteReader();

        var columns = new List<string>();
        while (reader.Read())
            columns.Add(reader.GetString(1));

        Assert.That(columns, Does.Not.Contain("VaultId"),
            "Category.VaultId is [NotMapped] — must not appear in the table.");
    }

    // ── 2. Collections ──────────────────────────────────────────────────────

    [Test]
    public void SeedCollections_OnEmptyDb_InsertsRows()
    {
        EnsureUser();
        TestDataSeeder.SeedCollections(_db, UserId);

        var count = _db.Collections.Count(c => c.UserId == UserId);
        Assert.That(count, Is.EqualTo(5), "Seeder should insert exactly 5 collections.");
    }

    [Test]
    public void SeedCollections_CalledTwice_DoesNotDuplicate()
    {
        EnsureUser();
        TestDataSeeder.SeedCollections(_db, UserId);
        TestDataSeeder.SeedCollections(_db, UserId); // second call should be a no-op

        var count = _db.Collections.Count(c => c.UserId == UserId);
        Assert.That(count, Is.EqualTo(5));
    }

    // ── 3. Categories ───────────────────────────────────────────────────────

    [Test]
    public void SeedCategories_OnEmptyDb_InsertsRows()
    {
        EnsureUser();
        TestDataSeeder.SeedCategories(_db, UserId);

        var count = _db.Categories.Count(c => c.UserId == UserId);
        Assert.That(count, Is.GreaterThan(0));
    }

    [Test]
    public void SeedCategories_WithoutVaultId_DoesNotThrow()
    {
        EnsureUser();

        // Must not throw even though VaultId column is absent from the schema.
        Assert.DoesNotThrow(() => TestDataSeeder.SeedCategories(_db, UserId),
            "SeedCategories must succeed without a VaultId column.");
    }

    // ── 4. Tags ─────────────────────────────────────────────────────────────

    [Test]
    public void SeedTags_OnEmptyDb_InsertsRows()
    {
        EnsureUser();
        TestDataSeeder.SeedTags(_db, UserId);

        var count = _db.Tags.Count(t => t.UserId == UserId);
        Assert.That(count, Is.EqualTo(19));
    }

    // ── 5. Full pipeline ────────────────────────────────────────────────────

    [Test]
    public void SeedPasswordItemsForUser_RequiresCollectionsCategoriesTags()
    {
        // Calling SeedPasswordItemsForUser WITHOUT seeding collections/categories/tags
        // first means the FK lookups fall back to a hardcoded Id=1, which doesn't exist -
        // CategoryId/CollectionId are nullable columns, but a non-null value pointing at a
        // nonexistent row is still a real FK violation under SQLite (this test class exists
        // specifically to catch that; see class remarks).
        EnsureUser();

        // Do NOT seed collections / categories / tags
        Assert.Throws<DbUpdateException>(() =>
        {
            TestDataSeeder.SeedPasswordItemsForUser(_db, UserId);
        },
        "Seeder should fail fast on the FK violation when its collections/categories/tags " +
        "dependencies haven't been seeded first, rather than silently writing bad references.");

        // The failed SaveChanges rolled back - nothing should have been inserted.
        var count = _db.PasswordItems.Count(p => p.UserId == UserId);
        Assert.That(count, Is.Zero,
            "No password items should have been inserted once the batch insert failed.");
    }

    [Test]
    public void FullSeedPipeline_CollectionsThenCategoriesThenTagsThenItems_Succeeds()
    {
        // This is the correct order: collections → categories → tags → items.
        // All FK references are satisfied.
        EnsureUser();

        Assert.DoesNotThrow(() =>
        {
            TestDataSeeder.SeedCollections(_db, UserId);
            TestDataSeeder.SeedCategories(_db, UserId);
            TestDataSeeder.SeedTags(_db, UserId);
            TestDataSeeder.SeedPasswordItemsForUser(_db, UserId);
        }, "Full seed pipeline must complete without exceptions on SQLite.");

        var items = _db.PasswordItems
            .Include(p => p.LoginItem)
            .Include(p => p.Tags)
            .Where(p => p.UserId == UserId)
            .ToList();

        Assert.That(items.Count, Is.GreaterThan(0), "Password items should be present after seeding.");

        var loginsWithLoginItem = items.Where(p => p.Type == ItemType.Login && p.LoginItem != null).ToList();
        Assert.That(loginsWithLoginItem.Count, Is.GreaterThan(0),
            "At least one Login item should have a populated LoginItem child.");
    }

    // ── 6. ForceSeedPasswordItems ───────────────────────────────────────────

    [Test]
    public void ForceSeedPasswordItems_ClearsExistingThenReseeds()
    {
        EnsureUser();
        TestDataSeeder.SeedCollections(_db, UserId);
        TestDataSeeder.SeedCategories(_db, UserId);
        TestDataSeeder.SeedTags(_db, UserId);
        TestDataSeeder.SeedPasswordItemsForUser(_db, UserId);

        var countBefore = _db.PasswordItems.Count(p => p.UserId == UserId);
        Assert.That(countBefore, Is.GreaterThan(0), "Should have items before force-seed.");

        // Force-seed: removes and re-inserts
        Assert.DoesNotThrow(() => TestDataSeeder.ForceSeedPasswordItems(_db, UserId),
            "ForceSeedPasswordItems must not throw.");

        var countAfter = _db.PasswordItems.Count(p => p.UserId == UserId);
        Assert.That(countAfter, Is.GreaterThan(0),
            "Should have items after force-seed.");
        Assert.That(countAfter, Is.EqualTo(countBefore),
            "Item count should be the same after a force-reseed.");
    }

    [Test]
    public void ForceSeedPasswordItems_OnEmptyItemSet_Succeeds()
    {
        EnsureUser();
        TestDataSeeder.SeedCollections(_db, UserId);
        TestDataSeeder.SeedCategories(_db, UserId);
        TestDataSeeder.SeedTags(_db, UserId);

        // No items exist yet — ForceSeed should not crash on empty RemoveRange
        Assert.DoesNotThrow(() => TestDataSeeder.ForceSeedPasswordItems(_db, UserId));

        var count = _db.PasswordItems.Count(p => p.UserId == UserId);
        Assert.That(count, Is.GreaterThan(0));
    }

    // ── 7. FK integrity after seeding ───────────────────────────────────────

    [Test]
    public void SeededItems_CategoryIds_ReferenceExistingCategories()
    {
        EnsureUser();
        TestDataSeeder.SeedCollections(_db, UserId);
        TestDataSeeder.SeedCategories(_db, UserId);
        TestDataSeeder.SeedTags(_db, UserId);
        TestDataSeeder.SeedPasswordItemsForUser(_db, UserId);

        var validCategoryIds = _db.Categories
            .Select(c => (int?)c.Id)
            .ToHashSet();

        var itemsWithInvalidCategory = _db.PasswordItems
            .Where(p => p.UserId == UserId && p.CategoryId != null)
            .AsEnumerable()
            .Where(p => !validCategoryIds.Contains(p.CategoryId))
            .ToList();

        Assert.That(itemsWithInvalidCategory, Is.Empty,
            "Every seeded PasswordItem with a CategoryId must reference a real Category row.");
    }

    [Test]
    public void SeededItems_CollectionIds_ReferenceExistingCollections()
    {
        EnsureUser();
        TestDataSeeder.SeedCollections(_db, UserId);
        TestDataSeeder.SeedCategories(_db, UserId);
        TestDataSeeder.SeedTags(_db, UserId);
        TestDataSeeder.SeedPasswordItemsForUser(_db, UserId);

        var validCollectionIds = _db.Collections
            .Select(c => (int?)c.Id)
            .ToHashSet();

        var itemsWithInvalidCollection = _db.PasswordItems
            .Where(p => p.UserId == UserId && p.CollectionId != null)
            .AsEnumerable()
            .Where(p => !validCollectionIds.Contains(p.CollectionId))
            .ToList();

        Assert.That(itemsWithInvalidCollection, Is.Empty,
            "Every seeded PasswordItem with a CollectionId must reference a real Collection row.");
    }

    // ── 8. SampleDataSeeder flow simulation ─────────────────────────────────

    [Test]
    public void SampleDataSeederFlow_SeedsOnlyOncePerUser()
    {
        // Simulates what SampleDataSeeder.SeedSampleDataAsync does for a real user.
        EnsureUser();

        void RunOnce()
        {
            TestDataSeeder.SeedCollections(_db, UserId);
            TestDataSeeder.SeedCategories(_db, UserId);
            TestDataSeeder.SeedTags(_db, UserId);
            if (!_db.PasswordItems.Any(p => p.UserId == UserId))
                TestDataSeeder.SeedPasswordItemsForUser(_db, UserId);
        }

        RunOnce();
        var countAfterFirst = _db.PasswordItems.Count(p => p.UserId == UserId);

        RunOnce(); // second call — guards should prevent duplicates
        var countAfterSecond = _db.PasswordItems.Count(p => p.UserId == UserId);

        Assert.That(countAfterFirst, Is.GreaterThan(0));
        Assert.That(countAfterSecond, Is.EqualTo(countAfterFirst),
            "Running the seeder flow twice must not duplicate rows.");
    }
}
