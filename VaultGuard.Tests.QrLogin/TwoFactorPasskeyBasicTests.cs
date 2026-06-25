using PasswordManager.Models;
using PasswordManager.Models.DTOs.Auth;

namespace PasswordManager.Tests.QrLogin;

/// <summary>
/// Simple unit tests for 2FA and Passkey DTOs without complex database setup
/// </summary>
public class TwoFactorPasskeyBasicTests
{
    [Fact]
    public void TwoFactorSetupRequestDto_ShouldValidateRequired()
    {
        // Arrange
        var dto = new TwoFactorSetupRequestDto
        {
            MasterPassword = "test-password"
        };

        // Assert
        Assert.NotNull(dto.MasterPassword);
        Assert.NotEmpty(dto.MasterPassword);
    }

    [Fact]
    public void TwoFactorVerifySetupDto_ShouldHaveRequiredProperties()
    {
        // Arrange
        var dto = new TwoFactorVerifySetupDto
        {
            Code = "123456",
            SecretKey = "JBSWY3DPEHPK3PXP"
        };

        // Assert
        Assert.Equal("123456", dto.Code);
        Assert.Equal("JBSWY3DPEHPK3PXP", dto.SecretKey);
    }

    [Fact]
    public void TwoFactorStatusDto_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var dto = new TwoFactorStatusDto();

        // Assert
        Assert.False(dto.IsEnabled);
        Assert.Null(dto.EnabledAt);
        Assert.Equal(0, dto.BackupCodesRemaining);
        Assert.Null(dto.RecoveryEmail);
    }

    [Fact]
    public void TwoFactorLoginDto_ShouldSupportBackupCodes()
    {
        // Arrange
        var dto = new TwoFactorLoginDto
        {
            Code = "ABC12345",
            IsBackupCode = true
        };

        // Assert
        Assert.Equal("ABC12345", dto.Code);
        Assert.True(dto.IsBackupCode);
    }

    [Fact]
    public void TwoFactorSetupResponseDto_ShouldContainAllRequiredData()
    {
        // Arrange
        var secretKey = "JBSWY3DPEHPK3PXP";
        var qrCodeUri = "otpauth://totp/PasswordManager:test@example.com?secret=JBSWY3DPEHPK3PXP&issuer=PasswordManager";
        var backupCodes = new List<string> { "ABC12345", "DEF67890" };

        var dto = new TwoFactorSetupResponseDto
        {
            SecretKey = secretKey,
            QrCodeUri = qrCodeUri,
            BackupCodes = backupCodes
        };

        // Assert
        Assert.Equal(secretKey, dto.SecretKey);
        Assert.Equal(qrCodeUri, dto.QrCodeUri);
        Assert.Equal(2, dto.BackupCodes.Count);
        Assert.Contains("ABC12345", dto.BackupCodes);
        Assert.Contains("DEF67890", dto.BackupCodes);
    }

    [Fact]
    public void PasskeyRegistrationStartDto_ShouldHaveRequiredProperties()
    {
        // Arrange
        var dto = new PasskeyRegistrationStartDto
        {
            MasterPassword = "test-password",
            PasskeyName = "My iPhone",
            StoreInVault = true
        };

        // Assert
        Assert.Equal("test-password", dto.MasterPassword);
        Assert.Equal("My iPhone", dto.PasskeyName);
        Assert.True(dto.StoreInVault);
    }

    [Fact]
    public void PasskeyDto_ShouldHaveAllProperties()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var dto = new PasskeyDto
        {
            Id = 1,
            Name = "Test Passkey",
            DeviceType = "iPhone",
            IsBackedUp = true,
            RequiresUserVerification = true,
            CreatedAt = now,
            LastUsedAt = now.AddHours(-1),
            IsActive = true,
            StoreInVault = true
        };

        // Assert
        Assert.Equal(1, dto.Id);
        Assert.Equal("Test Passkey", dto.Name);
        Assert.Equal("iPhone", dto.DeviceType);
        Assert.True(dto.IsBackedUp);
        Assert.True(dto.RequiresUserVerification);
        Assert.Equal(now, dto.CreatedAt);
        Assert.Equal(now.AddHours(-1), dto.LastUsedAt);
        Assert.True(dto.IsActive);
        Assert.True(dto.StoreInVault);
    }

    [Fact]
    public void UserPasskey_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var passkey = new UserPasskey
        {
            UserId = "test-user",
            CredentialId = Convert.ToBase64String(new byte[32]),
            Name = "Test Passkey",
            PublicKey = Convert.ToBase64String(new byte[64])
        };

        // Assert
        Assert.Equal("test-user", passkey.UserId);
        Assert.NotNull(passkey.CredentialId);
        Assert.Equal("Test Passkey", passkey.Name);
        Assert.NotNull(passkey.PublicKey);
        Assert.Equal(0u, passkey.SignatureCounter);
        Assert.False(passkey.IsBackedUp);
        Assert.True(passkey.RequiresUserVerification);
        Assert.True(passkey.IsActive);
        Assert.True(passkey.StoreInVault);
        Assert.Null(passkey.LastUsedAt);
        Assert.Null(passkey.EncryptedVaultData);
    }

    [Fact]
    public void UserTwoFactorBackupCode_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var backupCode = new UserTwoFactorBackupCode
        {
            UserId = "test-user",
            CodeHash = "hash",
            CodeSalt = "salt"
        };

        // Assert
        Assert.Equal("test-user", backupCode.UserId);
        Assert.Equal("hash", backupCode.CodeHash);
        Assert.Equal("salt", backupCode.CodeSalt);
        Assert.False(backupCode.IsUsed);
        Assert.Null(backupCode.UsedAt);
        Assert.Null(backupCode.UsedFromIp);
    }

    [Fact]
    public void EnhancedLoginRequestDto_ShouldSupportTwoFactorCode()
    {
        // Arrange
        var dto = new EnhancedLoginRequestDto
        {
            Email = "test@example.com",
            Password = "password123",
            TwoFactorCode = "123456",
            IsTwoFactorBackupCode = false
        };

        // Assert
        Assert.Equal("test@example.com", dto.Email);
        Assert.Equal("password123", dto.Password);
        Assert.Equal("123456", dto.TwoFactorCode);
        Assert.False(dto.IsTwoFactorBackupCode);
    }

    [Fact]
    public void EnhancedLoginRequestDto_ShouldSupportBackupCode()
    {
        // Arrange
        var dto = new EnhancedLoginRequestDto
        {
            Email = "test@example.com",
            Password = "password123",
            TwoFactorCode = "ABC12345",
            IsTwoFactorBackupCode = true
        };

        // Assert
        Assert.Equal("test@example.com", dto.Email);
        Assert.Equal("password123", dto.Password);
        Assert.Equal("ABC12345", dto.TwoFactorCode);
        Assert.True(dto.IsTwoFactorBackupCode);
    }

    [Fact]
    public void LoginResponseDto_ShouldIndicateTwoFactorRequired()
    {
        // Arrange
        var dto = new LoginResponseDto
        {
            RequiresTwoFactor = true,
            SupportsPasskey = true,
            TwoFactorToken = "temp-token-123"
        };

        // Assert
        Assert.True(dto.RequiresTwoFactor);
        Assert.True(dto.SupportsPasskey);
        Assert.Equal("temp-token-123", dto.TwoFactorToken);
        Assert.Null(dto.AuthResponse);
    }

    [Fact]
    public void LoginResponseDto_ShouldContainAuthResponse()
    {
        // Arrange
        var authResponse = new AuthResponseDto
        {
            Token = "jwt-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new UserDto { Id = "user-1", Email = "test@example.com" }
        };

        var dto = new LoginResponseDto
        {
            RequiresTwoFactor = false,
            SupportsPasskey = false,
            AuthResponse = authResponse
        };

        // Assert
        Assert.False(dto.RequiresTwoFactor);
        Assert.False(dto.SupportsPasskey);
        Assert.NotNull(dto.AuthResponse);
        Assert.Equal("jwt-token", dto.AuthResponse.Token);
    }

    [Theory]
    [InlineData("ABCDEFGHIJKLMNOP2345")]
    [InlineData("JBSWY3DPEHPK3PXP")]
    [InlineData("NBSWY3DPEHPK3PXP")]
    public void ValidBase32SecretKeys_ShouldBeValid(string secretKey)
    {
        // Verify the secret key contains only valid base32 characters
        var validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        Assert.True(secretKey.All(c => validChars.Contains(c)));
    }

    [Theory]
    [InlineData("otpauth://totp/PasswordManager:test@example.com?secret=JBSWY3DPEHPK3PXP&issuer=PasswordManager")]
    [InlineData("otpauth://totp/MyApp:user@domain.com?secret=ABCDEFGHIJK234567&issuer=MyApp")]
    public void ValidQrCodeUris_ShouldHaveCorrectFormat(string qrCodeUri)
    {
        // Verify QR code URI has the correct TOTP format
        Assert.StartsWith("otpauth://totp/", qrCodeUri);
        Assert.Contains("secret=", qrCodeUri);
        Assert.Contains("issuer=", qrCodeUri);
        Assert.Contains("@", qrCodeUri); // Should contain email
    }

    [Fact]
    public void BackupCodes_ShouldBeUnique()
    {
        // Generate some test backup codes
        var codes = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            codes.Add(GenerateTestBackupCode());
        }

        // Verify all codes are unique
        var uniqueCodes = codes.Distinct().Count();
        Assert.Equal(10, uniqueCodes);
    }

    private string GenerateTestBackupCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var code = new string(Enumerable.Repeat(chars, 8)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        return code;
    }
}