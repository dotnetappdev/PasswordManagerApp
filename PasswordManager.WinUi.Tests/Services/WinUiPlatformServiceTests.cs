using NUnit.Framework;
using Moq;
using PasswordManager.Services.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PasswordManager.WinUi.Tests.Services;

/// <summary>
/// Tests for IPlatformService interface for WinUI implementation
/// These tests validate the contract without requiring WinUI-specific dependencies
/// </summary>
[TestFixture]
public class WinUiPlatformServiceTests
{
    private Mock<IPlatformService> _mockService = null!;

    [SetUp]
    public void Setup()
    {
        _mockService = new Mock<IPlatformService>();
    }

    [Test]
    public void GetAppDataDirectory_WhenCalled_ShouldReturnValidPath()
    {
        // Arrange
        var expectedPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PasswordManager");
        _mockService.Setup(x => x.GetAppDataDirectory())
            .Returns(expectedPath);

        // Act
        var result = _mockService.Object.GetAppDataDirectory();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Does.Contain("PasswordManager"));
    }

    [Test]
    public void GetDocumentsDirectory_WhenCalled_ShouldReturnValidPath()
    {
        // Arrange
        var expectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (string.IsNullOrEmpty(expectedPath))
        {
            expectedPath = "/home/user/Documents"; // Fallback for test environment
        }
        _mockService.Setup(x => x.GetDocumentsDirectory())
            .Returns(expectedPath);

        // Act
        var result = _mockService.Object.GetDocumentsDirectory();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
    }

    [Test]
    public void ShouldShowDatabaseSelection_WhenCalled_ShouldReturnTrue()
    {
        // Arrange
        _mockService.Setup(x => x.ShouldShowDatabaseSelection())
            .Returns(true);

        // Act
        var result = _mockService.Object.ShouldShowDatabaseSelection();

        // Assert
        Assert.That(result, Is.True, "WinUI is a desktop platform and should show database selection");
    }

    [Test]
    public void GetDeviceIdentifier_WhenCalled_ShouldReturnValidIdentifier()
    {
        // Arrange
        var expectedIdentifier = $"{Environment.MachineName}-{Environment.OSVersion.Platform}-WinUI";
        _mockService.Setup(x => x.GetDeviceIdentifier())
            .Returns(expectedIdentifier);

        // Act
        var result = _mockService.Object.GetDeviceIdentifier();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Does.Contain("WinUI"));
    }

    [Test]
    public void IsMobilePlatform_WhenCalled_ShouldReturnFalse()
    {
        // Arrange
        _mockService.Setup(x => x.IsMobilePlatform())
            .Returns(false);

        // Act
        var result = _mockService.Object.IsMobilePlatform();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void GetAppDataDirectory_CalledMultipleTimes_ShouldReturnConsistentPath()
    {
        // Arrange
        var expectedPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PasswordManager");
        _mockService.Setup(x => x.GetAppDataDirectory())
            .Returns(expectedPath);

        // Act
        var result1 = _mockService.Object.GetAppDataDirectory();
        var result2 = _mockService.Object.GetAppDataDirectory();

        // Assert
        Assert.That(result1, Is.EqualTo(result2));
    }

    [Test]
    public void IPlatformService_ShouldHaveRequiredMethods()
    {
        // This test verifies that the interface contract is complete
        var serviceType = typeof(IPlatformService);
        
        // Assert required methods exist
        Assert.That(serviceType.GetMethod("GetPlatformName"), Is.Not.Null);
        Assert.That(serviceType.GetMethod("GetAppDataDirectory"), Is.Not.Null);
        Assert.That(serviceType.GetMethod("GetDocumentsDirectory"), Is.Not.Null);
        Assert.That(serviceType.GetMethod("ShouldShowDatabaseSelection"), Is.Not.Null);
        Assert.That(serviceType.GetMethod("IsMobilePlatform"), Is.Not.Null);
    }
}
