using PasswordManager.Models;
using PasswordManager.Models.DTOs;

namespace PasswordManager.Services.Interfaces;

/// <summary>
/// Interface for managing scheduled backups
/// </summary>
public interface IScheduledBackupService
{
    /// <summary>
    /// Start the scheduled backup service
    /// </summary>
    Task StartAsync();

    /// <summary>
    /// Stop the scheduled backup service
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Check for and execute any due backups for a specific user
    /// </summary>
    Task<bool> CheckAndExecuteDueBackupsAsync(string userId);

    /// <summary>
    /// Check for and execute all due backups across all users
    /// </summary>
    Task CheckAndExecuteAllDueBackupsAsync();

    /// <summary>
    /// Get the next scheduled backup time for a user
    /// </summary>
    Task<DateTime?> GetNextScheduledBackupAsync(string userId);

    /// <summary>
    /// Update the backup schedule for a user
    /// </summary>
    Task UpdateBackupScheduleAsync(string userId, BackupScheduleInterval interval, TimeSpan preferredTime);

    /// <summary>
    /// Manually trigger a backup for a user (outside of schedule)
    /// </summary>
    Task<bool> TriggerManualBackupAsync(string userId, string? description = null);
}