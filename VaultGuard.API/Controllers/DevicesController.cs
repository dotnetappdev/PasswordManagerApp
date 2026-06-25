using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PasswordManager.Models.DTOs.Device;
using PasswordManager.Services.Interfaces;
using System.Security.Claims;

namespace PasswordManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DevicesController : ControllerBase
{
    private readonly IDeviceService _deviceService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<DevicesController> _logger;

    public DevicesController(
        IDeviceService deviceService,
        IAuditLogService auditLogService,
        ILogger<DevicesController> logger)
    {
        _deviceService = deviceService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Get all devices linked to the current user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ListDevicesResponseDto>> GetDevices()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID not found");

            var response = await _deviceService.GetUserDevicesAsync(userId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting devices");
            return StatusCode(500, "An error occurred while retrieving devices");
        }
    }

    /// <summary>
    /// Link a new device to the user's account
    /// </summary>
    [HttpPost("link")]
    public async Task<ActionResult<LinkDeviceResponseDto>> LinkDevice([FromBody] LinkDeviceRequestDto request)
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

            var response = await _deviceService.LinkDeviceAsync(userId, request, ipAddress, userAgent);

            if (response.Success && response.Device != null)
            {
                // Create audit log
                await _auditLogService.CreateAuditLogAsync(
                    userId,
                    "Link",
                    "Device",
                    response.Device.Id,
                    response.Device.DeviceName,
                    null,
                    true,
                    null,
                    ipAddress,
                    userAgent,
                    response.Device.Id,
                    response.Device.DeviceName);
            }

            return response.Success ? Ok(response) : BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking device");
            return StatusCode(500, "An error occurred while linking device");
        }
    }

    /// <summary>
    /// Unlink a device from the user's account
    /// </summary>
    [HttpPost("unlink")]
    public async Task<ActionResult<UnlinkDeviceResponseDto>> UnlinkDevice([FromBody] UnlinkDeviceRequestDto request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID not found");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Get device info before unlinking for audit
            var device = await _deviceService.GetDeviceByIdAsync(request.DeviceId);
            
            var response = await _deviceService.UnlinkDeviceAsync(userId, request.DeviceId);

            if (response.Success && device != null)
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = HttpContext.Request.Headers["User-Agent"].FirstOrDefault();

                // Create audit log
                await _auditLogService.CreateAuditLogAsync(
                    userId,
                    "Unlink",
                    "Device",
                    device.Id,
                    device.DeviceName,
                    null,
                    true,
                    null,
                    ipAddress,
                    userAgent);
            }

            return response.Success ? Ok(response) : BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlinking device");
            return StatusCode(500, "An error occurred while unlinking device");
        }
    }

    /// <summary>
    /// Update device information
    /// </summary>
    [HttpPut("{deviceId}")]
    public async Task<ActionResult<UpdateDeviceResponseDto>> UpdateDevice(
        string deviceId, 
        [FromBody] UpdateDeviceRequestDto request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID not found");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _deviceService.UpdateDeviceAsync(userId, deviceId, request);

            if (response.Success && response.Device != null)
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = HttpContext.Request.Headers["User-Agent"].FirstOrDefault();

                // Create audit log
                await _auditLogService.CreateAuditLogAsync(
                    userId,
                    "Update",
                    "Device",
                    deviceId,
                    response.Device.DeviceName,
                    System.Text.Json.JsonSerializer.Serialize(request),
                    true,
                    null,
                    ipAddress,
                    userAgent,
                    deviceId,
                    response.Device.DeviceName);
            }

            return response.Success ? Ok(response) : BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating device");
            return StatusCode(500, "An error occurred while updating device");
        }
    }

    /// <summary>
    /// Update device last seen timestamp (for heartbeat)
    /// </summary>
    [HttpPost("{deviceId}/heartbeat")]
    public async Task<ActionResult> UpdateDeviceHeartbeat(string deviceId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID not found");

            // Verify device ownership
            if (!await _deviceService.IsDeviceOwnedByUserAsync(deviceId, userId))
                return Forbid();

            await _deviceService.UpdateDeviceLastSeenAsync(deviceId);
            return Ok(new { message = "Device heartbeat updated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating device heartbeat");
            return StatusCode(500, "An error occurred while updating device heartbeat");
        }
    }
}
