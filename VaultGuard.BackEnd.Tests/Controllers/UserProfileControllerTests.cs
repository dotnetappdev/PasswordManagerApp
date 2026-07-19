using Allure.NUnit;
using Allure.NUnit.Attributes;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using VaultGuard.API.Controllers;
using VaultGuard.Models.DTOs.Auth;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.BackEnd.Tests.Controllers;

/// <summary>
/// Tests for <see cref="UserProfileController"/> - the admin-only user settings CRUD (list/get/create/
/// update/deactivate/reactivate/delete) plus the master-password change endpoint. All controller logic
/// here just delegates to <see cref="IUserProfileService"/> and translates the result to the right HTTP
/// status, so the tests exercise exactly that mapping.
/// </summary>
[TestFixture]
[AllureNUnit]
[AllureEpic("Accounts & API Access")]
[AllureFeature("User Profiles")]
public class UserProfileControllerTests
{
    private Mock<IUserProfileService> _mockUserProfileService = null!;
    private Mock<ILogger<UserProfileController>> _mockLogger = null!;
    private UserProfileController _controller = null!;

    private const string UserId = "user-1";

    [SetUp]
    public void Setup()
    {
        _mockUserProfileService = new Mock<IUserProfileService>();
        _mockLogger = new Mock<ILogger<UserProfileController>>();
        _controller = new UserProfileController(_mockUserProfileService.Object, _mockLogger.Object);
    }

    private static UserDto MakeUserDto(string id = UserId) => new()
    {
        Id = id,
        Email = "user@example.com",
        FirstName = "Ada",
        LastName = "Lovelace",
        CreatedAt = DateTime.UtcNow.AddDays(-30),
        IsActive = true
    };

    // ----- GetAllUsers -----

    [Test]
    public async Task GetAllUsers_ReturnsOkWithUsers()
    {
        var users = new List<UserDto> { MakeUserDto("user-1"), MakeUserDto("user-2") };
        _mockUserProfileService.Setup(x => x.GetAllUsersAsync()).ReturnsAsync(users);

        var result = await _controller.GetAllUsers();

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        Assert.That(((OkObjectResult)result.Result!).Value, Is.EqualTo(users));
    }

    [Test]
    public async Task GetAllUsers_WhenServiceThrows_ReturnsInternalServerError()
    {
        _mockUserProfileService.Setup(x => x.GetAllUsersAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _controller.GetAllUsers();

        Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
        Assert.That(((ObjectResult)result.Result!).StatusCode, Is.EqualTo(500));
    }

    // ----- GetUserById -----

    [Test]
    public async Task GetUserById_UnknownId_ReturnsNotFound()
    {
        _mockUserProfileService.Setup(x => x.GetUserByIdAsync("missing")).ReturnsAsync((UserProfileDetailsDto?)null);

        var result = await _controller.GetUserById("missing");

        Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task GetUserById_KnownId_ReturnsOkWithDetails()
    {
        var details = new UserProfileDetailsDto { Id = UserId, Email = "user@example.com", TwoFactorEnabled = true };
        _mockUserProfileService.Setup(x => x.GetUserByIdAsync(UserId)).ReturnsAsync(details);

        var result = await _controller.GetUserById(UserId);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        Assert.That(((OkObjectResult)result.Result!).Value, Is.EqualTo(details));
    }

    // ----- CreateUser -----

    [Test]
    public async Task CreateUser_InvalidModel_ReturnsBadRequest()
    {
        _controller.ModelState.AddModelError("Email", "Email is required");

        var result = await _controller.CreateUser(new CreateUserProfileDto());

        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        _mockUserProfileService.Verify(x => x.CreateUserAsync(It.IsAny<CreateUserProfileDto>()), Times.Never);
    }

    [Test]
    public async Task CreateUser_ServiceFails_ReturnsBadRequestWithErrors()
    {
        var createDto = new CreateUserProfileDto { Email = "new@example.com", Password = "password123", ConfirmPassword = "password123", FirstName = "A", LastName = "B" };
        var failedResult = IdentityResult.Failed(new IdentityError { Description = "Email already taken" });
        _mockUserProfileService.Setup(x => x.CreateUserAsync(createDto)).ReturnsAsync((failedResult, null, "Email already taken"));

        var result = await _controller.CreateUser(createDto);

        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateUser_Valid_ReturnsCreatedAtGetUserById()
    {
        var createDto = new CreateUserProfileDto { Email = "new@example.com", Password = "password123", ConfirmPassword = "password123", FirstName = "A", LastName = "B" };
        var newUser = new VaultGuard.Models.ApplicationUser { Id = "new-user-id", Email = createDto.Email, FirstName = "A", LastName = "B" };
        _mockUserProfileService.Setup(x => x.CreateUserAsync(createDto)).ReturnsAsync((IdentityResult.Success, newUser, (string?)null));

        var result = await _controller.CreateUser(createDto);

        Assert.That(result.Result, Is.InstanceOf<CreatedAtActionResult>());
        var createdResult = (CreatedAtActionResult)result.Result!;
        Assert.That(createdResult.ActionName, Is.EqualTo(nameof(UserProfileController.GetUserById)));
        Assert.That(createdResult.RouteValues?["id"], Is.EqualTo(newUser.Id));
    }

    // ----- UpdateUserProfile -----

    [Test]
    public async Task UpdateUserProfile_IdMismatch_ReturnsBadRequest()
    {
        var updateDto = new UpdateUserProfileDto { Id = "different-id" };

        var result = await _controller.UpdateUserProfile(UserId, updateDto);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        _mockUserProfileService.Verify(x => x.UpdateUserProfileAsync(It.IsAny<UpdateUserProfileDto>()), Times.Never);
    }

    [Test]
    public async Task UpdateUserProfile_ServiceFails_ReturnsBadRequest()
    {
        var updateDto = new UpdateUserProfileDto { Id = UserId, Email = "bad" };
        _mockUserProfileService.Setup(x => x.UpdateUserProfileAsync(updateDto)).ReturnsAsync((false, "Invalid email"));

        var result = await _controller.UpdateUserProfile(UserId, updateDto);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task UpdateUserProfile_Valid_ReturnsNoContent()
    {
        var updateDto = new UpdateUserProfileDto { Id = UserId, FirstName = "Updated" };
        _mockUserProfileService.Setup(x => x.UpdateUserProfileAsync(updateDto)).ReturnsAsync((true, (string?)null));

        var result = await _controller.UpdateUserProfile(UserId, updateDto);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    // ----- ChangeUserPassword -----

    [Test]
    public async Task ChangeUserPassword_IdMismatch_ReturnsBadRequest()
    {
        var changeDto = new ChangePasswordDto { Id = "different-id" };

        var result = await _controller.ChangeUserPassword(UserId, changeDto);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        _mockUserProfileService.Verify(x => x.ChangeMasterPasswordAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task ChangeUserPassword_WrongCurrentPassword_ReturnsBadRequest()
    {
        var changeDto = new ChangePasswordDto
        {
            Id = UserId,
            CurrentPassword = "wrong-old-password",
            NewPassword = "new-password-123",
            ConfirmNewPassword = "new-password-123"
        };
        _mockUserProfileService.Setup(x => x.ChangeMasterPasswordAsync(UserId, "wrong-old-password", "new-password-123", null))
            .ReturnsAsync((false, "Current password is incorrect"));

        var result = await _controller.ChangeUserPassword(UserId, changeDto);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        Assert.That(((BadRequestObjectResult)result).Value, Is.EqualTo("Current password is incorrect"));
    }

    [Test]
    public async Task ChangeUserPassword_Valid_ReturnsNoContent()
    {
        var changeDto = new ChangePasswordDto
        {
            Id = UserId,
            CurrentPassword = "old-password-123",
            NewPassword = "new-password-123",
            ConfirmNewPassword = "new-password-123"
        };
        _mockUserProfileService.Setup(x => x.ChangeMasterPasswordAsync(UserId, "old-password-123", "new-password-123", null))
            .ReturnsAsync((true, (string?)null));

        var result = await _controller.ChangeUserPassword(UserId, changeDto);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        _mockUserProfileService.Verify(x => x.ChangeMasterPasswordAsync(UserId, "old-password-123", "new-password-123", null), Times.Once);
    }

    // ----- Deactivate / Reactivate -----

    [Test]
    public async Task DeactivateUser_UnknownId_ReturnsNotFound()
    {
        _mockUserProfileService.Setup(x => x.DeactivateUserAsync("missing")).ReturnsAsync(false);

        var result = await _controller.DeactivateUser("missing");

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task DeactivateUser_Valid_ReturnsNoContent()
    {
        _mockUserProfileService.Setup(x => x.DeactivateUserAsync(UserId)).ReturnsAsync(true);

        var result = await _controller.DeactivateUser(UserId);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task ReactivateUser_UnknownId_ReturnsNotFound()
    {
        _mockUserProfileService.Setup(x => x.ReactivateUserAsync("missing")).ReturnsAsync(false);

        var result = await _controller.ReactivateUser("missing");

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task ReactivateUser_Valid_ReturnsNoContent()
    {
        _mockUserProfileService.Setup(x => x.ReactivateUserAsync(UserId)).ReturnsAsync(true);

        var result = await _controller.ReactivateUser(UserId);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    // ----- DeleteUser -----

    [Test]
    public async Task DeleteUser_UnknownId_ReturnsNotFound()
    {
        _mockUserProfileService.Setup(x => x.DeleteUserAsync("missing")).ReturnsAsync(false);

        var result = await _controller.DeleteUser("missing");

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task DeleteUser_Valid_ReturnsNoContent()
    {
        _mockUserProfileService.Setup(x => x.DeleteUserAsync(UserId)).ReturnsAsync(true);

        var result = await _controller.DeleteUser(UserId);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }
}
