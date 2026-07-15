using System.Security.Claims;
using Allure.NUnit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using VaultGuard.API.Controllers;
using VaultGuard.Crypto.Interfaces;
using VaultGuard.Models;
using VaultGuard.Models.DTOs;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.BackEnd.Tests.Controllers;

/// <summary>
/// Tests for <see cref="PasswordItemsController"/> - the CRUD surface for stored password items
/// (get/create/update/delete), across a couple of the different <see cref="ItemType"/> values
/// (Login and CryptoWallet) to make sure the type field round-trips regardless of which item kind
/// is being stored. Also covers the permission checks that gate every action and the IDOR guard
/// (missing item vs. someone else's item both return 404, never 403, so an id can't be enumerated).
/// </summary>
[TestFixture]
[AllureNUnit]
public class PasswordItemsControllerTests
{
    private Mock<IPasswordItemApiService> _mockItemService = null!;
    private Mock<IPasswordEncryptionService> _mockEncryptionService = null!;
    private Mock<IVaultSessionService> _mockVaultSessionService = null!;
    private Mock<IPasswordCryptoService> _mockCryptoService = null!;
    private Mock<IPermissionService> _mockPermissionService = null!;
    private Mock<UserManager<ApplicationUser>> _mockUserManager = null!;
    private Mock<ILogger<PasswordItemsController>> _mockLogger = null!;
    private PasswordItemsController _controller = null!;

    private const string UserId = "user-1";
    private const string OtherUserId = "user-2";

    [SetUp]
    public void Setup()
    {
        _mockItemService = new Mock<IPasswordItemApiService>();
        _mockEncryptionService = new Mock<IPasswordEncryptionService>();
        _mockVaultSessionService = new Mock<IVaultSessionService>();
        _mockCryptoService = new Mock<IPasswordCryptoService>();
        _mockPermissionService = new Mock<IPermissionService>();
        var store = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
        _mockLogger = new Mock<ILogger<PasswordItemsController>>();

        _controller = new PasswordItemsController(
            _mockItemService.Object,
            _mockEncryptionService.Object,
            _mockVaultSessionService.Object,
            _mockCryptoService.Object,
            _mockPermissionService.Object,
            _mockUserManager.Object,
            _mockLogger.Object);

        // Default: authenticated as UserId, not a child, and allowed to do everything -
        // individual tests override the specific permission they're exercising.
        SetAuthenticatedUser(UserId);
        _mockPermissionService.Setup(x => x.HasPermissionAsync(UserId, It.IsAny<string>())).ReturnsAsync(true);
        _mockPermissionService.Setup(x => x.CanAccessResourceAsync(UserId, UserId, It.IsAny<string>())).ReturnsAsync(true);
        _mockPermissionService.Setup(x => x.IsInRoleAsync(UserId, ApplicationRoles.Child)).ReturnsAsync(false);
    }

    private void SetAuthenticatedUser(string userId)
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    private static PasswordItemDto MakeItem(int id, string userId = UserId, ItemType type = ItemType.Login) => new()
    {
        Id = id,
        Title = "Test item",
        Type = type,
        UserId = userId
    };

    // ----- GetAll -----

    [Test]
    public async Task GetAll_WithoutViewPermission_ReturnsForbid()
    {
        _mockPermissionService.Setup(x => x.HasPermissionAsync(UserId, Permissions.Passwords.View)).ReturnsAsync(false);

        var result = await _controller.GetAll();

        Assert.That(result.Result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task GetAll_WithPermission_ReturnsOkWithItems()
    {
        var items = new List<PasswordItemDto> { MakeItem(1), MakeItem(2, type: ItemType.CryptoWallet) };
        _mockItemService.Setup(x => x.GetAllAsync()).ReturnsAsync(items);

        var result = await _controller.GetAll();

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var returned = (IEnumerable<PasswordItemDto>)((OkObjectResult)result.Result!).Value!;
        Assert.That(returned, Is.EqualTo(items));
    }

    [Test]
    public async Task GetAll_ChildWithoutOperationPermission_ReturnsForbid()
    {
        _mockPermissionService.Setup(x => x.IsInRoleAsync(UserId, ApplicationRoles.Child)).ReturnsAsync(true);
        _mockPermissionService.Setup(x => x.ChildCanPerformPasswordOperationAsync(UserId, "view")).ReturnsAsync(false);

        var result = await _controller.GetAll();

        Assert.That(result.Result, Is.InstanceOf<ForbidResult>());
        _mockItemService.Verify(x => x.GetAllAsync(), Times.Never);
    }

    // ----- GetById -----

    [Test]
    public async Task GetById_ItemDoesNotExist_ReturnsNotFound()
    {
        _mockItemService.Setup(x => x.GetByIdAsync(42)).ReturnsAsync((PasswordItemDto?)null);

        var result = await _controller.GetById(42);

        Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task GetById_ItemBelongsToAnotherUser_ReturnsNotFound_NotForbidden()
    {
        var item = MakeItem(1, userId: OtherUserId);
        _mockItemService.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(item);
        _mockPermissionService.Setup(x => x.CanAccessResourceAsync(UserId, OtherUserId, Permissions.Passwords.View)).ReturnsAsync(false);

        var result = await _controller.GetById(1);

        // 404, not 403 - so a caller can't tell "not mine" apart from "doesn't exist" (IDOR guard).
        Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task GetById_OwnedItem_ReturnsOk()
    {
        var item = MakeItem(1);
        _mockItemService.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(item);

        var result = await _controller.GetById(1);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        Assert.That(((OkObjectResult)result.Result!).Value, Is.EqualTo(item));
    }

    // ----- Create -----

    [Test]
    public async Task Create_InvalidModel_ReturnsBadRequest()
    {
        _controller.ModelState.AddModelError("Title", "Title is required");

        var result = await _controller.Create(new CreatePasswordItemDto());

        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        _mockItemService.Verify(x => x.CreateAsync(It.IsAny<CreatePasswordItemDto>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Create_WithoutCreatePermission_ReturnsForbid()
    {
        _mockPermissionService.Setup(x => x.HasPermissionAsync(UserId, Permissions.Passwords.Create)).ReturnsAsync(false);

        var result = await _controller.Create(new CreatePasswordItemDto { Title = "New login", Type = ItemType.Login });

        Assert.That(result.Result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task Create_ChildRestrictedFromCreating_ReturnsForbid()
    {
        _mockPermissionService.Setup(x => x.IsInRoleAsync(UserId, ApplicationRoles.Child)).ReturnsAsync(true);
        _mockPermissionService.Setup(x => x.ChildCanPerformPasswordOperationAsync(UserId, "create")).ReturnsAsync(false);

        var result = await _controller.Create(new CreatePasswordItemDto { Title = "New login", Type = ItemType.Login });

        Assert.That(result.Result, Is.InstanceOf<ForbidResult>());
        _mockItemService.Verify(x => x.CreateAsync(It.IsAny<CreatePasswordItemDto>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Create_LoginItem_ReturnsCreatedAtGetById()
    {
        var createDto = new CreatePasswordItemDto { Title = "My login", Type = ItemType.Login };
        var created = MakeItem(10, type: ItemType.Login);
        _mockItemService.Setup(x => x.CreateAsync(createDto, UserId)).ReturnsAsync(created);

        var result = await _controller.Create(createDto);

        Assert.That(result.Result, Is.InstanceOf<CreatedAtActionResult>());
        var createdResult = (CreatedAtActionResult)result.Result!;
        Assert.That(createdResult.ActionName, Is.EqualTo(nameof(PasswordItemsController.GetById)));
        Assert.That(createdResult.RouteValues?["id"], Is.EqualTo(created.Id));
        Assert.That(((PasswordItemDto)createdResult.Value!).Type, Is.EqualTo(ItemType.Login));
    }

    [Test]
    public async Task Create_CryptoWalletItem_PreservesType()
    {
        var createDto = new CreatePasswordItemDto { Title = "My wallet", Type = ItemType.CryptoWallet };
        var created = MakeItem(11, type: ItemType.CryptoWallet);
        _mockItemService.Setup(x => x.CreateAsync(createDto, UserId)).ReturnsAsync(created);

        var result = await _controller.Create(createDto);

        Assert.That(result.Result, Is.InstanceOf<CreatedAtActionResult>());
        var value = (PasswordItemDto)((CreatedAtActionResult)result.Result!).Value!;
        Assert.That(value.Type, Is.EqualTo(ItemType.CryptoWallet));

        _mockItemService.Verify(x => x.CreateAsync(
            It.Is<CreatePasswordItemDto>(d => d.Type == ItemType.CryptoWallet), UserId), Times.Once);
    }

    // ----- Update -----

    [Test]
    public async Task Update_InvalidModel_ReturnsBadRequest()
    {
        _controller.ModelState.AddModelError("Title", "Title is required");

        var result = await _controller.Update(1, new UpdatePasswordItemDto());

        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Update_ItemDoesNotExist_ReturnsNotFound()
    {
        _mockItemService.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((PasswordItemDto?)null);

        var result = await _controller.Update(1, new UpdatePasswordItemDto { Title = "Updated" });

        Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task Update_WithoutEditPermission_ReturnsForbid()
    {
        var existing = MakeItem(1, userId: OtherUserId);
        _mockItemService.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(existing);
        _mockPermissionService.Setup(x => x.CanAccessResourceAsync(UserId, OtherUserId, Permissions.Passwords.Edit)).ReturnsAsync(false);

        var result = await _controller.Update(1, new UpdatePasswordItemDto { Title = "Updated" });

        Assert.That(result.Result, Is.InstanceOf<ForbidResult>());
        _mockItemService.Verify(x => x.UpdateAsync(It.IsAny<int>(), It.IsAny<UpdatePasswordItemDto>()), Times.Never);
    }

    [Test]
    public async Task Update_Valid_ReturnsOkWithUpdatedItem()
    {
        var existing = MakeItem(1);
        var updateDto = new UpdatePasswordItemDto { Title = "Updated title" };
        var updated = MakeItem(1);
        updated.Title = "Updated title";

        _mockItemService.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(existing);
        _mockItemService.Setup(x => x.UpdateAsync(1, updateDto)).ReturnsAsync(updated);

        var result = await _controller.Update(1, updateDto);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        Assert.That(((PasswordItemDto)((OkObjectResult)result.Result!).Value!).Title, Is.EqualTo("Updated title"));
    }

    // ----- Delete -----

    [Test]
    public async Task Delete_ItemDoesNotExist_ReturnsNotFound()
    {
        _mockItemService.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((PasswordItemDto?)null);

        var result = await _controller.Delete(1);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task Delete_ItemBelongsToAnotherUser_ReturnsNotFound()
    {
        var item = MakeItem(1, userId: OtherUserId);
        _mockItemService.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(item);
        _mockPermissionService.Setup(x => x.CanAccessResourceAsync(UserId, OtherUserId, Permissions.Passwords.Delete)).ReturnsAsync(false);

        var result = await _controller.Delete(1);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        _mockItemService.Verify(x => x.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task Delete_Valid_ReturnsNoContent()
    {
        var item = MakeItem(1);
        _mockItemService.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(item);
        _mockItemService.Setup(x => x.DeleteAsync(1)).ReturnsAsync(true);

        var result = await _controller.Delete(1);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task Delete_ServiceReturnsFalse_ReturnsNotFound()
    {
        var item = MakeItem(1);
        _mockItemService.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(item);
        _mockItemService.Setup(x => x.DeleteAsync(1)).ReturnsAsync(false);

        var result = await _controller.Delete(1);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }
}
