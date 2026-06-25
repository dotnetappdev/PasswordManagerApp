using Microsoft.Extensions.Logging;
using VaultGuard.Services.Interfaces;
using VaultGuard.Models.DTOs;
using VaultGuard.ExceptionReporting;

namespace VaultGuard.Services.Services;

/// <summary>
/// Service for managing cloud backup operations across different providers
/// </summary>
public class CloudBackupManager
{
    private readonly ILogger<CloudBackupManager> _logger;
    private readonly IDatabaseBackupService _databaseBackupService;
    private readonly IOneDriveBackupService _oneDriveService;
    private readonly IiCloudBackupService _iCloudService;
    private readonly INetworkLocationBackupService _networkLocationService;
    private readonly IBackupSettingsService _backupSettingsService;
    private readonly IGoogleDriveBackupService _googleDriveService;
    private readonly IFtpBackupService _ftpService;
    private readonly IExceptionReporter _exceptionReporter;

    public IGoogleDriveBackupService GoogleDrive => _googleDriveService;
    public IFtpBackupService Ftp => _ftpService;

    public CloudBackupManager(
        ILogger<CloudBackupManager> logger,
        IDatabaseBackupService databaseBackupService,
        IOneDriveBackupService oneDriveService,
        IiCloudBackupService iCloudService,
        INetworkLocationBackupService networkLocationService,
        IBackupSettingsService backupSettingsService,
        IGoogleDriveBackupService googleDriveService,
        IFtpBackupService ftpService,
        IExceptionReporter exceptionReporter)
    {
        _logger = logger;
        _databaseBackupService = databaseBackupService;
        _oneDriveService = oneDriveService;
        _iCloudService = iCloudService;
        _networkLocationService = networkLocationService;
        _backupSettingsService = backupSettingsService;
        _googleDriveService = googleDriveService;
        _ftpService = ftpService;
        _exceptionReporter = exceptionReporter;
    }

    /// <summary>
    /// Get all available cloud backup providers
    /// </summary>
    /// <returns>List of available providers</returns>
    public List<CloudProviderInfo> GetAvailableProviders()
    {
        return new List<CloudProviderInfo>
        {
            new CloudProviderInfo
            {
                Provider = CloudBackupProvider.OneDrive,
                DisplayName = _oneDriveService.ServiceName,
                IsAvailable = true,
                MaxBackupSizeMB = _oneDriveService.MaxBackupSizeBytes / (1024 * 1024)
            },
            new CloudProviderInfo
            {
                Provider = CloudBackupProvider.GoogleDrive,
                DisplayName = _googleDriveService.ServiceName,
                IsAvailable = true,
                MaxBackupSizeMB = _googleDriveService.MaxBackupSizeBytes / (1024 * 1024)
            },
            new CloudProviderInfo
            {
                Provider = CloudBackupProvider.iCloud,
                DisplayName = _iCloudService.ServiceName,
                IsAvailable = _iCloudService.IsAvailable,
                MaxBackupSizeMB = _iCloudService.MaxBackupSizeBytes / (1024 * 1024)
            },
            new CloudProviderInfo
            {
                Provider = CloudBackupProvider.NetworkLocation,
                DisplayName = _networkLocationService.ServiceName,
                IsAvailable = true,
                MaxBackupSizeMB = 1000
            },
            new CloudProviderInfo
            {
                Provider = CloudBackupProvider.Ftp,
                DisplayName = _ftpService.ServiceName,
                IsAvailable = true,
                MaxBackupSizeMB = _ftpService.MaxBackupSizeBytes / (1024 * 1024)
            }
        };
    }

    /// <summary>
    /// Create and upload backup to specified cloud provider
    /// </summary>
    /// <param name="provider">Cloud provider to use</param>
    /// <param name="masterPassword">User's master password for encryption</param>
    /// <param name="fileName">Name for the backup file</param>
    /// <param name="description">Optional description</param>
    /// <returns>Result of the backup operation</returns>
    public async Task<CloudBackupResult> CreateAndUploadBackupAsync(
        CloudBackupProvider provider, 
        string masterPassword, 
        string fileName, 
        string? description = null)
    {
        try
        {
            _logger.LogInformation("Creating backup for provider {Provider}", provider);

            // Create the database backup
            var backupResult = await _databaseBackupService.CreateBackupAsync(masterPassword, compress: true);
            if (!backupResult.Success || backupResult.BackupData == null)
            {
                return new CloudBackupResult
                {
                    Success = false,
                    ErrorMessage = backupResult.ErrorMessage ?? "Failed to create backup"
                };
            }

            // Upload to the specified provider
            var cloudService = GetCloudService(provider);
            if (cloudService == null)
            {
                return new CloudBackupResult
                {
                    Success = false,
                    ErrorMessage = $"Provider {provider} is not available"
                };
            }

            // Ensure authentication
            if (!await cloudService.IsAuthenticatedAsync())
            {
                var authResult = await cloudService.AuthenticateAsync();
                if (!authResult)
                {
                    return new CloudBackupResult
                    {
                        Success = false,
                        ErrorMessage = $"Authentication failed for {provider}"
                    };
                }
            }

            // Ensure the filename has the correct extension
            if (!fileName.EndsWith(".pwmbackup"))
            {
                fileName += ".pwmbackup";
            }

            // Upload the backup
            var uploadResult = await cloudService.UploadBackupAsync(backupResult.BackupData, fileName, description);
            
            _logger.LogInformation("Backup upload completed for provider {Provider}. Success: {Success}", 
                provider, uploadResult.Success);

            return uploadResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create and upload backup to {Provider}", provider);
            _exceptionReporter.CaptureException(ex, new Dictionary<string, string> { ["operation"] = "CreateAndUploadBackup" });
            return new CloudBackupResult
            {
                Success = false,
                ErrorMessage = $"Backup failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Download and restore backup from specified cloud provider
    /// </summary>
    /// <param name="provider">Cloud provider to use</param>
    /// <param name="backupId">ID of the backup to restore</param>
    /// <param name="masterPassword">User's master password for decryption</param>
    /// <returns>Result of the restore operation</returns>
    public async Task<bool> DownloadAndRestoreBackupAsync(
        CloudBackupProvider provider, 
        string backupId, 
        string masterPassword)
    {
        try
        {
            _logger.LogInformation("Downloading backup {BackupId} from provider {Provider}", backupId, provider);

            var cloudService = GetCloudService(provider);
            if (cloudService == null)
            {
                _logger.LogError("Provider {Provider} is not available", provider);
                return false;
            }

            // Download the backup
            var downloadResult = await cloudService.DownloadBackupAsync(backupId);
            if (!downloadResult.Success || downloadResult.BackupData == null)
            {
                _logger.LogError("Failed to download backup: {Error}", downloadResult.ErrorMessage);
                return false;
            }

            // Restore the backup
            var restoreResult = await _databaseBackupService.RestoreBackupAsync(
                downloadResult.BackupData, masterPassword);

            _logger.LogInformation("Backup restore completed. Success: {Success}", restoreResult);
            return restoreResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download and restore backup from {Provider}", provider);
            _exceptionReporter.CaptureException(ex, new Dictionary<string, string> { ["operation"] = "DownloadAndRestoreBackup" });
            return false;
        }
    }

    /// <summary>
    /// Download raw (still-encrypted) backup bytes for browse/selective restore.
    /// </summary>
    public async Task<byte[]?> DownloadBackupDataAsync(CloudBackupInfo backup)
    {
        try
        {
            var cloudService = GetCloudService(backup.Provider);
            if (cloudService == null) return null;
            var result = await cloudService.DownloadBackupAsync(backup.Id);
            return result.Success ? result.BackupData : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DownloadBackupDataAsync failed");
            return null;
        }
    }

    /// <summary>
    /// Convenience overload: download + full restore using a CloudBackupInfo.
    /// </summary>
    public async Task<bool> DownloadAndRestoreBackupAsync(CloudBackupInfo backup, string masterPassword)
        => await DownloadAndRestoreBackupAsync(backup.Provider, backup.Id, masterPassword);

    /// <summary>
    /// List backups from all providers
    /// </summary>
    /// <returns>List of available backups from all providers</returns>
    public async Task<List<CloudBackupInfo>> ListAllBackupsAsync()
    {
        var allBackups = new List<CloudBackupInfo>();

        // Get backups from OneDrive
        try
        {
            if (await _oneDriveService.IsAuthenticatedAsync())
            {
                var oneDriveBackups = await _oneDriveService.ListBackupsAsync();
                allBackups.AddRange(oneDriveBackups);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list OneDrive backups");
        }

        // Get backups from iCloud
        try
        {
            if (await _iCloudService.IsAuthenticatedAsync())
            {
                var iCloudBackups = await _iCloudService.ListBackupsAsync();
                allBackups.AddRange(iCloudBackups);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list iCloud backups");
        }

        // Get backups from Google Drive
        try
        {
            if (await _googleDriveService.IsAuthenticatedAsync())
            {
                var googleDriveBackups = await _googleDriveService.ListBackupsAsync();
                allBackups.AddRange(googleDriveBackups);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list Google Drive backups");
        }

        // Get backups from the configured FTP/NAS site
        try
        {
            if (await _ftpService.IsAuthenticatedAsync())
            {
                var ftpBackups = await _ftpService.ListBackupsAsync();
                allBackups.AddRange(ftpBackups);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list FTP backups");
        }

        return allBackups.OrderByDescending(b => b.ModifiedAt).ToList();
    }

    /// <summary>
    /// Delete backup from specified provider
    /// </summary>
    public async Task<bool> DeleteBackupAsync(CloudBackupProvider provider, string backupId)
    {
        try
        {
            var cloudService = GetCloudService(provider);
            if (cloudService == null)
            {
                return false;
            }

            return await cloudService.DeleteBackupAsync(backupId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete backup {BackupId} from {Provider}", backupId, provider);
            return false;
        }
    }

    /// <summary>
    /// Configure network location path for network backup provider
    /// </summary>
    /// <param name="networkPath">UNC path, mapped drive, or network location</param>
    public void SetNetworkPath(string networkPath)
    {
        _networkLocationService.SetNetworkPath(networkPath);
        _logger.LogInformation("Network backup path configured: {Path}", networkPath);
    }

    /// <summary>
    /// Get the configured network path
    /// </summary>
    /// <returns>Network path or empty string if not configured</returns>
    public string GetNetworkPath()
    {
        return _networkLocationService.GetNetworkPath();
    }

    /// <summary>
    /// Validate network location accessibility
    /// </summary>
    /// <returns>True if network location is accessible</returns>
    public async Task<bool> ValidateNetworkLocationAsync()
    {
        return await _networkLocationService.ValidateNetworkLocationAsync();
    }

    /// <summary>
    /// Restore database from backup file with file dialog
    /// </summary>
    /// <param name="backupFilePath">Path to backup file</param>
    /// <param name="masterPassword">Master password for decryption</param>
    /// <returns>True if restore was successful</returns>
    public async Task<bool> RestoreFromFileAsync(string backupFilePath, string masterPassword)
    {
        try
        {
            if (!File.Exists(backupFilePath))
            {
                _logger.LogWarning("Backup file not found: {Path}", backupFilePath);
                return false;
            }

            var backupData = await File.ReadAllBytesAsync(backupFilePath);
            
            // Use database backup service to restore
            var result = await _databaseBackupService.RestoreBackupAsync(backupData, masterPassword);
            
            if (result)
            {
                _logger.LogInformation("Successfully restored database from file: {Path}", backupFilePath);
            }
            else
            {
                _logger.LogWarning("Failed to restore database from file: {Path}", backupFilePath);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring database from file: {Path}", backupFilePath);
            return false;
        }
    }

    private ICloudBackupService? GetCloudService(CloudBackupProvider provider)
    {
        return provider switch
        {
            CloudBackupProvider.OneDrive => _oneDriveService,
            CloudBackupProvider.GoogleDrive => _googleDriveService,
            CloudBackupProvider.iCloud => _iCloudService,
            CloudBackupProvider.NetworkLocation => _networkLocationService,
            CloudBackupProvider.Ftp => _ftpService,
            _ => null
        };
    }

    /// <summary>
    /// Create a scheduled backup for a user
    /// </summary>
    public async Task<CloudBackupResult> CreateScheduledBackupAsync(string userId, string description)
    {
        try
        {
            _logger.LogInformation("Creating scheduled backup for user {UserId}", userId);
            
            // Get user settings
            var settings = await _backupSettingsService.GetSettingsAsync(userId);
            if (settings == null || !settings.EnableCloudBackup)
            {
                return new CloudBackupResult
                {
                    Success = false,
                    ErrorMessage = "Cloud backup is not enabled for this user"
                };
            }

            // Generate filename for scheduled backup
            var fileName = $"VaultGuard_Scheduled_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pwmbackup";
            
            // For scheduled backups, we'll need to handle master password differently
            // This is a placeholder - in production, you'd need a secure way to handle this
            var masterPassword = ""; // TODO: Handle master password for scheduled backups
            
            return await CreateAndUploadBackupAsync(settings.SelectedCloudProvider, masterPassword, fileName, description);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating scheduled backup for user {UserId}", userId);
            return new CloudBackupResult
            {
                Success = false,
                ErrorMessage = $"Failed to create scheduled backup: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Create a manual backup for a user
    /// </summary>
    public async Task<CloudBackupResult> CreateManualBackupAsync(string userId, string description)
    {
        try
        {
            _logger.LogInformation("Creating manual backup for user {UserId}", userId);
            
            // Get user settings
            var settings = await _backupSettingsService.GetSettingsAsync(userId);
            if (settings == null || !settings.EnableCloudBackup)
            {
                return new CloudBackupResult
                {
                    Success = false,
                    ErrorMessage = "Cloud backup is not enabled for this user"
                };
            }

            // Generate filename for manual backup
            var fileName = $"VaultGuard_Manual_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pwmbackup";
            
            // For manual backups, we'll need to handle master password differently
            // This is a placeholder - in production, you'd need a secure way to handle this
            var masterPassword = ""; // TODO: Handle master password for manual backups
            
            return await CreateAndUploadBackupAsync(settings.SelectedCloudProvider, masterPassword, fileName, description);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating manual backup for user {UserId}", userId);
            return new CloudBackupResult
            {
                Success = false,
                ErrorMessage = $"Failed to create manual backup: {ex.Message}"
            };
        }
    }
}

/// <summary>
/// Information about a cloud backup provider
/// </summary>
public class CloudProviderInfo
{
    public CloudBackupProvider Provider { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public long MaxBackupSizeMB { get; set; }
}