using System.Security.Cryptography;
using System.Text;

namespace PasswordManager.Uno.Services.Biometric;

/// <summary>
/// Platform-agnostic biometric authentication service
/// Delegates to platform-specific implementations
/// </summary>
public class BiometricAuthService : IBiometricAuthService
{
    private readonly ILogger<BiometricAuthService> _logger;
    private const string BiometricEnabledKey = "BiometricLoginEnabled";
    private const string StoredEmailKey = "BiometricStoredEmail";
    private const string StoredCredentialsKey = "BiometricStoredCredentials";

#if __ANDROID__
    private readonly AndroidBiometricService _platformService;
#elif __IOS__
    private readonly iOSBiometricService _platformService;
#else
    private readonly object? _platformService = null;
#endif

    public BiometricAuthService(ILogger<BiometricAuthService> logger)
    {
        _logger = logger;

#if __ANDROID__
        _platformService = new AndroidBiometricService(logger);
#elif __IOS__
        _platformService = new iOSBiometricService(logger);
#endif
    }

    public async Task<bool> IsBiometricAvailableAsync()
    {
#if __ANDROID__ || __IOS__
        return await _platformService!.IsBiometricAvailableAsync();
#else
        return await Task.FromResult(false);
#endif
    }

    public async Task<string> GetBiometricTypeAsync()
    {
#if __ANDROID__ || __IOS__
        return await _platformService!.GetBiometricTypeAsync();
#else
        return await Task.FromResult("Not Available");
#endif
    }

    public async Task<bool> AuthenticateAsync(string reason)
    {
#if __ANDROID__ || __IOS__
        try
        {
            return await _platformService!.AuthenticateAsync(reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Biometric authentication failed");
            return false;
        }
#else
        return await Task.FromResult(false);
#endif
    }

    public bool IsBiometricLoginEnabled()
    {
        try
        {
            var enabled = Preferences.Get(BiometricEnabledKey, false);
            return enabled;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking biometric login status");
            return false;
        }
    }

    public async Task EnableBiometricLoginAsync(string email, string encryptedCredentials)
    {
        try
        {
            // Verify biometric authentication before enabling
            var authenticated = await AuthenticateAsync("Enable biometric login");
            if (!authenticated)
            {
                throw new UnauthorizedAccessException("Biometric authentication failed");
            }

            // Store credentials securely
            Preferences.Set(BiometricEnabledKey, true);
            await SecureStorage.SetAsync(StoredEmailKey, email);
            await SecureStorage.SetAsync(StoredCredentialsKey, encryptedCredentials);

            _logger.LogInformation("Biometric login enabled for user");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling biometric login");
            throw;
        }
    }

    public async Task DisableBiometricLoginAsync()
    {
        try
        {
            Preferences.Set(BiometricEnabledKey, false);
            SecureStorage.Remove(StoredEmailKey);
            SecureStorage.Remove(StoredCredentialsKey);

            _logger.LogInformation("Biometric login disabled");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling biometric login");
            throw;
        }
    }

    public async Task<(string email, string encryptedCredentials)?> GetStoredCredentialsAsync()
    {
        try
        {
            if (!IsBiometricLoginEnabled())
            {
                return null;
            }

            // Require biometric authentication to retrieve credentials
            var authenticated = await AuthenticateAsync("Sign in with biometrics");
            if (!authenticated)
            {
                return null;
            }

            var email = await SecureStorage.GetAsync(StoredEmailKey);
            var credentials = await SecureStorage.GetAsync(StoredCredentialsKey);

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(credentials))
            {
                return null;
            }

            return (email, credentials);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving stored credentials");
            return null;
        }
    }
}

// Helper class for preferences (stub - will use platform APIs)
public static class Preferences
{
    private static readonly Dictionary<string, object> _storage = new();

    public static T Get<T>(string key, T defaultValue)
    {
        if (_storage.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }

    public static void Set(string key, object value)
    {
        _storage[key] = value;
    }
}

// Helper class for secure storage (stub - will use platform APIs)
public static class SecureStorage
{
    private static readonly Dictionary<string, string> _secureStorage = new();

    public static Task SetAsync(string key, string value)
    {
        _secureStorage[key] = value;
        return Task.CompletedTask;
    }

    public static Task<string?> GetAsync(string key)
    {
        _secureStorage.TryGetValue(key, out var value);
        return Task.FromResult(value);
    }

    public static void Remove(string key)
    {
        _secureStorage.Remove(key);
    }
}
