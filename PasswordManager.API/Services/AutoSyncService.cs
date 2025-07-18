using Microsoft.Extensions.Configuration;
using PasswordManager.API.Interfaces;
using PasswordManager.Models.DTOs.Sync;

namespace PasswordManager.API.Services;

public class AutoSyncService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AutoSyncService> _logger;
    private readonly TimeSpan _syncInterval;

    public AutoSyncService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<AutoSyncService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
        
        var intervalMinutes = _configuration.GetValue<int>("Sync:SyncIntervalMinutes", 30);
        _syncInterval = TimeSpan.FromMinutes(intervalMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enableAutoSync = _configuration.GetValue<bool>("Sync:EnableAutoSync", true);
        
        if (!enableAutoSync)
        {
            _logger.LogInformation("Auto-sync is disabled");
            return;
        }

        _logger.LogInformation("Auto-sync service started with interval: {Interval}", _syncInterval);

        // Initial sync on startup
        await PerformSync("Startup sync");

        // Periodic sync
        using var timer = new PeriodicTimer(_syncInterval);
        
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await PerformSync("Scheduled sync");
        }
    }

    private async Task PerformSync(string syncReason)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<ISyncService>();

            _logger.LogInformation("Starting {SyncReason}", syncReason);

            // Sync from SQLite to SQL Server (push local changes to cloud)
            var syncResult = await syncService.SyncFromSqliteToSqlServerAsync();

            if (syncResult.Success)
            {
                _logger.LogInformation("{SyncReason} completed successfully. Records processed: {Records}, Duration: {Duration}",
                    syncReason, syncResult.Statistics.TotalRecordsProcessed, syncResult.Statistics.Duration);
            }
            else
            {
                _logger.LogWarning("{SyncReason} failed: {Message}", syncReason, syncResult.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during {SyncReason}", syncReason);
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Auto-sync service is stopping");
        await base.StopAsync(stoppingToken);
    }
}
