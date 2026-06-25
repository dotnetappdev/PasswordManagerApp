using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VaultGuard.DAL.Interfaces;
using VaultGuard.Models;
using VaultGuard.Models.DTOs;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.Services.Services;

/// <summary>
/// Service for managing user backup settings in the database
/// </summary>
public class BackupSettingsService : IBackupSettingsService
{
    private readonly IVaultGuardDbContext _context;
    private readonly ILogger<BackupSettingsService> _logger;

    public BackupSettingsService(IVaultGuardDbContext context, ILogger<BackupSettingsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserBackupSettings?> GetSettingsAsync(string userId)
    {
        try
        {
            return await _context.UserBackupSettings
                .FirstOrDefaultAsync(s => s.UserId == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting backup settings for user {UserId}", userId);
            return null;
        }
    }

    public async Task<bool> SaveSettingsAsync(UserBackupSettings settings)
    {
        try
        {
            settings.LastModified = DateTime.UtcNow;

            var existingSettings = await _context.UserBackupSettings
                .FirstOrDefaultAsync(s => s.UserId == settings.UserId);

            if (existingSettings != null)
            {
                // Update existing settings
                existingSettings.EnableCloudBackup = settings.EnableCloudBackup;
                existingSettings.SelectedCloudProvider = settings.SelectedCloudProvider;
                existingSettings.AutoBackupEnabled = settings.AutoBackupEnabled;
                existingSettings.MaxBackupsToKeep = settings.MaxBackupsToKeep;
                existingSettings.BackupIntervalHours = settings.BackupIntervalHours;
                existingSettings.BackupScheduleInterval = settings.BackupScheduleInterval;
                existingSettings.PreferredBackupTime = settings.PreferredBackupTime;
                existingSettings.CompressBackups = settings.CompressBackups;
                existingSettings.BackupFolderPath = settings.BackupFolderPath;
                existingSettings.NetworkPath = settings.NetworkPath;
                existingSettings.NextBackupAt = settings.NextBackupAt;
                existingSettings.LastModified = settings.LastModified;

                _context.UserBackupSettings.Update(existingSettings);
            }
            else
            {
                // Create new settings
                settings.CreatedAt = DateTime.UtcNow;
                await _context.UserBackupSettings.AddAsync(settings);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Backup settings saved for user {UserId}", settings.UserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving backup settings for user {UserId}", settings.UserId);
            return false;
        }
    }

    public async Task<bool> UpdateSettingsAsync(string userId, UserBackupSettings settings)
    {
        try
        {
            settings.UserId = userId;
            return await SaveSettingsAsync(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating backup settings for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> DeleteSettingsAsync(string userId)
    {
        try
        {
            var settings = await _context.UserBackupSettings
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (settings != null)
            {
                _context.UserBackupSettings.Remove(settings);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Backup settings deleted for user {UserId}", userId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting backup settings for user {UserId}", userId);
            return false;
        }
    }

    public async Task<UserBackupSettings> GetOrCreateSettingsAsync(string userId)
    {
        try
        {
            var settings = await GetSettingsAsync(userId);
            if (settings != null)
            {
                return settings;
            }

            // Create default settings
            settings = new UserBackupSettings
            {
                UserId = userId,
                EnableCloudBackup = false,
                SelectedCloudProvider = CloudBackupProvider.OneDrive,
                AutoBackupEnabled = false,
                MaxBackupsToKeep = 10,
                BackupIntervalHours = 24,
                BackupScheduleInterval = BackupScheduleInterval.Manual,
                PreferredBackupTime = new TimeSpan(2, 0, 0), // 2:00 AM
                CompressBackups = true,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            await SaveSettingsAsync(settings);
            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting or creating backup settings for user {UserId}", userId);
            
            // Return default settings even if save fails
            return new UserBackupSettings
            {
                UserId = userId,
                EnableCloudBackup = false,
                SelectedCloudProvider = CloudBackupProvider.OneDrive,
                AutoBackupEnabled = false,
                MaxBackupsToKeep = 10,
                BackupIntervalHours = 24,
                BackupScheduleInterval = BackupScheduleInterval.Manual,
                PreferredBackupTime = new TimeSpan(2, 0, 0),
                CompressBackups = true
            };
        }
    }

    public async Task UpdateLastBackupAsync(string userId, DateTime backupTime)
    {
        try
        {
            var settings = await _context.UserBackupSettings
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (settings != null)
            {
                settings.LastBackupAt = backupTime;
                settings.LastModified = DateTime.UtcNow;

                // Calculate next backup time based on interval
                if (settings.AutoBackupEnabled)
                {
                    settings.NextBackupAt = backupTime.AddHours(settings.BackupIntervalHours);
                }

                _context.UserBackupSettings.Update(settings);
                await _context.SaveChangesAsync();

                _logger.LogDebug("Updated last backup time for user {UserId}", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last backup time for user {UserId}", userId);
        }
    }

    public async Task<List<UserBackupSettings>> GetDueBackupsAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            return await _context.UserBackupSettings
                .Where(s => s.AutoBackupEnabled && 
                           s.EnableCloudBackup &&
                           s.NextBackupAt.HasValue &&
                           s.NextBackupAt <= now)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting due backups");
            return new List<UserBackupSettings>();
        }
    }

    public async Task UpdateNextBackupTimeAsync(string userId, DateTime nextBackupTime)
    {
        try
        {
            var settings = await _context.UserBackupSettings
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (settings != null)
            {
                settings.NextBackupAt = nextBackupTime;
                settings.LastModified = DateTime.UtcNow;

                _context.UserBackupSettings.Update(settings);
                await _context.SaveChangesAsync();

                _logger.LogDebug("Updated next backup time for user {UserId}", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating next backup time for user {UserId}", userId);
        }
    }

    public async Task<List<UserBackupSettings>> GetAllSettingsAsync()
    {
        try
        {
            return await _context.UserBackupSettings.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all backup settings");
            return new List<UserBackupSettings>();
        }
    }
}