using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PasswordManager.DAL;
using PasswordManager.Services.Interfaces;
using System.Security.Cryptography;

namespace PasswordManager.Services.Services;

/// <summary>
/// Service for resetting database tables
/// </summary>
public class DatabaseResetService : IDatabaseResetService
{
    private readonly PasswordManagerDbContext _dbContext;
    private readonly ILogger<DatabaseResetService> _logger;
    private readonly IDatabaseConfigurationService? _databaseConfigurationService;

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

    // Data tables that will be cleared in data reset. Includes every per-item child table so
    // nothing (credit cards, secure notes, wifi, passkeys, …) is left orphaned behind a deleted item.
    private readonly List<string> _dataTables = new()
    {
        "PasswordItems",
        "LoginItems",
        "CreditCardItems",
        "SecureNoteItems",
        "WiFiItems",
        "PasskeyItem",
        "PasskeyItems",
        "Collections",
        "Categories",
        "Tags",
        "Vaults",
        "ApiKeys",
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
        ILogger<DatabaseResetService> logger,
        IDatabaseConfigurationService? databaseConfigurationService = null)
    {
        _dbContext = dbContext;
        _logger = logger;
        _databaseConfigurationService = databaseConfigurationService;
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
                // Clear data tables in order (respecting dependencies). All per-item child
                // tables are cleared before PasswordItems so no orphaned rows linger.
                var tablesToClear = new[]
                {
                    "PasswordItemTags",       // Junction table first
                    "SharedPasswordPermissions",
                    "SharedPasswords",
                    "PasswordHistory",
                    "CustomFields",
                    "Passkeys",
                    "LoginItems",
                    "CreditCardItems",
                    "SecureNoteItems",
                    "WiFiItems",
                    "PasskeyItem",
                    "PasskeyItems",
                    "PasswordItems",          // Main items after dependencies
                    "Categories",
                    "Tags",
                    "Collections",
                    "Vaults",
                    "ApiKeys",
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

                // Record that this database has been seeded so startup does not re-add demo data
                // after the user has deliberately cleared their vault.
                TryMarkSeedComplete();

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

    public async Task<DatabaseResetResult> ClearNonAdminUsersAsync()
    {
        var result = new DatabaseResetResult();

        // Set of user ids that are NOT in the Admin role. Reused as a sub-query
        // in every DELETE so admin accounts (and their data) are never touched.
        const string nonAdminFilter =
            "SELECT Id FROM AspNetUsers WHERE Id NOT IN (" +
            "SELECT ur.UserId FROM AspNetUserRoles ur " +
            "INNER JOIN AspNetRoles r ON ur.RoleId = r.Id " +
            "WHERE r.NormalizedName = 'ADMIN')";

        try
        {
            _logger.LogInformation("Starting non-admin user purge (preserving Admin accounts)");

            var recordsDeleted = 0;

            // Local helper: run a DELETE, swallow failures for tables/columns that
            // may not exist on every provider/schema, and accumulate the row count.
            async Task DeleteAsync(string sql, string label)
            {
                try
                {
                    var count = await _dbContext.Database.ExecuteSqlRawAsync(sql);
                    recordsDeleted += count;
                    _logger.LogInformation("Cleared {Label}, deleted {Count} records", label, count);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not clear {Label}", label);
                    result.Errors.Add($"Failed to clear {label}: {ex.Message}");
                }
            }

            await DisableForeignKeyConstraintsAsync();
            try
            {
                // Junction rows for the affected users' password items first.
                await DeleteAsync(
                    $"DELETE FROM PasswordItemTags WHERE PasswordItemId IN " +
                    $"(SELECT Id FROM PasswordItems WHERE UserId IN ({nonAdminFilter}))",
                    "PasswordItemTags");

                // Vault data and other rows owned directly by a UserId column.
                var userScopedTables = new[]
                {
                    "SharedPasswordPermissions", "SharedPasswords", "PasswordHistory",
                    "CustomFields", "Passkeys", "PasskeyItem", "PasskeyItems",
                    "LoginItems", "CreditCardItems",
                    "SecureNoteItems", "WiFiItems", "PasswordItems",
                    "Categories", "Tags", "Collections", "Vaults",
                    "AuditLogs", "Devices", "ApiKeys", "SmsSettings",
                    "UserPasskeys", "UserTwoFactorBackupCodes",
                    "VaultSessions", "UserProfiles"
                };
                foreach (var table in userScopedTables)
                {
                    await DeleteAsync(
                        $"DELETE FROM {table} WHERE UserId IN ({nonAdminFilter})", table);
                }

                // Identity link rows for the affected users.
                foreach (var table in new[] { "AspNetUserRoles", "AspNetUserClaims", "AspNetUserLogins", "AspNetUserTokens" })
                {
                    await DeleteAsync(
                        $"DELETE FROM {table} WHERE UserId IN ({nonAdminFilter})", table);
                }

                // Finally the user accounts themselves.
                int usersDeleted = 0;
                try
                {
                    usersDeleted = await _dbContext.Database.ExecuteSqlRawAsync(
                        $"DELETE FROM AspNetUsers WHERE Id IN ({nonAdminFilter})");
                    recordsDeleted += usersDeleted;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Could not delete non-admin user accounts");
                    result.Errors.Add($"Failed to delete user accounts: {ex.Message}");
                }

                result.Success = result.Errors.Count == 0;
                result.RecordsDeleted = recordsDeleted;
                result.Message = result.Success
                    ? $"Removed {usersDeleted} non-admin user account(s) and {recordsDeleted - usersDeleted} related record(s). Admin accounts preserved."
                    : $"Removed {usersDeleted} non-admin user account(s), but some related data could not be cleared.";

                _logger.LogInformation("Non-admin user purge completed. Users removed: {Users}", usersDeleted);
            }
            finally
            {
                await EnableForeignKeyConstraintsAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during non-admin user purge");
            result.Success = false;
            result.Message = $"Failed to clear non-admin users: {ex.Message}";
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    private async Task DisableForeignKeyConstraintsAsync()
    {
        try
        {
            var providerName = _dbContext.Database.ProviderName ?? string.Empty;
            
            if (providerName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                await _dbContext.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF");
            }
            else if (providerName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                // SQL Server: Disable constraints for all tables
                await _dbContext.Database.ExecuteSqlRawAsync("EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'");
            }
            else if (providerName.Contains("MySql", StringComparison.OrdinalIgnoreCase))
            {
                // MySQL: Disable foreign key checks
                await _dbContext.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 0");
            }
            else if (providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) || 
                     providerName.Contains("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            {
                // PostgreSQL: Set constraints to deferred
                await _dbContext.Database.ExecuteSqlRawAsync("SET CONSTRAINTS ALL DEFERRED");
            }
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
            var providerName = _dbContext.Database.ProviderName ?? string.Empty;
            
            if (providerName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                await _dbContext.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON");
            }
            else if (providerName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                // SQL Server: Enable constraints for all tables
                await _dbContext.Database.ExecuteSqlRawAsync("EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL'");
            }
            else if (providerName.Contains("MySql", StringComparison.OrdinalIgnoreCase))
            {
                // MySQL: Enable foreign key checks
                await _dbContext.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 1");
            }
            else if (providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) || 
                     providerName.Contains("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            {
                // PostgreSQL: Set constraints to immediate
                await _dbContext.Database.ExecuteSqlRawAsync("SET CONSTRAINTS ALL IMMEDIATE");
            }
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

    public async Task<DatabaseResetResult> SecureWipeDatabaseAsync()
    {
        var result = new DatabaseResetResult();
        
        try
        {
            _logger.LogInformation("Starting secure database wipe");
            
            // First, reset all tables
            var resetResult = await ResetAllTablesAsync(reseedData: false);
            if (!resetResult.Success)
            {
                result.Success = false;
                result.Message = $"Failed to reset tables before wipe: {resetResult.Message}";
                result.Errors.AddRange(resetResult.Errors);
                return result;
            }

            // Get database provider type
            var providerName = _dbContext.Database.ProviderName ?? string.Empty;
            
            if (providerName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                // For SQLite, close connection and securely delete the file
                var connectionString = _dbContext.Database.GetConnectionString();
                if (!string.IsNullOrEmpty(connectionString))
                {
                    // Extract database path from connection string
                    var dbPath = ExtractSqlitePath(connectionString);
                    
                    if (!string.IsNullOrEmpty(dbPath) && File.Exists(dbPath))
                    {
                        // Close all connections
                        await _dbContext.Database.CloseConnectionAsync();
                        
                        // Wait a moment for connections to fully close
                        await Task.Delay(500);
                        
                        try
                        {
                            // Securely overwrite the file before deletion
                            await SecureDeleteFileAsync(dbPath);
                            
                            result.Success = true;
                            result.Message = $"Database securely wiped. Tables cleared: {resetResult.TablesCleared}, Records deleted: {resetResult.RecordsDeleted}, Database file deleted.";
                            _logger.LogInformation("SQLite database file securely deleted: {DbPath}", dbPath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Could not delete SQLite database file: {DbPath}", dbPath);
                            result.Success = true; // Tables were still cleared
                            result.Message = $"Tables cleared successfully but could not delete database file: {ex.Message}";
                            result.Errors.Add($"File deletion failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        result.Success = true;
                        result.Message = $"Tables cleared successfully. Database file not found or in-memory database.";
                    }
                }
            }
            else
            {
                // For SQL Server, PostgreSQL, MySQL - tables are already cleared
                result.Success = true;
                result.Message = $"Database securely wiped. Tables cleared: {resetResult.TablesCleared}, Records deleted: {resetResult.RecordsDeleted}";
                _logger.LogInformation("Server-based database wiped (all tables cleared)");
            }
            
            result.TablesCleared = resetResult.TablesCleared;
            result.RecordsDeleted = resetResult.RecordsDeleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during secure database wipe");
            result.Success = false;
            result.Message = $"Failed to securely wipe database: {ex.Message}";
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    public async Task<DatabaseResetResult> ReseedSampleDataAsync(string? userId = null)
    {
        var result = new DatabaseResetResult();
        
        try
        {
            _logger.LogInformation("Starting sample data reseeding");
            
            var recordsAdded = 0;

            // First, reseed default data (roles and collections)
            await ReseedDefaultDataAsync();
            recordsAdded += 4; // 2 roles + 2 collections

            // Add sample categories using EF Core entities
            var categoryIds = new List<int>();
            var sampleCategories = new[]
            {
                ("Social Media", "Social networking accounts", "👥", "#3b82f6"),
                ("Banking", "Financial and banking services", "🏦", "#10b981"),
                ("Email", "Email accounts", "📧", "#f59e0b"),
                ("Shopping", "E-commerce accounts", "🛒", "#8b5cf6"),
                ("Entertainment", "Streaming and gaming", "🎮", "#ec4899")
            };

            foreach (var (name, description, icon, color) in sampleCategories)
            {
                var category = new PasswordManager.Models.Category
                {
                    Name = name,
                    Description = description,
                    Icon = icon,
                    Color = color,
                    CreatedAt = DateTime.UtcNow
                };
                _dbContext.Categories.Add(category);
                recordsAdded++;
            }
            await _dbContext.SaveChangesAsync();

            // Add sample password items using EF Core entities
            var sampleItems = new[]
            {
                ("GitHub Account", "github.com", "github_user"),
                ("Gmail Account", "gmail.com", "user@gmail.com"),
                ("Netflix", "netflix.com", "netflix_user"),
                ("Amazon", "amazon.com", "amazon_user")
            };

            foreach (var (title, website, username) in sampleItems)
            {
                // Note: Not adding actual passwords for security reasons
                // Users can add these manually after seeing the sample structure
                var item = new PasswordManager.Models.PasswordItem
                {
                    Title = title,
                    Website = website,
                    Type = PasswordManager.Models.ItemType.Login,
                    Description = "Sample password item - please update with real credentials",
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    LoginItem = new PasswordManager.Models.LoginItem
                    {
                        Username = username,
                        EncryptedPassword = string.Empty // No password for security
                    }
                };
                _dbContext.PasswordItems.Add(item);
                recordsAdded++;
            }
            await _dbContext.SaveChangesAsync();

            result.Success = true;
            result.RecordsDeleted = 0;
            result.TablesCleared = 0;
            result.Message = $"Successfully reseeded sample data. Added {recordsAdded} sample records.";
            
            _logger.LogInformation("Sample data reseeded successfully: {RecordsAdded} records", recordsAdded);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sample data reseeding");
            result.Success = false;
            result.Message = $"Failed to reseed sample data: {ex.Message}";
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    /// <summary>
    /// Securely deletes a file by overwriting it with random data before deletion
    /// </summary>
    private async Task SecureDeleteFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return;

        try
        {
            var fileInfo = new FileInfo(filePath);
            var fileLength = fileInfo.Length;

            // Overwrite file with random data (DoD 5220.22-M standard - 3 passes)
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.None))
            {
                // Pass 1: Random data
                var randomBuffer = new byte[8192];
                RandomNumberGenerator.Fill(randomBuffer);
                for (long pos = 0; pos < fileLength; pos += randomBuffer.Length)
                {
                    var bytesToWrite = (int)Math.Min(randomBuffer.Length, fileLength - pos);
                    await fileStream.WriteAsync(randomBuffer.AsMemory(0, bytesToWrite));
                }
                await fileStream.FlushAsync();

                // Pass 2: Complement of random data
                fileStream.Seek(0, SeekOrigin.Begin);
                for (int i = 0; i < randomBuffer.Length; i++)
                    randomBuffer[i] = (byte)~randomBuffer[i];
                for (long pos = 0; pos < fileLength; pos += randomBuffer.Length)
                {
                    var bytesToWrite = (int)Math.Min(randomBuffer.Length, fileLength - pos);
                    await fileStream.WriteAsync(randomBuffer.AsMemory(0, bytesToWrite));
                }
                await fileStream.FlushAsync();

                // Pass 3: Random data again
                RandomNumberGenerator.Fill(randomBuffer);
                fileStream.Seek(0, SeekOrigin.Begin);
                for (long pos = 0; pos < fileLength; pos += randomBuffer.Length)
                {
                    var bytesToWrite = (int)Math.Min(randomBuffer.Length, fileLength - pos);
                    await fileStream.WriteAsync(randomBuffer.AsMemory(0, bytesToWrite));
                }
                await fileStream.FlushAsync();
            }

            // Delete the file
            File.Delete(filePath);
            
            _logger.LogInformation("File securely deleted: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during secure file deletion: {FilePath}", filePath);
            throw;
        }
    }

    // Writes a "<db>.seeded" marker next to the SQLite database (matching AppStartupService) so the
    // startup demo-data seeder treats the now-empty vault as intentionally cleared, not brand new.
    private void TryMarkSeedComplete()
    {
        try
        {
            var providerName = _dbContext.Database.ProviderName ?? string.Empty;
            if (!providerName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
                return;

            var connectionString = _dbContext.Database.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString)) return;

            var path = ExtractSqlitePath(connectionString);
            if (string.IsNullOrEmpty(path) || path.Equals(":memory:", StringComparison.OrdinalIgnoreCase))
                return;

            var marker = path + ".seeded";
            if (!File.Exists(marker))
                File.WriteAllText(marker, DateTime.UtcNow.ToString("o"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not write seed-complete marker");
        }
    }

    /// <summary>
    /// Extracts the database file path from a SQLite connection string
    /// </summary>
    private string? ExtractSqlitePath(string connectionString)
    {
        try
        {
            // Parse common SQLite connection string formats
            // "Data Source=path" or "DataSource=path"
            var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("DataSource=", StringComparison.OrdinalIgnoreCase))
                {
                    var equalsIndex = trimmed.IndexOf('=');
                    if (equalsIndex >= 0 && equalsIndex < trimmed.Length - 1)
                    {
                        var path = trimmed[(equalsIndex + 1)..].Trim();
                        return path;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting SQLite path from connection string");
        }
        
        return null;
    }
}
