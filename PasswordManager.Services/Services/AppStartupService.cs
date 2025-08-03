using Microsoft.Extensions.Logging;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.Services.Services;

/// <summary>
/// Service for handling application startup operations
/// </summary>
public class AppStartupService : IAppStartupService
{
    private readonly IAppSyncService? _syncService;
    private readonly ILogger<AppStartupService> _logger;

    public AppStartupService(
        IAppSyncService? syncService,
        ILogger<AppStartupService> logger)
    {
        _syncService = syncService;
        _logger = logger;
    }

    public Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Starting app initialization");

            // Perform startup sync in a fire-and-forget manner to not block app startup
            _ = Task.Run(async () => await PerformStartupSyncAsync());

            _logger.LogInformation("App initialization completed (sync running in background)");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during app initialization");
            // Don't re-throw - allow app to continue starting
            return Task.CompletedTask;
        }
    }

    private async Task PerformStartupSyncAsync()
    {
        try
        {
            _logger.LogInformation("Starting background startup sync");

            // Check if sync service is available
            if (_syncService == null)
            {
                _logger.LogInformation("Sync service is not available, skipping sync");
                return;
            }

            // Add timeout to prevent hanging
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            // Check if sync is available
            var isSyncAvailable = await _syncService.IsSyncAvailableAsync();

            if (!isSyncAvailable)
            {
                _logger.LogInformation("Sync service is not available during startup, skipping sync");
                return;
            }

            // Perform the startup sync with timeout
            var syncResult = await _syncService.SyncOnStartupAsync().WaitAsync(cts.Token);

            if (syncResult.Success)
            {
                _logger.LogInformation("Background startup sync completed successfully. Records processed: {Records}, Duration: {Duration}",
                    syncResult.Statistics?.TotalRecordsProcessed ?? 0,
                    syncResult.Statistics?.Duration ?? TimeSpan.Zero);
            }
            else
            {
                _logger.LogWarning("Background startup sync failed: {Message}", syncResult.Message);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Background startup sync was cancelled due to timeout");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during background startup sync");
        }
    }
}
