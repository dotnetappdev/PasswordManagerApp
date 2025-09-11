using PasswordManager.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace PasswordManager.WinUi.Services;

public class WinUiSecureStorageService : ISecureStorageService
{
    private readonly string _applicationName = "PasswordManagerWinUI";

    public async Task<string?> GetAsync(string key)
    {
        try
        {
            // Use Windows Data Protection API (DPAPI) for secure storage
            var filePath = GetSecureFilePath(key);
            if (!File.Exists(filePath))
            {
                // Debug logging for missing salt files
                if (key.StartsWith("userSalt_"))
                {
                    System.Diagnostics.Debug.WriteLine($"SecureStorage: Salt file not found for key '{key}' at path '{filePath}'");
                }
                return null;
            }

            var encryptedData = await File.ReadAllBytesAsync(filePath);
            var decryptedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
            var result = Encoding.UTF8.GetString(decryptedData);
            
            // Debug logging for salt retrieval
            if (key.StartsWith("userSalt_"))
            {
                System.Diagnostics.Debug.WriteLine($"SecureStorage: Successfully retrieved salt for key '{key}'");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            // Enhanced error logging
            System.Diagnostics.Debug.WriteLine($"SecureStorage GetAsync error for key '{key}': {ex.Message}");
            return null;
        }
    }

    public async Task SetAsync(string key, string value)
    {
        try
        {
            var dataToEncrypt = Encoding.UTF8.GetBytes(value);
            var encryptedData = ProtectedData.Protect(dataToEncrypt, null, DataProtectionScope.CurrentUser);
            
            var filePath = GetSecureFilePath(key);
            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }

            await File.WriteAllBytesAsync(filePath, encryptedData);
            
            // Debug logging for salt storage
            if (key.StartsWith("userSalt_"))
            {
                System.Diagnostics.Debug.WriteLine($"SecureStorage: Stored salt for key '{key}' at path '{filePath}'");
            }
        }
        catch (Exception ex)
        {
            // Enhanced error logging for debugging
            System.Diagnostics.Debug.WriteLine($"SecureStorage SetAsync error for key '{key}': {ex.Message}");
            // Don't throw - secure storage failures shouldn't crash the app, but we should know about them
        }
    }

    public Task<bool> RemoveAsync(string key)
    {
        try
        {
            var filePath = GetSecureFilePath(key);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task RemoveAllAsync()
    {
        try
        {
            var directory = GetSecureStorageDirectory();
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
            return Task.CompletedTask;
        }
        catch
        {
            // Log error but don't throw
            return Task.CompletedTask;
        }
    }

    public bool Remove(string key)
    {
        try
        {
            var filePath = GetSecureFilePath(key);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public void RemoveAll()
    {
        try
        {
            var directory = GetSecureStorageDirectory();
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
        catch
        {
            // Log error but don't throw
        }
    }

    private string GetSecureFilePath(string key)
    {
        var directory = GetSecureStorageDirectory();
        var fileName = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(key)));
        return Path.Combine(directory, fileName);
    }

    private string GetSecureStorageDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, _applicationName, "SecureStorage");
    }
}