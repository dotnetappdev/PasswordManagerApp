namespace PasswordManager.Services.Interfaces;

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
