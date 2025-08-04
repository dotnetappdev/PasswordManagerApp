using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PasswordManager.Crypto.Interfaces;
using PasswordManager.DAL.Interfaces;
using PasswordManager.Models.Configuration;
using PasswordManager.Services.Interfaces;
using PasswordManager.Services.Services;
using Xunit;

namespace PasswordManager.Tests.OTP;

public class OtpServiceTests
{
    private readonly Mock<IPasswordManagerDbContext> _mockContext;
    private readonly Mock<ISmsService> _mockSmsService;
    private readonly Mock<ICryptographyService> _mockCryptoService;
    private readonly Mock<ILogger<OtpService>> _mockLogger;
    private readonly SmsConfiguration _config;
    private readonly OtpService _otpService;

    public OtpServiceTests()
    {
        _mockContext = new Mock<IPasswordManagerDbContext>();
        _mockSmsService = new Mock<ISmsService>();
        _mockCryptoService = new Mock<ICryptographyService>();
        _mockLogger = new Mock<ILogger<OtpService>>();
        
        _config = new SmsConfiguration
        {
            Enabled = true,
            CodeLength = 6,
            ExpirationMinutes = 5,
            MaxAttempts = 3,
            MaxSmsPerHour = 10
        };

        var configOptions = Options.Create(_config);
        _otpService = new OtpService(_mockContext.Object, _mockSmsService.Object, configOptions, _mockCryptoService.Object, _mockLogger.Object);
    }

    [Fact]
    public void GenerateBackupCodes_ShouldGenerate10Codes()
    {
        // Act
        var codes = _otpService.GenerateBackupCodes();

        // Assert
        Assert.Equal(10, codes.Count);
        Assert.All(codes, code => 
        {
            Assert.Equal(8, code.Length);
            Assert.All(code, c => Assert.True(char.IsDigit(c)));
        });
    }

    [Fact]
    public void GenerateBackupCodes_WithCustomCount_ShouldGenerateCorrectNumber()
    {
        // Act
        var codes = _otpService.GenerateBackupCodes(5);

        // Assert
        Assert.Equal(5, codes.Count);
    }
}

public class PlatformDetectionServiceTests
{
    private readonly PlatformDetectionService _platformService;
    private readonly Mock<ILogger<PlatformDetectionService>> _mockLogger;

    public PlatformDetectionServiceTests()
    {
        _mockLogger = new Mock<ILogger<PlatformDetectionService>>();
        _platformService = new PlatformDetectionService(_mockLogger.Object);
    }

    [Theory]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36", PlatformType.Web)]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36", PlatformType.Web)]
    [InlineData("Mozilla/5.0 (Linux; Android 11; SM-G991B) AppleWebKit/537.36", PlatformType.MobileAndroid)]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 15_0 like Mac OS X) AppleWebKit/605.1.15", PlatformType.MobileIOS)]
    [InlineData("PasswordManager/1.0 (Windows NT 10.0; MAUI)", PlatformType.DesktopWindows)]
    [InlineData("", PlatformType.Unknown)]
    [InlineData(null, PlatformType.Unknown)]
    public void DetectPlatform_ShouldReturnCorrectPlatformType(string? userAgent, PlatformType expectedPlatform)
    {
        // Act
        var result = _platformService.DetectPlatform(userAgent);

        // Assert
        Assert.Equal(expectedPlatform, result);
    }

    [Theory]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36", true)]  // Web
    [InlineData("Mozilla/5.0 (Linux; Android 11; SM-G991B) AppleWebKit/537.36", true)]  // Android
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 15_0 like Mac OS X) AppleWebKit/605.1.15", true)]  // iOS
    [InlineData("PasswordManager/1.0 (Windows NT 10.0; MAUI)", false)]  // Desktop Windows
    [InlineData("PasswordManager/1.0 (Macintosh; MAUI)", false)]  // Desktop macOS
    [InlineData("", false)]  // Unknown
    [InlineData(null, false)]  // Unknown
    public void IsOtpSupported_ShouldReturnCorrectSupport(string? userAgent, bool expectedSupport)
    {
        // Act
        var result = _platformService.IsOtpSupported(userAgent);

        // Assert
        Assert.Equal(expectedSupport, result);
    }
}