using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PasswordManager.Crypto.Interfaces;
using PasswordManager.Models;
using PasswordManager.Models.DTOs.Auth;
using PasswordManager.Services.Interfaces;
using System.Net;

namespace PasswordManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QrLoginController : ControllerBase
{
    private readonly IQrLoginService _qrLoginService;
    private readonly IPasswordCryptoService _passwordCryptoService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<QrLoginController> _logger;

    public QrLoginController(
        IQrLoginService qrLoginService,
        IPasswordCryptoService passwordCryptoService,
        UserManager<ApplicationUser> userManager,
        ILogger<QrLoginController> logger)
    {
        _qrLoginService = qrLoginService ?? throw new ArgumentNullException(nameof(qrLoginService));
        _passwordCryptoService = passwordCryptoService ?? throw new ArgumentNullException(nameof(passwordCryptoService));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initiate QR login by generating a QR code
    /// </summary>
    [HttpPost("initiate")]
    public async Task<ActionResult<QrLoginInitiateResponseDto>> InitiateQrLogin([FromBody] QrLoginInitiateRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Find user by email
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return Unauthorized("Invalid email or password");
            }

            // Verify password
            if (!_passwordCryptoService.VerifyMasterPassword(request.Password, user.MasterPasswordHash!, Convert.FromBase64String(user.UserSalt!), user.MasterPasswordIterations))
            {
                return Unauthorized("Invalid email or password");
            }

            // Get client info
            var ipAddress = GetClientIpAddress();
            var userAgent = Request.Headers["User-Agent"].FirstOrDefault();

            // Generate QR code
            var response = await _qrLoginService.GenerateQrCodeAsync(user.Id, user.Email!, ipAddress, userAgent);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating QR login for user {Email}", request.Email);
            return StatusCode(500, "An error occurred while generating QR code");
        }
    }

    /// <summary>
    /// Validate a QR token (used by mobile app before authentication)
    /// </summary>
    [HttpPost("validate")]
    public async Task<ActionResult<QrLoginValidateResponseDto>> ValidateQrToken([FromBody] QrLoginValidateRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _qrLoginService.ValidateQrTokenAsync(request.QrToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating QR token {Token}", request.QrToken);
            return StatusCode(500, "An error occurred while validating QR token");
        }
    }

    /// <summary>
    /// Authenticate using QR token (used by mobile app)
    /// </summary>
    [HttpPost("authenticate")]
    public async Task<ActionResult<QrLoginAuthenticateResponseDto>> AuthenticateWithQrToken([FromBody] QrLoginAuthenticateRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var ipAddress = GetClientIpAddress();
            var userAgent = Request.Headers["User-Agent"].FirstOrDefault();

            var response = await _qrLoginService.AuthenticateWithQrTokenAsync(request.QrToken, ipAddress, userAgent);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating with QR token {Token}", request.QrToken);
            return StatusCode(500, "An error occurred during authentication");
        }
    }

    /// <summary>
    /// Get QR login status (used by web app polling)
    /// </summary>
    [HttpGet("status/{token}")]
    public async Task<ActionResult<QrLoginStatusResponseDto>> GetQrLoginStatus(string token)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest("Token is required");
            }

            var response = await _qrLoginService.GetQrLoginStatusAsync(token);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting QR login status for token {Token}", token);
            return StatusCode(500, "An error occurred while checking status");
        }
    }

    /// <summary>
    /// Alternative authenticate endpoint for QR code URLs (GET request)
    /// This allows QR codes to contain direct links that mobile apps can open
    /// </summary>
    [HttpGet("authenticate")]
    public async Task<ActionResult<QrLoginAuthenticateResponseDto>> AuthenticateWithQrTokenGet([FromQuery] string token)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest("Token is required");
            }

            var ipAddress = GetClientIpAddress();
            var userAgent = Request.Headers["User-Agent"].FirstOrDefault();

            var response = await _qrLoginService.AuthenticateWithQrTokenAsync(token, ipAddress, userAgent);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating with QR token {Token}", token);
            return StatusCode(500, "An error occurred during authentication");
        }
    }

    /// <summary>
    /// Clean up expired QR tokens (maintenance endpoint)
    /// </summary>
    [HttpPost("cleanup")]
    public async Task<ActionResult<int>> CleanupExpiredTokens()
    {
        try
        {
            var count = await _qrLoginService.CleanupExpiredTokensAsync();
            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired QR tokens");
            return StatusCode(500, "An error occurred during cleanup");
        }
    }

    private string? GetClientIpAddress()
    {
        // Check for forwarded IP first (if behind a proxy)
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }
        }

        // Check for real IP header
        if (Request.Headers.ContainsKey("X-Real-IP"))
        {
            return Request.Headers["X-Real-IP"].FirstOrDefault();
        }

        // Fall back to remote IP
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}