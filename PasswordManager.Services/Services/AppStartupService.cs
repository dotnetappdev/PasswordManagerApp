using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Services.Interfaces;
using PasswordManager.DAL;
using Microsoft.EntityFrameworkCore;
using PasswordManager.DAL.Seed;

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

            // Use a scope for all dbContext operations
            using (var scope = _scopeFactory.CreateScope())
            {
                try
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<PasswordManagerDbContext>();
                    var dbContextApp = scope.ServiceProvider.GetRequiredService<PasswordManagerDbContextApp>();
                    
                    // First, ensure the database exists (this creates it if it doesn't exist)
                    // Use EnsureCreatedAsync for initial database creation to avoid migration conflicts on first run
                    var canConnect = await dbContext.Database.CanConnectAsync();
                    if (!canConnect)
                    {
                        _logger.LogInformation("Database not found, creating initial database structure");
                        await dbContext.Database.EnsureCreatedAsync();
                        await dbContextApp.Database.EnsureCreatedAsync();
                        _logger.LogInformation("Initial database structure created successfully");
                        return; // Skip migrations on fresh database creation
                    }

                    // If database exists, check if database is properly configured before applying migrations
                    var isConfigured = await IsDatabaseConfiguredAsync();
                    if (!isConfigured)
                    {
                        _logger.LogInformation("Database exists but not fully configured, ensuring basic structure is available");
                        // Ensure basic database structure without migrations for unconfigured databases
                        await dbContext.Database.EnsureCreatedAsync();
                        await dbContextApp.Database.EnsureCreatedAsync();
                        return;
                    }

                    // Check for pending migrations but do not apply them automatically
                    var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
                    var pendingMigrationsApp = await dbContextApp.Database.GetPendingMigrationsAsync();
                    
                    if (pendingMigrations.Any() || pendingMigrationsApp.Any())
                    {
                        _logger.LogInformation("Database has pending migrations (API: {ApiMigrations}, App: {AppMigrations}). User will need to apply them manually.", 
                            pendingMigrations.Count(), pendingMigrationsApp.Count());
                    }
                    else
                    {
                        _logger.LogInformation("Database is up to date");
                        
                        // Seed test data if database is empty
                        await SeedTestDataIfNeeded(dbContext);
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
            _logger.LogError(ex, "Error initializing database, but allowing app to continue startup");
            // Don't re-throw - allow app to continue starting even if database initialization fails
            // Users can then use the setup menu to configure the database properly
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

    private async Task SeedTestDataIfNeeded(PasswordManagerDbContext dbContext)
    {
        try
        {
            // Check if we already have data
            if (await dbContext.PasswordItems.AnyAsync())
            {
                _logger.LogDebug("Database already contains password items, skipping test data seeding");
                return;
            }

            _logger.LogInformation("Seeding test data to populate empty database");
            TestDataSeeder.SeedTestData(dbContext);
            _logger.LogInformation("Test data seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding test data");
            // Don't throw - seeding failure shouldn't prevent app startup
        }
    }
}
