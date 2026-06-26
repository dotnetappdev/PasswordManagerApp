using VaultGuard.Services.Interfaces;

namespace VaultGuard.App.Services;

/// <summary>
/// MAUI implementation of secure storage service using Microsoft.Maui.Authentication.SecureStorage
/// </summary>
public class MauiSecureStorageService : ISecureStorageService
{
    public async Task SetAsync(string key, string value)
    {
        await SecureStorage.SetAsync(key, value);
    }

    public async Task<string?> GetAsync(string key)
    {
        try
        {
            return await SecureStorage.GetAsync(key);
        }
        catch (Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", logEx); return null; }
    }

    public bool Remove(string key)
    {
        try
        {
            return SecureStorage.Remove(key);
        }
        catch (Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", logEx); return false; }
    }

    public void RemoveAll()
    {
        try
        {
            SecureStorage.RemoveAll();
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Error($"Failed to remove all secure storage entries", ex);
        }
    }
}