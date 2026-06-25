namespace VaultGuard.Services.Interfaces;

/// <summary>
/// Service for resetting database tables
/// </summary>
public interface IDatabaseResetService
{
    /// <summary>
    /// Clears all data tables except user-related tables (AspNetUsers, AspNetRoles, etc.)
    /// </summary>
    Task<DatabaseResetResult> ResetDataTablesAsync();
    
    /// <summary>
    /// Clears all tables including user tables and reseeds default data
    /// </summary>
    Task<DatabaseResetResult> ResetAllTablesAsync(bool reseedData = true);

    /// <summary>
    /// Deletes all non-admin user accounts (and their owned data) while leaving
    /// any account in the Admin role intact. Vault data belonging to admin
    /// accounts is preserved.
    /// </summary>
    Task<DatabaseResetResult> ClearNonAdminUsersAsync();

    /// <summary>
    /// Securely wipes the database by clearing all tables and, for SQLite, securely deleting the database file
    /// </summary>
    Task<DatabaseResetResult> SecureWipeDatabaseAsync();
    
    /// <summary>
    /// Reseeds the database with sample data including categories, collections, and sample password items
    /// </summary>
    Task<DatabaseResetResult> ReseedSampleDataAsync(string? userId = null);
    
    /// <summary>
    /// Gets a list of tables that will be affected by each reset operation
    /// </summary>
    Task<DatabaseResetInfo> GetResetInfoAsync();
}

/// <summary>
/// Result of a database reset operation
/// </summary>
public class DatabaseResetResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TablesCleared { get; set; }
    public int RecordsDeleted { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Information about tables affected by reset operations
/// </summary>
public class DatabaseResetInfo
{
    public List<string> DataTables { get; set; } = new();
    public List<string> UserTables { get; set; } = new();
    public List<string> SystemTables { get; set; } = new();
}
