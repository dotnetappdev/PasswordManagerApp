using Allure.NUnit;
using Allure.NUnit.Attributes;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using VaultGuard.DAL;
using VaultGuard.Models;
using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Services;

namespace VaultGuard.BackEnd.Tests.Services;

/// <summary>
/// Tests for <see cref="ApiKeyService"/> - the service behind "Generate API Key" and the
/// X-API-Key middleware. The security-critical properties are: keys are stored HASH-ONLY (the
/// plaintext is returned exactly once), validation matches by hash, and revoke is a soft delete
/// scoped to the owning user.
///
/// Each logical operation runs on its own DbContext over a shared in-memory database, mirroring the
/// request-scoped DbContext in production. That matters here because CreateApiKeyAsync deliberately
/// overwrites the returned entity's KeyHash with the plaintext AFTER saving (for one-time display);
/// a shared context would leak that mutation into later queries.
/// </summary>
[TestFixture]
[AllureNUnit]
[AllureEpic("Accounts & API Access")]
[AllureFeature("API Keys")]
public class ApiKeyServiceTests
{
    private string _dbName = null!;
    private Mock<IApiKeySqliteMirror> _mirror = null!;
    private const string UserId = "user-1";
    private const string OtherUserId = "user-2";

    [SetUp]
    public void Setup()
    {
        _dbName = Guid.NewGuid().ToString();
        _mirror = new Mock<IApiKeySqliteMirror>();
        _mirror.Setup(m => m.UpsertAsync(It.IsAny<ApiKey>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mirror.Setup(m => m.DeactivateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Seed the owning users. ValidateApiKeyAsync does .Include(k => k.User), and EF InMemory drops a
        // row whose required navigation principal is missing - so the key must belong to a real user row.
        using var ctx = NewContext();
        ctx.Users.Add(new ApplicationUser { Id = UserId, UserName = UserId, Email = "user1@example.com" });
        ctx.Users.Add(new ApplicationUser { Id = OtherUserId, UserName = OtherUserId, Email = "user2@example.com" });
        ctx.SaveChanges();
    }

    [TearDown]
    public void TearDown()
    {
        using var ctx = NewContext();
        ctx.Database.EnsureDeleted();
    }

    private VaultGuardDbContext NewContext() =>
        new(new DbContextOptionsBuilder<VaultGuardDbContext>().UseInMemoryDatabase(_dbName).Options);

    // Run an operation against a fresh service + context (like a single request), returning a result.
    private async Task<T> WithServiceAsync<T>(Func<IApiKeyService, Task<T>> op)
    {
        await using var ctx = NewContext();
        var service = new ApiKeyService(ctx, Mock.Of<IDatabaseConfigurationService>(), _mirror.Object);
        return await op(service);
    }

    [Test]
    public async Task CreateApiKey_ReturnsPlaintextOnce_ButStoresOnlyTheHash()
    {
        var created = await WithServiceAsync(s => s.CreateApiKeyAsync("My key", UserId));
        var plaintext = created.KeyHash; // the returned object carries the PLAINTEXT (one-time display)
        Assert.That(plaintext, Is.Not.Empty);

        await using var ctx = NewContext();
        var stored = await ctx.ApiKeys.AsNoTracking().SingleAsync(k => k.Id == created.Id);
        Assert.That(stored.KeyHash, Is.Not.EqualTo(plaintext), "The plaintext key must never be persisted.");
        Assert.That(stored.IsActive, Is.True);
        Assert.That(stored.UserId, Is.EqualTo(UserId));
    }

    [Test]
    public async Task CreateApiKey_GeneratesUniqueKeys()
    {
        var a = await WithServiceAsync(s => s.CreateApiKeyAsync("a", UserId));
        var b = await WithServiceAsync(s => s.CreateApiKeyAsync("b", UserId));

        Assert.That(a.KeyHash, Is.Not.EqualTo(b.KeyHash));
    }

    [Test]
    public async Task ValidateApiKey_WithCorrectPlaintext_ReturnsTheKey()
    {
        var created = await WithServiceAsync(s => s.CreateApiKeyAsync("My key", UserId));
        var plaintext = created.KeyHash;

        var validated = await WithServiceAsync(s => s.ValidateApiKeyAsync(plaintext));

        Assert.That(validated, Is.Not.Null);
        Assert.That(validated!.Id, Is.EqualTo(created.Id));
    }

    [Test]
    public async Task ValidateApiKey_WithWrongValue_ReturnsNull()
    {
        await WithServiceAsync(s => s.CreateApiKeyAsync("My key", UserId));

        var validated = await WithServiceAsync(s => s.ValidateApiKeyAsync("not-a-real-key"));

        Assert.That(validated, Is.Null);
    }

    [Test]
    public async Task ValidateApiKey_AfterRevoke_ReturnsNull()
    {
        var created = await WithServiceAsync(s => s.CreateApiKeyAsync("My key", UserId));
        var plaintext = created.KeyHash;

        await WithServiceAsync(s => s.DeleteApiKeyAsync(created.Id, UserId));

        var validated = await WithServiceAsync(s => s.ValidateApiKeyAsync(plaintext));
        Assert.That(validated, Is.Null, "A revoked (inactive) key must not authenticate.");
    }

    [Test]
    public async Task DeleteApiKey_ByNonOwner_DoesNothing()
    {
        var created = await WithServiceAsync(s => s.CreateApiKeyAsync("My key", UserId));

        var result = await WithServiceAsync(s => s.DeleteApiKeyAsync(created.Id, OtherUserId));

        Assert.That(result, Is.False, "A user must not be able to revoke another user's key.");
        await using var ctx = NewContext();
        var stored = await ctx.ApiKeys.AsNoTracking().SingleAsync(k => k.Id == created.Id);
        Assert.That(stored.IsActive, Is.True);
    }

    [Test]
    public async Task GetUserApiKeys_ReturnsOnlyActiveKeysForThatUser()
    {
        await WithServiceAsync(s => s.CreateApiKeyAsync("mine-1", UserId));
        var revoked = await WithServiceAsync(s => s.CreateApiKeyAsync("mine-2", UserId));
        await WithServiceAsync(s => s.CreateApiKeyAsync("theirs", OtherUserId));
        await WithServiceAsync(s => s.DeleteApiKeyAsync(revoked.Id, UserId));

        var keys = await WithServiceAsync(s => s.GetUserApiKeysAsync(UserId));

        Assert.That(keys, Has.Count.EqualTo(1));
        Assert.That(keys[0].Name, Is.EqualTo("mine-1"));
    }

    [Test]
    public async Task UpdateLastUsed_SetsLastUsedAt()
    {
        var created = await WithServiceAsync(s => s.CreateApiKeyAsync("My key", UserId));

        await WithServiceAsync<object?>(async s => { await s.UpdateLastUsedAsync(created.Id); return null; });

        await using var ctx = NewContext();
        var stored = await ctx.ApiKeys.AsNoTracking().SingleAsync(k => k.Id == created.Id);
        Assert.That(stored.LastUsedAt, Is.Not.Null);
    }
}
