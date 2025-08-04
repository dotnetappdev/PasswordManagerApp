using Moq;
using NUnit.Framework;
using PasswordManager.Crypto.Interfaces;
using PasswordManager.Crypto.Services;
using PasswordManager.Models;
using System.Text;

namespace PasswordManager.BackEnd.Tests.Services;

[TestFixture]
public class PasswordCryptoServiceTests
{
    private Mock<ICryptographyService> _mockCryptographyService = null!;
    private PasswordCryptoService _passwordCryptoService = null!;
    private byte[] _testUserSalt = null!;
    private const string TestMasterPassword = "TestMasterPassword123!";
    private const string TestPassword = "MySecretPassword";

    [SetUp]
    public void Setup()
    {
        _mockCryptographyService = new Mock<ICryptographyService>();
        _passwordCryptoService = new PasswordCryptoService(_mockCryptographyService.Object);
        _testUserSalt = Encoding.UTF8.GetBytes("test-salt-32-bytes-long-for-testing");
        
        // Ensure the salt is exactly 32 bytes
        Array.Resize(ref _testUserSalt, 32);
    }

    [Test]
    public void EncryptPassword_WithValidInputs_ShouldReturnEncryptedData()
    {
        // Arrange
        var masterKey = new byte[32];
        var encryptedData = new EncryptedData
        {
            Ciphertext = Encoding.UTF8.GetBytes("encrypted-password"),
            Nonce = new byte[12],
            AuthenticationTag = new byte[16]
        };

        _mockCryptographyService
            .Setup(x => x.DeriveKey(TestMasterPassword, _testUserSalt, 600000, 32))
            .Returns(masterKey);

        _mockCryptographyService
            .Setup(x => x.EncryptAes256Gcm(TestPassword, masterKey))
            .Returns(encryptedData);

        // Act
        var result = _passwordCryptoService.EncryptPassword(TestPassword, TestMasterPassword, _testUserSalt);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.EncryptedPassword, Is.Not.Null.And.Not.Empty);
        Assert.That(result.Nonce, Is.Not.Null.And.Not.Empty);
        Assert.That(result.AuthenticationTag, Is.Not.Null.And.Not.Empty);

        _mockCryptographyService.Verify(x => x.DeriveKey(TestMasterPassword, _testUserSalt, 600000, 32), Times.Once);
        _mockCryptographyService.Verify(x => x.EncryptAes256Gcm(TestPassword, masterKey), Times.Once);
    }

    [Test]
    public void EncryptPassword_WithNullPassword_ShouldThrowArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => 
            _passwordCryptoService.EncryptPassword(null, TestMasterPassword, _testUserSalt));
        
        Assert.That(ex.ParamName, Is.EqualTo("password"));
        Assert.That(ex.Message, Does.Contain("Password cannot be null or empty"));
    }

    [Test]
    public void EncryptPassword_WithEmptyPassword_ShouldThrowArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => 
            _passwordCryptoService.EncryptPassword("", TestMasterPassword, _testUserSalt));
        
        Assert.That(ex.ParamName, Is.EqualTo("password"));
        Assert.That(ex.Message, Does.Contain("Password cannot be null or empty"));
    }

    [Test]
    public void EncryptPassword_WithNullMasterPassword_ShouldThrowArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => 
            _passwordCryptoService.EncryptPassword(TestPassword, null, _testUserSalt));
        
        Assert.That(ex.ParamName, Is.EqualTo("masterPassword"));
        Assert.That(ex.Message, Does.Contain("Master password cannot be null or empty"));
    }

    [Test]
    public void EncryptPassword_WithNullUserSalt_ShouldThrowArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => 
            _passwordCryptoService.EncryptPassword(TestPassword, TestMasterPassword, null));
        
        Assert.That(ex.ParamName, Is.EqualTo("userSalt"));
        Assert.That(ex.Message, Does.Contain("User salt cannot be null or empty"));
    }

    [Test]
    public void EncryptPassword_WithEmptyUserSalt_ShouldThrowArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => 
            _passwordCryptoService.EncryptPassword(TestPassword, TestMasterPassword, Array.Empty<byte>()));
        
        Assert.That(ex.ParamName, Is.EqualTo("userSalt"));
        Assert.That(ex.Message, Does.Contain("User salt cannot be null or empty"));
    }

    [Test]
    public void DecryptPassword_WithValidInputs_ShouldReturnOriginalPassword()
    {
        // Arrange
        var masterKey = new byte[32];
        var encryptedData = new EncryptedPasswordData
        {
            EncryptedPassword = Convert.ToBase64String(Encoding.UTF8.GetBytes("encrypted-password")),
            Nonce = Convert.ToBase64String(new byte[12]),
            AuthenticationTag = Convert.ToBase64String(new byte[16])
        };

        _mockCryptographyService
            .Setup(x => x.DeriveKey(TestMasterPassword, _testUserSalt, 600000, 32))
            .Returns(masterKey);

        _mockCryptographyService
            .Setup(x => x.DecryptAes256Gcm(
                It.IsAny<EncryptedData>(), 
                masterKey))
            .Returns(TestPassword);

        // Act
        var result = _passwordCryptoService.DecryptPassword(encryptedData, TestMasterPassword, _testUserSalt);

        // Assert
        Assert.That(result, Is.EqualTo(TestPassword));

        _mockCryptographyService.Verify(x => x.DeriveKey(TestMasterPassword, _testUserSalt, 600000, 32), Times.Once);
        _mockCryptographyService.Verify(x => x.DecryptAes256Gcm(
            It.IsAny<EncryptedData>(), 
            masterKey), Times.Once);
    }

    [Test]
    public void DecryptPassword_WithNullEncryptedData_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => 
            _passwordCryptoService.DecryptPassword(null, TestMasterPassword, _testUserSalt));
        
        Assert.That(ex.ParamName, Is.EqualTo("encryptedData"));
    }

    [Test]
    public void DecryptPassword_WithInvalidBase64_ShouldThrowFormatException()
    {
        // Arrange
        var invalidEncryptedData = new EncryptedPasswordData
        {
            EncryptedPassword = "invalid-base64!",
            Nonce = Convert.ToBase64String(new byte[12]),
            AuthenticationTag = Convert.ToBase64String(new byte[16])
        };

        // Act & Assert
        Assert.Throws<FormatException>(() => 
            _passwordCryptoService.DecryptPassword(invalidEncryptedData, TestMasterPassword, _testUserSalt));
    }

    [Test]
    public void GenerateUserSalt_ShouldReturnCorrectLength()
    {
        // Arrange
        var expectedSalt = new byte[32];
        new Random().NextBytes(expectedSalt);

        _mockCryptographyService
            .Setup(x => x.GenerateSalt(32))
            .Returns(expectedSalt);

        // Act
        var result = _passwordCryptoService.GenerateUserSalt();

        // Assert
        Assert.That(result, Has.Length.EqualTo(32));
        Assert.That(result, Is.EqualTo(expectedSalt));

        _mockCryptographyService.Verify(x => x.GenerateSalt(32), Times.Once);
    }

    [Test]
    public void CreateMasterPasswordHash_WithValidInputs_ShouldReturnHash()
    {
        // Arrange
        var userSalt = new byte[32];
        var expectedHash = "hashed-master-password";

        _mockCryptographyService
            .Setup(x => x.DeriveKey(TestMasterPassword, userSalt, 600000, 32))
            .Returns(new byte[32]);

        _mockCryptographyService
            .Setup(x => x.HashPassword(It.IsAny<string>(), userSalt, 600000))
            .Returns(expectedHash);

        // Act
        var result = _passwordCryptoService.CreateMasterPasswordHash(TestMasterPassword, userSalt);

        // Assert
        Assert.That(result, Is.EqualTo(expectedHash));

        _mockCryptographyService.Verify(x => x.HashPassword(It.IsAny<string>(), userSalt, 600000), Times.Once);
    }

    [Test]
    public void CreateMasterPasswordHash_WithNullMasterPassword_ShouldThrowArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => 
            _passwordCryptoService.CreateMasterPasswordHash(null, _testUserSalt));
        
        Assert.That(ex.ParamName, Is.EqualTo("masterPassword"));
    }

    [Test]
    public void VerifyMasterPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var userSalt = new byte[32];
        var storedHash = "stored-hash";

        _mockCryptographyService
            .Setup(x => x.VerifyPassword(TestMasterPassword, storedHash, userSalt, 600000))
            .Returns(true);

        // Act
        var result = _passwordCryptoService.VerifyMasterPassword(TestMasterPassword, storedHash, userSalt);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void VerifyMasterPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var userSalt = new byte[32];
        var storedHash = "stored-hash";

        _mockCryptographyService
            .Setup(x => x.VerifyPassword("wrong-password", storedHash, userSalt, 600000))
            .Returns(false);

        // Act
        var result = _passwordCryptoService.VerifyMasterPassword("wrong-password", storedHash, userSalt);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void EncryptPassword_CryptographyServiceThrows_ShouldPropagateException()
    {
        // Arrange
        _mockCryptographyService
            .Setup(x => x.DeriveKey(TestMasterPassword, _testUserSalt, 600000, 32))
            .Throws(new InvalidOperationException("Cryptography error"));

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            _passwordCryptoService.EncryptPassword(TestPassword, TestMasterPassword, _testUserSalt));
        
        Assert.That(ex.Message, Is.EqualTo("Cryptography error"));
    }

    [Test]
    public void DecryptPassword_CryptographyServiceThrows_ShouldPropagateException()
    {
        // Arrange
        var encryptedData = new EncryptedPasswordData
        {
            EncryptedPassword = Convert.ToBase64String(Encoding.UTF8.GetBytes("encrypted-password")),
            Nonce = Convert.ToBase64String(new byte[12]),
            AuthenticationTag = Convert.ToBase64String(new byte[16])
        };

        _mockCryptographyService
            .Setup(x => x.DeriveKey(TestMasterPassword, _testUserSalt, 600000, 32))
            .Returns(new byte[32]);

        _mockCryptographyService
            .Setup(x => x.DecryptAes256Gcm(
                It.IsAny<EncryptedData>(), 
                It.IsAny<byte[]>()))
            .Throws(new UnauthorizedAccessException("Decryption failed"));

        // Act & Assert
        var ex = Assert.Throws<UnauthorizedAccessException>(() => 
            _passwordCryptoService.DecryptPassword(encryptedData, TestMasterPassword, _testUserSalt));
        
        Assert.That(ex.Message, Is.EqualTo("Decryption failed"));
    }
}