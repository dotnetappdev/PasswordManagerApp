using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using PasswordManager.DAL;
using PasswordManager.Models;
using PasswordManager.Services.Services;

namespace PasswordManager.BackEnd.Tests.Services;

[TestFixture]
public class CollectionServiceTests
{
    private DbContextOptions<PasswordManagerDbContext> _options = null!;
    private PasswordManagerDbContext _context = null!;
    private CollectionService _collectionService = null!;
    private const string TestUserId = "test-user-id";

    [SetUp]
    public void Setup()
    {
        _options = new DbContextOptionsBuilder<PasswordManagerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PasswordManagerDbContext(_options);
        _collectionService = new CollectionService(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Test]
    public async Task CreateAsync_WhenNoCollectionsExist_ShouldMarkAsDefault()
    {
        // Arrange
        var collection = new Collection
        {
            Name = "Test Collection",
            Description = "Test Description",
            UserId = TestUserId
        };

        // Act
        var result = await _collectionService.CreateAsync(collection);

        // Assert
        Assert.That(result.IsDefault, Is.True);
        Assert.That(result.Name, Is.EqualTo("Test Collection"));
        Assert.That(result.UserId, Is.EqualTo(TestUserId));
    }

    [Test]
    public async Task CreateAsync_WhenCollectionsExist_ShouldNotMarkAsDefault()
    {
        // Arrange
        var existingCollection = new Collection
        {
            Name = "Existing Collection",
            UserId = TestUserId,
            IsDefault = true
        };
        _context.Collections.Add(existingCollection);
        await _context.SaveChangesAsync();

        var newCollection = new Collection
        {
            Name = "New Collection",
            Description = "New Description",
            UserId = TestUserId
        };

        // Act
        var result = await _collectionService.CreateAsync(newCollection);

        // Assert
        Assert.That(result.IsDefault, Is.False);
        Assert.That(result.Name, Is.EqualTo("New Collection"));
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnAllCollectionsWithCategories()
    {
        // Arrange
        var collection1 = new Collection { Name = "Collection 1", UserId = TestUserId };
        var collection2 = new Collection { Name = "Collection 2", UserId = TestUserId };
        var category1 = new Category { Name = "Category 1", Collection = collection1, UserId = TestUserId };
        
        collection1.Categories.Add(category1);

        _context.Collections.AddRange(collection1, collection2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _collectionService.GetAllAsync();

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.First(c => c.Name == "Collection 1").Categories, Has.Count.EqualTo(1));
        Assert.That(result.First(c => c.Name == "Collection 2").Categories, Has.Count.EqualTo(0));
    }

    [Test]
    public async Task GetByIdAsync_WithValidId_ShouldReturnCollectionWithRelatedData()
    {
        // Arrange
        var collection = new Collection { Name = "Test Collection", UserId = TestUserId };
        var category = new Category { Name = "Test Category", Collection = collection, UserId = TestUserId };
        var passwordItem = new PasswordItem 
        { 
            Title = "Test Password", 
            Collection = collection, 
            UserId = TestUserId,
            Type = ItemType.Login
        };
        
        collection.Categories.Add(category);
        collection.PasswordItems.Add(passwordItem);

        _context.Collections.Add(collection);
        await _context.SaveChangesAsync();

        // Act
        var result = await _collectionService.GetByIdAsync(collection.Id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("Test Collection"));
        Assert.That(result.Categories, Has.Count.EqualTo(1));
        Assert.That(result.PasswordItems, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await _collectionService.GetByIdAsync(999);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task UpdateAsync_WithValidCollection_ShouldUpdateProperties()
    {
        // Arrange
        var originalCollection = new Collection
        {
            Name = "Original Name",
            Description = "Original Description",
            UserId = TestUserId
        };
        _context.Collections.Add(originalCollection);
        await _context.SaveChangesAsync();

        var updatedCollection = new Collection
        {
            Id = originalCollection.Id,
            Name = "Updated Name",
            Description = "Updated Description",
            Icon = "new-icon",
            Color = "#FF0000",
            UserId = TestUserId
        };

        // Act
        var result = await _collectionService.UpdateAsync(updatedCollection);

        // Assert
        Assert.That(result.Name, Is.EqualTo("Updated Name"));
        Assert.That(result.Description, Is.EqualTo("Updated Description"));
        Assert.That(result.Icon, Is.EqualTo("new-icon"));
        Assert.That(result.Color, Is.EqualTo("#FF0000"));
    }

    [Test]
    public async Task UpdateAsync_WithInvalidId_ShouldThrowException()
    {
        // Arrange
        var collection = new Collection
        {
            Id = 999,
            Name = "Non-existent Collection",
            UserId = TestUserId
        };

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            () => _collectionService.UpdateAsync(collection));
        
        Assert.That(ex?.Message, Does.Contain("Collection with ID 999 not found"));
    }

    [Test]
    public async Task DeleteAsync_WithDefaultCollection_ShouldPromoteAnotherToDefault()
    {
        // Arrange
        var defaultCollection = new Collection
        {
            Name = "Default Collection",
            IsDefault = true,
            UserId = TestUserId
        };
        var otherCollection = new Collection
        {
            Name = "Other Collection",
            IsDefault = false,
            UserId = TestUserId
        };

        _context.Collections.AddRange(defaultCollection, otherCollection);
        await _context.SaveChangesAsync();

        // Act
        await _collectionService.DeleteAsync(defaultCollection.Id);

        // Assert
        var remaining = await _context.Collections.ToListAsync();
        Assert.That(remaining, Has.Count.EqualTo(1));
        Assert.That(remaining.First().IsDefault, Is.True);
        Assert.That(remaining.First().Name, Is.EqualTo("Other Collection"));
    }

    [Test]
    public async Task DeleteAsync_WithPasswordItems_ShouldMoveToDefaultCollection()
    {
        // Arrange
        var defaultCollection = new Collection
        {
            Name = "Default Collection",
            IsDefault = true,
            UserId = TestUserId
        };
        var collectionToDelete = new Collection
        {
            Name = "Collection to Delete",
            IsDefault = false,
            UserId = TestUserId
        };
        var passwordItem = new PasswordItem
        {
            Title = "Test Password",
            Collection = collectionToDelete,
            UserId = TestUserId,
            Type = ItemType.Login
        };

        _context.Collections.AddRange(defaultCollection, collectionToDelete);
        _context.PasswordItems.Add(passwordItem);
        await _context.SaveChangesAsync();

        // Act
        await _collectionService.DeleteAsync(collectionToDelete.Id);

        // Assert
        var remainingItem = await _context.PasswordItems.FirstAsync();
        Assert.That(remainingItem.CollectionId, Is.EqualTo(defaultCollection.Id));
    }

    [Test]
    public async Task DeleteAsync_WithNonExistentCollection_ShouldNotThrow()
    {
        // Act
        Assert.DoesNotThrowAsync(() => _collectionService.DeleteAsync(999));
    }

    [Test]
    public async Task GetDefaultCollectionAsync_WhenDefaultExists_ShouldReturnIt()
    {
        // Arrange
        var defaultCollection = new Collection
        {
            Name = "Default Collection",
            IsDefault = true,
            UserId = TestUserId
        };
        var otherCollection = new Collection
        {
            Name = "Other Collection",
            IsDefault = false,
            UserId = TestUserId
        };

        _context.Collections.AddRange(defaultCollection, otherCollection);
        await _context.SaveChangesAsync();

        // Act
        var result = await _collectionService.GetDefaultCollectionAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("Default Collection"));
        Assert.That(result.IsDefault, Is.True);
    }

    [Test]
    public async Task GetDefaultCollectionAsync_WhenNoDefaultButCollectionsExist_ShouldPromoteFirst()
    {
        // Arrange
        var collection1 = new Collection
        {
            Name = "Collection 1",
            IsDefault = false,
            UserId = TestUserId
        };
        var collection2 = new Collection
        {
            Name = "Collection 2",
            IsDefault = false,
            UserId = TestUserId
        };

        _context.Collections.AddRange(collection1, collection2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _collectionService.GetDefaultCollectionAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsDefault, Is.True);
        
        // Verify the first collection was promoted to default
        var updatedFirst = await _context.Collections.FirstAsync();
        Assert.That(updatedFirst.IsDefault, Is.True);
    }

    [Test]
    public async Task GetDefaultCollectionAsync_WhenNoCollectionsExist_ShouldReturnNull()
    {
        // Act
        var result = await _collectionService.GetDefaultCollectionAsync();

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task SetAsDefaultAsync_ShouldSetCorrectCollectionAsDefault()
    {
        // Arrange
        var collection1 = new Collection
        {
            Name = "Collection 1",
            IsDefault = true,
            UserId = TestUserId
        };
        var collection2 = new Collection
        {
            Name = "Collection 2",
            IsDefault = false,
            UserId = TestUserId
        };
        var collection3 = new Collection
        {
            Name = "Collection 3",
            IsDefault = false,
            UserId = TestUserId
        };

        _context.Collections.AddRange(collection1, collection2, collection3);
        await _context.SaveChangesAsync();

        // Act
        await _collectionService.SetAsDefaultAsync(collection2.Id);

        // Assert
        var collections = await _context.Collections.ToListAsync();
        Assert.That(collections.Single(c => c.Id == collection1.Id).IsDefault, Is.False);
        Assert.That(collections.Single(c => c.Id == collection2.Id).IsDefault, Is.True);
        Assert.That(collections.Single(c => c.Id == collection3.Id).IsDefault, Is.False);
    }
}