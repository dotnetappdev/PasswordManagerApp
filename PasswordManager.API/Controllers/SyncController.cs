using Microsoft.AspNetCore.Mvc;
using PasswordManager.Services.Interfaces;
using PasswordManager.Models.DTOs.Sync;

namespace PasswordManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SyncController : ControllerBase
{
    private readonly ISyncService _syncService;
    private readonly ILogger<SyncController> _logger;

    public SyncController(
        ISyncService syncService,
        ILogger<SyncController> logger)
    {
        _syncService = syncService;
        _logger = logger;
    }

    /// <summary>
    /// Sync data between databases
    /// </summary>
    [HttpPost("sync")]
    public async Task<ActionResult<SyncResponseDto>> SyncDatabases([FromBody] SyncRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _syncService.SyncDatabasesAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database sync");
            return StatusCode(500, "An error occurred during sync operation");
        }
    }

    /// <summary>
    /// Sync from SQLite to SQL Server
    /// </summary>
    [HttpPost("sync/sqlite-to-sqlserver")]
    public async Task<ActionResult<SyncResponseDto>> SyncFromSqliteToSqlServer([FromQuery] DateTime? lastSyncTime = null)
    {
        try
        {
            var result = await _syncService.SyncFromSqliteToSqlServerAsync(lastSyncTime);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing from SQLite to SQL Server");
            return StatusCode(500, "An error occurred during sync operation");
        }
    }

    /// <summary>
    /// Sync from SQL Server to SQLite
    /// </summary>
    [HttpPost("sync/sqlserver-to-sqlite")]
    public async Task<ActionResult<SyncResponseDto>> SyncFromSqlServerToSqlite([FromQuery] DateTime? lastSyncTime = null)
    {
        try
        {
            var result = await _syncService.SyncFromSqlServerToSqliteAsync(lastSyncTime);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing from SQL Server to SQLite");
            return StatusCode(500, "An error occurred during sync operation");
        }
    }

    /// <summary>
    /// Perform full sync in specified direction
    /// </summary>
    [HttpPost("sync/full")]
    public async Task<ActionResult<SyncResponseDto>> FullSync([FromQuery] SyncDirection direction)
    {
        try
        {
            var result = await _syncService.FullSyncAsync(direction);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during full sync");
            return StatusCode(500, "An error occurred during full sync operation");
        }
    }

    /// <summary>
    /// Test database connection
    /// </summary>
    [HttpGet("test-connection")]
    public async Task<ActionResult<bool>> TestConnection([FromQuery] string connectionString, [FromQuery] string provider)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(provider))
                return BadRequest("Connection string and provider are required");

            var canConnect = await _syncService.CanConnectToDatabase(connectionString, provider);
            return Ok(canConnect);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing database connection");
            return StatusCode(500, "An error occurred while testing database connection");
        }
    }

    /// <summary>
    /// Get last sync time between databases
    /// </summary>
    [HttpGet("last-sync-time")]
    public async Task<ActionResult<DateTime?>> GetLastSyncTime([FromQuery] string sourceDb, [FromQuery] string targetDb)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sourceDb) || string.IsNullOrWhiteSpace(targetDb))
                return BadRequest("Source and target database names are required");

            var lastSyncTime = await _syncService.GetLastSyncTimeAsync(sourceDb, targetDb);
            return Ok(lastSyncTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting last sync time");
            return StatusCode(500, "An error occurred while getting last sync time");
        }
    }
}
