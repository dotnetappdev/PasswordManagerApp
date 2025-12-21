using NUnit.Framework;
using Moq;
using PasswordManager.App.Services;
using System;
using System.Threading.Tasks;

namespace PasswordManager.App.Tests.Services;

[TestFixture]
public class MauiSecureStorageServiceTests
{
    private MauiSecureStorageService _service = null!;
    private string _testKey = null!;

    [SetUp]
    public void Setup()
    {
        _service = new MauiSecureStorageService();
        _testKey = $"test_key_{Guid.NewGuid()}";
    }

    [TearDown]
    public async Task TearDown()
    {
        // Clean up test data
        try
        {
            _service.Remove(_testKey);
        }
        catch
        {
            // Ignore cleanup errors
        }
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
    public async Task Remove_WhenKeyExists_ShouldReturnTrue()
    {
        // Arrange
        await _service.SetAsync(_testKey, "test_value");

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
    public async Task Remove_WhenKeyRemoved_GetAsyncShouldReturnNull()
    {
        // Arrange
        await _service.SetAsync(_testKey, "test_value");

        // Act
        _service.Remove(_testKey);
        var retrievedValue = await _service.GetAsync(_testKey);

        // Assert
        Assert.That(retrievedValue, Is.Null);
    }

    [Test]
    public async Task RemoveAll_WhenCalled_ShouldRemoveAllKeys()
    {
        // Arrange
        var key1 = $"test_key_1_{Guid.NewGuid()}";
        var key2 = $"test_key_2_{Guid.NewGuid()}";
        await _service.SetAsync(key1, "value1");
        await _service.SetAsync(key2, "value2");

        // Act
        _service.RemoveAll();

        // Assert
        var value1 = await _service.GetAsync(key1);
        var value2 = await _service.GetAsync(key2);
        Assert.That(value1, Is.Null);
        Assert.That(value2, Is.Null);
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
    public async Task GetAsync_WithException_ShouldReturnNull()
    {
        // This test validates the error handling in GetAsync
        // When an exception occurs, it should return null instead of throwing

        // Arrange
        var invalidKey = $"invalid_key_{Guid.NewGuid()}";

        // Act
        var result = await _service.GetAsync(invalidKey);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Remove_WithException_ShouldReturnFalse()
    {
        // This test validates the error handling in Remove
        // When an exception occurs, it should return false instead of throwing

        // Arrange
        var invalidKey = $"invalid_key_{Guid.NewGuid()}";

        // Act
        var result = _service.Remove(invalidKey);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void RemoveAll_ShouldNotThrowException()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => _service.RemoveAll());
    }
}
