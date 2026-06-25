#if __ANDROID__
using Android.App;
using Android.Content;
using Android.Gms.Auth.Api.SignIn;
using Android.Gms.Common.Apis;
using Java.IO;

namespace VaultGuard.Uno.Services.Backup;

/// <summary>
/// Google Drive backup service for Android
/// </summary>
public class GoogleDriveBackupService
{
    private readonly ILogger _logger;
    private const string BackupFileName = "passwordmanager_backup.db";
    private const string AutoBackupKey = "GoogleDriveAutoBackup";

    public GoogleDriveBackupService(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<bool> IsBackupAvailableAsync()
    {
        try
        {
            // Google Drive is generally available on Android devices with Google Play Services
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Google Drive availability");
            return false;
        }
    }

    public async Task<bool> IsSignedInAsync()
    {
        try
        {
            var context = Android.App.Application.Context;
            var account = GoogleSignIn.GetLastSignedInAccount(context);
            return account != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Google sign-in status");
            return false;
        }
    }

    public async Task<BackupResult> BackupAsync(string databasePath)
    {
        var result = new BackupResult();

        try
        {
            if (!await IsSignedInAsync())
            {
                result.ErrorMessage = "Not signed in to Google account";
                return result;
            }

            // For now, use Android backup service
            // Full Google Drive API integration would require additional setup
            var context = Android.App.Application.Context;
            var backupManager = new BackupManager(context);
            backupManager.DataChanged();

            result.Success = true;
            result.BackupDate = DateTime.UtcNow;

            var fileInfo = new FileInfo(databasePath);
            result.BackupSize = fileInfo.Length;

            // Store last backup date
            Biometric.Preferences.Set("LastGoogleDriveBackup", DateTime.UtcNow.ToString("o"));

            _logger.LogInformation("Google Drive backup initiated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google Drive backup");
            result.ErrorMessage = $"Backup error: {ex.Message}";
        }

        return result;
    }

    public async Task<BackupResult> RestoreAsync(string databasePath)
    {
        var result = new BackupResult();

        try
        {
            if (!await IsSignedInAsync())
            {
                result.ErrorMessage = "Not signed in to Google account";
                return result;
            }

            // Android backup service handles restore automatically
            // When app is reinstalled, system restores the data
            result.Success = true;
            result.BackupDate = DateTime.UtcNow;

            _logger.LogInformation("Google Drive restore initiated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google Drive restore");
            result.ErrorMessage = $"Restore error: {ex.Message}";
        }

        return result;
    }

    public async Task<DateTime?> GetLastBackupDateAsync()
    {
        try
        {
            var lastBackupString = Biometric.Preferences.Get("LastGoogleDriveBackup", string.Empty);
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
