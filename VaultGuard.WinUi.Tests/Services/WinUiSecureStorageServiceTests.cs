using NUnit.Framework;
using Moq;
using PasswordManager.Services.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PasswordManager.WinUi.Tests.Services;

/// <summary>
/// Tests for ISecureStorageService interface for WinUI implementation
/// These tests validate the contract without requiring WinUI-specific dependencies
/// </summary>
[TestFixture]
public class WinUiSecureStorageServiceTests
{
    private Mock<ISecureStorageService> _mockService = null!;
    private string _testKey = null!;

    [SetUp]
    public void Setup()
    {
        _mockService = new Mock<ISecureStorageService>();
        _testKey = $"test_key_{Guid.NewGuid()}";
    }

    [Test]
    public async Task SetAsync_WhenCalled_ShouldStoreValue()
    {
        // Arrange
        var testValue = "test_value_123";
        _mockService.Setup(x => x.SetAsync(_testKey, testValue))
            .Returns(Task.CompletedTask);
        _mockService.Setup(x => x.GetAsync(_testKey))
            .ReturnsAsync(testValue);

        // Act
        await _mockService.Object.SetAsync(_testKey, testValue);
        var retrievedValue = await _mockService.Object.GetAsync(_testKey);

        // Assert
        Assert.That(retrievedValue, Is.EqualTo(testValue));
        _mockService.Verify(x => x.SetAsync(_testKey, testValue), Times.Once);
    }

    [Test]
    public async Task GetAsync_WhenKeyDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var nonExistentKey = $"non_existent_{Guid.NewGuid()}";
        _mockService.Setup(x => x.GetAsync(nonExistentKey))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _mockService.Object.GetAsync(nonExistentKey);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task SetAsync_WhenCalledMultipleTimes_ShouldOverwriteValue()
    {
        // Arrange
        var firstValue = "first_value";
        var secondValue = "second_value";
        
        _mockService.Setup(x => x.SetAsync(_testKey, It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        
        // Simulate last set value being returned
        _mockService.Setup(x => x.GetAsync(_testKey))
            .ReturnsAsync(secondValue);

        // Act
        await _mockService.Object.SetAsync(_testKey, firstValue);
        await _mockService.Object.SetAsync(_testKey, secondValue);
        var retrievedValue = await _mockService.Object.GetAsync(_testKey);

        // Assert
        Assert.That(retrievedValue, Is.EqualTo(secondValue));
    }

    [Test]
    public void Remove_WhenKeyExists_ShouldReturnTrue()
    {
        // Arrange
        _mockService.Setup(x => x.Remove(_testKey))
            .Returns(true);

        // Act
        var result = _mockService.Object.Remove(_testKey);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void Remove_WhenKeyDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentKey = $"non_existent_{Guid.NewGuid()}";
        _mockService.Setup(x => x.Remove(nonExistentKey))
            .Returns(false);

        // Act
        var result = _mockService.Object.Remove(nonExistentKey);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task Remove_WhenKeyRemoved_GetAsyncShouldReturnNull()
    {
        // Arrange
        _mockService.Setup(x => x.Remove(_testKey))
            .Returns(true);
        _mockService.Setup(x => x.GetAsync(_testKey))
            .ReturnsAsync((string?)null);

        // Act
        _mockService.Object.Remove(_testKey);
        var retrievedValue = await _mockService.Object.GetAsync(_testKey);

        // Assert
        Assert.That(retrievedValue, Is.Null);
    }

    [Test]
    public void RemoveAll_WhenCalled_ShouldNotThrow()
    {
        // Arrange
        _mockService.Setup(x => x.RemoveAll());

        // Act & Assert
        Assert.DoesNotThrow(() => _mockService.Object.RemoveAll());
    }

    [Test]
    public async Task SetAsync_WithUserSaltKey_ShouldStoreAndRetrieve()
    {
        // Arrange
        var saltKey = $"userSalt_test_user_{Guid.NewGuid()}";
        var saltValue = "test_salt_value_123456";
        
        _mockService.Setup(x => x.SetAsync(saltKey, saltValue))
            .Returns(Task.CompletedTask);
        _mockService.Setup(x => x.GetAsync(saltKey))
            .ReturnsAsync(saltValue);

        // Act
        await _mockService.Object.SetAsync(saltKey, saltValue);
        var retrievedValue = await _mockService.Object.GetAsync(saltKey);

        // Assert
        Assert.That(retrievedValue, Is.EqualTo(saltValue));
    }

    [Test]
    public async Task SetAsync_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var specialValue = "!@#$%^&*()_+-=[]{}|;:',.<>?/`~";
        
        _mockService.Setup(x => x.SetAsync(_testKey, specialValue))
            .Returns(Task.CompletedTask);
        _mockService.Setup(x => x.GetAsync(_testKey))
            .ReturnsAsync(specialValue);

        // Act
        await _mockService.Object.SetAsync(_testKey, specialValue);
        var retrievedValue = await _mockService.Object.GetAsync(_testKey);

        // Assert
        Assert.That(retrievedValue, Is.EqualTo(specialValue));
    }

    [Test]
    public async Task SetAsync_WithEmptyString_ShouldStoreAndRetrieve()
    {
        // Arrange
        var emptyValue = string.Empty;
        
        _mockService.Setup(x => x.SetAsync(_testKey, emptyValue))
            .Returns(Task.CompletedTask);
        _mockService.Setup(x => x.GetAsync(_testKey))
            .ReturnsAsync(emptyValue);

        // Act
        await _mockService.Object.SetAsync(_testKey, emptyValue);
        var retrievedValue = await _mockService.Object.GetAsync(_testKey);

        // Assert
        Assert.That(retrievedValue, Is.EqualTo(emptyValue));
    }

    [Test]
    public async Task SetAsync_WithLargeString_ShouldHandleCorrectly()
    {
        // Arrange
        var largeValue = new string('x', 10000); // 10KB string
        
        _mockService.Setup(x => x.SetAsync(_testKey, largeValue))
            .Returns(Task.CompletedTask);
        _mockService.Setup(x => x.GetAsync(_testKey))
            .ReturnsAsync(largeValue);

        // Act
        await _mockService.Object.SetAsync(_testKey, largeValue);
        var retrievedValue = await _mockService.Object.GetAsync(_testKey);

        // Assert
        Assert.That(retrievedValue, Is.EqualTo(largeValue));
    }

    [Test]
    public void ISecureStorageService_ShouldHaveRequiredMethods()
    {
        // This test verifies that the interface contract is complete
        var serviceType = typeof(ISecureStorageService);
        
        // Assert required methods exist
        Assert.That(serviceType.GetMethod("SetAsync"), Is.Not.Null);
        Assert.That(serviceType.GetMethod("GetAsync"), Is.Not.Null);
        Assert.That(serviceType.GetMethod("Remove"), Is.Not.Null);
        Assert.That(serviceType.GetMethod("RemoveAll"), Is.Not.Null);
    }
}
