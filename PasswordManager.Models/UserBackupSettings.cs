using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PasswordManager.Models.DTOs;

namespace PasswordManager.Models;

/// <summary>
/// User backup preferences stored in database
/// </summary>
public class UserBackupSettings
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// User ID - relationship to ApplicationUser
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Whether cloud backup is enabled
    /// </summary>
    public bool EnableCloudBackup { get; set; } = false;

    /// <summary>
    /// Selected cloud backup provider
    /// </summary>
    public CloudBackupProvider SelectedCloudProvider { get; set; } = CloudBackupProvider.None;

    /// <summary>
    /// Whether automatic backup is enabled
    /// </summary>
    public bool AutoBackupEnabled { get; set; } = false;

    /// <summary>
    /// Maximum number of backups to keep
    /// </summary>
    public int MaxBackupsToKeep { get; set; } = 10;

    /// <summary>
    /// Backup interval in hours (default: 24 hours)
    /// </summary>
    public int BackupIntervalHours { get; set; } = 24;

    /// <summary>
    /// Whether to compress backups
    /// </summary>
    public bool CompressBackups { get; set; } = true;

    /// <summary>
    /// Custom backup folder path (optional)
    /// </summary>
    [MaxLength(500)]
    public string? BackupFolderPath { get; set; }

    /// <summary>
    /// Network location path for network backups (UNC path, mapped drive, etc.)
    /// </summary>
    [MaxLength(500)]
    public string? NetworkPath { get; set; }

    /// <summary>
    /// Last successful backup timestamp
    /// </summary>
    public DateTime? LastBackupAt { get; set; }

    /// <summary>
    /// Next scheduled backup timestamp
    /// </summary>
    public DateTime? NextBackupAt { get; set; }

    /// <summary>
    /// When settings were created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When settings were last modified
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}