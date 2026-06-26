using VaultGuard.Services.Interfaces;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace VaultGuard.WPF.Services;

public class WpfSecureStorageService : ISecureStorageService
{
    private readonly string _applicationName = "VaultGuardWPF";

    public async Task<string?> GetAsync(string key)
    {
        try
        {
            // Use Windows Data Protection API (DPAPI) for secure storage
            var filePath = GetSecureFilePath(key);
            if (!File.Exists(filePath))
            {
                return null;
            }

            var encryptedData = await File.ReadAllBytesAsync(filePath);
            var decryptedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
            var result = Encoding.UTF8.GetString(decryptedData);
            
            return result;
        }
        catch (Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", logEx); return null; }
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
        catch (Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", logEx); }
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
        catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", logEx); return Task.FromResult(false); }
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
        catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", logEx); return false; }
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
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Error($"Failed to remove all secure storage", ex);
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