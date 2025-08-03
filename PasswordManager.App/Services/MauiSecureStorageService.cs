using PasswordManager.Services.Interfaces;

namespace PasswordManager.App.Services;

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
        catch (Exception)
        {
            return null;
        }
    }

    public bool Remove(string key)
    {
        try
        {
            return SecureStorage.Remove(key);
        }
        catch (Exception)
        {
            return false;
        }
    }

    public void RemoveAll()
    {
        try
        {
            SecureStorage.RemoveAll();
        }
        catch (Exception)
        {
            // Ignore errors during cleanup
        }
    }
}