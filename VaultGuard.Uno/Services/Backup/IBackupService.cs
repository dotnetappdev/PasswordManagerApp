namespace VaultGuard.Uno.Services.Backup;

/// <summary>
/// Interface for cloud backup services
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Check if backup is available on this platform
    /// </summary>
    Task<bool> IsBackupAvailableAsync();

    /// <summary>
    /// Check if user is signed in to cloud service
    /// </summary>
    Task<bool> IsSignedInAsync();

    /// <summary>
    /// Backup database to cloud
    /// </summary>
    Task<BackupResult> BackupAsync();

    /// <summary>
    /// Restore database from cloud
    /// </summary>
    Task<BackupResult> RestoreAsync();

    /// <summary>
    /// Get the last backup date
    /// </summary>
    Task<DateTime?> GetLastBackupDateAsync();

    /// <summary>
    /// Enable automatic backup
    /// </summary>
    Task EnableAutoBackupAsync();

    /// <summary>
    /// Disable automatic backup
    /// </summary>
    Task DisableAutoBackupAsync();

    /// <summary>
    /// Check if auto backup is enabled
    /// </summary>
    bool IsAutoBackupEnabled();
}

public class BackupResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? BackupDate { get; set; }
    public long? BackupSize { get; set; }
}
