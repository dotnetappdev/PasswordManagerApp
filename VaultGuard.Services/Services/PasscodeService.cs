using PasswordManager.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace PasswordManager.Services.Services;

/// <summary>
/// Implementation of passcode service for MAUI apps
/// </summary>
public class PasscodeService : IPasscodeService
{
    private readonly ISecureStorageService _secureStorage;
    
    private const string PASSCODE_HASH_KEY = "app_passcode_hash";
    private const string PASSCODE_SALT_KEY = "app_passcode_salt";
    private const string FAILED_ATTEMPTS_KEY = "passcode_failed_attempts";
    private const string LOCKOUT_TIME_KEY = "passcode_lockout_time";
    private const int MAX_FAILED_ATTEMPTS = 5;
    private const int LOCKOUT_DURATION_MINUTES = 15;
    private const int PBKDF2_ITERATIONS = 100000;

    public PasscodeService(ISecureStorageService secureStorage)
    {
        _secureStorage = secureStorage;
    }

    public async Task<bool> IsPasscodeSetAsync()
    {
        try
        {
            var hash = await _secureStorage.GetAsync(PASSCODE_HASH_KEY);
            return !string.IsNullOrEmpty(hash);
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> SetPasscodeAsync(string passcode)
    {
        if (string.IsNullOrEmpty(passcode) || passcode.Length != 8 || !IsNumeric(passcode))
        {
            return false;
        }

        try
        {
            // Generate a random salt
            var salt = GenerateSalt();
            var hash = HashPasscode(passcode, salt);

            await _secureStorage.SetAsync(PASSCODE_HASH_KEY, hash);
            await _secureStorage.SetAsync(PASSCODE_SALT_KEY, Convert.ToBase64String(salt));
            
            // Reset failed attempts when setting new passcode
            await ResetFailedAttemptsAsync();
            
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> VerifyPasscodeAsync(string passcode)
    {
        if (string.IsNullOrEmpty(passcode) || passcode.Length != 8 || !IsNumeric(passcode))
        {
            return false;
        }

        // Check if locked out
        if (await IsPasscodeLockedAsync())
        {
            return false;
        }

        try
        {
            var storedHash = await _secureStorage.GetAsync(PASSCODE_HASH_KEY);
            var storedSaltBase64 = await _secureStorage.GetAsync(PASSCODE_SALT_KEY);

            if (string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(storedSaltBase64))
            {
                return false;
            }

            var salt = Convert.FromBase64String(storedSaltBase64);
            var computedHash = HashPasscode(passcode, salt);

            var isValid = storedHash == computedHash;
            
            if (isValid)
            {
                await ResetFailedAttemptsAsync();
            }
            else
            {
                await IncrementFailedAttemptsAsync();
            }

            return isValid;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> RemovePasscodeAsync()
    {
        try
        {
            _secureStorage.Remove(PASSCODE_HASH_KEY);
            _secureStorage.Remove(PASSCODE_SALT_KEY);
            await ResetFailedAttemptsAsync();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> ChangePasscodeAsync(string currentPasscode, string newPasscode)
    {
        // Verify current passcode first
        if (!await VerifyPasscodeAsync(currentPasscode))
        {
            return false;
        }

        return await SetPasscodeAsync(newPasscode);
    }

    public async Task<int> GetFailedAttemptsAsync()
    {
        try
        {
            var attempts = await _secureStorage.GetAsync(FAILED_ATTEMPTS_KEY);
            return int.TryParse(attempts, out var count) ? count : 0;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    public async Task IncrementFailedAttemptsAsync()
    {
        try
        {
            var currentAttempts = await GetFailedAttemptsAsync();
            var newAttempts = currentAttempts + 1;
            
            await _secureStorage.SetAsync(FAILED_ATTEMPTS_KEY, newAttempts.ToString());

            // If we've reached max attempts, set lockout time
            if (newAttempts >= MAX_FAILED_ATTEMPTS)
            {
                var lockoutUntil = DateTime.UtcNow.AddMinutes(LOCKOUT_DURATION_MINUTES);
                await _secureStorage.SetAsync(LOCKOUT_TIME_KEY, lockoutUntil.ToBinary().ToString());
            }
        }
        catch (Exception)
        {
            // Ignore errors when incrementing attempts
        }
    }

    public async Task ResetFailedAttemptsAsync()
    {
        try
        {
            _secureStorage.Remove(FAILED_ATTEMPTS_KEY);
            _secureStorage.Remove(LOCKOUT_TIME_KEY);
        }
        catch (Exception)
        {
            // Ignore errors when resetting attempts
        }
    }

    public async Task<bool> IsPasscodeLockedAsync()
    {
        try
        {
            var lockoutTimeString = await _secureStorage.GetAsync(LOCKOUT_TIME_KEY);
            if (string.IsNullOrEmpty(lockoutTimeString))
            {
                return false;
            }

            if (long.TryParse(lockoutTimeString, out var lockoutTimeBinary))
            {
                var lockoutTime = DateTime.FromBinary(lockoutTimeBinary);
                return DateTime.UtcNow < lockoutTime;
            }
        }
        catch (Exception)
        {
            // If we can't determine lockout status, assume not locked
        }

        return false;
    }

    public async Task<int> GetLockoutTimeRemainingAsync()
    {
        try
        {
            var lockoutTimeString = await _secureStorage.GetAsync(LOCKOUT_TIME_KEY);
            if (string.IsNullOrEmpty(lockoutTimeString))
            {
                return 0;
            }

            if (long.TryParse(lockoutTimeString, out var lockoutTimeBinary))
            {
                var lockoutTime = DateTime.FromBinary(lockoutTimeBinary);
                var remaining = (lockoutTime - DateTime.UtcNow).TotalSeconds;
                return remaining > 0 ? (int)remaining : 0;
            }
        }
        catch (Exception)
        {
            // If we can't determine time remaining, assume not locked
        }

        return 0;
    }

    private static byte[] GenerateSalt()
    {
        var salt = new byte[32]; // 256-bit salt
        RandomNumberGenerator.Fill(salt);
        return salt;
    }

    private static string HashPasscode(string passcode, byte[] salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(
            Encoding.UTF8.GetBytes(passcode), 
            salt, 
            PBKDF2_ITERATIONS, 
            HashAlgorithmName.SHA256);
        
        var hash = pbkdf2.GetBytes(32); // 256-bit hash
        return Convert.ToBase64String(hash);
    }

    private static bool IsNumeric(string value)
    {
        return value.All(char.IsDigit);
    }
}