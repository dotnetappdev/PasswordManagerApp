using System.Security.Claims;
using Allure.NUnit;
using Allure.NUnit.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using VaultGuard.Models;
using VaultGuard.Services.Services;

namespace VaultGuard.BackEnd.Tests.Services;

/// <summary>
/// Covers VaultGuard.Services.Services.JwtService - the real, DI-wired JWT/refresh-token implementation
/// (there's a second, unregistered JwtService under VaultGuard.API that nothing actually uses; this one
/// is what PasskeyService.CreateAuthResponseAsync calls). No existing coverage existed for this class or
/// for AuthController before this suite.
///
/// _refreshTokens is a private static Dictionary shared across every JwtService instance for the life of
/// the test process, so every test here uses a fresh Guid as the userId to stay isolated from the others
/// regardless of run order.
/// </summary>
[TestFixture]
[AllureNUnit]
[AllureEpic("Accounts & API Access")]
[AllureFeature("API Tokens")]
[AllureParentSuite("Accounts & API Access")]
[AllureSuite("API Tokens")]
public class JwtServiceTests
{
    private const string SigningKey = "test-signing-key-that-is-at-least-32-bytes-long-for-hmac-sha256";

    private JwtService _jwtService = null!;
    private Mock<ILogger<JwtService>> _mockLogger = null!;

    [AllureBefore("Create a real JwtService with test JwtSettings, a mocked logger and mocked configuration")]
    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<JwtService>>();
        _jwtService = NewService(SigningKey, expiryMinutes: 60);
    }

    private JwtService NewService(string key, int expiryMinutes) => new(
        Options.Create(new JwtSettings
        {
            Key = key,
            Issuer = "VaultGuard.Tests",
            Audience = "VaultGuard.Tests",
            ExpiryMinutes = expiryMinutes,
            RefreshTokenExpiryDays = 7
        }),
        _mockLogger.Object,
        new Mock<IConfiguration>().Object);

    private static ApplicationUser NewUser() => new()
    {
        Id = Guid.NewGuid().ToString(),
        Email = "ada@example.com",
        UserName = "ada@example.com",
        FirstName = "Ada",
        LastName = "Lovelace"
    };

    // ── GenerateToken ──────────────────────────────────────────────────────────

    [Test]
    public void GenerateToken_ProducesWellFormedJwtWithExpectedClaims()
    {
        var user = NewUser();
        var token = _jwtService.GenerateToken(user);

        Assert.That(token, Is.Not.Null.And.Not.Empty);
        Assert.That(token.Split('.').Length, Is.EqualTo(3), "A JWT is header.payload.signature.");

        // GetPrincipalFromExpiredToken always sets ValidateLifetime = false, so it decodes a
        // freshly-minted (non-expired) token just as well - reuse it here to verify the claims
        // GenerateToken actually embedded, without needing to pull in JwtSecurityTokenHandler directly.
        var principal = _jwtService.GetPrincipalFromExpiredToken(token);
        Assert.That(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, Is.EqualTo(user.Id));
        Assert.That(principal.FindFirst(ClaimTypes.Email)?.Value, Is.EqualTo(user.Email));
        Assert.That(principal.FindFirst(ClaimTypes.Name)?.Value, Is.EqualTo("Ada Lovelace"));
    }

    [Test]
    public void GenerateToken_TwoCallsForSameUser_ProduceDifferentTokens()
    {
        var user = NewUser();
        var a = _jwtService.GenerateToken(user);
        var b = _jwtService.GenerateToken(user);
        Assert.That(a, Is.Not.EqualTo(b), "Each token embeds a unique jti + iat, so identical input must still differ.");
    }

    [Test]
    public void GenerateToken_MissingNames_TrimsToEmptyNameClaim()
    {
        var user = NewUser();
        user.FirstName = null;
        user.LastName = null;

        var token = _jwtService.GenerateToken(user);
        var principal = _jwtService.GetPrincipalFromExpiredToken(token);

        Assert.That(principal.FindFirst(ClaimTypes.Name)?.Value, Is.EqualTo(string.Empty));
    }

    // ── GenerateRefreshToken ───────────────────────────────────────────────────

    [Test]
    public void GenerateRefreshToken_ProducesNonEmptyBase64String()
    {
        var token = _jwtService.GenerateRefreshToken();
        Assert.That(token, Is.Not.Null.And.Not.Empty);
        Assert.DoesNotThrow(() => Convert.FromBase64String(token), "Should be valid Base64 (32 random bytes).");
    }

    [Test]
    public void GenerateRefreshToken_VariesPerCall()
    {
        var a = _jwtService.GenerateRefreshToken();
        var b = _jwtService.GenerateRefreshToken();
        Assert.That(a, Is.Not.EqualTo(b));
    }

    // ── GetPrincipalFromExpiredToken ──────────────────────────────────────────

    [Test]
    public void GetPrincipalFromExpiredToken_AcceptsAnAlreadyExpiredToken()
    {
        var user = NewUser();
        var expiredService = NewService(SigningKey, expiryMinutes: -60);
        var expiredToken = expiredService.GenerateToken(user);

        var principal = _jwtService.GetPrincipalFromExpiredToken(expiredToken);

        Assert.That(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, Is.EqualTo(user.Id));
    }

    [Test]
    public void GetPrincipalFromExpiredToken_WrongSigningKey_Throws()
    {
        var user = NewUser();
        var otherService = NewService("a-completely-different-signing-key-of-sufficient-length!!", expiryMinutes: 60);
        var tokenSignedByOther = otherService.GenerateToken(user);

        // The exact exception type is whatever Microsoft.IdentityModel.Tokens' signature validation
        // throws for a key mismatch - asserting the general Exception base keeps this robust to which
        // specific subtype that turns out to be, rather than guessing wrong.
        Assert.Catch<Exception>(() => _jwtService.GetPrincipalFromExpiredToken(tokenSignedByOther));
    }

    // ── CreateAuthResponseAsync ────────────────────────────────────────────────

    [Test]
    public async Task CreateAuthResponseAsync_ReturnsPopulatedResponse_AndPersistsTheRefreshToken()
    {
        var user = NewUser();
        var response = await _jwtService.CreateAuthResponseAsync(user);

        Assert.That(response.Token, Is.Not.Null.And.Not.Empty);
        Assert.That(response.RefreshToken, Is.Not.Null.And.Not.Empty);
        Assert.That(response.User.Id, Is.EqualTo(user.Id));
        Assert.That(response.User.Email, Is.EqualTo(user.Email));
        Assert.That(response.ExpiresAt, Is.GreaterThan(DateTime.UtcNow));

        Assert.That(await _jwtService.ValidateRefreshTokenAsync(user.Id, response.RefreshToken), Is.True);
    }

    // ── ValidateRefreshTokenAsync / SaveRefreshTokenAsync / RevokeRefreshTokenAsync ──

    [Test]
    public async Task ValidateRefreshTokenAsync_UnknownUser_ReturnsFalse()
    {
        Assert.That(await _jwtService.ValidateRefreshTokenAsync(Guid.NewGuid().ToString(), "some-token"), Is.False);
    }

    [Test]
    public async Task ValidateRefreshTokenAsync_KnownUserWrongToken_ReturnsFalse()
    {
        var userId = Guid.NewGuid().ToString();
        await _jwtService.SaveRefreshTokenAsync(userId, _jwtService.GenerateRefreshToken());

        Assert.That(await _jwtService.ValidateRefreshTokenAsync(userId, "not-the-saved-token"), Is.False);
    }

    [Test]
    public async Task SaveRefreshTokenAsync_ThenValidate_RoundTrips()
    {
        var userId = Guid.NewGuid().ToString();
        var token = _jwtService.GenerateRefreshToken();

        await _jwtService.SaveRefreshTokenAsync(userId, token);

        Assert.That(await _jwtService.ValidateRefreshTokenAsync(userId, token), Is.True);
    }

    [Test]
    public async Task SaveRefreshTokenAsync_KeepsOnlyTheLastFiveTokensPerUser()
    {
        var userId = Guid.NewGuid().ToString();
        var tokens = new List<string>();
        for (var i = 0; i < 6; i++)
        {
            var token = _jwtService.GenerateRefreshToken();
            tokens.Add(token);
            await _jwtService.SaveRefreshTokenAsync(userId, token);
        }

        // Saving a 6th token evicts the oldest (first) one; the 5 most recent must remain valid.
        Assert.That(await _jwtService.ValidateRefreshTokenAsync(userId, tokens[0]), Is.False,
            "The oldest of 6 saved tokens should have been evicted once the 5-token cap was exceeded.");
        Assert.That(await _jwtService.ValidateRefreshTokenAsync(userId, tokens[^1]), Is.True);
    }

    [Test]
    public async Task RevokeRefreshTokenAsync_RemovesTheToken()
    {
        var userId = Guid.NewGuid().ToString();
        var token = _jwtService.GenerateRefreshToken();
        await _jwtService.SaveRefreshTokenAsync(userId, token);

        await _jwtService.RevokeRefreshTokenAsync(userId, token);

        Assert.That(await _jwtService.ValidateRefreshTokenAsync(userId, token), Is.False);
    }

    [Test]
    public async Task RevokeRefreshTokenAsync_DoesNotAffectOtherTokensForSameUser()
    {
        var userId = Guid.NewGuid().ToString();
        var keep = _jwtService.GenerateRefreshToken();
        var revoke = _jwtService.GenerateRefreshToken();
        await _jwtService.SaveRefreshTokenAsync(userId, keep);
        await _jwtService.SaveRefreshTokenAsync(userId, revoke);

        await _jwtService.RevokeRefreshTokenAsync(userId, revoke);

        Assert.That(await _jwtService.ValidateRefreshTokenAsync(userId, keep), Is.True);
        Assert.That(await _jwtService.ValidateRefreshTokenAsync(userId, revoke), Is.False);
    }

    [Test]
    public void RevokeRefreshTokenAsync_UnknownUserOrToken_DoesNotThrow()
    {
        Assert.DoesNotThrowAsync(async () =>
            await _jwtService.RevokeRefreshTokenAsync(Guid.NewGuid().ToString(), "never-saved"));
    }
}
