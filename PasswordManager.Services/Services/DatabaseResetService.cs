using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PasswordManager.DAL;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.Services.Services;

/// <summary>
/// Service for resetting database tables
/// </summary>
public class DatabaseResetService : IDatabaseResetService
{
    private readonly PasswordManagerDbContext _dbContext;
    private readonly ILogger<DatabaseResetService> _logger;

    // User-related tables that should be preserved when resetting data
    private readonly List<string> _userTables = new()
    {
        "AspNetUsers",
        "AspNetRoles",
        "AspNetUserRoles",
        "AspNetUserClaims",
        "AspNetRoleClaims",
        "AspNetUserLogins",
        "AspNetUserTokens",
        "UserProfiles",
        "VaultSessions"
    };

    // Data tables that will be cleared in data reset
    private readonly List<string> _dataTables = new()
    {
        "PasswordItems",
        "LoginItems",
        "Collections",
        "Categories",
        "Tags",
        "PasswordItemTags",
        "CustomFields",
        "Passkeys",
        "SharedPasswords",
        "SharedPasswordPermissions",
        "PasswordHistory",
        "AuditLogs"
    };

    public DatabaseResetService(
        PasswordManagerDbContext dbContext,
        ILogger<DatabaseResetService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<DatabaseResetInfo> GetResetInfoAsync()
    {
        return new DatabaseResetInfo
        {
            DataTables = new List<string>(_dataTables),
            UserTables = new List<string>(_userTables),
            SystemTables = new List<string> { "__EFMigrationsHistory" }
        };
    }

    public async Task<DatabaseResetResult> ResetDataTablesAsync()
    {
        var result = new DatabaseResetResult();
        
        try
        {
            _logger.LogInformation("Starting data tables reset (preserving user tables)");
            
            var tablesCleared = 0;
            var recordsDeleted = 0;

            // Disable foreign key constraints temporarily
            await DisableForeignKeyConstraintsAsync();

            try
            {
                // Clear data tables in order (respecting dependencies)
                var tablesToClear = new[]
                {
                    "PasswordItemTags",       // Junction table first
                    "SharedPasswordPermissions",
                    "SharedPasswords",
                    "PasswordHistory",
                    "CustomFields",
                    "Passkeys",
                    "LoginItems",
                    "PasswordItems",          // Main items after dependencies
                    "Categories",
                    "Tags",
                    "Collections",
                    "AuditLogs"
                };

                foreach (var table in tablesToClear)
                {
                    // Validate table name against whitelist to prevent SQL injection
                    if (!IsValidTableName(table))
                    {
                        _logger.LogWarning("Attempted to clear invalid table name: {Table}", table);
                        result.Errors.Add($"Invalid table name: {table}");
                        continue;
                    }

                    try
                    {
                        // Table name is validated against whitelist, safe to use in SQL
                        var count = await _dbContext.Database.ExecuteSqlRawAsync($"DELETE FROM {table}");
                        tablesCleared++;
                        recordsDeleted += count;
                        _logger.LogInformation("Cleared table {Table}, deleted {Count} records", table, count);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not clear table {Table}", table);
                        result.Errors.Add($"Failed to clear {table}: {ex.Message}");
                    }
                }

                result.Success = tablesCleared > 0;
                result.TablesCleared = tablesCleared;
                result.RecordsDeleted = recordsDeleted;
                result.Message = $"Successfully cleared {tablesCleared} data tables, deleted {recordsDeleted} records. User accounts preserved.";
                
                _logger.LogInformation("Data tables reset completed successfully");
            }
            finally
            {
                // Re-enable foreign key constraints
                await EnableForeignKeyConstraintsAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during data tables reset");
            result.Success = false;
            result.Message = $"Failed to reset data tables: {ex.Message}";
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    public async Task<DatabaseResetResult> ResetAllTablesAsync(bool reseedData = true)
    {
        var result = new DatabaseResetResult();
        
        try
        {
            _logger.LogInformation("Starting full database reset (including user tables)");
            
            var tablesCleared = 0;
            var recordsDeleted = 0;

            // Disable foreign key constraints temporarily
            await DisableForeignKeyConstraintsAsync();

            try
            {
                // Clear all tables in reverse dependency order
                var allTables = new[]
                {
                    // Data tables first
                    "PasswordItemTags",
                    "SharedPasswordPermissions",
                    "SharedPasswords",
                    "PasswordHistory",
                    "CustomFields",
                    "Passkeys",
                    "LoginItems",
                    "PasswordItems",
                    "Categories",
                    "Tags",
                    "Collections",
                    "AuditLogs",
                    // User tables
                    "VaultSessions",
                    "AspNetUserTokens",
                    "AspNetUserLogins",
                    "AspNetUserClaims",
                    "AspNetUserRoles",
                    "AspNetRoleClaims",
                    "UserProfiles",
                    "AspNetUsers",
                    "AspNetRoles"
                };

                foreach (var table in allTables)
                {
                    // Validate table name against whitelist to prevent SQL injection
                    if (!IsValidTableName(table))
                    {
                        _logger.LogWarning("Attempted to clear invalid table name: {Table}", table);
                        result.Errors.Add($"Invalid table name: {table}");
                        continue;
                    }

                    try
                    {
                        // Table name is validated against whitelist, safe to use in SQL
                        var count = await _dbContext.Database.ExecuteSqlRawAsync($"DELETE FROM {table}");
                        tablesCleared++;
                        recordsDeleted += count;
                        _logger.LogInformation("Cleared table {Table}, deleted {Count} records", table, count);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not clear table {Table}", table);
                        result.Errors.Add($"Failed to clear {table}: {ex.Message}");
                    }
                }

                // Reseed default data if requested
                if (reseedData)
                {
                    await ReseedDefaultDataAsync();
                }

                result.Success = tablesCleared > 0;
                result.TablesCleared = tablesCleared;
                result.RecordsDeleted = recordsDeleted;
                result.Message = reseedData 
                    ? $"Successfully reset all {tablesCleared} tables, deleted {recordsDeleted} records, and reseeded default data."
                    : $"Successfully reset all {tablesCleared} tables, deleted {recordsDeleted} records.";
                
                _logger.LogInformation("Full database reset completed successfully");
            }
            finally
            {
                // Re-enable foreign key constraints
                await EnableForeignKeyConstraintsAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during full database reset");
            result.Success = false;
            result.Message = $"Failed to reset database: {ex.Message}";
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    private async Task DisableForeignKeyConstraintsAsync()
    {
        try
        {
            // SQLite syntax
            await _dbContext.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not disable foreign key constraints");
        }
    }

    private async Task EnableForeignKeyConstraintsAsync()
    {
        try
        {
            // SQLite syntax
            await _dbContext.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not enable foreign key constraints");
        }
    }

    private async Task ReseedDefaultDataAsync()
    {
        try
        {
            // Use GUIDs for IDs to avoid conflicts
            var adminRoleId = Guid.NewGuid().ToString();
            var userRoleId = Guid.NewGuid().ToString();

            // Create default admin role
            await _dbContext.Database.ExecuteSqlRawAsync(
                $"INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) " +
                $"VALUES ('{adminRoleId}', 'Admin', 'ADMIN', '{Guid.NewGuid()}')");

            // Create default user role
            await _dbContext.Database.ExecuteSqlRawAsync(
                $"INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) " +
                $"VALUES ('{userRoleId}', 'User', 'USER', '{Guid.NewGuid()}')");

            // Create default collections
            await _dbContext.Database.ExecuteSqlRawAsync(
                "INSERT INTO Collections (Name, Description, Icon, Color, IsDefault, CreatedAt) " +
                "VALUES ('Personal', 'Personal passwords and credentials', '📱', '#3b82f6', 1, datetime('now'))");

            await _dbContext.Database.ExecuteSqlRawAsync(
                "INSERT INTO Collections (Name, Description, Icon, Color, IsDefault, CreatedAt) " +
                "VALUES ('Work', 'Work-related passwords', '💼', '#10b981', 0, datetime('now'))");

            _logger.LogInformation("Reseeded default data successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not reseed default data");
        }
    }

    /// <summary>
    /// Validates that a table name is in the allowed whitelist
    /// </summary>
    private bool IsValidTableName(string tableName)
    {
        var allAllowedTables = _dataTables.Concat(_userTables).ToList();
        return allAllowedTables.Contains(tableName, StringComparer.OrdinalIgnoreCase);
    }
}
