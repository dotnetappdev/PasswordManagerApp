using NUnit.Framework;
using PasswordManager.WinUi.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PasswordManager.WinUi.Tests.Services;

[TestFixture]
public class WinUiPlatformServiceTests
{
    private WinUiPlatformService _service = null!;

    [SetUp]
    public void Setup()
    {
        _service = new WinUiPlatformService();
    }

    [Test]
    public void GetAppDataDirectory_WhenCalled_ShouldReturnValidPath()
    {
        // Act
        var result = _service.GetAppDataDirectory();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Does.Contain("PasswordManager"));
        Assert.That(Directory.Exists(result), Is.True, "Directory should be created if it doesn't exist");
    }

    [Test]
    public void GetDocumentsDirectory_WhenCalled_ShouldReturnValidPath()
    {
        // Act
        var result = _service.GetDocumentsDirectory();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
        Assert.That(Directory.Exists(result), Is.True);
    }

    [Test]
    public void GetDownloadsDirectory_WhenCalled_ShouldReturnValidPath()
    {
        // Act
        var result = _service.GetDownloadsDirectory();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Does.Contain("Downloads"));
    }

    [Test]
    public void GetTempDirectory_WhenCalled_ShouldReturnValidPath()
    {
        // Act
        var result = _service.GetTempDirectory();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
        Assert.That(Directory.Exists(result), Is.True);
    }

    [Test]
    public void GetPlatformName_WhenCalled_ShouldReturnWinUI()
    {
        // Act
        var result = _service.GetPlatformName();

        // Assert
        Assert.That(result, Is.EqualTo("WinUI"));
    }

    [Test]
    public void IsDesktop_WhenCalled_ShouldReturnTrue()
    {
        // Act
        var result = _service.IsDesktop();

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsMobile_WhenCalled_ShouldReturnFalse()
    {
        // Act
        var result = _service.IsMobile();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsMobilePlatform_WhenCalled_ShouldReturnFalse()
    {
        // Act
        var result = _service.IsMobilePlatform();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsWeb_WhenCalled_ShouldReturnFalse()
    {
        // Act
        var result = _service.IsWeb();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void ShouldShowDatabaseSelection_WhenCalled_ShouldReturnTrue()
    {
        // Act
        var result = _service.ShouldShowDatabaseSelection();

        // Assert
        Assert.That(result, Is.True, "WinUI is a desktop platform and should show database selection");
    }

    [Test]
    public void GetDeviceIdentifier_WhenCalled_ShouldReturnValidIdentifier()
    {
        // Act
        var result = _service.GetDeviceIdentifier();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Does.Contain("WinUI"));
        Assert.That(result, Does.Contain(Environment.MachineName));
    }

    [Test]
    public async Task SaveFileAsync_WithValidData_ShouldReturnTrue()
    {
        // Arrange
        var filename = $"test_file_{Guid.NewGuid()}.txt";
        var data = System.Text.Encoding.UTF8.GetBytes("Test content");

        // Act
        var result = await _service.SaveFileAsync(filename, data);

        // Assert
        Assert.That(result, Is.True);

        // Cleanup
        var downloadsPath = Path.Combine(_service.GetDownloadsDirectory(), filename);
        if (File.Exists(downloadsPath))
        {
            File.Delete(downloadsPath);
        }
    }

    [Test]
    public async Task SaveFileAsync_WithEmptyData_ShouldReturnTrue()
    {
        // Arrange
        var filename = $"test_empty_{Guid.NewGuid()}.txt";
        var data = Array.Empty<byte>();

        // Act
        var result = await _service.SaveFileAsync(filename, data);

        // Assert
        Assert.That(result, Is.True);

        // Cleanup
        var downloadsPath = Path.Combine(_service.GetDownloadsDirectory(), filename);
        if (File.Exists(downloadsPath))
        {
            File.Delete(downloadsPath);
        }
    }

    [Test]
    public void GetAppDataDirectory_CalledMultipleTimes_ShouldReturnSamePath()
    {
        // Act
        var result1 = _service.GetAppDataDirectory();
        var result2 = _service.GetAppDataDirectory();

        // Assert
        Assert.That(result1, Is.EqualTo(result2));
    }

    [Test]
    public void AllDirectories_ShouldReturnAbsolutePaths()
    {
        // Act
        var appDataDir = _service.GetAppDataDirectory();
        var documentsDir = _service.GetDocumentsDirectory();
        var downloadsDir = _service.GetDownloadsDirectory();
        var tempDir = _service.GetTempDirectory();

        // Assert
        Assert.That(Path.IsPathRooted(appDataDir), Is.True, "AppData path should be absolute");
        Assert.That(Path.IsPathRooted(documentsDir), Is.True, "Documents path should be absolute");
        Assert.That(Path.IsPathRooted(downloadsDir), Is.True, "Downloads path should be absolute");
        Assert.That(Path.IsPathRooted(tempDir), Is.True, "Temp path should be absolute");
    }
}
