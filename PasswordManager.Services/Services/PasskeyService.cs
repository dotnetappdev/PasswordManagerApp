using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PasswordManager.Crypto.Interfaces;
using PasswordManager.DAL.Interfaces;
using PasswordManager.Models;
using PasswordManager.Models.DTOs.Auth;
using PasswordManager.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Fido2NetLib;
using Fido2NetLib.Objects;

namespace PasswordManager.Services.Services;

/// <summary>
/// Service for managing WebAuthn Passkey operations
/// </summary>
public class PasskeyService : IPasskeyService
{
    private readonly IPasswordManagerDbContext _context;
    private readonly IPasswordCryptoService _passwordCryptoService;
    private readonly IPasswordEncryptionService _passwordEncryptionService;
    private readonly ILogger<PasskeyService> _logger;
    private readonly IFido2 _fido2;
    private readonly Dictionary<string, (string Challenge, string UserId, DateTime Expiry)> _challenges;

    public PasskeyService(
        IPasswordManagerDbContext context,
        IPasswordCryptoService passwordCryptoService,
        IPasswordEncryptionService passwordEncryptionService,
        ILogger<PasskeyService> logger,
        IFido2 fido2)
    {
        _context = context;
        _passwordCryptoService = passwordCryptoService;
        _passwordEncryptionService = passwordEncryptionService;
        _logger = logger;
        _fido2 = fido2;
        _challenges = new Dictionary<string, (string, string, DateTime)>();
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
                CredentialCreationOptions = JsonSerializer.Serialize(options)
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

            // Verify attestation
            var options = new CredentialCreateOptions
            {
                Challenge = Convert.FromBase64String(completeDto.Challenge),
                // Add other required options based on your setup
            };

            var result = await _fido2.MakeNewCredentialAsync(credentialResponse, options, null);
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
                CredentialRequestOptions = JsonSerializer.Serialize(options)
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

            // Verify assertion
            var options = new AssertionOptions
            {
                Challenge = Convert.FromBase64String(completeDto.Challenge),
                // Add other required options
            };

            var result = await _fido2.MakeAssertionAsync(credentialResponse, options, Convert.FromBase64String(passkey.PublicKey), passkey.SignatureCounter, null);
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

            // Generate JWT token (assuming you have a JWT service)
            // This would need to be implemented based on your existing JWT service
            
            _logger.LogInformation("Passkey authentication successful for user: {UserId}", passkey.UserId);
            
            // Return a basic response - you'll need to implement JWT generation
            return new AuthResponseDto
            {
                Token = "jwt_token_here", // Replace with actual JWT generation
                RefreshToken = "refresh_token_here",
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                User = new UserDto
                {
                    Id = passkey.User.Id,
                    Email = passkey.User.Email ?? "",
                    FirstName = passkey.User.FirstName,
                    LastName = passkey.User.LastName,
                    CreatedAt = passkey.User.CreatedAt,
                    LastLoginAt = passkey.User.LastLoginAt,
                    IsActive = passkey.User.IsActive
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing passkey authentication");
            return null;
        }
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
        // This would implement the WebAuthn assertion verification
        // For now, returning a basic implementation
        return true;
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
                _challenges.Remove(expired.Key);
            }
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> VerifyAndRemoveChallengeAsync(string challenge, string userId)
    {
        try
        {
            if (_challenges.TryGetValue(challenge, out var storedChallenge))
            {
                if (storedChallenge.UserId == userId && storedChallenge.Expiry > DateTime.UtcNow)
                {
                    _challenges.Remove(challenge);
                    return true;
                }
                else
                {
                    _challenges.Remove(challenge); // Remove expired or invalid challenge
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    #region Private Methods

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