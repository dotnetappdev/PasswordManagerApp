using Microsoft.AspNetCore.Mvc;
using PasswordManager.Models.DTOs.Auth;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QRLoginController : ControllerBase
{
    private readonly IQRLoginService _qrLoginService;
    private readonly ILogger<QRLoginController> _logger;

    public QRLoginController(
        IQRLoginService qrLoginService,
        ILogger<QRLoginController> logger)
    {
        _qrLoginService = qrLoginService;
        _logger = logger;
    }

    /// <summary>
    /// Generate a new QR code for login
    /// </summary>
    [HttpPost("generate")]
    public async Task<ActionResult<QRLoginGenerateResponseDto>> GenerateQRCode([FromBody] QRLoginGenerateRequestDto request)
    {
        try
        {
            var result = await _qrLoginService.GenerateQRCodeAsync(request.DeviceInfo);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QR code");
            return StatusCode(500, "An error occurred while generating the QR code");
        }
    }

    /// <summary>
    /// Validate QR token status (for polling from web)
    /// </summary>
    [HttpGet("validate/{qrToken}")]
    public async Task<ActionResult<QRLoginValidateResponseDto>> ValidateQRToken(string qrToken)
    {
        try
        {
            if (string.IsNullOrEmpty(qrToken))
            {
                return BadRequest("QR token is required");
            }

            var result = await _qrLoginService.ValidateQRTokenAsync(qrToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating QR token: {Token}", qrToken);
            return StatusCode(500, "An error occurred while validating the QR token");
        }
    }

    /// <summary>
    /// Authenticate user via QR token (mobile app endpoint)
    /// </summary>
    [HttpPost("authenticate")]
    public async Task<ActionResult<QRLoginValidateResponseDto>> AuthenticateQRToken([FromBody] QRLoginAuthenticateRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _qrLoginService.AuthenticateQRTokenAsync(request);
            
            if (!result.IsValid)
            {
                return Unauthorized("Invalid credentials or QR token");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating QR token: {Token}", request.QRToken);
            return StatusCode(500, "An error occurred during QR authentication");
        }
    }
}