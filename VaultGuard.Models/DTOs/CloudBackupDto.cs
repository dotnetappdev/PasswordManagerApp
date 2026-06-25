namespace VaultGuard.Models.DTOs;

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
    public CloudBackupProvider Provider { get; set; }

    public string CreatedAtFormatted => CreatedAt.ToLocalTime().ToString("MMM d, yyyy  h:mm tt");
    public string SizeFormatted => SizeInBytes < 1024 * 1024
        ? $"{SizeInBytes / 1024.0:F1} KB"
        : $"{SizeInBytes / (1024.0 * 1024):F1} MB";
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
/// Contents of a decrypted backup — used for browse/selective restore
/// </summary>
public class BackupContentsDto
{
    public string BackupId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int TotalCount => LoginItems.Count + SecureNotes.Count + CreditCards.Count + WifiItems.Count;

    public List<BackupItemDto> LoginItems { get; set; } = new();
    public List<BackupItemDto> SecureNotes { get; set; } = new();
    public List<BackupItemDto> CreditCards { get; set; } = new();
    public List<BackupItemDto> WifiItems { get; set; } = new();
}

/// <summary>
/// A single selectable item from a backup
/// </summary>
public class BackupItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }   // username / card number / network name
    public string ItemType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsSelected { get; set; } = true;
    // Raw JSON payload kept for actual import
    public string RawJson { get; set; } = string.Empty;

    // The vault this item belonged to at backup time (resolved by name, not id, since restoring
    // into a different install/database means the original numeric vault id may not exist or may
    // now belong to a different vault). Null means the item had no vault at backup time.
    public string? VaultName { get; set; }
}

/// <summary>
/// Supported cloud backup providers
/// </summary>
public enum CloudBackupProvider
{
    None = 0,
    OneDrive = 1,
    iCloud = 2,
    NetworkLocation = 3,
    GoogleDrive = 4,
    Ftp = 5
}