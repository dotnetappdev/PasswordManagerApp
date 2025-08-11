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
                return null;

            var encryptedData = await File.ReadAllBytesAsync(filePath);
            var decryptedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedData);
        }
        catch
        {
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
        }
        catch
        {
            // Log error but don't throw - secure storage failures shouldn't crash the app
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