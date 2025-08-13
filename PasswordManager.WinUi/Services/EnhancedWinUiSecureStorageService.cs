using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PasswordManager.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace PasswordManager.WinUi.Services;

/// <summary>
/// Enhanced secure storage service that uses Windows Credential Manager for highly sensitive data
/// like master key material, and DPAPI for less sensitive data like user preferences.
/// This follows 1Password-like security practices by using the most appropriate storage for each type of data.
/// </summary>
public class EnhancedWinUiSecureStorageService : ISecureStorageService
{
    private readonly WindowsCredentialManagerService _credentialManagerService;
    private readonly WinUiSecureStorageService _dpapiStorageService;
    private readonly ILogger<EnhancedWinUiSecureStorageService> _logger;

    // Define which keys should use the more secure Credential Manager vs DPAPI
    private readonly HashSet<string> _highSecurityKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "userSalt_",      // User salt for master key derivation
        "masterKey_",     // Any master key material (if ever stored - should be avoided)
        "vaultKey_",      // Vault encryption keys
        "deviceKey_",     // Device-specific encryption keys
        "apiSessionToken" // API session tokens
    };

    public EnhancedWinUiSecureStorageService(
        WindowsCredentialManagerService credentialManagerService,
        WinUiSecureStorageService dpapiStorageService,
        ILogger<EnhancedWinUiSecureStorageService> logger)
    {
        _credentialManagerService = credentialManagerService;
        _dpapiStorageService = dpapiStorageService;
        _logger = logger;
    }

    public async Task<string?> GetAsync(string key)
    {
        try
        {
            if (IsHighSecurityKey(key))
            {
                _logger.LogDebug("Retrieving high-security key from Windows Credential Manager: {Key}", key);
                return await _credentialManagerService.GetAsync(key);
            }
            else
            {
                _logger.LogDebug("Retrieving standard key from DPAPI storage: {Key}", key);
                return await _dpapiStorageService.GetAsync(key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve key from secure storage: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync(string key, string value)
    {
        try
        {
            if (IsHighSecurityKey(key))
            {
                _logger.LogDebug("Storing high-security key in Windows Credential Manager: {Key}", key);
                await _credentialManagerService.SetAsync(key, value);
            }
            else
            {
                _logger.LogDebug("Storing standard key in DPAPI storage: {Key}", key);
                await _dpapiStorageService.SetAsync(key, value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store key in secure storage: {Key}", key);
            throw;
        }
    }

    public bool Remove(string key)
    {
        try
        {
            if (IsHighSecurityKey(key))
            {
                _logger.LogDebug("Removing high-security key from Windows Credential Manager: {Key}", key);
                return _credentialManagerService.Remove(key);
            }
            else
            {
                _logger.LogDebug("Removing standard key from DPAPI storage: {Key}", key);
                return _dpapiStorageService.Remove(key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove key from secure storage: {Key}", key);
            return false;
        }
    }

    public void RemoveAll()
    {
        try
        {
            _logger.LogWarning("Removing all secure storage data");
            _credentialManagerService.RemoveAll();
            _dpapiStorageService.RemoveAll();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove all secure storage data");
        }
    }

    public async Task<bool> RemoveAsync(string key)
    {
        return await Task.Run(() => Remove(key));
    }

    public async Task RemoveAllAsync()
    {
        await Task.Run(() => RemoveAll());
    }

    /// <summary>
    /// Determines if a key should use high-security storage (Windows Credential Manager)
    /// </summary>
    private bool IsHighSecurityKey(string key)
    {
        return _highSecurityKeys.Any(prefix => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets storage statistics for monitoring and debugging
    /// </summary>
    public async Task<SecureStorageStats> GetStorageStatsAsync()
    {
        return new SecureStorageStats
        {
            HighSecurityKeysConfigured = _highSecurityKeys.Count,
            CredentialManagerAvailable = await IsCredentialManagerAvailableAsync(),
            DpapiAvailable = await IsDpapiAvailableAsync()
        };
    }

    private async Task<bool> IsCredentialManagerAvailableAsync()
    {
        try
        {
            // Test by trying to store and retrieve a dummy value
            var testKey = "test_credential_manager";
            var testValue = "test_value";
            
            await _credentialManagerService.SetAsync(testKey, testValue);
            var retrieved = await _credentialManagerService.GetAsync(testKey);
            _credentialManagerService.Remove(testKey);
            
            return retrieved == testValue;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> IsDpapiAvailableAsync()
    {
        try
        {
            // Test by trying to store and retrieve a dummy value
            var testKey = "test_dpapi";
            var testValue = "test_value";
            
            await _dpapiStorageService.SetAsync(testKey, testValue);
            var retrieved = await _dpapiStorageService.GetAsync(testKey);
            _dpapiStorageService.Remove(testKey);
            
            return retrieved == testValue;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Statistics about secure storage availability and configuration
/// </summary>
public class SecureStorageStats
{
    public int HighSecurityKeysConfigured { get; set; }
    public bool CredentialManagerAvailable { get; set; }
    public bool DpapiAvailable { get; set; }
}