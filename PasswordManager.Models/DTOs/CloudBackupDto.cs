namespace PasswordManager.Models.DTOs;

/// <summary>
/// Result of cloud backup operations
/// </summary>
public class CloudBackupResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public byte[]? BackupData { get; set; }
    public CloudBackupInfo? BackupInfo { get; set; }
}

/// <summary>
/// Information about a cloud backup
/// </summary>
public class CloudBackupInfo
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public long SizeInBytes { get; set; }
    public string? CloudPath { get; set; }
    public bool IsCompressed { get; set; }
    public string ServiceName { get; set; } = string.Empty;
}

/// <summary>
/// Cloud backup configuration settings
/// </summary>
public class CloudBackupSettings
{
    public bool AutoBackupEnabled { get; set; }
    public CloudBackupProvider PreferredProvider { get; set; } = CloudBackupProvider.None;
    public int MaxBackupsToKeep { get; set; } = 10;
    public TimeSpan BackupInterval { get; set; } = TimeSpan.FromDays(1);
    public bool CompressBackups { get; set; } = true;
    public string? BackupFolderPath { get; set; }
}

/// <summary>
/// Supported cloud backup providers
/// </summary>
public enum CloudBackupProvider
{
    None = 0,
    OneDrive = 1,
    iCloud = 2
}