using Microsoft.Extensions.Logging;
using VaultGuard.Services.Interfaces;
using VaultGuard.Models.DTOs;

namespace VaultGuard.Services.Services;

/// <summary>
/// iCloud backup service implementation using Windows iCloud Drive folder detection
/// Note: iCloud integration is limited on non-Apple platforms
/// This provides a basic file-based approach for Windows/Linux
/// 
/// Security Model (similar to Microsoft Authenticator):
/// - Stores backups in iCloudDrive/Apps/VaultGuard folder (app-specific secure location)
/// - Sets folder and file attributes as Hidden for additional security
/// - Files are encrypted before storage and have .pwmbackup extension
/// - Works with existing iCloud for Windows installation
/// </summary>
public class iCloudBackupService : IiCloudBackupService
{
    private readonly ILogger<iCloudBackupService> _logger;
    private readonly IPlatformService _platformService;
    
    public string ServiceName => "iCloud";
    public long MaxBackupSizeBytes => 50 * 1024 * 1024; // 50MB conservative limit
    
    public bool IsAvailable => GetiCloudPath() != null;

    public iCloudBackupService(ILogger<iCloudBackupService> logger, IPlatformService platformService)
    {
        _logger = logger;
        _platformService = platformService;
    }

    public Task<bool> AuthenticateAsync()
    {
        // On Windows, iCloud authentication is handled by the iCloud for Windows app
        // We can only check if iCloud folder is available
        var available = IsAvailable;
        if (!available)
        {
            _logger.LogWarning("iCloud for Windows not detected. Please install iCloud for Windows and sign in.");
        }
        return Task.FromResult(available);
    }

    public Task<bool> IsAuthenticatedAsync()
    {
        return Task.FromResult(IsAvailable);
    }

    public Task SignOutAsync()
    {
        // Cannot programmatically sign out of iCloud on Windows
        // User needs to sign out through iCloud for Windows app
        return Task.CompletedTask;
    }

    public async Task<CloudBackupResult> UploadBackupAsync(byte[] backupData, string fileName, string? description = null)
    {
        try
        {
            if (!IsAvailable)
            {
                return new CloudBackupResult 
                { 
                    Success = false, 
                    ErrorMessage = "iCloud is not available on this platform" 
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

            var icloudPath = GetiCloudPath();
            if (icloudPath == null)
            {
                return new CloudBackupResult 
                { 
                    Success = false, 
                    ErrorMessage = "iCloud path not found" 
                };
            }

            // Create secure VaultGuard folder in iCloud Drive (similar to Microsoft Authenticator)
            var backupFolder = Path.Combine(icloudPath, "Apps", "VaultGuard");
            if (!Directory.Exists(backupFolder))
            {
                Directory.CreateDirectory(backupFolder);
                _logger.LogInformation("Created secure iCloud backup folder at: {Path}", backupFolder);
                
                // Set folder as hidden for additional security
                try
                {
                    var directoryInfo = new DirectoryInfo(backupFolder);
                    directoryInfo.Attributes |= FileAttributes.Hidden;
                    _logger.LogDebug("Set secure attributes on iCloud backup folder");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not set secure attributes on iCloud backup folder");
                }
            }

            var filePath = Path.Combine(backupFolder, fileName);
            await File.WriteAllBytesAsync(filePath, backupData);

            // Set secure file attributes
            try
            {
                var secureFileInfo = new FileInfo(filePath);
                secureFileInfo.Attributes |= FileAttributes.Hidden;
                _logger.LogDebug("Set secure attributes on iCloud backup file: {FileName}", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not set secure attributes on iCloud backup file: {FileName}", fileName);
            }

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
            _logger.LogError(ex, "Failed to upload backup to iCloud");
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
            _logger.LogError(ex, "Failed to download backup from iCloud");
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
            if (!IsAvailable)
            {
                return new List<CloudBackupInfo>();
            }

            var icloudPath = GetiCloudPath();
            if (icloudPath == null)
            {
                return new List<CloudBackupInfo>();
            }

            var backupFolder = Path.Combine(icloudPath, "Apps", "VaultGuard");
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
            _logger.LogError(ex, "Failed to list backups from iCloud");
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
            _logger.LogError(ex, "Failed to delete backup from iCloud");
            return false;
        }
    }

    public Task<iCloudAccountInfo?> GetAccountInfoAsync()
    {
        try
        {
            if (!IsAvailable)
            {
                return Task.FromResult<iCloudAccountInfo?>(null);
            }

            var icloudPath = GetiCloudPath();
            if (icloudPath == null)
            {
                return Task.FromResult<iCloudAccountInfo?>(null);
            }

            // Try to get available space
            var driveInfo = new DriveInfo(Path.GetPathRoot(icloudPath) ?? icloudPath);
            
            return Task.FromResult<iCloudAccountInfo?>(new iCloudAccountInfo
            {
                AppleId = "unknown", // Cannot determine Apple ID programmatically
                DisplayName = "iCloud User",
                AvailableSpace = driveInfo.AvailableFreeSpace
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get iCloud account info");
            return Task.FromResult<iCloudAccountInfo?>(null);
        }
    }

    private string? GetiCloudPath()
    {
        try
        {
            // Check for iCloud Drive on Windows
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var icloudDrivePath = Path.Combine(userProfile, "iCloudDrive");
            
            if (Directory.Exists(icloudDrivePath))
            {
                return icloudDrivePath;
            }

            // Alternative path for older iCloud versions
            var alternativePath = Path.Combine(userProfile, "iCloud Drive (Archive)");
            if (Directory.Exists(alternativePath))
            {
                return alternativePath;
            }

            // Check for iCloud Documents folder
            var documentsPath = Path.Combine(userProfile, "iCloud Drive", "Documents");
            if (Directory.Exists(documentsPath))
            {
                return Path.Combine(userProfile, "iCloud Drive");
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting iCloud path");
            return null;
        }
    }
}