using Microsoft.AspNetCore.Mvc;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.API.Controllers
{
    /// <summary>
    /// Controller for managing database migrations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MigrationController : ControllerBase
    {
        private readonly IDatabaseMigrationService _migrationService;
        private readonly ILogger<MigrationController> _logger;

        public MigrationController(
            IDatabaseMigrationService migrationService,
            ILogger<MigrationController> logger)
        {
            _migrationService = migrationService;
            _logger = logger;
        }

        /// <summary>
        /// Get the current migration status
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetMigrationStatus()
        {
            try
            {
                var status = await _migrationService.GetMigrationStatusAsync();
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting migration status");
                return StatusCode(500, new { message = "Error getting migration status", error = ex.Message });
            }
        }

        /// <summary>
        /// Apply pending migrations
        /// </summary>
        [HttpPost("apply")]
        public async Task<IActionResult> ApplyMigrations()
        {
            try
            {
                var result = await _migrationService.ApplyPendingMigrationsAsync();

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
                _logger.LogError(ex, "Error applying migrations");
                return StatusCode(500, new { message = "Error applying migrations", error = ex.Message });
            }
        }

        /// <summary>
        /// Get applied migrations
        /// </summary>
        [HttpGet("applied")]
        public async Task<IActionResult> GetAppliedMigrations()
        {
            try
            {
                var migrations = await _migrationService.GetAppliedMigrationsAsync();
                return Ok(migrations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting applied migrations");
                return StatusCode(500, new { message = "Error getting applied migrations", error = ex.Message });
            }
        }

        /// <summary>
        /// Get pending migrations
        /// </summary>
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingMigrations()
        {
            try
            {
                var migrations = await _migrationService.GetPendingMigrationsAsync();
                return Ok(migrations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending migrations");
                return StatusCode(500, new { message = "Error getting pending migrations", error = ex.Message });
            }
        }
    }
}
