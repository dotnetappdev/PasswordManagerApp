using NUnit.Framework;
using PasswordManager.WinUi.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PasswordManager.WinUi.Tests.Services;

[TestFixture]
public class WinUiSecureStorageServiceTests
{
    private WinUiSecureStorageService _service = null!;
    private string _testKey = null!;

    [SetUp]
    public void Setup()
    {
        _service = new WinUiSecureStorageService();
        _testKey = $"test_key_{Guid.NewGuid()}";
    }

    [TearDown]
    public async Task TearDown()
    {
        // Clean up test data
        await _service.RemoveAsync(_testKey);
    }

    [Test]
    public async Task SetAsync_WhenCalled_ShouldStoreValue()
    {
        // Arrange
        var testValue = "test_value_123";

        // Act
        await _service.SetAsync(_testKey, testValue);
        var retrievedValue = await _service.GetAsync(_testKey);

        // Assert
        Assert.That(retrievedValue, Is.EqualTo(testValue));
    }

    [Test]
    public async Task GetAsync_WhenKeyDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var nonExistentKey = $"non_existent_{Guid.NewGuid()}";

        // Act
        var result = await _service.GetAsync(nonExistentKey);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task SetAsync_WhenCalledMultipleTimes_ShouldOverwriteValue()
    {
        // Arrange
        var firstValue = "first_value";
        var secondValue = "second_value";

        // Act
        await _service.SetAsync(_testKey, firstValue);
        await _service.SetAsync(_testKey, secondValue);
        var retrievedValue = await _service.GetAsync(_testKey);

        // Assert
        Assert.That(retrievedValue, Is.EqualTo(secondValue));
    }

    [Test]
    public async Task RemoveAsync_WhenKeyExists_ShouldReturnTrue()
    {
        // Arrange
        await _service.SetAsync(_testKey, "test_value");

        // Act
        var result = await _service.RemoveAsync(_testKey);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task RemoveAsync_WhenKeyDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentKey = $"non_existent_{Guid.NewGuid()}";

        // Act
        var result = await _service.RemoveAsync(nonExistentKey);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task RemoveAsync_WhenKeyRemoved_GetAsyncShouldReturnNull()
    {
        // Arrange
        await _service.SetAsync(_testKey, "test_value");

        // Act
        await _service.RemoveAsync(_testKey);
        var retrievedValue = await _service.GetAsync(_testKey);

        // Assert
        Assert.That(retrievedValue, Is.Null);
    }

    [Test]
    public void Remove_WhenKeyExists_ShouldReturnTrue()
    {
        // Arrange
        _service.SetAsync(_testKey, "test_value").Wait();

        // Act
        var result = _service.Remove(_testKey);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void Remove_WhenKeyDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentKey = $"non_existent_{Guid.NewGuid()}";

        // Act
        var result = _service.Remove(nonExistentKey);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task RemoveAllAsync_WhenCalled_ShouldRemoveAllKeys()
    {
        // Arrange
        var key1 = $"test_key_1_{Guid.NewGuid()}";
        var key2 = $"test_key_2_{Guid.NewGuid()}";
        await _service.SetAsync(key1, "value1");
        await _service.SetAsync(key2, "value2");

        // Act
        await _service.RemoveAllAsync();

        // Assert
        var value1 = await _service.GetAsync(key1);
        var value2 = await _service.GetAsync(key2);
        Assert.That(value1, Is.Null);
        Assert.That(value2, Is.Null);
    }

    [Test]
    public void RemoveAll_WhenCalled_ShouldRemoveAllKeys()
    {
        // Arrange
        var key1 = $"test_key_1_{Guid.NewGuid()}";
        var key2 = $"test_key_2_{Guid.NewGuid()}";
        _service.SetAsync(key1, "value1").Wait();
        _service.SetAsync(key2, "value2").Wait();

        // Act
        _service.RemoveAll();

        // Assert
        var value1 = _service.GetAsync(key1).Result;
        var value2 = _service.GetAsync(key2).Result;
        Assert.That(value1, Is.Null);
        Assert.That(value2, Is.Null);
    }

    [Test]
    public async Task SetAsync_WithUserSaltKey_ShouldStoreAndRetrieve()
    {
        // Arrange
        var saltKey = $"userSalt_test_user_{Guid.NewGuid()}";
        var saltValue = "test_salt_value_123456";

        // Act
        await _service.SetAsync(saltKey, saltValue);
        var retrievedValue = await _service.GetAsync(saltKey);

        // Assert
        Assert.That(retrievedValue, Is.EqualTo(saltValue));

        // Cleanup
        await _service.RemoveAsync(saltKey);
    }

    [Test]
    public async Task SetAsync_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var specialValue = "!@#$%^&*()_+-=[]{}|;:',.<>?/`~";

        // Act
        await _service.SetAsync(_testKey, specialValue);
        var retrievedValue = await _service.GetAsync(_testKey);

        // Assert
        Assert.That(retrievedValue, Is.EqualTo(specialValue));
    }

    [Test]
    public async Task SetAsync_WithEmptyString_ShouldStoreAndRetrieve()
    {
        // Arrange
        var emptyValue = string.Empty;

        // Act
        await _service.SetAsync(_testKey, emptyValue);
        var retrievedValue = await _service.GetAsync(_testKey);

        // Assert
        Assert.That(retrievedValue, Is.EqualTo(emptyValue));
    }

    [Test]
    public async Task SetAsync_WithLargeString_ShouldHandleCorrectly()
    {
        // Arrange
        var largeValue = new string('x', 10000); // 10KB string

        // Act
        await _service.SetAsync(_testKey, largeValue);
        var retrievedValue = await _service.GetAsync(_testKey);

        // Assert
        Assert.That(retrievedValue, Is.EqualTo(largeValue));
    }
}
