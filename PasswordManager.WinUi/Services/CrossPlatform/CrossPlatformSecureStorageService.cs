using PasswordManager.Services.Interfaces;

namespace PasswordManager.WinUi.Services.CrossPlatform;

/// <summary>
/// Cross-platform implementation of ISecureStorageService for non-Windows environments.
/// Note: This is a simplified implementation for cross-platform builds.
/// </summary>
public class CrossPlatformSecureStorageService : ISecureStorageService
{
    private readonly string _storageDirectory;

    public CrossPlatformSecureStorageService()
    {
        var platformService = new CrossPlatformService();
        _storageDirectory = Path.Combine(platformService.GetAppDataDirectory(), "secure");
        
        if (!Directory.Exists(_storageDirectory))
        {
            Directory.CreateDirectory(_storageDirectory);
        }
    }

    public Task<string?> GetAsync(string key)
    {
        try
        {
            var filePath = Path.Combine(_storageDirectory, $"{key}.dat");
            if (File.Exists(filePath))
            {
                var content = File.ReadAllText(filePath);
                return Task.FromResult<string?>(content);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading secure storage: {ex.Message}");
        }
        return Task.FromResult<string?>(null);
    }

    public Task SetAsync(string key, string value)
    {
        try
        {
            var filePath = Path.Combine(_storageDirectory, $"{key}.dat");
            File.WriteAllText(filePath, value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to secure storage: {ex.Message}");
        }
        return Task.CompletedTask;
    }

    public Task<bool> RemoveAsync(string key)
    {
        try
        {
            var filePath = Path.Combine(_storageDirectory, $"{key}.dat");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return Task.FromResult(true);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error removing from secure storage: {ex.Message}");
        }
        return Task.FromResult(false);
    }

    public bool Remove(string key)
    {
        try
        {
            var filePath = Path.Combine(_storageDirectory, $"{key}.dat");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error removing from secure storage: {ex.Message}");
        }
        return false;
    }

    public Task RemoveAllAsync()
    {
        try
        {
            if (Directory.Exists(_storageDirectory))
            {
                Directory.Delete(_storageDirectory, true);
                Directory.CreateDirectory(_storageDirectory);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clearing secure storage: {ex.Message}");
        }
        return Task.CompletedTask;
    }

    public void RemoveAll()
    {
        try
        {
            if (Directory.Exists(_storageDirectory))
            {
                Directory.Delete(_storageDirectory, true);
                Directory.CreateDirectory(_storageDirectory);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clearing secure storage: {ex.Message}");
        }
    }
}
