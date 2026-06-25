using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using VaultGuard.Services.Interfaces;
using VaultGuard.DAL;
using Microsoft.EntityFrameworkCore;
using VaultGuard.DAL.Seed;
using VaultGuard.Models;
using System.Linq;

namespace VaultGuard.Services.Services;

/// <summary>
/// Service for handling application startup operations
/// </summary>
public class AppStartupService : IAppStartupService
{
    private readonly IAppSyncService? _syncService;
    private readonly IDatabaseConfigurationService _databaseConfigService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AppStartupService> _logger;
    private readonly IDatabaseMigrationService _migrationService;

    public AppStartupService(
        IAppSyncService? syncService,
        IDatabaseConfigurationService databaseConfigService,
        IServiceScopeFactory scopeFactory,
        ILogger<AppStartupService> logger,
        IDatabaseMigrationService migrationService)
    {
        _syncService = syncService;
        _databaseConfigService = databaseConfigService;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _migrationService = migrationService;
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
                    var dbContext = scope.ServiceProvider.GetRequiredService<VaultGuardDbContext>();
                    var dbContextApp = scope.ServiceProvider.GetRequiredService<VaultGuardDbContextApp>();

                    // Attempt to apply any pending migrations via migration service first
                    try
                    {
                        await _migrationService.ApplyPendingMigrationsAsync();
                    }
                    catch (Exception migEx)
                    {
                        _logger.LogWarning(migEx, "Failed to apply migrations via migration service during startup (continuing)");
                    }

                    // Check if the database exists and can connect
                    var canConnect = await dbContext.Database.CanConnectAsync();
                    var canConnectApp = await dbContextApp.Database.CanConnectAsync();

                    // Apply vault schema additions on EVERY startup for existing databases.
                    // This must run before any EF INSERT/UPDATE touching the Collections table.
                    if (canConnect)
                    {
                        await EnsureVaultSchemaAsync(dbContext);

                        // Every user needs a default "Personal" vault, and any password items
                        // created before vaults existed (or imported with no collection) need
                        // to be attached to it so they actually show up when that vault is opened.
                        await BackfillDefaultVaultsAsync(dbContext);
                    }

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

                        // Brand-new database: clear any stale seed marker left over from a previously
                        // deleted database so this fresh install is fully seeded (accounts + demo data).
                        TryDeleteSeedMarker(dbContext);

                        // Seed everything for new installations
                        await SeedIdentityDataIfNeeded(scope);
                        await SeedEssentialDataIfNeeded(dbContext);
                        await SeedTestDataIfNeeded(dbContext);
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

                            // Verify tables were created successfully
                            var tablesExistAfterMigration = await CheckIdentityTablesExistAsync(dbContextApp);
                            if (!tablesExistAfterMigration)
                            {
                                _logger.LogError("Identity tables still missing after migration, attempting EnsureCreated fallback");
                                await dbContextApp.Database.EnsureCreatedAsync();
                            }
                        }
                        catch (Exception migrationEx)
                        {
                            _logger.LogError(migrationEx, "Failed to create Identity tables via migration, trying EnsureCreated");
                            try
                            {
                                await dbContextApp.Database.EnsureCreatedAsync();
                                _logger.LogInformation("Database structure created using EnsureCreated fallback");
                            }
                            catch (Exception ensureEx)
                            {
                                _logger.LogError(ensureEx, "Failed to create database structure using EnsureCreated");
                                throw new InvalidOperationException("Could not initialize database: both migration and EnsureCreated failed", ensureEx);
                            }
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
                        try
                        {
                            await dbContext.Database.EnsureCreatedAsync();
                            await dbContextApp.Database.EnsureCreatedAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to ensure database structure for unconfigured database");
                            // Try migrations as fallback
                            await dbContext.Database.MigrateAsync();
                            await dbContextApp.Database.MigrateAsync();
                        }

                        // Seed everything for new/unconfigured installations
                        await SeedIdentityDataIfNeeded(scope);
                        await SeedEssentialDataIfNeeded(dbContext);
                        await SeedTestDataIfNeeded(dbContext);
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

                    // Seed essential data (categories, collections, tags) - required for app to function
                    await SeedEssentialDataIfNeeded(dbContext);

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

    /// <summary>
    /// Comprehensive schema compatibility guard — idempotent, safe on every startup.
    /// Detects and creates any tables or columns that exist in the EF Core model
    /// but are missing from the physical SQLite database (common when the DB was
    /// created before a feature was added without a proper migration).
    /// Must run BEFORE any EF Core INSERT/UPDATE that touches these tables.
    /// </summary>
    private async Task EnsureVaultSchemaAsync(VaultGuardDbContext dbContext)
    {
        try
        {
            var conn = dbContext.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync();

            // Helper: check whether a table exists
            async Task<bool> TableExists(string name)
            {
                using var c = conn.CreateCommand();
                c.CommandText = $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{name}'";
                return Convert.ToInt64(await c.ExecuteScalarAsync() ?? 0) > 0;
            }

            // Helper: check whether a column exists in a table
            async Task<bool> ColumnExists(string table, string column)
            {
                using var c = conn.CreateCommand();
                c.CommandText = $"PRAGMA table_info({table})";
                using var r = await c.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    if (string.Equals(r["name"]?.ToString(), column, StringComparison.OrdinalIgnoreCase))
                        return true;
                return false;
            }

            // Helper: run DDL silently
            async Task Exec(string sql)
            {
                using var c = conn.CreateCommand();
                c.CommandText = sql;
                await c.ExecuteNonQueryAsync();
            }

            // ── 1. Vault table ───────────────────────────────────────────────
            if (!await TableExists("Vault"))
            {
                _logger.LogInformation("Schema fix: creating Vault table");
                await Exec(@"CREATE TABLE IF NOT EXISTS Vault (
                    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name        TEXT    NOT NULL,
                    Description TEXT,
                    IsDefault   INTEGER NOT NULL DEFAULT 0,
                    Icon        TEXT,
                    Color       TEXT,
                    CreatedAt   TEXT    NOT NULL,
                    UpdatedAt   TEXT    NOT NULL,
                    UserId      TEXT    NOT NULL)");
            }

            // ── 2. VaultId column on Collections ─────────────────────────────
            if (await TableExists("Collections") && !await ColumnExists("Collections", "VaultId"))
            {
                _logger.LogInformation("Schema fix: adding VaultId to Collections");
                await Exec("ALTER TABLE Collections ADD COLUMN VaultId INTEGER NULL");
            }

            // ── 3. PasswordItemTags join table (PasswordItem ↔ Tag) ──────────
            // EF Core column convention: PasswordItemsId and TagsId
            if (!await TableExists("PasswordItemTags"))
            {
                _logger.LogInformation("Schema fix: creating PasswordItemTags join table");
                await Exec(@"CREATE TABLE IF NOT EXISTS PasswordItemTags (
                    PasswordItemsId INTEGER NOT NULL,
                    TagsId          INTEGER NOT NULL,
                    PRIMARY KEY (PasswordItemsId, TagsId))");
            }

            // ── 4. PasskeyItems table (if missing from older databases) ───────
            if (!await TableExists("PasskeyItems"))
            {
                _logger.LogInformation("Schema fix: creating PasskeyItems table");
                await Exec(@"CREATE TABLE IF NOT EXISTS PasskeyItems (
                    Id                     INTEGER PRIMARY KEY AUTOINCREMENT,
                    PasswordItemId         INTEGER NOT NULL,
                    UserId                 TEXT,
                    WebsiteUrl             TEXT,
                    Website                TEXT,
                    Username               TEXT,
                    DisplayName            TEXT,
                    CredentialId           TEXT,
                    PublicKey              TEXT,
                    SignatureCount         INTEGER NOT NULL DEFAULT 0,
                    IsBackedUp             INTEGER NOT NULL DEFAULT 0,
                    RequiresUserVerification INTEGER NOT NULL DEFAULT 0,
                    DeviceType             TEXT,
                    PlatformName           TEXT,
                    LastUsedAt             TEXT,
                    UsageCount             INTEGER NOT NULL DEFAULT 0,
                    Notes                  TEXT,
                    CreatedAt              TEXT    NOT NULL DEFAULT '',
                    LastModified           TEXT    NOT NULL DEFAULT '',
                    EncryptedCredentialId  TEXT,
                    CredentialIdNonce      TEXT,
                    CredentialIdAuthTag    TEXT)");
            }

            // ── 5. AuditLogs table ────────────────────────────────────────────
            if (!await TableExists("AuditLogs"))
            {
                _logger.LogInformation("Schema fix: creating AuditLogs table");
                await Exec(@"CREATE TABLE IF NOT EXISTS AuditLogs (
                    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId      TEXT,
                    Action      TEXT    NOT NULL DEFAULT '',
                    EntityType  TEXT,
                    EntityId    TEXT,
                    Details     TEXT,
                    IpAddress   TEXT,
                    UserAgent   TEXT,
                    CreatedAt   TEXT    NOT NULL DEFAULT '',
                    IsSuccess   INTEGER NOT NULL DEFAULT 1)");
            }

            // ── 6. Devices table ──────────────────────────────────────────────
            if (!await TableExists("Devices"))
            {
                _logger.LogInformation("Schema fix: creating Devices table");
                await Exec(@"CREATE TABLE IF NOT EXISTS Devices (
                    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId       TEXT    NOT NULL,
                    DeviceName   TEXT,
                    DeviceType   TEXT,
                    Platform     TEXT,
                    PushToken    TEXT,
                    IsActive     INTEGER NOT NULL DEFAULT 1,
                    LastSeen     TEXT,
                    CreatedAt    TEXT    NOT NULL DEFAULT '',
                    UpdatedAt    TEXT    NOT NULL DEFAULT '')");
            }

            // ── 7. UserBackupSettings table ───────────────────────────────────
            if (!await TableExists("UserBackupSettings"))
            {
                _logger.LogInformation("Schema fix: creating UserBackupSettings table");
                await Exec(@"CREATE TABLE IF NOT EXISTS UserBackupSettings (
                    Id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId              TEXT    NOT NULL,
                    IsEnabled           INTEGER NOT NULL DEFAULT 0,
                    BackupFrequencyDays INTEGER NOT NULL DEFAULT 7,
                    LastBackupAt        TEXT,
                    BackupPath          TEXT,
                    CreatedAt           TEXT    NOT NULL DEFAULT '',
                    UpdatedAt           TEXT    NOT NULL DEFAULT '')");
            }

            // ── 8. ExpiresAt column on UserTwoFactorBackupCodes (recovery-code expiry) ──
            if (await TableExists("UserTwoFactorBackupCodes") && !await ColumnExists("UserTwoFactorBackupCodes", "ExpiresAt"))
            {
                _logger.LogInformation("Schema fix: adding ExpiresAt to UserTwoFactorBackupCodes");
                await Exec("ALTER TABLE UserTwoFactorBackupCodes ADD COLUMN ExpiresAt TEXT NULL");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "EnsureVaultSchemaAsync encountered an error — app will continue but some features may be unavailable");
        }
    }

    /// <summary>
    /// Ensures every user who has at least one password item (or an account) has a default
    /// "Personal" vault, and reassigns any orphaned items (CollectionId == null — created before
    /// vaults existed, or imported without one) into that vault's default collection.
    /// Idempotent and safe to run on every startup.
    /// </summary>
    private async Task BackfillDefaultVaultsAsync(VaultGuardDbContext dbContext)
    {
        try
        {
            var itemUserIds = await dbContext.PasswordItems
                .Where(p => !string.IsNullOrEmpty(p.UserId))
                .Select(p => p.UserId!)
                .Distinct()
                .ToListAsync();

            var accountUserIds = await dbContext.Users.Select(u => u.Id).ToListAsync();

            var userIds = itemUserIds.Union(accountUserIds).Where(id => !string.IsNullOrEmpty(id)).Distinct();

            foreach (var userId in userIds)
            {
                var vault = await dbContext.Vaults.FirstOrDefaultAsync(v => v.UserId == userId && v.IsDefault)
                         ?? await dbContext.Vaults.FirstOrDefaultAsync(v => v.UserId == userId);

                if (vault == null)
                {
                    vault = new Vault
                    {
                        Name = "Personal",
                        Description = "Your personal password vault",
                        IsDefault = true,
                        Icon = "🔐",
                        Color = "#2563EB",
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    dbContext.Vaults.Add(vault);
                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("Backfill: created default Personal vault for user {UserId}", userId);
                }

                var defaultCollection = await dbContext.Collections.FirstOrDefaultAsync(c => c.VaultId == vault.Id && c.IsDefault)
                                      ?? await dbContext.Collections.FirstOrDefaultAsync(c => c.VaultId == vault.Id);

                if (defaultCollection == null)
                {
                    defaultCollection = new Collection
                    {
                        Name = vault.Name,
                        Description = vault.Description,
                        Icon = vault.Icon,
                        Color = vault.Color,
                        IsDefault = true,
                        VaultId = vault.Id,
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow
                    };
                    dbContext.Collections.Add(defaultCollection);
                    await dbContext.SaveChangesAsync();
                }

                var orphanItems = await dbContext.PasswordItems
                    .Where(p => p.UserId == userId && p.CollectionId == null)
                    .ToListAsync();

                if (orphanItems.Count > 0)
                {
                    foreach (var item in orphanItems)
                        item.CollectionId = defaultCollection.Id;

                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("Backfill: attached {Count} orphaned item(s) to the Personal vault for user {UserId}", orphanItems.Count, userId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "BackfillDefaultVaultsAsync encountered an error — app will continue but some items may be missing from vaults");
        }
    }

    private async Task SeedEssentialDataIfNeeded(VaultGuardDbContext dbContext)
    {
        try
        {
            // Schema guard runs here too as a safety net (primary call is in InitializeDatabaseAsync)
            await EnsureVaultSchemaAsync(dbContext);

            // Essential data (categories, collections, tags) is required for the app to function
            // This should be seeded even if password items exist
            _logger.LogInformation("Checking if essential data (categories, collections, tags) needs to be seeded");

            var hasCategoriesSeed = await dbContext.Categories.AnyAsync(c => c.UserId == TestDataSeeder.TestUserId);
            var hasCollections = await dbContext.Collections.AnyAsync(c => c.UserId == TestDataSeeder.TestUserId);
            var hasTags = await dbContext.Tags.AnyAsync(t => t.UserId == TestDataSeeder.TestUserId);

            if (!hasCategoriesSeed || !hasCollections || !hasTags)
            {
                _logger.LogInformation("Essential data missing, seeding now");
                
                // Get or create test user using shared constant
                var testUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == TestDataSeeder.TestUserId);
                if (testUser == null)
                {
                    _logger.LogInformation("Creating test user for essential data");
                    testUser = new Models.ApplicationUser
                    {
                        Id = TestDataSeeder.TestUserId,
                        UserName = "testuser@example.com",
                        Email = "testuser@example.com",
                        EmailConfirmed = true,
                        IsActive = true
                    };
                    dbContext.Users.Add(testUser);
                    await dbContext.SaveChangesAsync();
                }

                // Seed collections if missing
                if (!hasCollections)
                {
                    _logger.LogInformation("Seeding collections");
                    TestDataSeeder.SeedCollections(dbContext, TestDataSeeder.TestUserId);
                }

                // Seed categories if missing
                if (!hasCategoriesSeed)
                {
                    _logger.LogInformation("Seeding categories");
                    TestDataSeeder.SeedCategories(dbContext, TestDataSeeder.TestUserId);
                }

                // Seed tags if missing
                if (!hasTags)
                {
                    _logger.LogInformation("Seeding tags");
                    TestDataSeeder.SeedTags(dbContext, TestDataSeeder.TestUserId);
                }

                _logger.LogInformation("Essential data seeding completed successfully");
            }
            else
            {
                _logger.LogDebug("Essential data already exists, skipping seeding");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding essential data");
            // Don't throw - seeding failure shouldn't prevent app startup
        }
    }

    private async Task SeedTestDataIfNeeded(VaultGuardDbContext dbContext)
    {
        try
        {
            var markerPath = GetSeedMarkerPath(dbContext);

            // Already populated: record that this database has been seeded (so a later manual
            // "Clear Vault Data" stays cleared) and skip.
            if (await dbContext.PasswordItems.AnyAsync())
            {
                TryWriteSeedMarker(markerPath);
                _logger.LogDebug("Database already contains password items, skipping test data seeding");
                return;
            }

            // Empty vault: only auto-seed demo data the FIRST time a database is created. If the
            // marker already exists the user deliberately cleared their data, so leave it empty.
            if (markerPath != null && System.IO.File.Exists(markerPath))
            {
                _logger.LogInformation("Vault is empty but seed marker present (data was cleared) - skipping re-seed");
                return;
            }

            _logger.LogInformation("Seeding test data to populate empty database");
            TestDataSeeder.SeedTestData(dbContext);
            TryWriteSeedMarker(markerPath);
            _logger.LogInformation("Test data seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding test data");
            // Don't throw - seeding failure shouldn't prevent app startup
        }
    }

    // Path of a small marker file next to the SQLite database that records the demo data has
    // already been seeded once. Returns null for non-file databases (server providers / in-memory).
    private string? GetSeedMarkerPath(VaultGuardDbContext dbContext)
    {
        try
        {
            var providerName = dbContext.Database.ProviderName ?? string.Empty;
            if (!providerName.Contains("Sqlite", System.StringComparison.OrdinalIgnoreCase))
                return null;

            var connectionString = dbContext.Database.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString)) return null;

            foreach (var part in connectionString.Split(';', System.StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith("Data Source=", System.StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("DataSource=", System.StringComparison.OrdinalIgnoreCase))
                {
                    var path = trimmed[(trimmed.IndexOf('=') + 1)..].Trim();
                    if (string.IsNullOrEmpty(path) || path.Equals(":memory:", System.StringComparison.OrdinalIgnoreCase))
                        return null;
                    return path + ".seeded";
                }
            }
        }
        catch { /* best-effort */ }
        return null;
    }

    private void TryWriteSeedMarker(string? markerPath)
    {
        try
        {
            if (markerPath != null && !System.IO.File.Exists(markerPath))
                System.IO.File.WriteAllText(markerPath, System.DateTime.UtcNow.ToString("o"));
        }
        catch { /* marker is best-effort; never block startup */ }
    }

    // Removes a stale "<db>.seeded" marker (e.g. left behind after the database file was deleted)
    // so a brand-new database is seeded normally instead of being treated as already-seeded.
    private void TryDeleteSeedMarker(VaultGuardDbContext dbContext)
    {
        try
        {
            var markerPath = GetSeedMarkerPath(dbContext);
            if (markerPath != null && System.IO.File.Exists(markerPath))
                System.IO.File.Delete(markerPath);
        }
        catch { /* best-effort */ }
    }

    private async Task SeedIdentityDataIfNeeded(IServiceScope scope)
    {
        // Try to get the Identity seeder (may not be available in all configurations)
        var identitySeeder = scope.ServiceProvider.GetService<VaultGuard.DAL.Seed.IdentityDataSeeder>();
        if (identitySeeder == null)
        {
            _logger.LogWarning("IdentityDataSeeder could not be resolved - default accounts (admin/parent/user/child) were NOT created");
            return;
        }

        try
        {
            _logger.LogInformation("Seeding Identity data (roles and default users)");
            await identitySeeder.SeedAsync();
            _logger.LogInformation("Identity data seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding Identity data");
            // Don't throw - seeding failure shouldn't prevent app startup
        }

        // Verify the default accounts actually landed. If the Identity schema wasn't ready the first
        // time (a known dual-context migration timing issue), ensure it and retry the seed once.
        try
        {
            var ctx = scope.ServiceProvider.GetRequiredService<VaultGuardDbContext>();
            var userCount = await ctx.Users.CountAsync();
            if (userCount == 0)
            {
                _logger.LogWarning("No user accounts found after identity seeding - ensuring Identity schema and retrying once");
                try
                {
                    await scope.ServiceProvider.GetRequiredService<VaultGuardDbContextApp>().Database.MigrateAsync();
                }
                catch (Exception migEx)
                {
                    _logger.LogWarning(migEx, "Could not apply Identity migrations before retry");
                }

                await identitySeeder.SeedAsync();
                userCount = await ctx.Users.CountAsync();
            }

            _logger.LogInformation("Default account check complete. User accounts in database: {Count}", userCount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not verify default accounts after seeding");
        }
    }

    /// <summary>
    /// Checks if ASP.NET Core Identity tables exist in the database
    /// This is crucial for ensuring the reported issue is resolved
    /// </summary>
    private async Task<bool> CheckIdentityTablesExistAsync(VaultGuardDbContextApp dbContext)
    {
        try
        {
            // Check if the database can be connected to first
            if (!await dbContext.Database.CanConnectAsync())
            {
                _logger.LogWarning("Cannot connect to database, Identity tables check skipped");
                return false;
            }

            // Use a more reliable method to check for table existence in SQLite
            // Query sqlite_master to check if AspNetUsers table exists
            using var connection = dbContext.Database.GetDbConnection();
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='AspNetUsers'";
            var result = await command.ExecuteScalarAsync();
            var aspNetUsersExists = Convert.ToInt32(result) > 0;

            if (!aspNetUsersExists)
            {
                _logger.LogWarning("AspNetUsers table does not exist in the database");
                return false;
            }

            // Double-check by querying the Users table (this will throw if table doesn't exist)
            var userCount = await dbContext.Users.CountAsync();

            _logger.LogInformation("Identity tables check passed: AspNetUsers table exists (user count: {UserCount})", userCount);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Identity tables do not exist or are not accessible: {Message}", ex.Message);
            return false;
        }
    }
}
