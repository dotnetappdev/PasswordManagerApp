using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VaultGuard.Models;
using VaultGuard.Models.DTOs;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.Services.Services;

/// <summary>
/// Service for managing scheduled backups with background processing
/// </summary>
public class ScheduledBackupService : BackgroundService, IScheduledBackupService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScheduledBackupService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15); // Check every 15 minutes

    public ScheduledBackupService(
        IServiceProvider serviceProvider,
        ILogger<ScheduledBackupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scheduled backup service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndExecuteAllDueBackupsAsync();
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Scheduled backup service stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in scheduled backup service");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Wait 5 minutes before retrying
            }
        }
    }

    public async Task StartAsync()
    {
        _logger.LogInformation("Starting scheduled backup service");
        await StartAsync(CancellationToken.None);
    }

    public async Task StopAsync()
    {
        _logger.LogInformation("Stopping scheduled backup service");
        await StopAsync(CancellationToken.None);
    }

    public async Task<bool> CheckAndExecuteDueBackupsAsync(string userId)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var backupSettingsService = scope.ServiceProvider.GetRequiredService<IBackupSettingsService>();
            var cloudBackupManager = scope.ServiceProvider.GetRequiredService<CloudBackupManager>();
            
            var settings = await backupSettingsService.GetSettingsAsync(userId);
            if (settings == null || !settings.AutoBackupEnabled || !settings.EnableCloudBackup)
            {
                return false; // No backup needed
            }

            if (!settings.IsBackupDue())
            {
                return false; // Backup not due yet
            }

            _logger.LogInformation("Executing scheduled backup for user {UserId}", userId);

            // Execute the backup
            var result = await cloudBackupManager.CreateScheduledBackupAsync(userId, $"Scheduled backup - {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
            
            if (result.Success)
            {
                // Update last backup time and calculate next backup
                settings.LastBackupAt = DateTime.UtcNow;
                settings.UpdateNextBackupTime();
                await backupSettingsService.SaveSettingsAsync(settings);

                _logger.LogInformation("Scheduled backup completed successfully for user {UserId}", userId);
                return true;
            }
            else
            {
                _logger.LogWarning("Scheduled backup failed for user {UserId}: {Error}", userId, result.ErrorMessage);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing scheduled backup for user {UserId}", userId);
            return false;
        }
    }

    public async Task CheckAndExecuteAllDueBackupsAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var backupSettingsService = scope.ServiceProvider.GetRequiredService<IBackupSettingsService>();
            
            var allSettings = await backupSettingsService.GetAllSettingsAsync();
            var dueBackups = allSettings.Where(s => s.IsBackupDue()).ToList();

            if (dueBackups.Any())
            {
                _logger.LogInformation("Found {Count} due backups to execute", dueBackups.Count);
                
                foreach (var settings in dueBackups)
                {
                    await CheckAndExecuteDueBackupsAsync(settings.UserId);
                    
                    // Add a small delay between backups to avoid overwhelming the system
                    await Task.Delay(TimeSpan.FromSeconds(10));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for due backups");
        }
    }

    public async Task<DateTime?> GetNextScheduledBackupAsync(string userId)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var backupSettingsService = scope.ServiceProvider.GetRequiredService<IBackupSettingsService>();
            
            var settings = await backupSettingsService.GetSettingsAsync(userId);
            return settings?.NextBackupAt;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting next scheduled backup for user {UserId}", userId);
            return null;
        }
    }

    public async Task UpdateBackupScheduleAsync(string userId, BackupScheduleInterval interval, TimeSpan preferredTime)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var backupSettingsService = scope.ServiceProvider.GetRequiredService<IBackupSettingsService>();
            
            var settings = await backupSettingsService.GetSettingsAsync(userId);
            if (settings != null)
            {
                settings.BackupScheduleInterval = interval;
                settings.PreferredBackupTime = preferredTime;
                settings.AutoBackupEnabled = interval != BackupScheduleInterval.Manual;
                settings.UpdateNextBackupTime();
                
                await backupSettingsService.SaveSettingsAsync(settings);
                _logger.LogInformation("Updated backup schedule for user {UserId}: {Interval} at {Time}", 
                    userId, interval, preferredTime);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating backup schedule for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> TriggerManualBackupAsync(string userId, string? description = null)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var cloudBackupManager = scope.ServiceProvider.GetRequiredService<CloudBackupManager>();
            var backupSettingsService = scope.ServiceProvider.GetRequiredService<IBackupSettingsService>();
            
            _logger.LogInformation("Triggering manual backup for user {UserId}", userId);
            
            var result = await cloudBackupManager.CreateManualBackupAsync(userId, description ?? $"Manual backup - {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
            
            if (result.Success)
            {
                // Update last backup time but don't change next scheduled time for manual backups
                var settings = await backupSettingsService.GetSettingsAsync(userId);
                if (settings != null)
                {
                    settings.LastBackupAt = DateTime.UtcNow;
                    await backupSettingsService.SaveSettingsAsync(settings);
                }
                
                _logger.LogInformation("Manual backup completed successfully for user {UserId}", userId);
                return true;
            }
            else
            {
                _logger.LogWarning("Manual backup failed for user {UserId}: {Error}", userId, result.ErrorMessage);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing manual backup for user {UserId}", userId);
            return false;
        }
    }
}