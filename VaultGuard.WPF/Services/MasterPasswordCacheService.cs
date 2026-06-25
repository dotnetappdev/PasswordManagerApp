using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace VaultGuard.WPF.Services;

/// <summary>
/// Caches a user's master password locally, encrypted with the Windows Data Protection API
/// (DPAPI), scoped to the current Windows user. This lets a 2FA-enabled profile unlock the
/// vault after verifying a TOTP/backup code without retyping the master password, while still
/// supplying that password to the existing AuthenticateAsync/AuthenticateSpecificUserAsync call
/// paths unchanged.
///
/// Modeled directly on WpfSecureStorageService's DPAPI pattern (ProtectedData.Protect/Unprotect,
/// DataProtectionScope.CurrentUser) used for OneDrive token caching.
/// </summary>
public interface IMasterPasswordCacheService
{
    Task<string?> GetCachedMasterPasswordAsync(string userId);
    Task CacheMasterPasswordAsync(string userId, string masterPassword);
    Task ForgetDeviceAsync(string userId);
    Task<bool> HasCachedMasterPasswordAsync(string userId);
}

public class MasterPasswordCacheService : IMasterPasswordCacheService
{
    private const string ApplicationName = "VaultGuardWPF";

    public Task<string?> GetCachedMasterPasswordAsync(string userId)
    {
        try
        {
            var filePath = GetFilePath(userId);
            if (!File.Exists(filePath))
            {
                return Task.FromResult<string?>(null);
            }

            var encryptedData = File.ReadAllBytes(filePath);
            var decryptedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
            return Task.FromResult<string?>(Encoding.UTF8.GetString(decryptedData));
        }
        catch
        {
            return Task.FromResult<string?>(null);
        }
    }

    public Task CacheMasterPasswordAsync(string userId, string masterPassword)
    {
        try
        {
            var dataToEncrypt = Encoding.UTF8.GetBytes(masterPassword);
            var encryptedData = ProtectedData.Protect(dataToEncrypt, null, DataProtectionScope.CurrentUser);

            var filePath = GetFilePath(userId);
            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }

            File.WriteAllBytes(filePath, encryptedData);
        }
        catch
        {
            // Caching failures shouldn't crash the app - just fall back to manual entry next time.
        }

        return Task.CompletedTask;
    }

    public Task ForgetDeviceAsync(string userId)
    {
        try
        {
            var filePath = GetFilePath(userId);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // Best-effort.
        }

        return Task.CompletedTask;
    }

    public Task<bool> HasCachedMasterPasswordAsync(string userId)
    {
        try
        {
            return Task.FromResult(File.Exists(GetFilePath(userId)));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private static string GetFilePath(string userId)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var directory = Path.Combine(localAppData, ApplicationName, "MasterPasswordCache");
        var fileName = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(userId)));
        return Path.Combine(directory, fileName);
    }
}
