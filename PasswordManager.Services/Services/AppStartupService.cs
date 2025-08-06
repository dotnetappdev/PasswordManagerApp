using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Services.Interfaces;
using PasswordManager.DAL;
using Microsoft.EntityFrameworkCore;

namespace PasswordManager.Services.Services;

/// <summary>
/// Service for handling application startup operations
/// </summary>
public class AppStartupService : IAppStartupService
{
    private readonly IAppSyncService? _syncService;
    private readonly IDatabaseConfigurationService _databaseConfigService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AppStartupService> _logger;

    public AppStartupService(
        IAppSyncService? syncService,
        IDatabaseConfigurationService databaseConfigService,
        IServiceScopeFactory scopeFactory,
        ILogger<AppStartupService> logger)
    {
        _syncService = syncService;
        _databaseConfigService = databaseConfigService;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Starting app initialization");

            // Initialize database first
            await InitializeDatabaseAsync();

            // Perform startup sync in a fire-and-forget manner to not block app startup
            _ = Task.Run(async () => await PerformStartupSyncAsync());

            _logger.LogInformation("App initialization completed (sync running in background)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during app initialization");
            // Don't re-throw - allow app to continue starting
        }
    }

    public async Task InitializeDatabaseAsync()
    {
        try
        {
            _logger.LogInformation("Initializing database");

            // Check if database is configured
            var isConfigured = await IsDatabaseConfiguredAsync();
            if (!isConfigured)
            {
                _logger.LogWarning("Database not configured, skipping database initialization");
                return;
            }

            // Use a scope for all dbContext operations
            using (var scope = _scopeFactory.CreateScope())
            {
                try
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<PasswordManagerDbContext>();
                    // Check for pending migrations
                    var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
                    if (pendingMigrations.Any())
                    {
                        _logger.LogInformation("Applying database migrations");
                        await dbContext.Database.MigrateAsync();
                        _logger.LogInformation("Database migrations applied successfully");
                    }
                    else
                    {
                        _logger.LogInformation("Database is up to date");
                    }
                }
                catch (ObjectDisposedException ex)
                {
                    _logger.LogError(ex, "ServiceProvider was disposed when trying to initialize database. This usually means you are calling this after the app has shut down or from a disposed scope.");
                    throw;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing database");
            throw;
        }
    }

    public async Task<bool> IsDatabaseConfiguredAsync()
    {
        try
        {
            var isFirstRun = await _databaseConfigService.IsFirstRunAsync();
            return !isFirstRun;
        }
        catch
        {
            return false;
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
