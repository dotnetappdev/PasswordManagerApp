using NUnit.Framework;
using Moq;
using PasswordManager.WinUi.Services;
using PasswordManager.Services.Interfaces;
using PasswordManager.Models.DTOs.Auth;
using System;
using System.Threading.Tasks;

namespace PasswordManager.WinUi.Tests.Services;

[TestFixture]
public class UserContextServiceTests
{
    private Mock<IAuthService> _mockAuthService = null!;
    private Mock<IUserProfileService> _mockUserProfileService = null!;
    private UserContextService _service = null!;

    [SetUp]
    public void Setup()
    {
        _mockAuthService = new Mock<IAuthService>();
        _mockUserProfileService = new Mock<IUserProfileService>();
        _service = new UserContextService(_mockAuthService.Object, _mockUserProfileService.Object);
    }

    [Test]
    public void CurrentUser_InitialState_ShouldBeNull()
    {
        // Assert
        Assert.That(_service.CurrentUser, Is.Null);
    }

    [Test]
    public void CurrentUserId_WhenNoUser_ShouldBeNull()
    {
        // Assert
        Assert.That(_service.CurrentUserId, Is.Null);
    }

    [Test]
    public async Task SetCurrentUserAsync_WithValidUserId_ShouldSetCurrentUser()
    {
        // Arrange
        var userId = "test-user-123";
        var user = new UserDto
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "testuser"
        };
        _mockUserProfileService.Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        await _service.SetCurrentUserAsync(userId);

        // Assert
        Assert.That(_service.CurrentUser, Is.Not.Null);
        Assert.That(_service.CurrentUser!.Id, Is.EqualTo(userId));
        Assert.That(_service.CurrentUser.Email, Is.EqualTo("test@example.com"));
        Assert.That(_service.CurrentUserId, Is.EqualTo(userId));
    }

    [Test]
    public async Task SetCurrentUserAsync_CallsUserProfileService_Once()
    {
        // Arrange
        var userId = "test-user-123";
        var user = new UserDto { Id = userId };
        _mockUserProfileService.Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        await _service.SetCurrentUserAsync(userId);

        // Assert
        _mockUserProfileService.Verify(x => x.GetUserByIdAsync(userId), Times.Once);
    }

    [Test]
    public void SetCurrentUser_WithUserDto_ShouldSetCurrentUser()
    {
        // Arrange
        var user = new UserDto
        {
            Id = "test-user-456",
            Email = "another@example.com",
            UserName = "anotheruser"
        };

        // Act
        _service.SetCurrentUser(user);

        // Assert
        Assert.That(_service.CurrentUser, Is.Not.Null);
        Assert.That(_service.CurrentUser!.Id, Is.EqualTo("test-user-456"));
        Assert.That(_service.CurrentUser.Email, Is.EqualTo("another@example.com"));
        Assert.That(_service.CurrentUserId, Is.EqualTo("test-user-456"));
    }

    [Test]
    public void ClearCurrentUser_WhenUserSet_ShouldClearUser()
    {
        // Arrange
        var user = new UserDto { Id = "test-user-789" };
        _service.SetCurrentUser(user);

        // Act
        _service.ClearCurrentUser();

        // Assert
        Assert.That(_service.CurrentUser, Is.Null);
        Assert.That(_service.CurrentUserId, Is.Null);
    }

    [Test]
    public async Task SwitchUserAsync_WithValidUser_ShouldReturnTrue()
    {
        // Arrange
        var targetUser = new UserDto
        {
            Id = "target-user-123",
            Email = "target@example.com"
        };

        // Act
        var result = await _service.SwitchUserAsync(targetUser);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(_service.CurrentUser, Is.Not.Null);
        Assert.That(_service.CurrentUser!.Id, Is.EqualTo("target-user-123"));
    }

    [Test]
    public async Task SwitchUserAsync_ShouldUpdateCurrentUser()
    {
        // Arrange
        var originalUser = new UserDto { Id = "original-user" };
        var newUser = new UserDto { Id = "new-user" };
        _service.SetCurrentUser(originalUser);

        // Act
        await _service.SwitchUserAsync(newUser);

        // Assert
        Assert.That(_service.CurrentUser!.Id, Is.EqualTo("new-user"));
    }

    [Test]
    public void ValidateUserContext_WhenUserSet_ShouldNotThrow()
    {
        // Arrange
        var user = new UserDto { Id = "test-user" };
        _service.SetCurrentUser(user);

        // Act & Assert
        Assert.DoesNotThrow(() => _service.ValidateUserContext());
    }

    [Test]
    public void ValidateUserContext_WhenNoUser_ShouldThrowInvalidOperationException()
    {
        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => _service.ValidateUserContext());
        Assert.That(ex!.Message, Does.Contain("No user context available"));
    }

    [Test]
    public void GetUserFilter_WhenUserSet_ShouldReturnExpression()
    {
        // Arrange
        var user = new UserDto { Id = "test-user" };
        _service.SetCurrentUser(user);

        // Act & Assert - Should not throw
        Assert.DoesNotThrow(() => _service.GetUserFilter<object>());
    }

    [Test]
    public void GetUserFilter_WhenNoUser_ShouldThrowInvalidOperationException()
    {
        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => _service.GetUserFilter<object>());
        Assert.That(ex!.Message, Does.Contain("No user context available"));
    }

    [Test]
    public async Task SetCurrentUserAsync_ThenClear_ShouldResultInNullUser()
    {
        // Arrange
        var userId = "test-user-999";
        var user = new UserDto { Id = userId };
        _mockUserProfileService.Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        await _service.SetCurrentUserAsync(userId);
        _service.ClearCurrentUser();

        // Assert
        Assert.That(_service.CurrentUser, Is.Null);
        Assert.That(_service.CurrentUserId, Is.Null);
    }

    [Test]
    public void SetCurrentUser_MultipleTimes_ShouldOverwritePreviousUser()
    {
        // Arrange
        var user1 = new UserDto { Id = "user-1" };
        var user2 = new UserDto { Id = "user-2" };
        var user3 = new UserDto { Id = "user-3" };

        // Act
        _service.SetCurrentUser(user1);
        _service.SetCurrentUser(user2);
        _service.SetCurrentUser(user3);

        // Assert
        Assert.That(_service.CurrentUser!.Id, Is.EqualTo("user-3"));
    }
}
