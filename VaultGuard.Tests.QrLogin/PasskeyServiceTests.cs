using System.Text;
using Fido2NetLib;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VaultGuard.Crypto.Services;
using VaultGuard.DAL;
using VaultGuard.Models;
using VaultGuard.Models.DTOs.Auth;
using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Services;

namespace VaultGuard.Tests.QrLogin;

/// <summary>
/// Service-level tests for <see cref="PasskeyService"/> - the passkey engine shared by the Blazor web
/// app, the API and the mobile clients. Covers the challenge store, the fail-closed low-level verifier,
/// and the full website-passkey (vault) create -> assert round trip against a real in-memory database and
/// real cryptography (so the zero-knowledge storage of the private key is actually exercised).
/// </summary>
public class PasskeyServiceTests
{
    private const string MasterPassword = "correct horse battery staple";
    private const string WrongPassword = "not the master password";

    private static PasskeyService BuildService(out string userId, string masterPassword = MasterPassword)
    {
        var options = new DbContextOptionsBuilder<VaultGuardDbContext>()
            .UseInMemoryDatabase($"passkey-tests-{Guid.NewGuid()}")
            .Options;
        var context = new VaultGuardDbContext(options);

        // Real crypto so master-password verification and key wrapping run for real.
        var crypto = new PasswordCryptoService(new CryptographyService());
        var salt = crypto.GenerateUserSalt();

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "passkey.tester@example.com",
            Email = "passkey.tester@example.com",
            UserSalt = Convert.ToBase64String(salt),
            MasterPasswordHash = crypto.CreateMasterPasswordHash(masterPassword, salt),
            MasterPasswordIterations = 600000,
        };
        context.Users.Add(user);
        context.SaveChanges();
        userId = user.Id;

        return new PasskeyService(
            context,
            crypto,
            Mock.Of<IPasswordEncryptionService>(), // encryption service is not used by the vault passkey path
            NullLogger<PasskeyService>.Instance,
            // IFido2 only drives the account register/authenticate ceremonies, which these tests don't
            // touch - the vault create/assert path signs with the software authenticator. So mock it.
            Mock.Of<IFido2>());
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static VaultPasskeyCreateRequestDto CreateRequest(string rpId, string masterPassword) => new()
    {
        MasterPassword = masterPassword,
        RpId = rpId,
        RpName = rpId,
        UserHandle = Base64Url(Encoding.UTF8.GetBytes("user-handle")),
        UserName = "octocat",
        UserDisplayName = "Octo Cat",
    };

    private static VaultPasskeyAssertRequestDto AssertRequest(string rpId, string masterPassword) => new()
    {
        MasterPassword = masterPassword,
        RpId = rpId,
        ClientDataJSON = Base64Url(Encoding.UTF8.GetBytes("{\"type\":\"webauthn.get\",\"challenge\":\"abc\"}")),
        AllowCredentialIds = new List<string>(),
    };

    // ── Challenge store ─────────────────────────────────────────────────────────────

    [Fact]
    public void GenerateChallenge_ReturnsUnique32ByteBase64()
    {
        var svc = BuildService(out _);

        var a = svc.GenerateChallenge();
        var b = svc.GenerateChallenge();

        Assert.NotEqual(a, b);
        Assert.Equal(32, Convert.FromBase64String(a).Length);
        Assert.Equal(32, Convert.FromBase64String(b).Length);
    }

    [Fact]
    public async Task StoreAndVerifyChallenge_IsSingleUse()
    {
        var svc = BuildService(out var userId);
        var challenge = svc.GenerateChallenge();
        await svc.StoreChallengeAsync(challenge, userId);

        Assert.True(await svc.VerifyAndRemoveChallengeAsync(challenge, userId));   // first use ok
        Assert.False(await svc.VerifyAndRemoveChallengeAsync(challenge, userId));  // replay rejected
    }

    [Fact]
    public async Task VerifyChallenge_WrongUser_Fails()
    {
        var svc = BuildService(out var userId);
        var challenge = svc.GenerateChallenge();
        await svc.StoreChallengeAsync(challenge, userId);

        Assert.False(await svc.VerifyAndRemoveChallengeAsync(challenge, "some-other-user"));
    }

    [Fact]
    public async Task VerifyChallenge_Expired_Fails()
    {
        var svc = BuildService(out var userId);
        var challenge = svc.GenerateChallenge();
        // Store already expired (negative expiry window).
        await svc.StoreChallengeAsync(challenge, userId, expiryMinutes: -1);

        Assert.False(await svc.VerifyAndRemoveChallengeAsync(challenge, userId));
    }

    // ── Low-level verifier must fail closed ─────────────────────────────────────────

    [Fact]
    public async Task VerifyPasskeyAssertion_FailsClosed()
    {
        var svc = BuildService(out _);

        // This lower-level helper does not (yet) perform real signature verification, so it MUST return
        // false rather than accept an unverified assertion. This guards against reintroducing the old
        // "return true unconditionally" auth-bypass.
        var result = await svc.VerifyPasskeyAssertionAsync(
            credentialId: "cred", clientDataJson: "cdj", authenticatorData: "ad",
            signature: "sig", challenge: "chal");

        Assert.False(result);
    }

    // ── Website (vault) passkey create/assert ───────────────────────────────────────

    [Fact]
    public async Task CreateVaultPasskey_WrongMasterPassword_Fails()
    {
        var svc = BuildService(out var userId);

        var result = await svc.CreateVaultPasskeyAsync(userId, CreateRequest("github.com", WrongPassword));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CreateVaultPasskey_ValidMasterPassword_Succeeds()
    {
        var svc = BuildService(out var userId);

        var result = await svc.CreateVaultPasskeyAsync(userId, CreateRequest("github.com", MasterPassword));

        Assert.True(result.Success);
        Assert.False(string.IsNullOrEmpty(result.CredentialId));
        Assert.False(string.IsNullOrEmpty(result.PublicKeyCose));
    }

    [Fact]
    public async Task CreateThenAssertVaultPasskey_RoundTrips()
    {
        var svc = BuildService(out var userId);

        var created = await svc.CreateVaultPasskeyAsync(userId, CreateRequest("github.com", MasterPassword));
        Assert.True(created.Success);

        var asserted = await svc.AssertVaultPasskeyAsync(userId, AssertRequest("github.com", MasterPassword));

        Assert.True(asserted.Success);
        Assert.Equal(created.CredentialId, asserted.CredentialId);
        Assert.False(string.IsNullOrEmpty(asserted.Signature));
        Assert.False(string.IsNullOrEmpty(asserted.AuthenticatorData));
    }

    [Fact]
    public async Task AssertVaultPasskey_WrongMasterPassword_Fails()
    {
        var svc = BuildService(out var userId);
        await svc.CreateVaultPasskeyAsync(userId, CreateRequest("github.com", MasterPassword));

        var asserted = await svc.AssertVaultPasskeyAsync(userId, AssertRequest("github.com", WrongPassword));

        Assert.False(asserted.Success);
    }

    [Fact]
    public async Task AssertVaultPasskey_NoPasskeyForSite_Fails()
    {
        var svc = BuildService(out var userId);
        await svc.CreateVaultPasskeyAsync(userId, CreateRequest("github.com", MasterPassword));

        // A different site has no stored passkey.
        var asserted = await svc.AssertVaultPasskeyAsync(userId, AssertRequest("example.com", MasterPassword));

        Assert.False(asserted.Success);
    }
}
