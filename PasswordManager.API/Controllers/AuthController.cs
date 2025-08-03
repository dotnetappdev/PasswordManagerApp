using Microsoft.AspNetCore.Mvc;
using PasswordManager.Models.DTOs.Auth;
using PasswordManager.Crypto.Interfaces;
using PasswordManager.Models;
using PasswordManager.DAL;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PasswordManager.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using PasswordManager.Models.Configuration;
using Microsoft.Extensions.Options;

namespace PasswordManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IPasswordCryptoService _passwordCryptoService;
    private readonly IVaultSessionService _vaultSessionService;
    private readonly IQrLoginService _qrLoginService;
    private readonly IOtpService _otpService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly SmsConfiguration _smsConfig;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IPasswordCryptoService passwordCryptoService,
        IVaultSessionService vaultSessionService,
        IQrLoginService qrLoginService,
        IOtpService otpService,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IOptions<SmsConfiguration> smsConfig,
        ILogger<AuthController> logger)
    {
        _passwordCryptoService = passwordCryptoService;
        _vaultSessionService = vaultSessionService;
        _qrLoginService = qrLoginService;
        _otpService = otpService;
        _userManager = userManager;
        _signInManager = signInManager;
        _smsConfig = smsConfig.Value;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate user and establish vault session
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto loginRequest)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Find user by email
            var user = await _userManager.FindByEmailAsync(loginRequest.Email);

            if (user == null)
            {
                return Unauthorized("Invalid email or password");
            }

            // Derive master key from password
            var masterKey = _passwordCryptoService.DeriveMasterKey(loginRequest.Password, Convert.FromBase64String(user.UserSalt));

            // Verify against stored hash using authentication
            var authHash = _passwordCryptoService.CreateAuthHash(masterKey, loginRequest.Password);
            
            if (!_passwordCryptoService.VerifyMasterPassword(loginRequest.Password, user.MasterPasswordHash, Convert.FromBase64String(user.UserSalt), user.MasterPasswordIterations))
            {
                return Unauthorized("Invalid email or password");
            }

            // Initialize vault session with the derived master key
            var sessionId = _vaultSessionService.InitializeSession(user.Id, masterKey);

            var response = new AuthResponseDto
            {
                Token = sessionId, // In a real implementation, this would be a JWT token
                RefreshToken = "",
                ExpiresAt = DateTime.UtcNow.AddHours(8), // Session expires in 8 hours
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                }
            };

            // Update last login time
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Email}", loginRequest.Email);
            return StatusCode(500, "An error occurred during login");
        }
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto registerRequest)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(registerRequest.Email);

            if (existingUser != null)
            {
                return Conflict("User with this email already exists");
            }

            // Generate salt and derive master key
            var salt = _passwordCryptoService.GenerateUserSalt();
            var masterKey = _passwordCryptoService.DeriveMasterKey(registerRequest.Password, salt);

            // Create authentication hash
            var authHash = _passwordCryptoService.CreateAuthHash(masterKey, registerRequest.Password);

            // Create new user
            var user = new ApplicationUser
            {
                Email = registerRequest.Email,
                UserName = registerRequest.Email,
                FirstName = registerRequest.FirstName,
                LastName = registerRequest.LastName,
                UserSalt = Convert.ToBase64String(salt),
                MasterPasswordHash = authHash,
                MasterPasswordIterations = 600000,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, registerRequest.Password);
            
            if (!result.Succeeded)
            {
                return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
            }

            // Initialize vault session
            var sessionId = _vaultSessionService.InitializeSession(user.Id, masterKey);

            var response = new AuthResponseDto
            {
                Token = sessionId,
                RefreshToken = "",
                ExpiresAt = DateTime.UtcNow.AddHours(8),
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for user {Email}", registerRequest.Email);
            return StatusCode(500, "An error occurred during registration");
        }
    }

    /// <summary>
    /// Logout and clear vault session
    /// </summary>
    [HttpPost("logout")]
    public ActionResult Logout()
    {
        try
        {
            var sessionId = HttpContext.Request.Headers["Authorization"]
                .FirstOrDefault()?.Replace("Bearer ", "");

            if (!string.IsNullOrEmpty(sessionId))
            {
                _vaultSessionService.ClearSession(sessionId);
            }

            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, "An error occurred during logout");
        }
    }

    /// <summary>
    /// Verify session and get user vault data
    /// </summary>
    [HttpGet("verify")]
    public async Task<ActionResult<UserDto>> VerifySession()
    {
        try
        {
            var sessionId = HttpContext.Request.Headers["Authorization"]
                .FirstOrDefault()?.Replace("Bearer ", "");

            if (string.IsNullOrEmpty(sessionId))
            {
                return Unauthorized("Session token required");
            }

            var userId = _vaultSessionService.GetSessionUserId(sessionId);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Invalid or expired session");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Unauthorized("User not found");
            }

            return Ok(new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during session verification");
            return StatusCode(500, "An error occurred during session verification");
        }
    }

    /// <summary>
    /// Generate QR code for login
    /// </summary>
    [HttpPost("qr/generate")]
    [Authorize]
    public async Task<ActionResult<QrLoginGenerateResponseDto>> GenerateQrLogin()
    {
        try
        {
            var sessionId = HttpContext.Request.Headers["Authorization"]
                .FirstOrDefault()?.Replace("Bearer ", "");

            if (string.IsNullOrEmpty(sessionId))
            {
                return Unauthorized("Session token required");
            }

            var userId = _vaultSessionService.GetSessionUserId(sessionId);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Invalid or expired session");
            }

            var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
            var response = await _qrLoginService.GenerateQrLoginTokenAsync(userId, baseUrl);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QR login token");
            return StatusCode(500, "An error occurred while generating QR code");
        }
    }

    /// <summary>
    /// Authenticate using QR code token
    /// </summary>
    [HttpPost("qr/authenticate")]
    public async Task<ActionResult<QrLoginAuthenticateResponseDto>> AuthenticateQrLogin([FromBody] QrLoginAuthenticateRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userAgent = HttpContext.Request.Headers["User-Agent"].FirstOrDefault();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var response = await _qrLoginService.AuthenticateQrTokenAsync(request, userAgent, ipAddress);

            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during QR authentication for token {Token}", request.Token);
            return StatusCode(500, "An error occurred during authentication");
        }
    }

    /// <summary>
    /// Check QR login status
    /// </summary>
    [HttpGet("qr/status/{token}")]
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
            _logger.LogError(ex, "Error checking QR login status for token {Token}", token);
            return StatusCode(500, "An error occurred while checking status");
        }
    }

    /// <summary>
    /// Setup two-factor authentication via SMS
    /// </summary>
    [HttpPost("otp/setup")]
    [Authorize]
    public async Task<ActionResult<OtpSetupResponseDto>> SetupOtp([FromBody] SetupOtpRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!_smsConfig.Enabled)
            {
                return BadRequest("SMS OTP is not enabled on this server");
            }

            var sessionId = HttpContext.Request.Headers["Authorization"]
                .FirstOrDefault()?.Replace("Bearer ", "");

            if (string.IsNullOrEmpty(sessionId))
            {
                return Unauthorized("Session token required");
            }

            var userId = _vaultSessionService.GetSessionUserId(sessionId);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Invalid or expired session");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Unauthorized("User not found");
            }

            // Send OTP to the provided phone number
            var otpResult = await _otpService.SendOtpAsync(userId, request.PhoneNumber);
            if (!otpResult.IsSuccess)
            {
                return BadRequest(otpResult.ErrorMessage);
            }

            return Ok(new OtpSetupResponseDto
            {
                PhoneNumber = request.PhoneNumber,
                IsSetupComplete = false,
                BackupCodes = new List<string>() // Will be provided after verification
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during OTP setup for user");
            return StatusCode(500, "An error occurred during OTP setup");
        }
    }

    /// <summary>
    /// Verify OTP setup and complete two-factor authentication configuration
    /// </summary>
    [HttpPost("otp/verify-setup")]
    [Authorize]
    public async Task<ActionResult<OtpSetupResponseDto>> VerifyOtpSetup([FromBody] VerifyOtpSetupRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var sessionId = HttpContext.Request.Headers["Authorization"]
                .FirstOrDefault()?.Replace("Bearer ", "");

            if (string.IsNullOrEmpty(sessionId))
            {
                return Unauthorized("Session token required");
            }

            var userId = _vaultSessionService.GetSessionUserId(sessionId);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Invalid or expired session");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Unauthorized("User not found");
            }

            // Verify the OTP code
            var verificationResult = await _otpService.VerifyOtpAsync(userId, request.Code);
            if (!verificationResult.IsValid)
            {
                return BadRequest(verificationResult.ErrorMessage);
            }

            // Generate backup codes
            var backupCodes = _otpService.GenerateBackupCodes();
            
            // Enable 2FA for the user
            user.IsTwoFactorEnabled = true;
            user.PhoneNumber = request.PhoneNumber;
            user.PhoneNumberConfirmed = true;
            user.PhoneNumberConfirmedAt = DateTime.UtcNow;
            
            // Store encrypted backup codes (in a real implementation, this would use the user's master key)
            var masterKey = new byte[32]; // This would come from vault session
            var backupCodesJson = System.Text.Json.JsonSerializer.Serialize(backupCodes);
            var encryptedBackupCodes = _passwordCryptoService.EncryptPasswordWithKey(backupCodesJson, masterKey);
            user.BackupCodes = System.Text.Json.JsonSerializer.Serialize(encryptedBackupCodes);
            user.BackupCodesUsed = 0;

            await _userManager.UpdateAsync(user);

            return Ok(new OtpSetupResponseDto
            {
                PhoneNumber = request.PhoneNumber,
                IsSetupComplete = true,
                BackupCodes = backupCodes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during OTP setup verification");
            return StatusCode(500, "An error occurred during OTP setup verification");
        }
    }

    /// <summary>
    /// Send OTP code for login
    /// </summary>
    [HttpPost("otp/send")]
    public async Task<ActionResult> SendOtp([FromBody] SendOtpRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!_smsConfig.Enabled)
            {
                return BadRequest("SMS OTP is not enabled on this server");
            }

            // Find user and verify credentials first
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !user.IsTwoFactorEnabled)
            {
                return BadRequest("Two-factor authentication is not enabled for this account");
            }

            // Verify master password
            if (!_passwordCryptoService.VerifyMasterPassword(request.Password, user.MasterPasswordHash, Convert.FromBase64String(user.UserSalt), user.MasterPasswordIterations))
            {
                return Unauthorized("Invalid email or password");
            }

            // Send OTP
            var otpResult = await _otpService.SendOtpAsync(user.Id, user.PhoneNumber!);
            if (!otpResult.IsSuccess)
            {
                return BadRequest(otpResult.ErrorMessage);
            }

            return Ok(new { message = "Verification code sent", expiresAt = otpResult.ExpiresAt });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending OTP for user {Email}", request.Email);
            return StatusCode(500, "An error occurred while sending verification code");
        }
    }

    /// <summary>
    /// Login with OTP (two-factor authentication)
    /// </summary>
    [HttpPost("otp/login")]
    public async Task<ActionResult<AuthResponseDto>> LoginWithOtp([FromBody] OtpLoginRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Find user
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return Unauthorized("Invalid email or password");
            }

            // Verify master password first
            var masterKey = _passwordCryptoService.DeriveMasterKey(request.Password, Convert.FromBase64String(user.UserSalt));
            if (!_passwordCryptoService.VerifyMasterPassword(request.Password, user.MasterPasswordHash, Convert.FromBase64String(user.UserSalt), user.MasterPasswordIterations))
            {
                return Unauthorized("Invalid email or password");
            }

            // If 2FA is not enabled, use regular login
            if (!user.IsTwoFactorEnabled)
            {
                return BadRequest("Two-factor authentication is not enabled for this account");
            }

            // Verify OTP code
            var verificationResult = await _otpService.VerifyOtpAsync(user.Id, request.OtpCode);
            if (!verificationResult.IsValid)
            {
                if (verificationResult.IsLocked)
                {
                    return BadRequest($"Account temporarily locked due to too many incorrect attempts. Try again later.");
                }
                return BadRequest($"Invalid verification code. {verificationResult.AttemptsRemaining} attempts remaining.");
            }

            // Initialize vault session
            var sessionId = _vaultSessionService.InitializeSession(user.Id, masterKey);

            var response = new AuthResponseDto
            {
                Token = sessionId,
                RefreshToken = "",
                ExpiresAt = DateTime.UtcNow.AddHours(8),
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                }
            };

            // Update last login time
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during OTP login for user {Email}", request.Email);
            return StatusCode(500, "An error occurred during login");
        }
    }

    /// <summary>
    /// Disable two-factor authentication
    /// </summary>
    [HttpPost("otp/disable")]
    [Authorize]
    public async Task<ActionResult> DisableOtp([FromBody] DisableOtpRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var sessionId = HttpContext.Request.Headers["Authorization"]
                .FirstOrDefault()?.Replace("Bearer ", "");

            if (string.IsNullOrEmpty(sessionId))
            {
                return Unauthorized("Session token required");
            }

            var userId = _vaultSessionService.GetSessionUserId(sessionId);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Invalid or expired session");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Unauthorized("User not found");
            }

            // Verify master password
            if (!_passwordCryptoService.VerifyMasterPassword(request.Password, user.MasterPasswordHash, Convert.FromBase64String(user.UserSalt), user.MasterPasswordIterations))
            {
                return Unauthorized("Invalid password");
            }

            // If OTP code is provided, verify it
            if (!string.IsNullOrEmpty(request.OtpCode))
            {
                var verificationResult = await _otpService.VerifyOtpAsync(userId, request.OtpCode);
                if (!verificationResult.IsValid)
                {
                    return BadRequest("Invalid verification code");
                }
            }
            // If backup code is provided, verify it
            else if (!string.IsNullOrEmpty(request.BackupCode))
            {
                var backupCodeValid = await _otpService.VerifyBackupCodeAsync(userId, request.BackupCode);
                if (!backupCodeValid)
                {
                    return BadRequest("Invalid backup code");
                }
            }
            else
            {
                return BadRequest("Either OTP code or backup code is required");
            }

            // Disable 2FA
            user.IsTwoFactorEnabled = false;
            user.PhoneNumber = null;
            user.PhoneNumberConfirmed = false;
            user.PhoneNumberConfirmedAt = null;
            user.BackupCodes = null;
            user.BackupCodesUsed = 0;

            await _userManager.UpdateAsync(user);

            // Clear any pending OTP codes
            await _otpService.ClearPendingOtpAsync(userId);

            return Ok(new { message = "Two-factor authentication has been disabled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling OTP for user");
            return StatusCode(500, "An error occurred while disabling two-factor authentication");
        }
    }
}
