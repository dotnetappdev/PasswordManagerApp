using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PasswordManager.Models.DTOs.Auth;
using PasswordManager.Services.Interfaces;
using System.Security.Claims;

namespace PasswordManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TwoFactorController : ControllerBase
{
    private readonly ITwoFactorService _twoFactorService;
    private readonly ILogger<TwoFactorController> _logger;

    public TwoFactorController(
        ITwoFactorService twoFactorService,
        ILogger<TwoFactorController> logger)
    {
        _twoFactorService = twoFactorService;
        _logger = logger;
    }

    /// <summary>
    /// Get the current 2FA status for the authenticated user
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<TwoFactorStatusDto>> GetStatus()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var status = await _twoFactorService.GetTwoFactorStatusAsync(userId);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting 2FA status");
            return StatusCode(500, new { message = "An error occurred while getting 2FA status" });
        }
    }

    /// <summary>
    /// Start the 2FA setup process
    /// </summary>
    [HttpPost("setup/start")]
    public async Task<ActionResult<TwoFactorSetupResponseDto>> StartSetup([FromBody] TwoFactorSetupRequestDto request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _twoFactorService.StartTwoFactorSetupAsync(userId, request.MasterPassword);
            if (result == null)
            {
                return BadRequest(new { message = "Failed to start 2FA setup. Please check your master password." });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting 2FA setup");
            return StatusCode(500, new { message = "An error occurred while starting 2FA setup" });
        }
    }

    /// <summary>
    /// Complete the 2FA setup process
    /// </summary>
    [HttpPost("setup/complete")]
    public async Task<ActionResult> CompleteSetup([FromBody] TwoFactorVerifySetupDto request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var success = await _twoFactorService.VerifyAndCompleteTwoFactorSetupAsync(userId, request);
            if (!success)
            {
                return BadRequest(new { message = "Failed to verify 2FA code. Please try again." });
            }

            return Ok(new { message = "2FA has been successfully enabled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing 2FA setup");
            return StatusCode(500, new { message = "An error occurred while completing 2FA setup" });
        }
    }

    /// <summary>
    /// Disable 2FA for the authenticated user
    /// </summary>
    [HttpPost("disable")]
    public async Task<ActionResult> Disable([FromBody] TwoFactorDisableDto request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var success = await _twoFactorService.DisableTwoFactorAsync(userId, request);
            if (!success)
            {
                return BadRequest(new { message = "Failed to disable 2FA. Please check your credentials." });
            }

            return Ok(new { message = "2FA has been successfully disabled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling 2FA");
            return StatusCode(500, new { message = "An error occurred while disabling 2FA" });
        }
    }

    /// <summary>
    /// Verify a 2FA code
    /// </summary>
    [HttpPost("verify")]
    public async Task<ActionResult> VerifyCode([FromBody] TwoFactorLoginDto request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var clientIp = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
            var success = await _twoFactorService.VerifyTwoFactorCodeAsync(userId, request.Code, request.IsBackupCode, clientIp);
            
            if (!success)
            {
                return BadRequest(new { message = "Invalid 2FA code" });
            }

            return Ok(new { message = "2FA code verified successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying 2FA code");
            return StatusCode(500, new { message = "An error occurred while verifying 2FA code" });
        }
    }

    /// <summary>
    /// Regenerate backup codes
    /// </summary>
    [HttpPost("backup-codes/regenerate")]
    public async Task<ActionResult<List<string>>> RegenerateBackupCodes([FromBody] RegenerateBackupCodesDto request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var backupCodes = await _twoFactorService.RegenerateBackupCodesAsync(userId, request);
            if (backupCodes == null)
            {
                return BadRequest(new { message = "Failed to regenerate backup codes. Please check your credentials." });
            }

            return Ok(new { backupCodes, message = "Backup codes have been regenerated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating backup codes");
            return StatusCode(500, new { message = "An error occurred while regenerating backup codes" });
        }
    }
}