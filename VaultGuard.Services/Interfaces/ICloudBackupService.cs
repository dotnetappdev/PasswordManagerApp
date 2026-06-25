using PasswordManager.Models.DTOs;

namespace PasswordManager.Services.Interfaces;

/// <summary>
/// Base interface for cloud backup services (OneDrive, iCloud, etc.)
/// </summary>
public interface ICloudBackupService
{
    /// <summary>
    /// Authenticate user with the cloud service
    /// </summary>
    /// <returns>True if authentication was successful</returns>
    Task<bool> AuthenticateAsync();

    /// <summary>
    /// Check if user is currently authenticated
    /// </summary>
    /// <returns>True if authenticated</returns>
    Task<bool> IsAuthenticatedAsync();

    /// <summary>
    /// Sign out from the cloud service
    /// </summary>
    Task SignOutAsync();

    /// <summary>
    /// Upload encrypted backup to cloud storage
    /// </summary>
    /// <param name="backupData">Encrypted and compressed backup data</param>
    /// <param name="fileName">Name of the backup file</param>
    /// <param name="description">Optional description for the backup</param>
    /// <returns>Upload result with success status and backup info</returns>
    Task<CloudBackupResult> UploadBackupAsync(byte[] backupData, string fileName, string? description = null);

    /// <summary>
    /// Download backup from cloud storage
    /// </summary>
    /// <param name="backupId">ID of the backup to download</param>
    /// <returns>Download result with backup data</returns>
    Task<CloudBackupResult> DownloadBackupAsync(string backupId);

    /// <summary>
    /// List all available backups
    /// </summary>
    /// <returns>List of available backups</returns>
    Task<List<CloudBackupInfo>> ListBackupsAsync();

    /// <summary>
    /// Delete a backup from cloud storage
    /// </summary>
    /// <param name="backupId">ID of the backup to delete</param>
    /// <returns>True if deletion was successful</returns>
    Task<bool> DeleteBackupAsync(string backupId);

    /// <summary>
    /// Get the display name of this cloud service
    /// </summary>
    string ServiceName { get; }

    /// <summary>
    /// Get the maximum allowed backup size in bytes
    /// </summary>
    long MaxBackupSizeBytes { get; }
}