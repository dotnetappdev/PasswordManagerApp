using VaultGuard.Models.DTOs;
using VaultGuard.Models;

namespace VaultGuard.Services.Interfaces;

/// <summary>
/// Service for managing database backups and exports
/// </summary>
public interface IDatabaseBackupService
{
    /// <summary>
    /// Create a backup of the current database
    /// </summary>
    /// <param name="encryptionKey">Key to encrypt the backup</param>
    /// <param name="compress">Whether to compress the backup</param>
    /// <returns>Backup data result</returns>
    Task<DatabaseBackupResult> CreateBackupAsync(string encryptionKey, bool compress = true);

    /// <summary>
    /// Restore database from backup data
    /// </summary>
    /// <param name="backupData">Encrypted backup data</param>
    /// <param name="encryptionKey">Key to decrypt the backup</param>
    /// <returns>True if restore was successful</returns>
    Task<bool> RestoreBackupAsync(byte[] backupData, string encryptionKey);

    /// <summary>
    /// Export passwords to browser password manager format
    /// </summary>
    /// <param name="format">Browser format (Chrome, Edge, Firefox)</param>
    /// <returns>Export data in the specified format</returns>
    Task<BrowserExportResult> ExportToBrowserAsync(BrowserExportFormat format);

    /// <summary>
    /// Get backup metadata for validation
    /// </summary>
    Task<BackupMetadata?> GetBackupMetadataAsync(byte[] backupData);

    /// <summary>
    /// Decrypt and parse a backup into browsable items — does NOT restore anything.
    /// </summary>
    Task<BackupContentsDto?> BrowseBackupAsync(byte[] encryptedData, string masterPassword);

    /// <summary>
    /// Replace all current data with the backup contents.
    /// </summary>
    Task<bool> RestoreFullAsync(byte[] encryptedData, string masterPassword);

    /// <summary>
    /// Import only the selected items from a previously-browsed backup into the current vault (merge, not replace).
    /// Returns the number of items actually imported.
    /// </summary>
    Task<int> ImportSelectedItemsAsync(BackupContentsDto contents, IEnumerable<string> selectedIds);
}

/// <summary>
/// Supported browser export formats
/// </summary>
public enum BrowserExportFormat
{
    Chrome,
    Edge,
    Firefox
}

/// <summary>
/// Result of database backup operation
/// </summary>
public class DatabaseBackupResult
{
    public bool Success { get; set; }
    public byte[]? BackupData { get; set; }
    public string? ErrorMessage { get; set; }
    public BackupMetadata? Metadata { get; set; }
}

/// <summary>
/// Result of browser export operation
/// </summary>
public class BrowserExportResult
{
    public bool Success { get; set; }
    public string? ExportData { get; set; }
    public string? ErrorMessage { get; set; }
    public int ExportedCount { get; set; }
}

/// <summary>
/// Backup metadata
/// </summary>
public class BackupMetadata
{
    public DateTime CreatedAt { get; set; }
    public string? DatabaseVersion { get; set; }
    public int PasswordCount { get; set; }
    public string? ApplicationVersion { get; set; }
    public bool IsCompressed { get; set; }
    public string? BackupDescription { get; set; }
}