using System.Security.Claims;
using Allure.NUnit;
using Allure.NUnit.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using VaultGuard.API.Controllers;
using VaultGuard.Crypto.Interfaces;
using VaultGuard.Models;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.BackEnd.Tests.Controllers;

/// <summary>
/// Tests for <see cref="ApiKeysController"/> - the "Generate API Key" / X-API-Key endpoints.
/// Covers the auth-key issue flow (email + master password -> real, usable API key), the
/// authenticated key CRUD (list/create/revoke), and that every authenticated action rejects
/// callers with no NameIdentifier claim.
/// </summary>
[TestFixture]
[AllureNUnit]
[AllureEpic("Accounts & API Access")]
[AllureFeature("API Keys")]
[AllureParentSuite("Accounts & API Access")]
[AllureSuite("API Keys")]
public class ApiKeysControllerTests
{
    private Mock<IApiKeyService> _mockApiKeyService = null!;
    private Mock<UserManager<ApplicationUser>> _mockUserManager = null!;
    private Mock<IPasswordCryptoService> _mockCryptoService = null!;
    private Mock<ILogger<ApiKeysController>> _mockLogger = null!;
    private ApiKeysController _controller = null!;

    private const string UserId = "user-1";
    private const string Email = "user@example.com";

    [AllureBefore("Create mocked IApiKeyService and UserManager, and construct the controller under test")]
    [SetUp]
    public void Setup()
    {
        _mockApiKeyService = new Mock<IApiKeyService>();
        var store = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
        _mockCryptoService = new Mock<IPasswordCryptoService>();
        _mockLogger = new Mock<ILogger<ApiKeysController>>();

        _controller = new ApiKeysController(
            _mockApiKeyService.Object,
            _mockUserManager.Object,
            _mockCryptoService.Object,
            _mockLogger.Object);
    }

    private void SetAuthenticatedUser(string userId)
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    private void SetAnonymousUser()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
        };
    }

    private static ApplicationUser MakeUser(string userId = UserId, string email = Email) => new()
    {
        Id = userId,
        UserName = email,
        Email = email,
        UserSalt = Convert.ToBase64String(new byte[16]),
        MasterPasswordHash = "hashed",
        MasterPasswordIterations = 600000
    };

    // ----- IssueApiKey -----

    [Test]
    public async Task IssueApiKey_MissingEmailOrPassword_ReturnsBadRequest()
    {
        var result = await _controller.IssueApiKey(new IssueApiKeyRequest { Email = "", MasterPassword = "" });

        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task IssueApiKey_UnknownEmail_ReturnsUnauthorized()
    {
        _mockUserManager.Setup(x => x.FindByEmailAsync(Email)).ReturnsAsync((ApplicationUser?)null);

        var result = await _controller.IssueApiKey(new IssueApiKeyRequest { Email = Email, MasterPassword = "pw" });

        Assert.That(result.Result, Is.InstanceOf<UnauthorizedObjectResult>());
        _mockApiKeyService.Verify(x => x.CreateApiKeyAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task IssueApiKey_WrongMasterPassword_ReturnsUnauthorized()
    {
        var user = MakeUser();
        _mockUserManager.Setup(x => x.FindByEmailAsync(Email)).ReturnsAsync(user);
        _mockCryptoService.Setup(x => x.VerifyMasterPassword(
                "wrong-password", user.MasterPasswordHash!, It.IsAny<byte[]>(), user.MasterPasswordIterations))
            .Returns(false);

        var result = await _controller.IssueApiKey(new IssueApiKeyRequest { Email = Email, MasterPassword = "wrong-password" });

        Assert.That(result.Result, Is.InstanceOf<UnauthorizedObjectResult>());
        _mockApiKeyService.Verify(x => x.CreateApiKeyAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task IssueApiKey_ValidCredentials_ReturnsOkWithPlaintextKeyOnce()
    {
        var user = MakeUser();
        _mockUserManager.Setup(x => x.FindByEmailAsync(Email)).ReturnsAsync(user);
        _mockCryptoService.Setup(x => x.VerifyMasterPassword(
                "correct-password", user.MasterPasswordHash!, It.IsAny<byte[]>(), user.MasterPasswordIterations))
            .Returns(true);

        var issuedKey = new ApiKey { Id = Guid.NewGuid(), Name = "My key", KeyHash = "plaintext-key-value", UserId = user.Id };
        _mockApiKeyService.Setup(x => x.CreateApiKeyAsync("My key", user.Id)).ReturnsAsync(issuedKey);

        var result = await _controller.IssueApiKey(new IssueApiKeyRequest { Email = Email, MasterPassword = "correct-password", Name = "My key" });

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var response = (ApiKeyResponse)((OkObjectResult)result.Result!).Value!;
        Assert.That(response.KeyValue, Is.EqualTo("plaintext-key-value"));
        Assert.That(response.Id, Is.EqualTo(issuedKey.Id));
    }

    [Test]
    public async Task IssueApiKey_NoNameProvided_GeneratesDefaultName()
    {
        var user = MakeUser();
        _mockUserManager.Setup(x => x.FindByEmailAsync(Email)).ReturnsAsync(user);
        _mockCryptoService.Setup(x => x.VerifyMasterPassword(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(true);
        _mockApiKeyService.Setup(x => x.CreateApiKeyAsync(It.IsAny<string>(), user.Id))
            .ReturnsAsync((string name, string uid) => new ApiKey { Id = Guid.NewGuid(), Name = name, KeyHash = "key", UserId = uid });

        var result = await _controller.IssueApiKey(new IssueApiKeyRequest { Email = Email, MasterPassword = "pw" });

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var response = (ApiKeyResponse)((OkObjectResult)result.Result!).Value!;
        Assert.That(response.Name, Does.StartWith("Test connection"));
    }

    // ----- GetUserApiKeys -----

    [Test]
    public async Task GetUserApiKeys_NoUserIdClaim_ReturnsUnauthorized()
    {
        SetAnonymousUser();

        var result = await _controller.GetUserApiKeys();

        Assert.That(result.Result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task GetUserApiKeys_ReturnsKeysWithHashHidden()
    {
        SetAuthenticatedUser(UserId);
        var keys = new List<ApiKey>
        {
            new() { Id = Guid.NewGuid(), Name = "key-1", KeyHash = "real-hash-1", UserId = UserId },
            new() { Id = Guid.NewGuid(), Name = "key-2", KeyHash = "real-hash-2", UserId = UserId }
        };
        _mockApiKeyService.Setup(x => x.GetUserApiKeysAsync(UserId)).ReturnsAsync(keys);

        var result = await _controller.GetUserApiKeys();

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var returned = (List<ApiKey>)((OkObjectResult)result.Result!).Value!;
        Assert.That(returned, Has.Count.EqualTo(2));
        Assert.That(returned.All(k => k.KeyHash == "***"), Is.True, "The key hash must never be exposed via the list endpoint.");
    }

    // ----- CreateApiKey -----

    [Test]
    public async Task CreateApiKey_NoUserIdClaim_ReturnsUnauthorized()
    {
        SetAnonymousUser();

        var result = await _controller.CreateApiKey(new CreateApiKeyRequest { Name = "My key" });

        Assert.That(result.Result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task CreateApiKey_EmptyName_ReturnsBadRequest()
    {
        SetAuthenticatedUser(UserId);

        var result = await _controller.CreateApiKey(new CreateApiKeyRequest { Name = "" });

        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        _mockApiKeyService.Verify(x => x.CreateApiKeyAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task CreateApiKey_Valid_ReturnsOkWithKey()
    {
        SetAuthenticatedUser(UserId);
        var created = new ApiKey { Id = Guid.NewGuid(), Name = "My key", KeyHash = "plaintext", UserId = UserId };
        _mockApiKeyService.Setup(x => x.CreateApiKeyAsync("My key", UserId)).ReturnsAsync(created);

        var result = await _controller.CreateApiKey(new CreateApiKeyRequest { Name = "My key" });

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var response = (ApiKeyResponse)((OkObjectResult)result.Result!).Value!;
        Assert.That(response.Id, Is.EqualTo(created.Id));
        Assert.That(response.KeyValue, Is.EqualTo("plaintext"));
    }

    // ----- DeleteApiKey -----

    [Test]
    public async Task DeleteApiKey_NoUserIdClaim_ReturnsUnauthorized()
    {
        SetAnonymousUser();

        var result = await _controller.DeleteApiKey(Guid.NewGuid());

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task DeleteApiKey_ServiceReturnsTrue_ReturnsOk()
    {
        SetAuthenticatedUser(UserId);
        var keyId = Guid.NewGuid();
        _mockApiKeyService.Setup(x => x.DeleteApiKeyAsync(keyId, UserId)).ReturnsAsync(true);

        var result = await _controller.DeleteApiKey(keyId);

        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    [Test]
    public async Task DeleteApiKey_ServiceReturnsFalse_ReturnsNotFound()
    {
        SetAuthenticatedUser(UserId);
        var keyId = Guid.NewGuid();
        _mockApiKeyService.Setup(x => x.DeleteApiKeyAsync(keyId, UserId)).ReturnsAsync(false);

        var result = await _controller.DeleteApiKey(keyId);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task DeleteApiKey_ScopesToCallingUser_NotArbitraryUserId()
    {
        SetAuthenticatedUser(UserId);
        var keyId = Guid.NewGuid();
        _mockApiKeyService.Setup(x => x.DeleteApiKeyAsync(keyId, UserId)).ReturnsAsync(true);

        await _controller.DeleteApiKey(keyId);

        _mockApiKeyService.Verify(x => x.DeleteApiKeyAsync(keyId, UserId), Times.Once);
        _mockApiKeyService.Verify(x => x.DeleteApiKeyAsync(keyId, It.Is<string>(s => s != UserId)), Times.Never);
    }
}
