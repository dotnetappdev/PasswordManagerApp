using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using VaultGuard.Services.Interfaces;
using VaultGuard.Models.DTOs;

namespace VaultGuard.Services.Services;

/// <summary>
/// Network location backup service implementation
/// Supports UNC paths, mapped drives, and custom network locations
/// 
/// Security Model:
/// - Uses existing network authentication (Windows integrated security)
/// - Supports UNC paths like \\server\share\folder
/// - Supports mapped network drives like Z:\backups
/// - Creates VaultGuard subfolder for organization
/// - No credentials stored - uses current user's network access
/// </summary>
public class NetworkLocationBackupService : INetworkLocationBackupService
{
    private readonly ILogger<NetworkLocationBackupService> _logger;
    private readonly IPlatformService _platformService;
    private string _networkPath = string.Empty;
    private NetworkConnectionSettings _settings = new();
    private bool _connected;

    public string ServiceName => "Network Location";
    public long MaxBackupSizeBytes => 5L * 1024 * 1024 * 1024; // 5GB limit for network locations

    public NetworkLocationBackupService(ILogger<NetworkLocationBackupService> logger, IPlatformService platformService)
    {
        _logger = logger;
        _platformService = platformService;
    }

    public void SetNetworkPath(string networkPath)
    {
        var trimmed = networkPath?.Trim() ?? string.Empty;
        if (!string.Equals(trimmed, _networkPath, StringComparison.OrdinalIgnoreCase))
            _connected = false; // path changed — force a reconnect on next access
        _networkPath = trimmed;
        _settings.Path = trimmed;
        _logger.LogInformation("Network backup path set to: {Path}", _networkPath);
    }

    public void SetConnectionSettings(NetworkConnectionSettings settings)
    {
        _settings = settings ?? new NetworkConnectionSettings();
        _networkPath = _settings.Path?.Trim() ?? string.Empty;
        _settings.Path = _networkPath;
        _connected = false; // re-establish with the new credentials on next access
    }

    public string GetNetworkPath()
    {
        return _networkPath;
    }

    public async Task<bool> IsAvailableAsync()
    {
        return !string.IsNullOrEmpty(_networkPath) && await ValidateNetworkLocationAsync();
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            return await ValidateNetworkLocationAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "NAS connection test failed for {Path}", _networkPath);
            return false;
        }
    }

    /// <summary>
    /// Establishes a credentialed connection to the share when one is required. Uses the Windows
    /// multiple-provider router (WNetAddConnection2). On non-Windows platforms, or when no
    /// credentials are required, this is a no-op and access relies on the current user's session.
    /// </summary>
    private bool EnsureConnected()
    {
        if (!_settings.RequireAuthentication) return true;
        if (_connected) return true;
        if (string.IsNullOrWhiteSpace(_networkPath)) return false;
        if (!OperatingSystem.IsWindows()) return true; // best effort on other OSes

        // WNetAddConnection2 expects the share root (\\server\share), not a sub-folder.
        var resource = GetShareRoot(_networkPath);
        var netResource = new NativeMethods.NETRESOURCE
        {
            dwType = NativeMethods.RESOURCETYPE_DISK,
            lpRemoteName = resource
        };

        // Drop any stale connection to the same resource first so changed credentials take effect.
        NativeMethods.WNetCancelConnection2(resource, 0, true);

        var result = NativeMethods.WNetAddConnection2(netResource, _settings.Password, _settings.Username, 0);
        if (result == NativeMethods.NO_ERROR)
        {
            _connected = true;
            return true;
        }

        _logger.LogWarning("WNetAddConnection2 to {Resource} failed with code {Code}", resource, result);
        return false;
    }

    private static string GetShareRoot(string path)
    {
        // For "\\server\share\sub\folder" return "\\server\share"; pass mapped drives through as-is.
        if (!path.StartsWith(@"\\")) return path;
        var parts = path.TrimStart('\\').Split('\\', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 ? $@"\\{parts[0]}\{parts[1]}" : path;
    }

    public IReadOnlyList<MappedDriveInfo> GetMappedDrives()
    {
        var drives = new List<MappedDriveInfo>();
        try
        {
            foreach (var d in DriveInfo.GetDrives())
            {
                if (d.DriveType != DriveType.Network) continue;
                var root = d.RootDirectory.FullName; // e.g. "Z:\"
                drives.Add(new MappedDriveInfo { Root = root, UncPath = ResolveUncPath(root) });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not enumerate mapped network drives");
        }
        return drives;
    }

    private static string ResolveUncPath(string driveRoot)
    {
        if (!OperatingSystem.IsWindows()) return string.Empty;
        try
        {
            var letter = driveRoot.TrimEnd('\\', '/'); // "Z:"
            int length = 1024;
            var sb = new System.Text.StringBuilder(length);
            var result = NativeMethods.WNetGetConnection(letter, sb, ref length);
            return result == NativeMethods.NO_ERROR ? sb.ToString() : string.Empty;
        }
        catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", logEx); return string.Empty; }
    }

    public async Task<bool> ValidateNetworkLocationAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_networkPath))
            {
                return false;
            }

            // Test if the path is accessible (connecting with credentials first if required)
            return await Task.Run(() =>
            {
                try
                {
                    if (!EnsureConnected()) return false;
                    return Directory.Exists(_networkPath);
                }
                catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", logEx); return false; }
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

            // Create VaultGuard folder in network location
            var backupFolder = Path.Combine(_networkPath, "VaultGuard");
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

            var backupFolder = Path.Combine(_networkPath, "VaultGuard");
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

    /// <summary>Win32 networking interop for credentialed share connections + UNC resolution.</summary>
    private static class NativeMethods
    {
        public const int NO_ERROR = 0;
        public const int RESOURCETYPE_DISK = 0x00000001;

        [StructLayout(LayoutKind.Sequential)]
        public struct NETRESOURCE
        {
            public int dwScope;
            public int dwType;
            public int dwDisplayType;
            public int dwUsage;
            public string? lpLocalName;
            public string? lpRemoteName;
            public string? lpComment;
            public string? lpProvider;
        }

        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        public static extern int WNetAddConnection2(NETRESOURCE netResource, string? password, string? username, int flags);

        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        public static extern int WNetCancelConnection2(string name, int flags, bool force);

        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        public static extern int WNetGetConnection(string localName, System.Text.StringBuilder remoteName, ref int length);
    }
}