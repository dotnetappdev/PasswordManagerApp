using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PasswordManager.DAL;
using PasswordManager.Services.DTOs;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.Services.Services
{
    /// <summary>
    /// Service for managing database migrations safely
    /// </summary>
    public class DatabaseMigrationService : IDatabaseMigrationService
    {
        private readonly PasswordManagerDbContextApp _contextApp;
        private readonly PasswordManagerDbContext _context;
        private readonly ILogger<DatabaseMigrationService> _logger;

        public DatabaseMigrationService(
            PasswordManagerDbContextApp contextApp,
            PasswordManagerDbContext context,
            ILogger<DatabaseMigrationService> logger)
        {
            _contextApp = contextApp;
            _context = context;
            _logger = logger;
        }

        public async Task<MigrationStatusDto> GetMigrationStatusAsync()
        {
            try
            {
                var pendingMigrationsApp = await _contextApp.Database.GetPendingMigrationsAsync();
                var appliedMigrationsApp = await _contextApp.Database.GetAppliedMigrationsAsync();
                var pendingMigrationsApi = await _context.Database.GetPendingMigrationsAsync();
                var appliedMigrationsApi = await _context.Database.GetAppliedMigrationsAsync();

                var allPending = pendingMigrationsApp.Concat(pendingMigrationsApi).Distinct();
                var allApplied = appliedMigrationsApp.Concat(appliedMigrationsApi).Distinct();

                return new MigrationStatusDto
                {
                    HasPendingMigrations = allPending.Any(),
                    PendingMigrations = allPending,
                    AppliedMigrations = allApplied,
                    IsDatabaseCreated = await _contextApp.Database.CanConnectAsync() && await _context.Database.CanConnectAsync()
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
                // Apply migrations for PasswordManagerDbContextApp
                var pendingMigrationsApp = await _contextApp.Database.GetPendingMigrationsAsync();
                if (pendingMigrationsApp.Any())
                {
                    _logger.LogInformation("Applying {Count} pending migrations for PasswordManagerDbContextApp", pendingMigrationsApp.Count());
                    await _contextApp.Database.MigrateAsync();
                    appliedMigrations.AddRange(pendingMigrationsApp);
                    _logger.LogInformation("Successfully applied migrations for PasswordManagerDbContextApp: {Migrations}", string.Join(", ", pendingMigrationsApp));
                }

                // Apply migrations for PasswordManagerDbContext
                var pendingMigrationsApi = await _context.Database.GetPendingMigrationsAsync();
                if (pendingMigrationsApi.Any())
                {
                    _logger.LogInformation("Applying {Count} pending migrations for PasswordManagerDbContext", pendingMigrationsApi.Count());
                    await _context.Database.MigrateAsync();
                    appliedMigrations.AddRange(pendingMigrationsApi);
                    _logger.LogInformation("Successfully applied migrations for PasswordManagerDbContext: {Migrations}", string.Join(", ", pendingMigrationsApi));
                }

                if (!appliedMigrations.Any())
                {
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

        public async Task<IEnumerable<string>> GetAppliedMigrationsAsync()
        {
            try
            {
                var appliedMigrationsApp = await _contextApp.Database.GetAppliedMigrationsAsync();
                var appliedMigrationsApi = await _context.Database.GetAppliedMigrationsAsync();
                return appliedMigrationsApp.Concat(appliedMigrationsApi).Distinct();
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
                var pendingMigrationsApp = await _contextApp.Database.GetPendingMigrationsAsync();
                var pendingMigrationsApi = await _context.Database.GetPendingMigrationsAsync();
                return pendingMigrationsApp.Concat(pendingMigrationsApi).Distinct();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending migrations");
                return new List<string>();
            }
        }
    }
}
