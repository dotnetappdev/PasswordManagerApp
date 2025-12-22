using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PasswordManager.Models.DTOs.Audit;
using PasswordManager.Services.Interfaces;
using System.Security.Claims;

namespace PasswordManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuditLogsController> _logger;

    public AuditLogsController(
        IAuditLogService auditLogService,
        ILogger<AuditLogsController> logger)
    {
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Get audit logs for the current user with filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ListAuditLogsResponseDto>> GetAuditLogs([FromQuery] ListAuditLogsRequestDto request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID not found");

            var response = await _auditLogService.GetAuditLogsAsync(userId, request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit logs");
            return StatusCode(500, "An error occurred while retrieving audit logs");
        }
    }

    /// <summary>
    /// Get audit logs for a specific entity
    /// </summary>
    [HttpGet("entity/{entityType}/{entityId}")]
    public async Task<ActionResult<List<AuditLogDto>>> GetEntityAuditLogs(string entityType, string entityId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID not found");

            var logs = await _auditLogService.GetEntityAuditLogsAsync(userId, entityType, entityId);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entity audit logs");
            return StatusCode(500, "An error occurred while retrieving entity audit logs");
        }
    }

    /// <summary>
    /// Create a manual audit log entry (for external events)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> CreateAuditLog([FromBody] CreateAuditLogRequestDto request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID not found");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].FirstOrDefault();

            await _auditLogService.CreateAuditLogAsync(
                userId,
                request.Action,
                request.EntityType,
                request.EntityId,
                request.EntityName,
                request.Changes,
                request.Success,
                request.ErrorMessage,
                ipAddress,
                userAgent);

            return Ok(new { message = "Audit log created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating audit log");
            return StatusCode(500, "An error occurred while creating audit log");
        }
    }
}
