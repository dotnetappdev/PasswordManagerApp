using PasswordManager.App.Services.Interfaces;

namespace PasswordManager.App.Services;

public class AppStartupService
{
    private readonly IAppSyncService _syncService;
    private readonly ILogger<AppStartupService> _logger;

    public AppStartupService(
        IAppSyncService syncService,
        ILogger<AppStartupService> logger)
    {
        _syncService = syncService;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Starting app initialization");

            // Perform startup sync
            await PerformStartupSyncAsync();

            _logger.LogInformation("App initialization completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during app initialization");
        }
    }

    private async Task PerformStartupSyncAsync()
    {
        try
        {
            // Check if sync is available
            var isSyncAvailable = await _syncService.IsSyncAvailableAsync();
            
            if (!isSyncAvailable)
            {
                _logger.LogInformation("Sync service is not available during startup, skipping sync");
                return;
            }

            // Perform the startup sync
            var syncResult = await _syncService.SyncOnStartupAsync();

            if (syncResult.Success)
            {
                _logger.LogInformation("Startup sync completed successfully. Records processed: {Records}, Duration: {Duration}",
                    syncResult.Statistics?.TotalRecordsProcessed ?? 0,
                    syncResult.Statistics?.Duration ?? TimeSpan.Zero);
            }
            else
            {
                _logger.LogWarning("Startup sync failed: {Message}", syncResult.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during startup sync");
        }
    }
}
