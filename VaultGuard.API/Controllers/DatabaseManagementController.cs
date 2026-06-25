using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.API.Controllers;

/// <summary>
/// Controller for database management operations including reset and wipe
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DatabaseManagementController : ControllerBase
{
    private readonly IDatabaseResetService _databaseResetService;
    private readonly ILogger<DatabaseManagementController> _logger;

    public DatabaseManagementController(
        IDatabaseResetService databaseResetService,
        ILogger<DatabaseManagementController> logger)
    {
        _databaseResetService = databaseResetService;
        _logger = logger;
    }

    /// <summary>
    /// Gets information about tables that will be affected by reset operations
    /// </summary>
    /// <returns>Information about data tables, user tables, and system tables</returns>
    [HttpGet("reset-info")]
    public async Task<ActionResult<DatabaseResetInfo>> GetResetInfo()
    {
        try
        {
            var info = await _databaseResetService.GetResetInfoAsync();
            return Ok(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reset info");
            return StatusCode(500, new { error = "Failed to retrieve reset information", detail = ex.Message });
        }
    }

    /// <summary>
    /// Resets password data tables while preserving user accounts
    /// </summary>
    /// <returns>Result of the reset operation</returns>
    [HttpPost("reset-data")]
    public async Task<ActionResult<DatabaseResetResult>> ResetDataTables()
    {
        try
        {
            _logger.LogInformation("Resetting data tables via API");
            var result = await _databaseResetService.ResetDataTablesAsync();
            
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting data tables");
            return StatusCode(500, new { error = "Failed to reset data tables", detail = ex.Message });
        }
    }

    /// <summary>
    /// Resets all database tables including user accounts
    /// </summary>
    /// <param name="reseedData">Whether to reseed default data after reset</param>
    /// <returns>Result of the reset operation</returns>
    [HttpPost("reset-all")]
    public async Task<ActionResult<DatabaseResetResult>> ResetAllTables([FromQuery] bool reseedData = true)
    {
        try
        {
            _logger.LogInformation("Resetting all tables via API (reseed: {ReseedData})", reseedData);
            var result = await _databaseResetService.ResetAllTablesAsync(reseedData);
            
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting all tables");
            return StatusCode(500, new { error = "Failed to reset all tables", detail = ex.Message });
        }
    }

    /// <summary>
    /// Securely wipes the database by clearing all tables and deleting the database file (for SQLite)
    /// </summary>
    /// <returns>Result of the wipe operation</returns>
    [HttpPost("wipe")]
    public async Task<ActionResult<DatabaseResetResult>> SecureWipeDatabase()
    {
        try
        {
            _logger.LogWarning("Secure database wipe initiated via API");
            var result = await _databaseResetService.SecureWipeDatabaseAsync();
            
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during secure database wipe");
            return StatusCode(500, new { error = "Failed to securely wipe database", detail = ex.Message });
        }
    }

    /// <summary>
    /// Reseeds the database with sample data
    /// </summary>
    /// <returns>Result of the reseed operation</returns>
    [HttpPost("reseed-sample")]
    public async Task<ActionResult<DatabaseResetResult>> ReseedSampleData()
    {
        try
        {
            _logger.LogInformation("Reseeding sample data via API");
            var userId = User.Identity?.Name;
            var result = await _databaseResetService.ReseedSampleDataAsync(userId);
            
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reseeding sample data");
            return StatusCode(500, new { error = "Failed to reseed sample data", detail = ex.Message });
        }
    }
}
