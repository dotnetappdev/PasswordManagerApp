using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PasswordManager.Crypto.Interfaces;
using PasswordManager.Models;
using PasswordManager.Models.DTOs.Auth;
using PasswordManager.Services.Services;

namespace PasswordManager.BackEnd.Tests.Services;

[TestFixture]
public class UserProfileServiceTests
{
    private Mock<UserManager<ApplicationUser>> _mockUserManager = null!;
    private Mock<IPasswordCryptoService> _mockPasswordCryptoService = null!;
    private Mock<ILogger<UserProfileService>> _mockLogger = null!;
    private UserProfileService _userProfileService = null!;
    private const string TestUserId = "test-user-id";
    private const string TestEmail = "test@example.com";

    [SetUp]
    public void Setup()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
        _mockPasswordCryptoService = new Mock<IPasswordCryptoService>();
        _mockLogger = new Mock<ILogger<UserProfileService>>();

        _userProfileService = new UserProfileService(
            _mockUserManager.Object,
            _mockPasswordCryptoService.Object,
            _mockLogger.Object);
    }

    [Test]
    public async Task GetAllUsersAsync_ShouldReturnAllUsers()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new ApplicationUser
            {
                Id = "user1",
                Email = "user1@example.com",
                FirstName = "John",
                LastName = "Doe",
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                LastLoginAt = DateTime.UtcNow.AddHours(-1),
                IsActive = true
            },
            new ApplicationUser
            {
                Id = "user2",
                Email = "user2@example.com",
                FirstName = "Jane",
                LastName = "Smith",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                LastLoginAt = DateTime.UtcNow.AddHours(-2),
                IsActive = false
            }
        }.AsQueryable();

        _mockUserManager.Setup(x => x.Users).Returns(users);

        // Act
        var result = await _userProfileService.GetAllUsersAsync();

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        
        var user1 = result.First(u => u.Id == "user1");
        Assert.That(user1.Email, Is.EqualTo("user1@example.com"));
        Assert.That(user1.FirstName, Is.EqualTo("John"));
        Assert.That(user1.LastName, Is.EqualTo("Doe"));
        Assert.That(user1.IsActive, Is.True);
        
        var user2 = result.First(u => u.Id == "user2");
        Assert.That(user2.Email, Is.EqualTo("user2@example.com"));
        Assert.That(user2.FirstName, Is.EqualTo("Jane"));
        Assert.That(user2.LastName, Is.EqualTo("Smith"));
        Assert.That(user2.IsActive, Is.False);
    }

    [Test]
    public async Task GetUserByIdAsync_WithValidUserId_ShouldReturnUserDetails()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = TestUserId,
            Email = TestEmail,
            FirstName = "John",
            LastName = "Doe",
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            LastLoginAt = DateTime.UtcNow.AddHours(-1),
            IsActive = true,
            PhoneNumber = "+1234567890",
            TwoFactorEnabled = true
        };

        _mockUserManager.Setup(x => x.FindByIdAsync(TestUserId))
            .ReturnsAsync(user);

        // Act
        var result = await _userProfileService.GetUserByIdAsync(TestUserId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(TestUserId));
        Assert.That(result.Email, Is.EqualTo(TestEmail));
        Assert.That(result.FirstName, Is.EqualTo("John"));
        Assert.That(result.LastName, Is.EqualTo("Doe"));
        Assert.That(result.IsActive, Is.True);
        
        // Check that it returns UserProfileDetailsDto with additional properties
        Assert.That(result, Is.InstanceOf<UserProfileDetailsDto>());
        var detailsDto = result as UserProfileDetailsDto;
        Assert.That(detailsDto?.PhoneNumber, Is.EqualTo("+1234567890"));
        Assert.That(detailsDto?.TwoFactorEnabled, Is.True);
    }

    [Test]
    public async Task GetUserByIdAsync_WithInvalidUserId_ShouldReturnNull()
    {
        // Arrange
        _mockUserManager.Setup(x => x.FindByIdAsync("invalid-id"))
            .ReturnsAsync((ApplicationUser)null);

        // Act
        var result = await _userProfileService.GetUserByIdAsync("invalid-id");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task CreateUserAsync_WithValidData_ShouldCreateUserWithApplicationUserId()
    {
        // Arrange
        var createDto = new CreateUserProfileDto
        {
            Email = TestEmail,
            FirstName = "John",
            LastName = "Doe",
            Password = "SecurePassword123!",
            ConfirmPassword = "SecurePassword123!"
        };

        var userSalt = new byte[32];
        var authHash = "auth-hash";

        _mockPasswordCryptoService.Setup(x => x.GenerateUserSalt())
            .Returns(userSalt);

        _mockPasswordCryptoService.Setup(x => x.CreateAuthHash(It.IsAny<byte[]>(), createDto.Password))
            .Returns(authHash);

        _mockPasswordCryptoService.Setup(x => x.DeriveMasterKey(createDto.Password, userSalt))
            .Returns(new byte[32]);

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), createDto.Password))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, string>((user, password) =>
            {
                user.Id = TestUserId; // Simulate ID assignment
            });

        // Act
        var result = await _userProfileService.CreateUserAsync(createDto);

        // Assert
        Assert.That(result.Item1.Succeeded, Is.True);
        Assert.That(result.Item2, Is.Not.Null);
        Assert.That(result.Item2?.Id, Is.EqualTo(TestUserId));
        Assert.That(result.Item3, Is.Null);

        _mockUserManager.Verify(x => x.CreateAsync(
            It.Is<ApplicationUser>(u => 
                u.Email == TestEmail &&
                u.FirstName == "John" &&
                u.LastName == "Doe" &&
                u.IsActive == true &&
                u.UserSalt == Convert.ToBase64String(userSalt) &&
                u.MasterPasswordHash == authHash),
            createDto.Password), Times.Once);
    }

    [Test]
    public async Task CreateUserAsync_WhenUserCreationFails_ShouldReturnFailureResult()
    {
        // Arrange
        var createDto = new CreateUserProfileDto
        {
            Email = TestEmail,
            FirstName = "John",
            LastName = "Doe",
            Password = "SecurePassword123!",
            ConfirmPassword = "SecurePassword123!"
        };

        var userSalt = new byte[32];
        _mockPasswordCryptoService.Setup(x => x.GenerateUserSalt())
            .Returns(userSalt);

        _mockPasswordCryptoService.Setup(x => x.CreateAuthHash(It.IsAny<byte[]>(), createDto.Password))
            .Returns("auth-hash");

        _mockPasswordCryptoService.Setup(x => x.DeriveMasterKey(createDto.Password, userSalt))
            .Returns(new byte[32]);

        var identityError = new IdentityError { Code = "DuplicateEmail", Description = "Email already exists" };
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), createDto.Password))
            .ReturnsAsync(IdentityResult.Failed(identityError));

        // Act
        var result = await _userProfileService.CreateUserAsync(createDto);

        // Assert
        Assert.That(result.Item1.Succeeded, Is.False);
        Assert.That(result.Item2, Is.Null);
        Assert.That(result.Item3, Does.Contain("Email already exists"));
    }

    [Test]
    public async Task UpdateUserProfileAsync_WithValidData_ShouldUpdateUser()
    {
        // Arrange
        var existingUser = new ApplicationUser
        {
            Id = TestUserId,
            Email = TestEmail,
            FirstName = "John",
            LastName = "Doe",
            IsActive = true
        };

        var updateDto = new UpdateUserProfileDto
        {
            Id = TestUserId,
            FirstName = "Johnny",
            LastName = "Smith",
            Email = "newemail@example.com"
        };

        _mockUserManager.Setup(x => x.FindByIdAsync(TestUserId))
            .ReturnsAsync(existingUser);

        _mockUserManager.Setup(x => x.FindByEmailAsync(updateDto.Email))
            .ReturnsAsync((ApplicationUser)null); // Email is available

        _mockUserManager.Setup(x => x.UpdateAsync(existingUser))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var (success, errorMessage) = await _userProfileService.UpdateUserProfileAsync(updateDto);

        // Assert
        Assert.That(success, Is.True);
        Assert.That(errorMessage, Is.Null);
        Assert.That(existingUser.FirstName, Is.EqualTo("Johnny"));
        Assert.That(existingUser.LastName, Is.EqualTo("Smith"));
        Assert.That(existingUser.Email, Is.EqualTo("newemail@example.com"));

        _mockUserManager.Verify(x => x.UpdateAsync(existingUser), Times.Once);
    }

    [Test]
    public async Task UpdateUserProfileAsync_WithNonExistentUser_ShouldReturnFailureResult()
    {
        // Arrange
        var updateDto = new UpdateUserProfileDto
        {
            Id = "non-existent-id",
            FirstName = "Johnny",
            LastName = "Smith"
        };

        _mockUserManager.Setup(x => x.FindByIdAsync("non-existent-id"))
            .ReturnsAsync((ApplicationUser)null);

        // Act
        var (success, errorMessage) = await _userProfileService.UpdateUserProfileAsync(updateDto);

        // Assert
        Assert.That(success, Is.False);
        Assert.That(errorMessage, Is.EqualTo("User not found"));
    }

    [Test]
    public void Constructor_WithNullUserManager_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => 
            new UserProfileService(null, _mockPasswordCryptoService.Object, _mockLogger.Object));
        
        Assert.That(ex.ParamName, Is.EqualTo("userManager"));
    }

    [Test]
    public void Constructor_WithNullPasswordCryptoService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => 
            new UserProfileService(_mockUserManager.Object, null, _mockLogger.Object));
        
        Assert.That(ex.ParamName, Is.EqualTo("passwordCryptoService"));
    }

    [Test]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => 
            new UserProfileService(_mockUserManager.Object, _mockPasswordCryptoService.Object, null));
        
        Assert.That(ex.ParamName, Is.EqualTo("logger"));
    }
}