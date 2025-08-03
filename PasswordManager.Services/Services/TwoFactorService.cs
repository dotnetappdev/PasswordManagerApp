using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OtpNet;
using PasswordManager.Crypto.Interfaces;
using PasswordManager.DAL.Interfaces;
using PasswordManager.Models;
using PasswordManager.Models.DTOs.Auth;
using PasswordManager.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace PasswordManager.Services.Services;

/// <summary>
/// Service for managing Two-Factor Authentication (2FA) operations
/// </summary>
public class TwoFactorService : ITwoFactorService
{
    private readonly IPasswordManagerDbContext _context;
    private readonly IPasswordCryptoService _passwordCryptoService;
    private readonly ILogger<TwoFactorService> _logger;
    private const string DefaultIssuer = "PasswordManager";
    private const int BackupCodeLength = 8;
    private const int DefaultBackupCodeCount = 10;

    public TwoFactorService(
        IPasswordManagerDbContext context,
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

            // Store the secret key temporarily (will be permanent after verification)
            user.TwoFactorSecretKey = secretKey;
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

            // Enable 2FA
            user.TwoFactorEnabled = true;
            user.TwoFactorEnabledAt = DateTime.UtcNow;
            
            // Clear any existing backup codes and create new ones
            var existingCodes = await _context.UserTwoFactorBackupCodes
                .Where(c => c.UserId == userId)
                .ToListAsync();
            _context.UserTwoFactorBackupCodes.RemoveRange(existingCodes);

            // Create new backup codes
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
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.UserTwoFactorBackupCodes.AddRangeAsync(backupCodeEntities);
            user.TwoFactorBackupCodesRemaining = backupCodes.Count;

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
                return ValidateTotpCode(user.TwoFactorSecretKey, code);
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

            // Generate new backup codes
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
                    CreatedAt = DateTime.UtcNow
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
                if (expectedCode == code)
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

    public string GenerateQrCodeUri(string userEmail, string secretKey, string issuer = "PasswordManager")
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
        var backupCodes = await _context.UserTwoFactorBackupCodes
            .Where(c => c.UserId == userId && !c.IsUsed)
            .ToListAsync();

        foreach (var backupCode in backupCodes)
        {
            var salt = Convert.FromBase64String(backupCode.CodeSalt);
            var hash = HashBackupCode(code, salt);
            var storedHash = Convert.FromBase64String(backupCode.CodeHash);

            if (hash.SequenceEqual(storedHash))
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
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var code = new StringBuilder();
        
        for (int i = 0; i < BackupCodeLength; i++)
        {
            code.Append(chars[random.Next(chars.Length)]);
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