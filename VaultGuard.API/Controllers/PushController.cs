using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.API.Controllers;

/// <summary>
/// Registers mobile device push tokens and lets the server notify a user's devices.
/// The mobile apps call <c>register</c> after sign-in with their FCM/APNs token.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PushController : ControllerBase
{
    private readonly IPushDeviceRegistry _registry;
    private readonly IPushNotificationService _push;
    private readonly ILogger<PushController> _logger;

    public PushController(IPushDeviceRegistry registry, IPushNotificationService push, ILogger<PushController> logger)
    {
        _registry = registry;
        _push = push;
        _logger = logger;
    }

    private string? UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public record RegisterDeviceDto(string Token, string Platform);
    public record SendTestDto(string? Title, string? Body);

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDeviceDto dto)
    {
        var userId = UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(dto.Token)) return BadRequest("Token is required");

        await _registry.RegisterAsync(userId, dto.Token, string.IsNullOrWhiteSpace(dto.Platform) ? "unknown" : dto.Platform);
        _logger.LogInformation("Registered push device ({Platform}) for user {UserId}", dto.Platform, userId);
        return Ok(new { message = "Device registered" });
    }

    [HttpPost("unregister")]
    public async Task<IActionResult> Unregister([FromBody] RegisterDeviceDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Token)) return BadRequest("Token is required");
        await _registry.UnregisterAsync(dto.Token);
        return Ok(new { message = "Device unregistered" });
    }

    /// <summary>Send a test notification to all of the caller's registered devices.</summary>
    [HttpPost("test")]
    public async Task<IActionResult> SendTest([FromBody] SendTestDto dto)
    {
        var userId = UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var result = await _push.SendToUserAsync(userId,
            dto.Title ?? "VaultGuard", dto.Body ?? "This is a test notification.");
        return Ok(new { result.Sent, result.Failed, result.Error });
    }
}
