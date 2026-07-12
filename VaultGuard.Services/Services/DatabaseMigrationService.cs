using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VaultGuard.DAL;
using VaultGuard.Services.DTOs;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.Services.Services
{
    /// <summary>
    /// Service for managing database migrations safely. There is now a single application
    /// <see cref="VaultGuardDbContext"/> (Identity + vault), so this operates on one context.
    /// </summary>
    public class DatabaseMigrationService : IDatabaseMigrationService
    {
        private readonly VaultGuardDbContext _context;
        private readonly ILogger<DatabaseMigrationService> _logger;

        private const string InMemoryProviderName = "Microsoft.EntityFrameworkCore.InMemory";

        public DatabaseMigrationService(
            VaultGuardDbContext context,
            ILogger<DatabaseMigrationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<MigrationStatusDto> GetMigrationStatusAsync()
        {
            try
            {
                var pending = await _context.Database.GetPendingMigrationsAsync();
                var applied = await _context.Database.GetAppliedMigrationsAsync();

                return new MigrationStatusDto
                {
                    HasPendingMigrations = pending.Any(),
                    PendingMigrations = pending,
                    AppliedMigrations = applied,
                    IsDatabaseCreated = await _context.Database.CanConnectAsync()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking migration status");
                return new MigrationStatusDto
                {
                    HasPendingMigrations = false,
                    PendingMigrations = new List<string>(),
                    AppliedMigrations = new List<string>(),
                    IsDatabaseCreated = false
                };
            }
        }

        public async Task<MigrationResultDto> ApplyPendingMigrationsAsync()
        {
            var appliedMigrations = new List<string>();

            try
            {
                // InMemory provider doesn't support migrations.
                if (_context.Database.ProviderName == InMemoryProviderName)
                {
                    _logger.LogInformation("Using InMemory database provider - migrations not supported");
                    return new MigrationResultDto
                    {
                        Success = true,
                        Message = "InMemory database does not require migrations",
                        AppliedMigrations = appliedMigrations
                    };
                }

                var pending = await _context.Database.GetPendingMigrationsAsync();
                if (pending.Any())
                {
                    _logger.LogInformation("Applying {Count} pending migrations for VaultGuardDbContext", pending.Count());
                    await _context.Database.MigrateAsync();
                    appliedMigrations.AddRange(pending);
                    _logger.LogInformation("Successfully applied migrations: {Migrations}", string.Join(", ", pending));
                }

                if (!appliedMigrations.Any())
                {
                    // Ensure critical junction tables exist even if there were no migrations applied
                    try
                    {
                        await EnsurePasswordItemTagsTableExistsAsync();
                    }
                    catch (Exception exEnsure)
                    {
                        _logger.LogWarning(exEnsure, "Failed to ensure PasswordItemTags table exists");
                    }
                    return new MigrationResultDto
                    {
                        Success = true,
                        Message = "No pending migrations found",
                        AppliedMigrations = appliedMigrations
                    };
                }

                return new MigrationResultDto
                {
                    Success = true,
                    Message = $"Successfully applied {appliedMigrations.Count} migrations",
                    AppliedMigrations = appliedMigrations
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying migrations");
                return new MigrationResultDto
                {
                    Success = false,
                    Message = $"Error applying migrations: {ex.Message}",
                    AppliedMigrations = appliedMigrations,
                    Exception = ex
                };
            }
        }

        public async Task EnsurePasswordItemTagsTableExistsAsync()
        {
            // Use a safe CREATE TABLE IF NOT EXISTS fallback for SQLite
            try
            {
                var sql = @"CREATE TABLE IF NOT EXISTS PasswordItemTags (
    PasswordItemsId INTEGER NOT NULL,
    TagsId INTEGER NOT NULL,
    PRIMARY KEY (PasswordItemsId, TagsId),
    FOREIGN KEY (PasswordItemsId) REFERENCES PasswordItems (Id) ON DELETE CASCADE,
    FOREIGN KEY (TagsId) REFERENCES Tags (Id) ON DELETE CASCADE
);";

                await _context.Database.ExecuteSqlRawAsync(sql);
                _logger.LogInformation("Ensured PasswordItemTags table exists (SQL fallback executed)");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not ensure PasswordItemTags table via SQL fallback");
                throw;
            }
        }

        public async Task<MigrationResultDto> CreateDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("Creating database schema");

                var created = await _context.Database.EnsureCreatedAsync();
                var message = created ? "Database schema created successfully" : "Database already exists";

                _logger.LogInformation("Database creation completed: {Message}", message);

                return new MigrationResultDto
                {
                    Success = true,
                    Message = message,
                    AppliedMigrations = new List<string>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating database");
                return new MigrationResultDto
                {
                    Success = false,
                    Message = $"Error creating database: {ex.Message}",
                    AppliedMigrations = new List<string>(),
                    Exception = ex
                };
            }
        }

        public async Task<IEnumerable<string>> GetAppliedMigrationsAsync()
        {
            try
            {
                return await _context.Database.GetAppliedMigrationsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting applied migrations");
                return new List<string>();
            }
        }

        public async Task<IEnumerable<string>> GetPendingMigrationsAsync()
        {
            try
            {
                return await _context.Database.GetPendingMigrationsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending migrations");
                return new List<string>();
            }
        }
    }
}
