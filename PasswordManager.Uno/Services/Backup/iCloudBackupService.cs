#if __IOS__
using Foundation;
using UIKit;

namespace PasswordManager.Uno.Services.Backup;

/// <summary>
/// iCloud backup service for iOS
/// </summary>
public class iCloudBackupService
{
    private readonly ILogger _logger;
    private const string BackupFileName = "passwordmanager_backup.db";
    private const string AutoBackupKey = "iCloudAutoBackup";

    public iCloudBackupService(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<bool> IsBackupAvailableAsync()
    {
        try
        {
            // Check if iCloud is available
            var ubiquityIdentityToken = NSFileManager.DefaultManager.UbiquityIdentityToken;
            return ubiquityIdentityToken != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking iCloud availability");
            return false;
        }
    }

    public async Task<bool> IsSignedInAsync()
    {
        return await IsBackupAvailableAsync();
    }

    public async Task<BackupResult> BackupAsync(string databasePath)
    {
        var result = new BackupResult();

        try
        {
            if (!await IsBackupAvailableAsync())
            {
                result.ErrorMessage = "iCloud is not available";
                return result;
            }

            // Get iCloud container URL
            var ubiquityUrl = NSFileManager.DefaultManager.GetUrlForUbiquityContainer(null);
            if (ubiquityUrl == null)
            {
                result.ErrorMessage = "Could not access iCloud container";
                return result;
            }

            // Create Documents directory in iCloud if it doesn't exist
            var documentsUrl = ubiquityUrl.Append("Documents", true);
            NSFileManager.DefaultManager.CreateDirectory(documentsUrl, true, null, out var error);

            // Copy database to iCloud
            var backupUrl = documentsUrl.Append(BackupFileName, false);
            var sourcePath = databasePath;

            if (NSFileManager.DefaultManager.FileExists(backupUrl.Path))
            {
                NSFileManager.DefaultManager.Remove(backupUrl, out _);
            }

            var copied = NSFileManager.DefaultManager.Copy(sourcePath, backupUrl.Path, out error);

            if (copied)
            {
                result.Success = true;
                result.BackupDate = DateTime.UtcNow;
                
                var fileInfo = new FileInfo(sourcePath);
                result.BackupSize = fileInfo.Length;

                // Store last backup date
                Biometric.Preferences.Set("LastiCloudBackup", DateTime.UtcNow.ToString("o"));

                _logger.LogInformation("iCloud backup successful");
            }
            else
            {
                result.ErrorMessage = $"Backup failed: {error?.LocalizedDescription}";
                _logger.LogError("iCloud backup failed: {Error}", error?.LocalizedDescription);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during iCloud backup");
            result.ErrorMessage = $"Backup error: {ex.Message}";
        }

        return result;
    }

    public async Task<BackupResult> RestoreAsync(string databasePath)
    {
        var result = new BackupResult();

        try
        {
            if (!await IsBackupAvailableAsync())
            {
                result.ErrorMessage = "iCloud is not available";
                return result;
            }

            // Get iCloud container URL
            var ubiquityUrl = NSFileManager.DefaultManager.GetUrlForUbiquityContainer(null);
            if (ubiquityUrl == null)
            {
                result.ErrorMessage = "Could not access iCloud container";
                return result;
            }

            var documentsUrl = ubiquityUrl.Append("Documents", true);
            var backupUrl = documentsUrl.Append(BackupFileName, false);

            if (!NSFileManager.DefaultManager.FileExists(backupUrl.Path))
            {
                result.ErrorMessage = "No backup found in iCloud";
                return result;
            }

            // Copy from iCloud to local database
            if (NSFileManager.DefaultManager.FileExists(databasePath))
            {
                NSFileManager.DefaultManager.Remove(databasePath, out _);
            }

            var copied = NSFileManager.DefaultManager.Copy(backupUrl.Path, databasePath, out var error);

            if (copied)
            {
                result.Success = true;
                result.BackupDate = DateTime.UtcNow;
                _logger.LogInformation("iCloud restore successful");
            }
            else
            {
                result.ErrorMessage = $"Restore failed: {error?.LocalizedDescription}";
                _logger.LogError("iCloud restore failed: {Error}", error?.LocalizedDescription);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during iCloud restore");
            result.ErrorMessage = $"Restore error: {ex.Message}";
        }

        return result;
    }

    public async Task<DateTime?> GetLastBackupDateAsync()
    {
        try
        {
            var lastBackupString = Biometric.Preferences.Get("LastiCloudBackup", string.Empty);
            if (!string.IsNullOrEmpty(lastBackupString))
            {
                return DateTime.Parse(lastBackupString);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting last backup date");
        }

        return null;
    }

    public async Task EnableAutoBackupAsync()
    {
        Biometric.Preferences.Set(AutoBackupKey, true);
        await Task.CompletedTask;
    }

    public async Task DisableAutoBackupAsync()
    {
        Biometric.Preferences.Set(AutoBackupKey, false);
        await Task.CompletedTask;
    }

    public bool IsAutoBackupEnabled()
    {
        return Biometric.Preferences.Get(AutoBackupKey, false);
    }
}
#endif
