using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PasswordManager.Models.DTOs.Auth;
using PasswordManager.Services.Interfaces;
using System.Security.Claims;

namespace PasswordManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PasskeyController : ControllerBase
{
    private readonly IPasskeyService _passkeyService;
    private readonly ILogger<PasskeyController> _logger;

    public PasskeyController(
        IPasskeyService passkeyService,
        ILogger<PasskeyController> logger)
    {
        _passkeyService = passkeyService;
        _logger = logger;
    }

    /// <summary>
    /// Get the current passkey status for the authenticated user
    /// </summary>
    [HttpGet("status")]
    [Authorize]
    public async Task<ActionResult<PasskeyStatusDto>> GetStatus()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var status = await _passkeyService.GetPasskeyStatusAsync(userId);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting passkey status");
            return StatusCode(500, new { message = "An error occurred while getting passkey status" });
        }
    }

    /// <summary>
    /// Get all passkeys for the authenticated user
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<PasskeyListResponseDto>> GetPasskeys()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var passkeys = await _passkeyService.GetUserPasskeysAsync(userId);
            return Ok(passkeys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user passkeys");
            return StatusCode(500, new { message = "An error occurred while getting passkeys" });
        }
    }

    /// <summary>
    /// Start the passkey registration process
    /// </summary>
    [HttpPost("register/start")]
    [Authorize]
    public async Task<ActionResult<PasskeyRegistrationStartResponseDto>> StartRegistration([FromBody] PasskeyRegistrationStartDto request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _passkeyService.StartPasskeyRegistrationAsync(userId, request);
            if (result == null)
            {
                return BadRequest(new { message = "Failed to start passkey registration. Please check your master password." });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting passkey registration");
            return StatusCode(500, new { message = "An error occurred while starting passkey registration" });
        }
    }

    /// <summary>
    /// Complete the passkey registration process
    /// </summary>
    [HttpPost("register/complete")]
    [Authorize]
    public async Task<ActionResult> CompleteRegistration([FromBody] PasskeyRegistrationCompleteDto request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var success = await _passkeyService.CompletePasskeyRegistrationAsync(userId, request);
            if (!success)
            {
                return BadRequest(new { message = "Failed to complete passkey registration" });
            }

            return Ok(new { message = "Passkey has been successfully registered" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing passkey registration");
            return StatusCode(500, new { message = "An error occurred while completing passkey registration" });
        }
    }

    /// <summary>
    /// Start the passkey authentication process (no authorization required)
    /// </summary>
    [HttpPost("authenticate/start")]
    public async Task<ActionResult<PasskeyAuthenticationStartResponseDto>> StartAuthentication([FromBody] PasskeyAuthenticationStartDto request)
    {
        try
        {
            var result = await _passkeyService.StartPasskeyAuthenticationAsync(request.Email);
            if (result == null)
            {
                return BadRequest(new { message = "Failed to start passkey authentication. User not found or passkeys not enabled." });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting passkey authentication");
            return StatusCode(500, new { message = "An error occurred while starting passkey authentication" });
        }
    }

    /// <summary>
    /// Complete the passkey authentication process (no authorization required)
    /// </summary>
    [HttpPost("authenticate/complete")]
    public async Task<ActionResult<AuthResponseDto>> CompleteAuthentication([FromBody] PasskeyAuthenticationCompleteDto request)
    {
        try
        {
            var result = await _passkeyService.CompletePasskeyAuthenticationAsync(request);
            if (result == null)
            {
                return BadRequest(new { message = "Failed to complete passkey authentication" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing passkey authentication");
            return StatusCode(500, new { message = "An error occurred while completing passkey authentication" });
        }
    }

    /// <summary>
    /// Delete a passkey
    /// </summary>
    [HttpDelete("{passkeyId}")]
    [Authorize]
    public async Task<ActionResult> DeletePasskey(int passkeyId, [FromBody] PasskeyDeleteDto request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var success = await _passkeyService.DeletePasskeyAsync(userId, passkeyId, request);
            if (!success)
            {
                return BadRequest(new { message = "Failed to delete passkey. Please check your master password." });
            }

            return Ok(new { message = "Passkey has been successfully deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting passkey");
            return StatusCode(500, new { message = "An error occurred while deleting passkey" });
        }
    }

    /// <summary>
    /// Update passkey settings
    /// </summary>
    [HttpPut("settings")]
    [Authorize]
    public async Task<ActionResult> UpdateSettings([FromBody] UpdatePasskeySettingsDto request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var success = await _passkeyService.UpdatePasskeySettingsAsync(userId, request.StoreInVault, request.MasterPassword);
            if (!success)
            {
                return BadRequest(new { message = "Failed to update passkey settings. Please check your master password." });
            }

            return Ok(new { message = "Passkey settings have been updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating passkey settings");
            return StatusCode(500, new { message = "An error occurred while updating passkey settings" });
        }
    }
}

public class UpdatePasskeySettingsDto
{
    public bool StoreInVault { get; set; }
    public string MasterPassword { get; set; } = string.Empty;
}