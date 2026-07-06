using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OtpNet;
using VaultGuard.Crypto.Interfaces;
using VaultGuard.DAL.Interfaces;
using VaultGuard.Models;
using VaultGuard.Models.DTOs.Auth;
using VaultGuard.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace VaultGuard.Services.Services;

/// <summary>
/// Service for managing Two-Factor Authentication (2FA) operations
/// </summary>
public class TwoFactorService : ITwoFactorService
{
    private readonly IVaultGuardDbContext _context;
    private readonly IPasswordCryptoService _passwordCryptoService;
    private readonly ILogger<TwoFactorService> _logger;
    private const string DefaultIssuer = "VaultGuard";
    private const int BackupCodeLength = 8;
    private const int DefaultBackupCodeCount = 10;

    public TwoFactorService(
        IVaultGuardDbContext context,
        IPasswordCryptoService passwordCryptoService,
        ILogger<TwoFactorService> logger)
    {
        _context = context;
        _passwordCryptoService = passwordCryptoService;
        _logger = logger;
    }

    public async Task<TwoFactorSetupResponseDto?> StartTwoFactorSetupAsync(string userId, string masterPassword)
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
            if (!await VerifyMasterPasswordAsync(user, masterPassword))
            {
                _logger.LogWarning("Invalid master password for 2FA setup: {UserId}", userId);
                return null;
            }

            // Check if 2FA is already enabled
            if (user.TwoFactorEnabled)
            {
                _logger.LogWarning("2FA already enabled for user: {UserId}", userId);
                return null;
            }

            // Generate new secret key
            var secretKey = GenerateSecretKey();
            var qrCodeUri = GenerateQrCodeUri(user.Email ?? "", secretKey, DefaultIssuer);
            var backupCodes = GenerateBackupCodes(DefaultBackupCodeCount);

            // Store the secret key temporarily (becomes permanent after verification)
            user.TwoFactorSecretKey = secretKey;

            // Persist the SAME backup codes we are about to show the user (hashed), so the codes
            // they save actually work for recovery. They stay unusable until 2FA is enabled on
            // verification. Re-running setup replaces any previous pending codes.
            var pendingCodes = await _context.UserTwoFactorBackupCodes
                .Where(c => c.UserId == userId)
                .ToListAsync();
            _context.UserTwoFactorBackupCodes.RemoveRange(pendingCodes);

            foreach (var code in backupCodes)
            {
                var salt = GenerateRandomBytes(32);
                var hash = HashBackupCode(code, salt);
                await _context.UserTwoFactorBackupCodes.AddAsync(new UserTwoFactorBackupCode
                {
                    UserId = userId,
                    CodeHash = Convert.ToBase64String(hash),
                    CodeSalt = Convert.ToBase64String(salt),
                    CreatedAt = DateTime.UtcNow
                });
            }
            user.TwoFactorBackupCodesRemaining = backupCodes.Count;

            await _context.SaveChangesAsync();

            return new TwoFactorSetupResponseDto
            {
                SecretKey = secretKey,
                QrCodeUri = qrCodeUri,
                BackupCodes = backupCodes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting 2FA setup for user: {UserId}", userId);
            return null;
        }
    }

    public async Task<bool> VerifyAndCompleteTwoFactorSetupAsync(string userId, TwoFactorVerifySetupDto verifyDto)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.TwoFactorSecretKey != verifyDto.SecretKey)
            {
                _logger.LogWarning("Invalid user or secret key for 2FA verification: {UserId}", userId);
                return false;
            }

            // Verify the TOTP code
            if (!ValidateTotpCode(verifyDto.SecretKey, verifyDto.Code))
            {
                _logger.LogWarning("Invalid TOTP code for 2FA setup: {UserId}", userId);
                return false;
            }

            // Enable 2FA. The backup codes were already persisted (hashed) when setup started, so
            // the codes shown to the user are exactly the ones that will work for recovery. We do
            // NOT regenerate them here — doing so previously stored a different set and locked
            // users out of their own recovery codes.
            user.TwoFactorEnabled = true;
            user.TwoFactorEnabledAt = DateTime.UtcNow;

            user.TwoFactorBackupCodesRemaining = await _context.UserTwoFactorBackupCodes
                .CountAsync(c => c.UserId == userId);

            await _context.SaveChangesAsync();

            _logger.LogInformation("2FA enabled successfully for user: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing 2FA setup for user: {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> DisableTwoFactorAsync(string userId, TwoFactorDisableDto disableDto)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.TwoFactorEnabled)
            {
                _logger.LogWarning("User not found or 2FA not enabled: {UserId}", userId);
                return false;
            }

            // Verify master password
            if (!await VerifyMasterPasswordAsync(user, disableDto.MasterPassword))
            {
                _logger.LogWarning("Invalid master password for 2FA disable: {UserId}", userId);
                return false;
            }

            // Verify 2FA code
            if (!await VerifyTwoFactorCodeAsync(userId, disableDto.Code))
            {
                _logger.LogWarning("Invalid 2FA code for disable: {UserId}", userId);
                return false;
            }

            // Disable 2FA
            user.TwoFactorEnabled = false;
            user.TwoFactorSecretKey = null;
            user.TwoFactorEnabledAt = null;
            user.TwoFactorBackupCodesRemaining = 0;

            // Remove all backup codes
            var backupCodes = await _context.UserTwoFactorBackupCodes
                .Where(c => c.UserId == userId)
                .ToListAsync();
            _context.UserTwoFactorBackupCodes.RemoveRange(backupCodes);

            await _context.SaveChangesAsync();

            _logger.LogInformation("2FA disabled successfully for user: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling 2FA for user: {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> VerifyTwoFactorCodeAsync(string userId, string code, bool isBackupCode = false, string? clientIp = null)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecretKey))
            {
                _logger.LogWarning("User not found or 2FA not enabled: {UserId}", userId);
                return false;
            }

            if (isBackupCode)
            {
                return await VerifyBackupCodeAsync(userId, code, clientIp);
            }
            else
            {
                // Accept codes entered with the display mask (e.g. "123-456") or stray spaces —
                // TOTP codes are always digits, so strip everything else before validating.
                var digitsOnly = new string((code ?? string.Empty).Where(char.IsDigit).ToArray());
                return ValidateTotpCode(user.TwoFactorSecretKey, digitsOnly);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying 2FA code for user: {UserId}", userId);
            return false;
        }
    }

    public async Task<List<string>?> RegenerateBackupCodesAsync(string userId, RegenerateBackupCodesDto regenerateDto)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.TwoFactorEnabled)
            {
                _logger.LogWarning("User not found or 2FA not enabled: {UserId}", userId);
                return null;
            }

            // Verify master password
            if (!await VerifyMasterPasswordAsync(user, regenerateDto.MasterPassword))
            {
                _logger.LogWarning("Invalid master password for backup code regeneration: {UserId}", userId);
                return null;
            }

            // Verify 2FA code
            if (!await VerifyTwoFactorCodeAsync(userId, regenerateDto.Code))
            {
                _logger.LogWarning("Invalid 2FA code for backup code regeneration: {UserId}", userId);
                return null;
            }

            // Remove existing backup codes
            var existingCodes = await _context.UserTwoFactorBackupCodes
                .Where(c => c.UserId == userId)
                .ToListAsync();
            _context.UserTwoFactorBackupCodes.RemoveRange(existingCodes);

            // Generate new backup codes, honouring an optional expiry window.
            var expiresAt = regenerateDto.ExpiryDays is > 0
                ? DateTime.UtcNow.AddDays(regenerateDto.ExpiryDays.Value)
                : (DateTime?)null;

            var backupCodes = GenerateBackupCodes(DefaultBackupCodeCount);
            var backupCodeEntities = new List<UserTwoFactorBackupCode>();

            foreach (var code in backupCodes)
            {
                var salt = GenerateRandomBytes(32);
                var hash = HashBackupCode(code, salt);

                backupCodeEntities.Add(new UserTwoFactorBackupCode
                {
                    UserId = userId,
                    CodeHash = Convert.ToBase64String(hash),
                    CodeSalt = Convert.ToBase64String(salt),
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt
                });
            }

            await _context.UserTwoFactorBackupCodes.AddRangeAsync(backupCodeEntities);
            user.TwoFactorBackupCodesRemaining = backupCodes.Count;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Backup codes regenerated for user: {UserId}", userId);
            return backupCodes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating backup codes for user: {UserId}", userId);
            return null;
        }
    }

    public async Task<TwoFactorStatusDto> GetTwoFactorStatusAsync(string userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return new TwoFactorStatusDto();
            }

            return new TwoFactorStatusDto
            {
                IsEnabled = user.TwoFactorEnabled,
                EnabledAt = user.TwoFactorEnabledAt,
                BackupCodesRemaining = user.TwoFactorBackupCodesRemaining,
                RecoveryEmail = user.TwoFactorRecoveryEmail
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting 2FA status for user: {UserId}", userId);
            return new TwoFactorStatusDto();
        }
    }

    public async Task<string?> GetCurrentTotpCodeAsync(string userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecretKey))
                return null;

            var secretKeyBytes = Base32Encoding.ToBytes(user.TwoFactorSecretKey);
            var totp = new Totp(secretKeyBytes);
            return totp.ComputeTotp(DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error computing current TOTP code for user: {UserId}", userId);
            return null;
        }
    }

    public bool ValidateTotpCode(string secretKey, string code, int window = 1)
    {
        try
        {
            var secretKeyBytes = Base32Encoding.ToBytes(secretKey);
            var totp = new Totp(secretKeyBytes);

            // Check current time window and adjacent windows for clock skew tolerance
            for (int i = -window; i <= window; i++)
            {
                var timeStepWithWindow = DateTime.UtcNow.AddSeconds(i * 30);
                var expectedCode = totp.ComputeTotp(timeStepWithWindow);
                // Constant-time comparison to avoid a timing side-channel on the one-time code.
                if (System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                        System.Text.Encoding.UTF8.GetBytes(expectedCode),
                        System.Text.Encoding.UTF8.GetBytes(code ?? string.Empty)))
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating TOTP code");
            return false;
        }
    }

    public string GenerateSecretKey()
    {
        var keyBytes = GenerateRandomBytes(20); // 160-bit key
        return Base32Encoding.ToString(keyBytes);
    }

    public string GenerateQrCodeUri(string userEmail, string secretKey, string issuer = "VaultGuard")
    {
        var encodedIssuer = Uri.EscapeDataString(issuer);
        var encodedEmail = Uri.EscapeDataString(userEmail);
        return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={secretKey}&issuer={encodedIssuer}";
    }

    public List<string> GenerateBackupCodes(int count = 10)
    {
        var codes = new List<string>();
        
        for (int i = 0; i < count; i++)
        {
            codes.Add(GenerateBackupCode());
        }
        
        return codes;
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

    private async Task<bool> VerifyBackupCodeAsync(string userId, string code, string? clientIp)
    {
        var now = DateTime.UtcNow;
        var backupCodes = await _context.UserTwoFactorBackupCodes
            .Where(c => c.UserId == userId && !c.IsUsed && (c.ExpiresAt == null || c.ExpiresAt > now))
            .ToListAsync();

        foreach (var backupCode in backupCodes)
        {
            var salt = Convert.FromBase64String(backupCode.CodeSalt);
            var hash = HashBackupCode(code, salt);
            var storedHash = Convert.FromBase64String(backupCode.CodeHash);

            // Constant-time comparison so response timing can't reveal how many leading hash bytes matched.
            if (System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(hash, storedHash))
            {
                // Mark backup code as used
                backupCode.IsUsed = true;
                backupCode.UsedAt = DateTime.UtcNow;
                backupCode.UsedFromIp = clientIp;

                // Update user's remaining backup codes count
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.TwoFactorBackupCodesRemaining = Math.Max(0, user.TwoFactorBackupCodesRemaining - 1);
                }

                await _context.SaveChangesAsync();
                return true;
            }
        }

        return false;
    }

    private string GenerateBackupCode()
    {
        // Recovery codes must be unguessable, so they are drawn from a cryptographically
        // secure RNG (NOT System.Random, which is predictable). Exclude visually ambiguous
        // characters (0/O, 1/I) so users can transcribe the codes reliably.
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var code = new StringBuilder(BackupCodeLength);

        for (int i = 0; i < BackupCodeLength; i++)
        {
            var index = RandomNumberGenerator.GetInt32(chars.Length);
            code.Append(chars[index]);
        }

        return code.ToString();
    }

    private byte[] GenerateRandomBytes(int length)
    {
        var bytes = new byte[length];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return bytes;
    }

    private byte[] HashBackupCode(string code, byte[] salt)
    {
        using (var pbkdf2 = new Rfc2898DeriveBytes(code, salt, 100000, HashAlgorithmName.SHA256))
        {
            return pbkdf2.GetBytes(32); // 256-bit hash
        }
    }

    #endregion
}