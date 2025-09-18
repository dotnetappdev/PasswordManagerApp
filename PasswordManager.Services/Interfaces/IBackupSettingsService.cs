using PasswordManager.Models;
using PasswordManager.Models.DTOs;

namespace PasswordManager.Services.Interfaces;

/// <summary>
/// Service for managing user backup settings in the database
/// </summary>
public interface IBackupSettingsService
{
    /// <summary>
    /// Get backup settings for the current user
    /// </summary>
    Task<UserBackupSettings?> GetSettingsAsync(string userId);

    /// <summary>
    /// Save backup settings for the current user
    /// </summary>
    Task<bool> SaveSettingsAsync(UserBackupSettings settings);

    /// <summary>
    /// Update backup settings for the current user
    /// </summary>
    Task<bool> UpdateSettingsAsync(string userId, UserBackupSettings settings);

    /// <summary>
    /// Delete backup settings for the current user
    /// </summary>
    Task<bool> DeleteSettingsAsync(string userId);

    /// <summary>
    /// Get or create default settings for a user
    /// </summary>
    Task<UserBackupSettings> GetOrCreateSettingsAsync(string userId);

    /// <summary>
    /// Update last backup timestamp
    /// </summary>
    Task UpdateLastBackupAsync(string userId, DateTime backupTime);

    /// <summary>
    /// Get users who have scheduled backups due
    /// </summary>
    Task<List<UserBackupSettings>> GetDueBackupsAsync();

    /// <summary>
    /// Update next backup time for a user
    /// </summary>
    Task UpdateNextBackupTimeAsync(string userId, DateTime nextBackupTime);
}