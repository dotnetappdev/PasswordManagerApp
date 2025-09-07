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
                    
                    // Check if the database exists and can connect
                    var canConnect = await dbContext.Database.CanConnectAsync();
                    var canConnectApp = await dbContextApp.Database.CanConnectAsync();
                    
                    if (!canConnect || !canConnectApp)
                    {
                        _logger.LogInformation("Database not found, creating initial database structure");
                        
                        // For new databases, use migrations to ensure proper Identity table creation
                        try
                        {
                            await dbContext.Database.MigrateAsync();
                            await dbContextApp.Database.MigrateAsync();
                            _logger.LogInformation("Initial database created successfully using migrations");
                        }
                        catch (Exception migrationEx)
                        {
                            _logger.LogWarning(migrationEx, "Migration failed during initial setup, falling back to EnsureCreated");
                            await dbContext.Database.EnsureCreatedAsync();
                            await dbContextApp.Database.EnsureCreatedAsync();
                        }
                        
                        // Seed Identity data for new installations
                        await SeedIdentityDataIfNeeded(scope);
                        return; 
                    }

                    // Check if Identity tables exist - this is crucial for the reported issue
                    var identityTablesExist = await CheckIdentityTablesExistAsync(dbContextApp);
                    if (!identityTablesExist)
                    {
                        _logger.LogWarning("Database exists but Identity tables are missing - applying migrations to create them");
                        try
                        {
                            await dbContextApp.Database.MigrateAsync();
                            _logger.LogInformation("Identity tables created successfully via migration");
                        }
                        catch (Exception migrationEx)
                        {
                            _logger.LogError(migrationEx, "Failed to create Identity tables via migration, trying EnsureCreated");
                            await dbContextApp.Database.EnsureCreatedAsync();
                        }
                        
                        // Seed Identity data after creating tables
                        await SeedIdentityDataIfNeeded(scope);
                    }

                    // If database exists, check if database is properly configured before applying migrations
                    var isConfigured = await IsDatabaseConfiguredAsync();
                    if (!isConfigured)
                    {
                        _logger.LogInformation("Database exists but not fully configured, ensuring basic structure is available");
                        // Ensure basic database structure without migrations for unconfigured databases
                        await dbContext.Database.EnsureCreatedAsync();
                        await dbContextApp.Database.EnsureCreatedAsync();
                        
                        // Seed Identity data for new installations
                        await SeedIdentityDataIfNeeded(scope);
                        return;
                    }

                    // Check for pending migrations
                    var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
                    var pendingMigrationsApp = await dbContextApp.Database.GetPendingMigrationsAsync();
                    
                    // For desktop applications (WinUI), automatically apply pending migrations
                    // to ensure the database schema is up to date
                    var isDesktopApp = Environment.OSVersion.Platform == PlatformID.Win32NT && 
                                      !Environment.GetCommandLineArgs().Any(arg => arg.Contains("server") || arg.Contains("web"));
                    
                    if (pendingMigrations.Any() || pendingMigrationsApp.Any())
                    {
                        if (isDesktopApp)
                        {
                            _logger.LogInformation("Desktop app detected with pending migrations (API: {ApiMigrations}, App: {AppMigrations}). Applying automatically.", 
                                pendingMigrations.Count(), pendingMigrationsApp.Count());
                            
                            try
                            {
                                // Apply pending migrations for both contexts
                                if (pendingMigrations.Any())
                                {
                                    _logger.LogInformation("Applying {Count} pending API migrations", pendingMigrations.Count());
                                    await dbContext.Database.MigrateAsync();
                                }
                                
                                if (pendingMigrationsApp.Any())
                                {
                                    _logger.LogInformation("Applying {Count} pending App migrations", pendingMigrationsApp.Count());
                                    await dbContextApp.Database.MigrateAsync();
                                }
                                
                                _logger.LogInformation("All pending migrations applied successfully");
                            }
                            catch (Exception migrationEx)
                            {
                                _logger.LogError(migrationEx, "Failed to apply pending migrations automatically");
                                // Continue with app startup even if migration fails
                            }
                        }
                        else
                        {
                            _logger.LogInformation("Web/server app detected with pending migrations (API: {ApiMigrations}, App: {AppMigrations}). User will need to apply them manually.", 
                                pendingMigrations.Count(), pendingMigrationsApp.Count());
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Database is up to date");
                    }
                    
                    // Seed Identity data and test data regardless of migration status
                    await SeedIdentityDataIfNeeded(scope);
                    
                    // Seed test data if database is empty
                    await SeedTestDataIfNeeded(dbContext);
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

    private async Task SeedIdentityDataIfNeeded(IServiceScope scope)
    {
        try
        {
            // Try to get the Identity seeder (may not be available in all configurations)
            var identitySeeder = scope.ServiceProvider.GetService<PasswordManager.DAL.Seed.IdentityDataSeeder>();
            if (identitySeeder != null)
            {
                _logger.LogInformation("Seeding Identity data (roles and default users)");
                await identitySeeder.SeedAsync();
                _logger.LogInformation("Identity data seeding completed successfully");
            }
            else
            {
                _logger.LogDebug("Identity seeder not available, skipping Identity data seeding");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding Identity data");
            // Don't throw - seeding failure shouldn't prevent app startup
        }
    }
    
    /// <summary>
    /// Checks if ASP.NET Core Identity tables exist in the database
    /// This is crucial for ensuring the reported issue is resolved
    /// </summary>
    private async Task<bool> CheckIdentityTablesExistAsync(PasswordManagerDbContextApp dbContext)
    {
        try
        {
            // Check if the main Identity tables exist by attempting to query them
            var aspNetUsersExists = await dbContext.Database.ExecuteSqlRawAsync(
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='AspNetUsers'") >= 0;
            
            var aspNetRolesExists = await dbContext.Database.ExecuteSqlRawAsync(
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='AspNetRoles'") >= 0;

            // If we can execute these queries without error, the tables exist
            // Additional verification by checking if we can query the Users table
            var userCount = await dbContext.Users.CountAsync();
            
            _logger.LogInformation("Identity tables check: AspNetUsers and AspNetRoles appear to exist (user count: {UserCount})", userCount);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Identity tables do not exist or are not accessible");
            return false;
        }
    }
}
