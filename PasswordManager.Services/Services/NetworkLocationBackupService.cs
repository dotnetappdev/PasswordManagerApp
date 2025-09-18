using Microsoft.Extensions.Logging;
using PasswordManager.Services.Interfaces;
using PasswordManager.Models.DTOs;

namespace PasswordManager.Services.Services;

/// <summary>
/// Network location backup service implementation
/// Supports UNC paths, mapped drives, and custom network locations
/// 
/// Security Model:
/// - Uses existing network authentication (Windows integrated security)
/// - Supports UNC paths like \\server\share\folder
/// - Supports mapped network drives like Z:\backups
/// - Creates PasswordManager subfolder for organization
/// - No credentials stored - uses current user's network access
/// </summary>
public class NetworkLocationBackupService : INetworkLocationBackupService
{
    private readonly ILogger<NetworkLocationBackupService> _logger;
    private readonly IPlatformService _platformService;
    private string _networkPath = string.Empty;
    
    public string ServiceName => "Network Location";
    public long MaxBackupSizeBytes => 5L * 1024 * 1024 * 1024; // 5GB limit for network locations

    public NetworkLocationBackupService(ILogger<NetworkLocationBackupService> logger, IPlatformService platformService)
    {
        _logger = logger;
        _platformService = platformService;
    }

    public void SetNetworkPath(string networkPath)
    {
        _networkPath = networkPath?.Trim() ?? string.Empty;
        _logger.LogInformation("Network backup path set to: {Path}", _networkPath);
    }

    public string GetNetworkPath()
    {
        return _networkPath;
    }

    public async Task<bool> IsAvailableAsync()
    {
        return !string.IsNullOrEmpty(_networkPath) && await ValidateNetworkLocationAsync();
    }

    public async Task<bool> ValidateNetworkLocationAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_networkPath))
            {
                return false;
            }

            // Test if the path is accessible
            return await Task.Run(() =>
            {
                try
                {
                    return Directory.Exists(_networkPath);
                }
                catch
                {
                    return false;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating network location: {Path}", _networkPath);
            return false;
        }
    }

    public async Task<CloudBackupResult> CreateBackupAsync(byte[] backupData, string fileName, string description = "")
    {
        try
        {
            if (string.IsNullOrEmpty(_networkPath))
            {
                return new CloudBackupResult
                {
                    Success = false,
                    ErrorMessage = "Network path not configured"
                };
            }

            if (!await ValidateNetworkLocationAsync())
            {
                return new CloudBackupResult
                {
                    Success = false,
                    ErrorMessage = "Network location is not accessible"
                };
            }

            // Create PasswordManager folder in network location
            var backupFolder = Path.Combine(_networkPath, "PasswordManager");
            if (!Directory.Exists(backupFolder))
            {
                Directory.CreateDirectory(backupFolder);
                _logger.LogInformation("Created network backup folder at: {Path}", backupFolder);
            }

            var filePath = Path.Combine(backupFolder, fileName);
            await File.WriteAllBytesAsync(filePath, backupData);

            var fileInfo = new FileInfo(filePath);
            var backupInfo = new CloudBackupInfo
            {
                Id = Path.GetFileNameWithoutExtension(fileName),
                FileName = fileName,
                Description = description,
                CreatedAt = fileInfo.CreationTime,
                ModifiedAt = fileInfo.LastWriteTime,
                SizeInBytes = fileInfo.Length,
                CloudPath = filePath,
                IsCompressed = true,
                ServiceName = ServiceName
            };

            _logger.LogInformation("Network backup created successfully: {FileName}", fileName);

            return new CloudBackupResult
            {
                Success = true,
                BackupInfo = backupInfo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating network backup: {FileName}", fileName);
            return new CloudBackupResult
            {
                Success = false,
                ErrorMessage = $"Failed to create network backup: {ex.Message}"
            };
        }
    }

    public async Task<List<CloudBackupInfo>> ListBackupsAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_networkPath) || !await ValidateNetworkLocationAsync())
            {
                return new List<CloudBackupInfo>();
            }

            var backupFolder = Path.Combine(_networkPath, "PasswordManager");
            if (!Directory.Exists(backupFolder))
            {
                return new List<CloudBackupInfo>();
            }

            var backups = new List<CloudBackupInfo>();
            var files = Directory.GetFiles(backupFolder, "*.pwmbackup", SearchOption.TopDirectoryOnly);

            foreach (var file in files)
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    var backup = new CloudBackupInfo
                    {
                        Id = Path.GetFileNameWithoutExtension(fileInfo.Name),
                        FileName = fileInfo.Name,
                        CreatedAt = fileInfo.CreationTime,
                        ModifiedAt = fileInfo.LastWriteTime,
                        SizeInBytes = fileInfo.Length,
                        CloudPath = file,
                        IsCompressed = true,
                        ServiceName = ServiceName
                    };
                    backups.Add(backup);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error reading network backup file: {File}", file);
                }
            }

            return backups.OrderByDescending(b => b.CreatedAt).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing network backups");
            return new List<CloudBackupInfo>();
        }
    }

    public async Task<CloudBackupResult> RestoreBackupAsync(string backupId, string masterPassword)
    {
        try
        {
            var backups = await ListBackupsAsync();
            var backup = backups.FirstOrDefault(b => b.Id == backupId);

            if (backup == null)
            {
                return new CloudBackupResult
                {
                    Success = false,
                    ErrorMessage = "Backup not found"
                };
            }

            if (!File.Exists(backup.CloudPath))
            {
                return new CloudBackupResult
                {
                    Success = false,
                    ErrorMessage = "Backup file not found on network location"
                };
            }

            var backupData = await File.ReadAllBytesAsync(backup.CloudPath);
            
            return new CloudBackupResult
            {
                Success = true,
                BackupData = backupData,
                BackupInfo = backup
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring network backup: {BackupId}", backupId);
            return new CloudBackupResult
            {
                Success = false,
                ErrorMessage = $"Failed to restore backup: {ex.Message}"
            };
        }
    }

    public async Task<bool> DeleteBackupAsync(string backupId)
    {
        try
        {
            var backups = await ListBackupsAsync();
            var backup = backups.FirstOrDefault(b => b.Id == backupId);

            if (backup == null || !File.Exists(backup.CloudPath))
            {
                return false;
            }

            File.Delete(backup.CloudPath);
            _logger.LogInformation("Deleted network backup: {BackupId}", backupId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting network backup: {BackupId}", backupId);
            return false;
        }
    }

    public async Task<bool> AuthenticateAsync()
    {
        // Network locations use Windows integrated authentication - no explicit auth needed
        return await ValidateNetworkLocationAsync();
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        // Network locations are "authenticated" if they are accessible
        return await ValidateNetworkLocationAsync();
    }

    public async Task SignOutAsync()
    {
        // No sign-out needed for network locations - just clear the path
        _networkPath = string.Empty;
        await Task.CompletedTask;
    }

    public async Task<CloudBackupResult> UploadBackupAsync(byte[] backupData, string fileName, string? description = null)
    {
        // This is the same as CreateBackupAsync for network locations
        return await CreateBackupAsync(backupData, fileName, description ?? string.Empty);
    }

    public async Task<CloudBackupResult> DownloadBackupAsync(string backupId)
    {
        // This is the same as RestoreBackupAsync for network locations
        return await RestoreBackupAsync(backupId, string.Empty); // No password needed for download
    }
}