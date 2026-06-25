using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VaultGuard.DAL;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.Services.Services;

/// <summary>
/// Service for checking database health and schema integrity
/// </summary>
public interface IDatabaseHealthService
{
    /// <summary>
    /// Checks if the database schema is healthy and has all required columns
    /// </summary>
    Task<DatabaseHealthResult> CheckDatabaseHealthAsync();
    
    /// <summary>
    /// Verifies that the MasterKeyIdentifier column exists in the Users table
    /// </summary>
    Task<bool> VerifyMasterKeyIdentifierColumnExistsAsync();
}

public class DatabaseHealthService : IDatabaseHealthService
{
    private readonly VaultGuardDbContext _dbContext;
    private readonly ILogger<DatabaseHealthService> _logger;

    public DatabaseHealthService(
        VaultGuardDbContext dbContext,
        ILogger<DatabaseHealthService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<DatabaseHealthResult> CheckDatabaseHealthAsync()
    {
        var result = new DatabaseHealthResult();
        
        try
        {
            // Check database connectivity
            result.CanConnect = await _dbContext.Database.CanConnectAsync();
            
            if (!result.CanConnect)
            {
                result.Issues.Add("Cannot connect to database");
                return result;
            }

            // Check for pending migrations
            var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync();
            result.HasPendingMigrations = pendingMigrations.Any();
            result.PendingMigrations = pendingMigrations.ToList();

            // Check MasterKeyIdentifier column
            result.MasterKeyIdentifierColumnExists = await VerifyMasterKeyIdentifierColumnExistsAsync();
            
            if (!result.MasterKeyIdentifierColumnExists)
            {
                result.Issues.Add("MasterKeyIdentifier column is missing from Users table");
            }

            // Overall health status
            result.IsHealthy = result.CanConnect && 
                             !result.HasPendingMigrations && 
                             result.MasterKeyIdentifierColumnExists;

            _logger.LogInformation("Database health check completed. Healthy: {IsHealthy}, Issues: {IssueCount}", 
                result.IsHealthy, result.Issues.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database health check");
            result.Issues.Add($"Health check failed: {ex.Message}");
            return result;
        }
    }

    public async Task<bool> VerifyMasterKeyIdentifierColumnExistsAsync()
    {
        try
        {
            // Try to query the MasterKeyIdentifier column directly
            var result = await _dbContext.Database.ExecuteSqlRawAsync(
                "SELECT COUNT(*) FROM pragma_table_info('AspNetUsers') WHERE name = 'MasterKeyIdentifier'");
            
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not verify MasterKeyIdentifier column existence using pragma_table_info");
            
            // Fallback: try to create a simple query that would fail if column doesn't exist
            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync(
                    "SELECT MasterKeyIdentifier FROM AspNetUsers WHERE 1=0");
                return true;
            }
            catch
            {
                _logger.LogWarning("MasterKeyIdentifier column does not exist in AspNetUsers table");
                return false;
            }
        }
    }
}

public class DatabaseHealthResult
{
    public bool IsHealthy { get; set; }
    public bool CanConnect { get; set; }
    public bool HasPendingMigrations { get; set; }
    public bool MasterKeyIdentifierColumnExists { get; set; }
    public List<string> PendingMigrations { get; set; } = new();
    public List<string> Issues { get; set; } = new();
}