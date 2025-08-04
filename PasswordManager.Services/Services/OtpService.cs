using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PasswordManager.Crypto.Interfaces;
using PasswordManager.DAL.Interfaces;
using PasswordManager.Models;
using PasswordManager.Models.Configuration;
using PasswordManager.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PasswordManager.Services.Services;

/// <summary>
/// Service for managing One-Time Passcodes (OTP)
/// </summary>
public class OtpService : IOtpService
{
    private readonly IPasswordManagerDbContext _context;
    private readonly ISmsService _smsService;
    private readonly SmsConfiguration _config;
    private readonly ICryptographyService _cryptoService;
    private readonly ILogger<OtpService> _logger;

    public OtpService(
        IPasswordManagerDbContext context,
        ISmsService smsService,
        IOptions<SmsConfiguration> config,
        ICryptographyService cryptoService,
        ILogger<OtpService> logger)
    {
        _context = context;
        _smsService = smsService;
        _config = config.Value;
        _cryptoService = cryptoService;
        _logger = logger;
    }

    public async Task<OtpResult> SendOtpAsync(string userId, string phoneNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_config.Enabled)
            {
                _logger.LogWarning("OTP service is disabled");
                return OtpResult.Failure("OTP service is not available");
            }

            // Check rate limiting
            var recentOtps = await _context.OtpCodes
                .Where(o => o.UserId == userId && o.CreatedAt > DateTime.UtcNow.AddHours(-1))
                .CountAsync(cancellationToken);

            if (recentOtps >= _config.MaxSmsPerHour)
            {
                _logger.LogWarning("Rate limit exceeded for user {UserId}", userId);
                return OtpResult.Failure("Too many OTP requests. Please try again later.");
            }

            // Clear any existing pending OTPs for this user
            await ClearPendingOtpAsync(userId, cancellationToken);

            // Generate OTP code
            var code = GenerateOtpCode(_config.CodeLength);
            var codeHash = HashOtpCode(code);
            
            var expiresAt = DateTime.UtcNow.AddMinutes(_config.ExpirationMinutes);

            // Store OTP in database
            var otpRecord = new OtpCode
            {
                UserId = userId,
                CodeHash = codeHash,
                PhoneNumber = phoneNumber,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow
            };

            _context.OtpCodes.Add(otpRecord);
            await _context.SaveChangesAsync(cancellationToken);

            // Send SMS
            var message = _config.MessageTemplate
                .Replace("{code}", code)
                .Replace("{expiration}", _config.ExpirationMinutes.ToString());

            var smsResult = await _smsService.SendSmsAsync(phoneNumber, message, cancellationToken);
            
            if (!smsResult.IsSuccess)
            {
                _logger.LogError("Failed to send OTP SMS to user {UserId}: {Error}", userId, smsResult.ErrorMessage);
                
                // Remove the OTP record if SMS failed
                _context.OtpCodes.Remove(otpRecord);
                await _context.SaveChangesAsync(cancellationToken);
                
                return OtpResult.Failure(smsResult.ErrorMessage ?? "Failed to send verification code");
            }

            _logger.LogInformation("OTP sent successfully to user {UserId}", userId);
            return OtpResult.Success(expiresAt, _config.MaxAttempts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while sending OTP for user {UserId}", userId);
            return OtpResult.Failure("An error occurred while sending verification code");
        }
    }

    public async Task<OtpVerificationResult> VerifyOtpAsync(string userId, string code, bool removeOnSuccess = true, CancellationToken cancellationToken = default)
    {
        try
        {
            var codeHash = HashOtpCode(code);
            
            var otpRecord = await _context.OtpCodes
                .Where(o => o.UserId == userId && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (otpRecord == null)
            {
                _logger.LogWarning("No valid OTP found for user {UserId}", userId);
                return OtpVerificationResult.Invalid("Invalid or expired verification code", 0);
            }

            // Increment attempt count
            otpRecord.AttemptCount++;
            
            // Check if max attempts exceeded
            if (otpRecord.AttemptCount > _config.MaxAttempts)
            {
                _logger.LogWarning("Max OTP attempts exceeded for user {UserId}", userId);
                otpRecord.IsUsed = true; // Mark as used to prevent further attempts
                await _context.SaveChangesAsync(cancellationToken);
                
                var lockoutEndsAt = DateTime.UtcNow.AddMinutes(30); // 30 minute lockout
                return OtpVerificationResult.Invalid("Too many incorrect attempts. Please request a new code.", 0, true, lockoutEndsAt);
            }

            // Verify the code
            if (otpRecord.CodeHash != codeHash)
            {
                await _context.SaveChangesAsync(cancellationToken);
                var attemptsRemaining = _config.MaxAttempts - otpRecord.AttemptCount;
                
                _logger.LogWarning("Invalid OTP attempt for user {UserId}. Attempts remaining: {AttemptsRemaining}", 
                    userId, attemptsRemaining);
                
                return OtpVerificationResult.Invalid("Invalid verification code", attemptsRemaining);
            }

            // Code is valid
            if (removeOnSuccess)
            {
                otpRecord.IsUsed = true;
                otpRecord.UsedAt = DateTime.UtcNow;
            }
            
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("OTP verified successfully for user {UserId}", userId);
            return OtpVerificationResult.Valid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while verifying OTP for user {UserId}", userId);
            return OtpVerificationResult.Invalid("An error occurred while verifying code", 0);
        }
    }

    public List<string> GenerateBackupCodes(int count = 10)
    {
        var codes = new List<string>();
        
        for (int i = 0; i < count; i++)
        {
            var code = GenerateBackupCode();
            codes.Add(code);
        }
        
        return codes;
    }

    public async Task<bool> VerifyBackupCodeAsync(string userId, string backupCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
            if (user?.BackupCodes == null)
            {
                return false;
            }

            // Decrypt and parse backup codes
            var encryptedCodes = user.BackupCodes;
            var masterKey = new byte[32]; // This would need to come from the vault session in a real implementation
            
            // Convert the encrypted JSON string to EncryptedData
            var encryptedData = JsonSerializer.Deserialize<EncryptedData>(encryptedCodes);
            if (encryptedData == null)
            {
                return false;
            }
            
            var decryptedCodes = _cryptoService.DecryptAes256Gcm(encryptedData, masterKey);
            
            var backupCodes = JsonSerializer.Deserialize<List<string>>(decryptedCodes);
            if (backupCodes == null || !backupCodes.Contains(backupCode))
            {
                return false;
            }

            // Remove the used backup code
            backupCodes.Remove(backupCode);
            user.BackupCodesUsed++;
            
            // Re-encrypt and save
            var updatedCodesJson = JsonSerializer.Serialize(backupCodes);
            var encryptedUpdatedCodes = _cryptoService.EncryptAes256Gcm(updatedCodesJson, masterKey);
            user.BackupCodes = JsonSerializer.Serialize(encryptedUpdatedCodes);
            
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Backup code used successfully for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while verifying backup code for user {UserId}", userId);
            return false;
        }
    }

    public async Task ClearPendingOtpAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var pendingOtps = await _context.OtpCodes
                .Where(o => o.UserId == userId && !o.IsUsed)
                .ToListAsync(cancellationToken);

            if (pendingOtps.Any())
            {
                _context.OtpCodes.RemoveRange(pendingOtps);
                await _context.SaveChangesAsync(cancellationToken);
                
                _logger.LogDebug("Cleared {Count} pending OTP codes for user {UserId}", pendingOtps.Count, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while clearing pending OTPs for user {UserId}", userId);
        }
    }

    private static string GenerateOtpCode(int length)
    {
        var code = new StringBuilder();
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        
        for (int i = 0; i < length; i++)
        {
            rng.GetBytes(bytes);
            var value = BitConverter.ToUInt32(bytes, 0);
            code.Append((value % 10).ToString());
        }
        
        return code.ToString();
    }

    private static string GenerateBackupCode()
    {
        var code = new StringBuilder();
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        
        // Generate 8-digit backup code
        for (int i = 0; i < 8; i++)
        {
            rng.GetBytes(bytes);
            var value = BitConverter.ToUInt32(bytes, 0);
            code.Append((value % 10).ToString());
        }
        
        return code.ToString();
    }

    private static string HashOtpCode(string code)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(code);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}