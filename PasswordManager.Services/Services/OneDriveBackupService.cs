using Microsoft.Extensions.Logging;
using PasswordManager.Services.Interfaces;
using PasswordManager.Models.DTOs;

namespace PasswordManager.Services.Services;

/// <summary>
/// OneDrive backup service implementation using local OneDrive folder detection
/// Works with users already signed into OneDrive on Windows - no credentials collected
/// </summary>
public class OneDriveBackupService : IOneDriveBackupService
{
    private readonly ILogger<OneDriveBackupService> _logger;
    private const string BackupFolderName = "PasswordManager";

    public string ServiceName => "OneDrive";
    public long MaxBackupSizeBytes => 100 * 1024 * 1024; // 100MB limit for personal OneDrive

    public OneDriveBackupService(ILogger<OneDriveBackupService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> AuthenticateAsync()
    {
        try
        {
            // Check if OneDrive is set up and syncing on this Windows machine
            var oneDrivePath = GetOneDrivePath();
            if (oneDrivePath != null)
            {
                _logger.LogInformation("OneDrive folder detected at: {Path}", oneDrivePath);
                return true;
            }

            _logger.LogInformation("OneDrive is not set up or not syncing on this machine");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect OneDrive");
            return false;
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        return GetOneDrivePath() != null;
    }

    public async Task SignOutAsync()
    {
        // Cannot programmatically sign out of OneDrive on Windows
        // User needs to sign out through OneDrive app or Windows settings
        _logger.LogInformation("User must sign out of OneDrive through Windows settings or OneDrive app");
    }

    public async Task<CloudBackupResult> UploadBackupAsync(byte[] backupData, string fileName, string? description = null)
    {
        try
        {
            if (!await IsAuthenticatedAsync())
            {
                return new CloudBackupResult 
                { 
                    Success = false, 
                    ErrorMessage = "OneDrive is not set up or not syncing on this machine" 
                };
            }

            if (backupData.Length > MaxBackupSizeBytes)
            {
                return new CloudBackupResult 
                { 
                    Success = false, 
                    ErrorMessage = $"Backup size exceeds maximum allowed size of {MaxBackupSizeBytes / (1024 * 1024)}MB" 
                };
            }

            var oneDrivePath = GetOneDrivePath();
            if (oneDrivePath == null)
            {
                return new CloudBackupResult 
                { 
                    Success = false, 
                    ErrorMessage = "OneDrive path not found" 
                };
            }

            // Create PasswordManager folder in OneDrive
            var backupFolder = Path.Combine(oneDrivePath, BackupFolderName);
            if (!Directory.Exists(backupFolder))
            {
                Directory.CreateDirectory(backupFolder);
                _logger.LogInformation("Created backup folder at: {Path}", backupFolder);
            }

            var filePath = Path.Combine(backupFolder, fileName);
            await File.WriteAllBytesAsync(filePath, backupData);

            var fileInfo = new FileInfo(filePath);
            return new CloudBackupResult
            {
                Success = true,
                BackupInfo = new CloudBackupInfo
                {
                    Id = filePath,
                    FileName = fileName,
                    Description = description,
                    CreatedAt = fileInfo.CreationTimeUtc,
                    ModifiedAt = fileInfo.LastWriteTimeUtc,
                    SizeInBytes = fileInfo.Length,
                    CloudPath = filePath,
                    ServiceName = ServiceName
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload backup to OneDrive");
            return new CloudBackupResult 
            { 
                Success = false, 
                ErrorMessage = $"Upload failed: {ex.Message}" 
            };
        }
    }

    public async Task<CloudBackupResult> DownloadBackupAsync(string backupId)
    {
        try
        {
            if (!File.Exists(backupId))
            {
                return new CloudBackupResult 
                { 
                    Success = false, 
                    ErrorMessage = "Backup file not found" 
                };
            }

            var backupData = await File.ReadAllBytesAsync(backupId);
            var fileInfo = new FileInfo(backupId);

            return new CloudBackupResult
            {
                Success = true,
                BackupData = backupData,
                BackupInfo = new CloudBackupInfo
                {
                    Id = backupId,
                    FileName = fileInfo.Name,
                    CreatedAt = fileInfo.CreationTimeUtc,
                    ModifiedAt = fileInfo.LastWriteTimeUtc,
                    SizeInBytes = fileInfo.Length,
                    CloudPath = backupId,
                    ServiceName = ServiceName
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download backup from OneDrive");
            return new CloudBackupResult 
            { 
                Success = false, 
                ErrorMessage = $"Download failed: {ex.Message}" 
            };
        }
    }

    public async Task<List<CloudBackupInfo>> ListBackupsAsync()
    {
        try
        {
            if (!await IsAuthenticatedAsync())
            {
                return new List<CloudBackupInfo>();
            }

            var oneDrivePath = GetOneDrivePath();
            if (oneDrivePath == null)
            {
                return new List<CloudBackupInfo>();
            }

            var backupFolder = Path.Combine(oneDrivePath, BackupFolderName);
            if (!Directory.Exists(backupFolder))
            {
                return new List<CloudBackupInfo>();
            }

            var backupFiles = Directory.GetFiles(backupFolder, "*.pwmbackup")
                .Select(filePath =>
                {
                    var fileInfo = new FileInfo(filePath);
                    return new CloudBackupInfo
                    {
                        Id = filePath,
                        FileName = fileInfo.Name,
                        CreatedAt = fileInfo.CreationTimeUtc,
                        ModifiedAt = fileInfo.LastWriteTimeUtc,
                        SizeInBytes = fileInfo.Length,
                        CloudPath = filePath,
                        ServiceName = ServiceName
                    };
                })
                .ToList();

            return backupFiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list backups from OneDrive");
            return new List<CloudBackupInfo>();
        }
    }

    public async Task<bool> DeleteBackupAsync(string backupId)
    {
        try
        {
            if (File.Exists(backupId))
            {
                File.Delete(backupId);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete backup from OneDrive");
            return false;
        }
    }

    public async Task<OneDriveAccountInfo?> GetAccountInfoAsync()
    {
        try
        {
            if (!await IsAuthenticatedAsync())
            {
                return null;
            }

            var oneDrivePath = GetOneDrivePath();
            if (oneDrivePath == null)
            {
                return null;
            }

            // Try to get available space from the OneDrive folder
            var driveInfo = new DriveInfo(Path.GetPathRoot(oneDrivePath) ?? oneDrivePath);
            
            return new OneDriveAccountInfo
            {
                DisplayName = "OneDrive User",
                Email = "user@outlook.com", // Cannot determine actual email without API
                TotalSpace = driveInfo.TotalSize,
                UsedSpace = driveInfo.TotalSize - driveInfo.AvailableFreeSpace,
                AvailableSpace = driveInfo.AvailableFreeSpace
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get OneDrive account info");
            return null;
        }
    }

    public async Task<long> GetAvailableStorageAsync()
    {
        try
        {
            var accountInfo = await GetAccountInfoAsync();
            return accountInfo?.AvailableSpace ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Detects OneDrive folder path on Windows when user is already signed in
    /// This approach doesn't collect credentials and works with existing Windows OneDrive integration
    /// </summary>
    private string? GetOneDrivePath()
    {
        try
        {
            // Method 1: Check environment variable (most reliable)
            var oneDriveEnv = Environment.GetEnvironmentVariable("OneDrive");
            if (!string.IsNullOrEmpty(oneDriveEnv) && Directory.Exists(oneDriveEnv))
            {
                _logger.LogDebug("OneDrive detected via environment variable: {Path}", oneDriveEnv);
                return oneDriveEnv;
            }

            // Method 2: Check user profile folder (common location)
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var commonPaths = new[]
            {
                Path.Combine(userProfile, "OneDrive"),
                Path.Combine(userProfile, "OneDrive - Personal"),
                Path.Combine(userProfile, "OneDrive - Microsoft")
            };

            foreach (var path in commonPaths)
            {
                if (Directory.Exists(path))
                {
                    _logger.LogDebug("OneDrive detected at: {Path}", path);
                    return path;
                }
            }

            // Method 3: Check for OneDrive registry key (Windows specific)
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\OneDrive\Accounts\Personal");
                var userFolder = key?.GetValue("UserFolder")?.ToString();
                if (!string.IsNullOrEmpty(userFolder) && Directory.Exists(userFolder))
                {
                    _logger.LogDebug("OneDrive detected via registry: {Path}", userFolder);
                    return userFolder;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not check OneDrive registry key");
            }

            _logger.LogInformation("OneDrive folder not found - user may not be signed in or OneDrive may not be syncing");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting OneDrive path");
            return null;
        }
    }
}