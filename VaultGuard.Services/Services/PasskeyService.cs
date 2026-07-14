using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VaultGuard.Crypto.Interfaces;
using VaultGuard.DAL.Interfaces;
using VaultGuard.Models;
using VaultGuard.Models.DTOs.Auth;
using VaultGuard.Services.Interfaces;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using Fido2NetLib;
using Fido2NetLib.Objects;

namespace VaultGuard.Services.Services;

/// <summary>
/// Service for managing WebAuthn Passkey operations
/// </summary>
public class PasskeyService : IPasskeyService
{
    private readonly IVaultGuardDbContext _context;
    private readonly IPasswordCryptoService _passwordCryptoService;
    private readonly IPasswordEncryptionService _passwordEncryptionService;
    private readonly ILogger<PasskeyService> _logger;
    private readonly IFido2 _fido2;
    private readonly IJwtService? _jwtService;

    // Process-wide so a challenge issued by the scoped "start" instance is still found by the
    // scoped "complete" instance (Blazor circuits / separate requests get different instances).
    private static readonly ConcurrentDictionary<string, (string Challenge, string UserId, DateTime Expiry)> _challenges = new();

    public PasskeyService(
        IVaultGuardDbContext context,
        IPasswordCryptoService passwordCryptoService,
        IPasswordEncryptionService passwordEncryptionService,
        ILogger<PasskeyService> logger,
        IFido2 fido2,
        IJwtService? jwtService = null)
    {
        _context = context;
        _passwordCryptoService = passwordCryptoService;
        _passwordEncryptionService = passwordEncryptionService;
        _logger = logger;
        _fido2 = fido2;
        _jwtService = jwtService;
    }

    public async Task<PasskeyRegistrationStartResponseDto?> StartPasskeyRegistrationAsync(string userId, PasskeyRegistrationStartDto startDto)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                return null;
            }

            // Verify master password
            if (!await VerifyMasterPasswordAsync(user, startDto.MasterPassword))
            {
                _logger.LogWarning("Invalid master password for passkey registration: {UserId}", userId);
                return null;
            }

            // Get existing passkeys for this user
            var existingPasskeys = await _context.UserPasskeys
                .Where(p => p.UserId == userId && p.IsActive)
                .Select(p => new PublicKeyCredentialDescriptor(Convert.FromBase64String(p.CredentialId)))
                .ToListAsync();

            // Create WebAuthn user
            var fido2User = new Fido2User
            {
                DisplayName = $"{user.FirstName} {user.LastName}".Trim(),
                Name = user.Email ?? "",
                Id = Encoding.UTF8.GetBytes(userId)
            };

            // Create registration options
            var options = _fido2.RequestNewCredential(
                fido2User,
                existingPasskeys,
                AuthenticatorSelection.Default,
                AttestationConveyancePreference.None
            );

            // Store challenge temporarily
            var challenge = Convert.ToBase64String(options.Challenge);
            await StoreChallengeAsync(challenge, userId, 5);

            return new PasskeyRegistrationStartResponseDto
            {
                Challenge = challenge,
                // Fido2's ToJson() emits the WebAuthn-correct shape (base64url byte fields) that
                // the browser's navigator.credentials.create() expects.
                CredentialCreationOptions = options.ToJson()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting passkey registration for user: {UserId}", userId);
            return null;
        }
    }

    public async Task<bool> CompletePasskeyRegistrationAsync(string userId, PasskeyRegistrationCompleteDto completeDto)
    {
        try
        {
            // Verify challenge
            if (!await VerifyAndRemoveChallengeAsync(completeDto.Challenge, userId))
            {
                _logger.LogWarning("Invalid challenge for passkey registration: {UserId}", userId);
                return false;
            }

            // Parse credential response
            var credentialResponse = JsonSerializer.Deserialize<AuthenticatorAttestationRawResponse>(completeDto.CredentialResponse);
            if (credentialResponse == null)
            {
                _logger.LogWarning("Invalid credential response for passkey registration: {UserId}", userId);
                return false;
            }

            // Rebuild the ORIGINAL options the browser was challenged with (echoed back by the
            // client). Verifying against a reconstructed-from-challenge stub would skip rp/user
            // checks; using the real options makes Fido2 enforce them.
            var options = CredentialCreateOptions.FromJson(completeDto.OriginalOptionsJson);
            if (!options.Challenge.SequenceEqual(Convert.FromBase64String(completeDto.Challenge)))
            {
                _logger.LogWarning("Challenge mismatch between options and request for passkey registration: {UserId}", userId);
                return false;
            }

            // Fido2 performs the full attestation verification (origin, rp id hash, signature).
            var result = await _fido2.MakeNewCredentialAsync(credentialResponse, options, IsCredentialIdUniqueToUserAsync);
            if (result.Status != "ok")
            {
                _logger.LogWarning("Passkey registration failed: {Result}", result.ErrorMessage);
                return false;
            }

            // Create passkey entity
            var passkey = new UserPasskey
            {
                UserId = userId,
                CredentialId = Convert.ToBase64String(result.Result.CredentialId),
                Name = completeDto.PasskeyName,
                PublicKey = Convert.ToBase64String(result.Result.PublicKey),
                SignatureCounter = result.Result.Counter,
                DeviceType = completeDto.DeviceType,
                IsBackedUp = false, // Simplified check for now
                RequiresUserVerification = true,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                StoreInVault = completeDto.StoreInVault
            };

            // Store in vault if requested
            if (completeDto.StoreInVault)
            {
                var passkeyData = new
                {
                    Name = completeDto.PasskeyName,
                    CredentialId = passkey.CredentialId,
                    DeviceType = completeDto.DeviceType,
                    CreatedAt = passkey.CreatedAt
                };
                
                var encryptedData = await EncryptForVaultAsync(userId, JsonSerializer.Serialize(passkeyData));
                passkey.EncryptedVaultData = encryptedData;
            }

            await _context.UserPasskeys.AddAsync(passkey);

            // Enable passkeys for user if not already enabled
            var user = await _context.Users.FindAsync(userId);
            if (user != null && !user.PasskeysEnabled)
            {
                user.PasskeysEnabled = true;
                user.PasskeysEnabledAt = DateTime.UtcNow;
                user.StorePasskeysInVault = completeDto.StoreInVault;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Passkey registered successfully for user: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing passkey registration for user: {UserId}", userId);
            return false;
        }
    }

    public async Task<PasskeyAuthenticationStartResponseDto?> StartPasskeyAuthenticationAsync(string email)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || !user.PasskeysEnabled)
            {
                _logger.LogWarning("User not found or passkeys not enabled: {Email}", email);
                return null;
            }

            // Get user's passkeys
            var userPasskeys = await _context.UserPasskeys
                .Where(p => p.UserId == user.Id && p.IsActive)
                .Select(p => new PublicKeyCredentialDescriptor(Convert.FromBase64String(p.CredentialId)))
                .ToListAsync();

            if (!userPasskeys.Any())
            {
                _logger.LogWarning("No active passkeys found for user: {Email}", email);
                return null;
            }

            // Create assertion options
            var options = _fido2.GetAssertionOptions(userPasskeys, UserVerificationRequirement.Preferred);

            // Store challenge temporarily
            var challenge = Convert.ToBase64String(options.Challenge);
            await StoreChallengeAsync(challenge, user.Id, 5);

            return new PasskeyAuthenticationStartResponseDto
            {
                Challenge = challenge,
                CredentialRequestOptions = options.ToJson()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting passkey authentication for email: {Email}", email);
            return null;
        }
    }

    public async Task<AuthResponseDto?> CompletePasskeyAuthenticationAsync(PasskeyAuthenticationCompleteDto completeDto)
    {
        try
        {
            var passkey = await VerifyAuthenticationAssertionAsync(completeDto);
            if (passkey == null)
            {
                return null;
            }

            // Issue a real signed token + refresh token via the JWT service. Without it we cannot
            // mint a valid session, so fail closed rather than return a placeholder token.
            if (_jwtService == null)
            {
                _logger.LogError("Passkey authentication succeeded but no IJwtService is registered to issue a token.");
                return null;
            }

            return await _jwtService.CreateAuthResponseAsync(passkey.User);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing passkey authentication");
            return null;
        }
    }

    public async Task<string?> VerifyPasskeyLoginAndGetUserIdAsync(PasskeyAuthenticationCompleteDto completeDto)
    {
        try
        {
            var passkey = await VerifyAuthenticationAssertionAsync(completeDto);
            return passkey?.UserId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying passkey login assertion");
            return null;
        }
    }

    // Cryptographically verifies a WebAuthn assertion against the stored credential and, on
    // success, updates LastUsedAt/SignatureCounter/LastLoginAt. Shared by the API-token login
    // flow (CompletePasskeyAuthenticationAsync) and the local/session-based login flow
    // (VerifyPasskeyLoginAndGetUserIdAsync) so both trust the exact same verification path.
    private async Task<UserPasskey?> VerifyAuthenticationAssertionAsync(PasskeyAuthenticationCompleteDto completeDto)
    {
        // Parse credential response
        var credentialResponse = JsonSerializer.Deserialize<AuthenticatorAssertionRawResponse>(completeDto.CredentialResponse);
        if (credentialResponse == null)
        {
            _logger.LogWarning("Invalid credential response for passkey authentication");
            return null;
        }

        // Find passkey
        var credentialId = Convert.ToBase64String(credentialResponse.Id);
        var passkey = await _context.UserPasskeys
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.CredentialId == credentialId && p.IsActive);

        if (passkey == null)
        {
            _logger.LogWarning("Passkey not found: {CredentialId}", credentialId);
            return null;
        }

        // Verify challenge
        if (!await VerifyAndRemoveChallengeAsync(completeDto.Challenge, passkey.UserId))
        {
            _logger.LogWarning("Invalid challenge for passkey authentication: {UserId}", passkey.UserId);
            return null;
        }

        // Rebuild the ORIGINAL assertion options the browser was challenged with.
        var options = AssertionOptions.FromJson(completeDto.OriginalOptionsJson);
        if (!options.Challenge.SequenceEqual(Convert.FromBase64String(completeDto.Challenge)))
        {
            _logger.LogWarning("Challenge mismatch for passkey authentication: {UserId}", passkey.UserId);
            return null;
        }

        // Fido2 verifies the signature against the stored public key and the signature counter.
        var result = await _fido2.MakeAssertionAsync(
            credentialResponse, options, Convert.FromBase64String(passkey.PublicKey),
            passkey.SignatureCounter, IsUserHandleOwnerOfCredentialIdAsync);
        if (result.Status != "ok")
        {
            _logger.LogWarning("Passkey authentication failed: {Result}", result.ErrorMessage);
            return null;
        }

        // Update passkey last used
        passkey.LastUsedAt = DateTime.UtcNow;
        passkey.SignatureCounter = result.Counter;

        // Update user last login
        passkey.User.LastLoginAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Passkey authentication successful for user: {UserId}", passkey.UserId);
        return passkey;
    }

    public async Task<PasskeyListResponseDto> GetUserPasskeysAsync(string userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            var passkeys = await _context.UserPasskeys
                .Where(p => p.UserId == userId && p.IsActive)
                .Select(p => new PasskeyDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    DeviceType = p.DeviceType,
                    IsBackedUp = p.IsBackedUp,
                    RequiresUserVerification = p.RequiresUserVerification,
                    CreatedAt = p.CreatedAt,
                    LastUsedAt = p.LastUsedAt,
                    IsActive = p.IsActive,
                    StoreInVault = p.StoreInVault
                })
                .ToListAsync();

            return new PasskeyListResponseDto
            {
                Passkeys = passkeys,
                PasskeysEnabled = user?.PasskeysEnabled ?? false,
                PasskeysEnabledAt = user?.PasskeysEnabledAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user passkeys: {UserId}", userId);
            return new PasskeyListResponseDto();
        }
    }

    public async Task<bool> DeletePasskeyAsync(string userId, int passkeyId, PasskeyDeleteDto deleteDto)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            // Verify master password
            if (!await VerifyMasterPasswordAsync(user, deleteDto.MasterPassword))
            {
                _logger.LogWarning("Invalid master password for passkey deletion: {UserId}", userId);
                return false;
            }

            var passkey = await _context.UserPasskeys
                .FirstOrDefaultAsync(p => p.Id == passkeyId && p.UserId == userId);

            if (passkey == null)
            {
                return false;
            }

            passkey.IsActive = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Passkey deleted for user: {UserId}, PasskeyId: {PasskeyId}", userId, passkeyId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting passkey: {UserId}, PasskeyId: {PasskeyId}", userId, passkeyId);
            return false;
        }
    }

    public async Task<PasskeyStatusDto> GetPasskeyStatusAsync(string userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            var passkeyCount = await _context.UserPasskeys.CountAsync(p => p.UserId == userId && p.IsActive);

            return new PasskeyStatusDto
            {
                IsEnabled = user?.PasskeysEnabled ?? false,
                EnabledAt = user?.PasskeysEnabledAt,
                PasskeyCount = passkeyCount,
                StoreInVault = user?.StorePasskeysInVault ?? true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting passkey status: {UserId}", userId);
            return new PasskeyStatusDto();
        }
    }

    public async Task<bool> UpdatePasskeySettingsAsync(string userId, bool storeInVault, string masterPassword)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            // Verify master password
            if (!await VerifyMasterPasswordAsync(user, masterPassword))
            {
                return false;
            }

            user.StorePasskeysInVault = storeInVault;
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating passkey settings: {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> StorePasskeyInVaultAsync(string userId, int passkeyId, object passkeyData)
    {
        try
        {
            var passkey = await _context.UserPasskeys
                .FirstOrDefaultAsync(p => p.Id == passkeyId && p.UserId == userId);

            if (passkey == null)
            {
                return false;
            }

            var encryptedData = await EncryptForVaultAsync(userId, JsonSerializer.Serialize(passkeyData));
            passkey.EncryptedVaultData = encryptedData;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing passkey in vault: {UserId}, PasskeyId: {PasskeyId}", userId, passkeyId);
            return false;
        }
    }

    public async Task<object?> RetrievePasskeyFromVaultAsync(string userId, int passkeyId)
    {
        try
        {
            var passkey = await _context.UserPasskeys
                .FirstOrDefaultAsync(p => p.Id == passkeyId && p.UserId == userId);

            if (passkey?.EncryptedVaultData == null)
            {
                return null;
            }

            var decryptedData = await DecryptFromVaultAsync(userId, passkey.EncryptedVaultData);
            return JsonSerializer.Deserialize<object>(decryptedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving passkey from vault: {UserId}, PasskeyId: {PasskeyId}", userId, passkeyId);
            return null;
        }
    }

    public async Task<bool> VerifyPasskeyAssertionAsync(string credentialId, string clientDataJson, string authenticatorData, string signature, string challenge)
    {
        // SECURITY: this previously returned true unconditionally, which would treat any
        // assertion as valid. Full WebAuthn assertion verification is performed by
        // CompletePasskeyAuthenticationAsync (Fido2.MakeAssertionAsync) and, for third-party
        // site passkeys, by AssertVaultPasskeyAsync. Until this lower-level helper performs a
        // real ECDSA/RSA signature check against the stored COSE public key it MUST fail closed
        // rather than report success.
        await Task.CompletedTask;
        _logger.LogWarning(
            "VerifyPasskeyAssertionAsync called for credential {CredentialId} but low-level " +
            "verification is not implemented; failing closed. Use CompletePasskeyAuthenticationAsync.",
            credentialId);
        return false;
    }

    public string GenerateChallenge()
    {
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes);
    }

    public async Task<bool> StoreChallengeAsync(string challenge, string userId, int expiryMinutes = 5)
    {
        try
        {
            var expiry = DateTime.UtcNow.AddMinutes(expiryMinutes);
            _challenges[challenge] = (challenge, userId, expiry);
            
            // Clean up expired challenges
            var expiredChallenges = _challenges.Where(c => c.Value.Expiry < DateTime.UtcNow).ToList();
            foreach (var expired in expiredChallenges)
            {
                _challenges.TryRemove(expired.Key, out _);
            }
            
            return true;
        }
        catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", logEx); return false; }
    }

    public async Task<bool> VerifyAndRemoveChallengeAsync(string challenge, string userId)
    {
        try
        {
            if (_challenges.TryGetValue(challenge, out var storedChallenge))
            {
                if (storedChallenge.UserId == userId && storedChallenge.Expiry > DateTime.UtcNow)
                {
                    _challenges.TryRemove(challenge, out _);
                    return true;
                }
                else
                {
                    _challenges.TryRemove(challenge, out _); // Remove expired or invalid challenge
                }
            }
            return false;
        }
        catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", logEx); return false; }
    }

    // Marks software (third-party site) passkeys so they're not confused with PM-login passkeys.
    private const string VaultPasskeyDeviceType = "VaultPasskey";

    public async Task<VaultPasskeyCreateResponseDto> CreateVaultPasskeyAsync(string userId, VaultPasskeyCreateRequestDto request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.RpId))
                return new VaultPasskeyCreateResponseDto { Success = false, Error = "rpId is required" };

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return new VaultPasskeyCreateResponseDto { Success = false, Error = "User not found" };

            if (!await VerifyMasterPasswordAsync(user, request.MasterPassword))
                return new VaultPasskeyCreateResponseDto { Success = false, Error = "Invalid master password" };

            var masterKey = _passwordCryptoService.DeriveMasterKey(request.MasterPassword, Convert.FromBase64String(user.UserSalt));
            try
            {
                var cred = WebAuthnSoftwareAuthenticator.CreateCredential(request.RpId);

                // Encrypt {rpId, userHandle, userName, privateKey} under the master key.
                var payload = JsonSerializer.Serialize(new VaultPasskeySecret
                {
                    RpId = request.RpId,
                    UserHandle = request.UserHandle,
                    UserName = request.UserName,
                    PrivateKeyPkcs8 = Convert.ToBase64String(cred.Pkcs8PrivateKey)
                });
                var enc = _passwordCryptoService.EncryptPasswordWithKey(payload, masterKey);

                var passkey = new UserPasskey
                {
                    UserId = userId,
                    // Stored as standard base64 to match the existing CredentialId convention.
                    CredentialId = Convert.ToBase64String(cred.CredentialId),
                    Name = $"{request.RpId}{(string.IsNullOrEmpty(request.UserName) ? "" : " · " + request.UserName)}",
                    PublicKey = Convert.ToBase64String(cred.CosePublicKey),
                    SignatureCounter = 0,
                    DeviceType = VaultPasskeyDeviceType,
                    IsBackedUp = true,
                    RequiresUserVerification = true,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    StoreInVault = true,
                    EncryptedVaultData = JsonSerializer.Serialize(enc)
                };
                _context.UserPasskeys.Add(passkey);
                await _context.SaveChangesAsync();

                Array.Clear(cred.Pkcs8PrivateKey, 0, cred.Pkcs8PrivateKey.Length);

                return new VaultPasskeyCreateResponseDto
                {
                    Success = true,
                    CredentialId = WebAuthnSoftwareAuthenticator.Base64Url(cred.CredentialId),
                    AttestationObject = Convert.ToBase64String(cred.AttestationObject),
                    PublicKeyCose = Convert.ToBase64String(cred.CosePublicKey)
                };
            }
            finally
            {
                Array.Clear(masterKey, 0, masterKey.Length);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating vault passkey for {UserId}", userId);
            return new VaultPasskeyCreateResponseDto { Success = false, Error = "Passkey creation failed" };
        }
    }

    public async Task<VaultPasskeyAssertResponseDto> AssertVaultPasskeyAsync(string userId, VaultPasskeyAssertRequestDto request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.RpId) || string.IsNullOrEmpty(request.ClientDataJSON))
                return new VaultPasskeyAssertResponseDto { Success = false, Error = "rpId and clientDataJSON are required" };

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return new VaultPasskeyAssertResponseDto { Success = false, Error = "User not found" };

            if (!await VerifyMasterPasswordAsync(user, request.MasterPassword))
                return new VaultPasskeyAssertResponseDto { Success = false, Error = "Invalid master password" };

            var masterKey = _passwordCryptoService.DeriveMasterKey(request.MasterPassword, Convert.FromBase64String(user.UserSalt));
            try
            {
                var candidates = await _context.UserPasskeys
                    .Where(p => p.UserId == userId && p.IsActive && p.DeviceType == VaultPasskeyDeviceType)
                    .ToListAsync();

                // Match by credential id (allowCredentials) by comparing raw bytes, so base64 vs
                // base64url encoding never matters.
                var allowBytes = request.AllowCredentialIds
                    .Select(SafeBase64UrlDecode).Where(b => b != null).Select(b => Convert.ToBase64String(b!)).ToHashSet();

                UserPasskey? match = null;
                VaultPasskeySecret? secret = null;

                foreach (var candidate in OrderCandidates(candidates, allowBytes.Count > 0))
                {
                    if (allowBytes.Count > 0 && !allowBytes.Contains(NormalizeCredId(candidate.CredentialId)))
                        continue;

                    var s = TryDecryptSecret(candidate, masterKey);
                    if (s == null) continue;
                    if (allowBytes.Count == 0 && !string.Equals(s.RpId, request.RpId, StringComparison.OrdinalIgnoreCase))
                        continue;

                    match = candidate;
                    secret = s;
                    break;
                }

                if (match == null || secret == null)
                    return new VaultPasskeyAssertResponseDto { Success = false, Error = "No matching passkey for this site" };

                var pkcs8 = Convert.FromBase64String(secret.PrivateKeyPkcs8);
                try
                {
                    var newCount = match.SignatureCounter + 1;
                    var (authData, signature) = WebAuthnSoftwareAuthenticator.SignAssertion(
                        request.RpId, pkcs8, WebAuthnSoftwareAuthenticator.Base64UrlDecode(request.ClientDataJSON), newCount);

                    match.SignatureCounter = newCount;
                    match.LastUsedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    return new VaultPasskeyAssertResponseDto
                    {
                        Success = true,
                        CredentialId = WebAuthnSoftwareAuthenticator.Base64Url(Convert.FromBase64String(match.CredentialId)),
                        AuthenticatorData = Convert.ToBase64String(authData),
                        Signature = Convert.ToBase64String(signature),
                        UserHandle = secret.UserHandle
                    };
                }
                finally
                {
                    Array.Clear(pkcs8, 0, pkcs8.Length);
                }
            }
            finally
            {
                Array.Clear(masterKey, 0, masterKey.Length);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error asserting vault passkey for {UserId}", userId);
            return new VaultPasskeyAssertResponseDto { Success = false, Error = "Passkey authentication failed" };
        }
    }

    private static IEnumerable<UserPasskey> OrderCandidates(List<UserPasskey> candidates, bool hasAllowList) =>
        hasAllowList ? candidates : candidates.OrderByDescending(p => p.LastUsedAt ?? p.CreatedAt);

    private static string NormalizeCredId(string credentialId)
    {
        // DB stores standard base64; normalize to standard base64 string of the raw bytes.
        try { return Convert.ToBase64String(Convert.FromBase64String(credentialId)); }
        catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", logEx); return credentialId; }
    }

    private static byte[]? SafeBase64UrlDecode(string s)
    {
        try { return WebAuthnSoftwareAuthenticator.Base64UrlDecode(s); }
        catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", logEx); return null; }
    }

    private VaultPasskeySecret? TryDecryptSecret(UserPasskey passkey, byte[] masterKey)
    {
        if (string.IsNullOrEmpty(passkey.EncryptedVaultData)) return null;
        try
        {
            var enc = JsonSerializer.Deserialize<EncryptedPasswordData>(passkey.EncryptedVaultData);
            if (enc == null) return null;
            var json = _passwordCryptoService.DecryptPasswordWithKey(enc, masterKey);
            return JsonSerializer.Deserialize<VaultPasskeySecret>(json);
        }
        catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", logEx); return null; }
    }

    private sealed class VaultPasskeySecret
    {
        public string RpId { get; set; } = string.Empty;
        public string UserHandle { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string PrivateKeyPkcs8 { get; set; } = string.Empty;
    }

    #region Private Methods

    // Fido2 callback: the new credential id must not already be registered to any user.
    private async Task<bool> IsCredentialIdUniqueToUserAsync(IsCredentialIdUniqueToUserParams args, CancellationToken cancellationToken)
    {
        var credentialId = Convert.ToBase64String(args.CredentialId);
        return !await _context.UserPasskeys.AnyAsync(p => p.CredentialId == credentialId, cancellationToken);
    }

    // Fido2 callback: the asserting user handle must actually own the credential. Registration
    // sets the WebAuthn user.Id to UTF-8 bytes of the VaultGuard user id, so decode and match.
    private async Task<bool> IsUserHandleOwnerOfCredentialIdAsync(IsUserHandleOwnerOfCredentialIdParams args, CancellationToken cancellationToken)
    {
        var credentialId = Convert.ToBase64String(args.CredentialId);

        // For non-resident credentials the authenticator may omit the user handle. Ownership is
        // still established because the caller already located the passkey by credential id, so
        // confirm that an active passkey with this credential id exists.
        if (args.UserHandle == null || args.UserHandle.Length == 0)
        {
            return await _context.UserPasskeys.AnyAsync(
                p => p.CredentialId == credentialId && p.IsActive, cancellationToken);
        }

        var userId = Encoding.UTF8.GetString(args.UserHandle);
        return await _context.UserPasskeys.AnyAsync(
            p => p.CredentialId == credentialId && p.UserId == userId && p.IsActive, cancellationToken);
    }

    private async Task<bool> VerifyMasterPasswordAsync(ApplicationUser user, string masterPassword)
    {
        if (string.IsNullOrEmpty(user.MasterPasswordHash) || string.IsNullOrEmpty(user.UserSalt))
        {
            return false;
        }

        var userSalt = Convert.FromBase64String(user.UserSalt);
        return _passwordCryptoService.VerifyMasterPassword(masterPassword, user.MasterPasswordHash, userSalt, user.MasterPasswordIterations);
    }

    private async Task<string> EncryptForVaultAsync(string userId, string data)
    {
        // This would use your existing vault encryption service
        // For now, returning a placeholder
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
    }

    private async Task<string> DecryptFromVaultAsync(string userId, string encryptedData)
    {
        // This would use your existing vault decryption service
        // For now, returning a placeholder
        return Encoding.UTF8.GetString(Convert.FromBase64String(encryptedData));
    }

    #endregion
}