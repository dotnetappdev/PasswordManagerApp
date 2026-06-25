using Microsoft.AspNetCore.Mvc;
using VaultGuard.Models.DTOs.Auth;
using VaultGuard.Crypto.Interfaces;
using VaultGuard.Models;
using VaultGuard.DAL;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VaultGuard.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using VaultGuard.Models.Configuration;
using Microsoft.Extensions.Options;

namespace VaultGuard.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IPasswordCryptoService _passwordCryptoService;
    private readonly IVaultSessionService _vaultSessionService;
    private readonly IQrLoginService _qrLoginService;

    private readonly IOtpService _otpService;
    private readonly IPlatformDetectionService _platformDetectionService;

    private readonly ITwoFactorService _twoFactorService;
    private readonly IPasskeyService _passkeyService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly VaultGuardDbContext _dbContext;
    private readonly SmsConfiguration _smsConfig;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IPasswordCryptoService passwordCryptoService,
        IVaultSessionService vaultSessionService,
        IQrLoginService qrLoginService,
        IOtpService otpService,
        IPlatformDetectionService platformDetectionService,
        ITwoFactorService twoFactorService,
        IPasskeyService passkeyService,
 
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        VaultGuardDbContext dbContext,
        IOptions<SmsConfiguration> smsConfig,
        ILogger<AuthController> logger)
    {
        _passwordCryptoService = passwordCryptoService;
        _vaultSessionService = vaultSessionService;
        _qrLoginService = qrLoginService;
        _otpService = otpService;
        _platformDetectionService = platformDetectionService;

        _twoFactorService = twoFactorService;
        _passkeyService = passkeyService;
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
        _smsConfig = smsConfig.Value;
        _logger = logger;
    }



    /// <summary>
    /// Master key login - allows users to login with just their master key
    /// </summary>
    [HttpPost("login/masterkey")]
    public async Task<ActionResult<LoginResponseDto>> LoginWithMasterKey([FromBody] MasterKeyLoginRequestDto loginRequest)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Find user by master key identifier
            ApplicationUser? user = null;
            var allUsers = await _userManager.Users.ToListAsync();
            
            foreach (var candidateUser in allUsers)
            {
                if (!string.IsNullOrEmpty(candidateUser.MasterKeyIdentifier) && 
                    !string.IsNullOrEmpty(candidateUser.UserSalt))
                {
                    var userSalt = Convert.FromBase64String(candidateUser.UserSalt);
                    if (_passwordCryptoService.VerifyMasterKeyIdentifier(
                        loginRequest.MasterKey, 
                        userSalt, 
                        candidateUser.MasterKeyIdentifier))
                    {
                        user = candidateUser;
                        break;
                    }
                }
            }

            if (user == null)
            {
                return Unauthorized("Invalid master key");
            }

            // Verify master password using the full authentication hash
            if (!_passwordCryptoService.VerifyMasterPassword(
                loginRequest.MasterKey, 
                user.MasterPasswordHash, 
                Convert.FromBase64String(user.UserSalt), 
                user.MasterPasswordIterations))
            {
                return Unauthorized("Invalid master key");
            }

            // Check if user has 2FA enabled
            if (user.TwoFactorEnabled)
            {
                // If 2FA code is provided, verify it
                if (!string.IsNullOrEmpty(loginRequest.TwoFactorCode))
                {
                    var clientIp = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
                    var isValidCode = await _twoFactorService.VerifyTwoFactorCodeAsync(
                        user.Id, 
                        loginRequest.TwoFactorCode, 
                        loginRequest.IsTwoFactorBackupCode, 
                        clientIp);
                    
                    if (!isValidCode)
                    {
                        return BadRequest("Invalid 2FA code");
                    }
                }
                else
                {
                    // Return response indicating 2FA is required
                    return Ok(new LoginResponseDto
                    {
                        RequiresTwoFactor = true,
                        SupportsPasskey = user.PasskeysEnabled,
                        TwoFactorToken = GenerateTemporaryToken(user.Id)
                    });
                }
            }

            // Complete login process
            var masterKey = _passwordCryptoService.DeriveMasterKey(loginRequest.MasterKey, Convert.FromBase64String(user.UserSalt));
            var sessionId = _vaultSessionService.InitializeSession(user.Id, masterKey);

            var authResponse = new AuthResponseDto
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
                    LastLoginAt = user.LastLoginAt,
                    IsActive = user.IsActive
                }
            };

            // Update last login time
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            return Ok(new LoginResponseDto
            {
                RequiresTwoFactor = false,
                SupportsPasskey = user.PasskeysEnabled,
                AuthResponse = authResponse
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during master key login");
            return StatusCode(500, "An error occurred during login");
        }
    }

    /// <summary>
    /// Enhanced login with 2FA and passkey support
    /// </summary>
    [HttpPost("login/enhanced")]
    public async Task<ActionResult<LoginResponseDto>> LoginEnhanced([FromBody] EnhancedLoginRequestDto loginRequest)
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

            // Verify master password
            if (!_passwordCryptoService.VerifyMasterPassword(loginRequest.Password, user.MasterPasswordHash, Convert.FromBase64String(user.UserSalt), user.MasterPasswordIterations))
            {
                return Unauthorized("Invalid email or password");
            }

            // Check if user has 2FA enabled
            if (user.TwoFactorEnabled)
            {
                // If 2FA code is provided, verify it
                if (!string.IsNullOrEmpty(loginRequest.TwoFactorCode))
                {
                    var clientIp = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
                    var isValidCode = await _twoFactorService.VerifyTwoFactorCodeAsync(user.Id, loginRequest.TwoFactorCode, loginRequest.IsTwoFactorBackupCode, clientIp);
                    
                    if (!isValidCode)
                    {
                        return BadRequest("Invalid 2FA code");
                    }
                }
                else
                {
                    // Return response indicating 2FA is required
                    return Ok(new LoginResponseDto
                    {
                        RequiresTwoFactor = true,
                        SupportsPasskey = user.PasskeysEnabled,
                        TwoFactorToken = GenerateTemporaryToken(user.Id)
                    });
                }
            }

            // Complete login process
            var masterKey = _passwordCryptoService.DeriveMasterKey(loginRequest.Password, Convert.FromBase64String(user.UserSalt));
            var sessionId = _vaultSessionService.InitializeSession(user.Id, masterKey);

            var authResponse = new AuthResponseDto
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
                    LastLoginAt = user.LastLoginAt,
                    IsActive = user.IsActive
                }
            };

            // Update last login time
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            return Ok(new LoginResponseDto
            {
                RequiresTwoFactor = false,
                SupportsPasskey = user.PasskeysEnabled,
                AuthResponse = authResponse
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during enhanced login for user {Email}", loginRequest.Email);
            return StatusCode(500, "An error occurred during login");
        }
    }

    private string GenerateTemporaryToken(string userId)
    {
        // Implement temporary token generation for 2FA
        // This should be secure and expire after a short time
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{userId}:{DateTime.UtcNow:O}"));
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

            // Check if OTP is supported on this platform
            var userAgent = HttpContext.Request.Headers["User-Agent"].FirstOrDefault();
            if (!_platformDetectionService.IsOtpSupported(userAgent))
            {
                return BadRequest("Two-factor authentication via SMS is not supported on this platform");
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
            
            // Store the backup codes as salted PBKDF2 hashes — they are shown to the user once here
            // and are never recoverable from the database. (Previously these were "encrypted" with an
            // all-zero key, i.e. effectively plaintext.)
            user.BackupCodes = _otpService.HashBackupCodesForStorage(backupCodes);
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

            // Check if OTP is supported on this platform
            var userAgent = HttpContext.Request.Headers["User-Agent"].FirstOrDefault();
            if (!_platformDetectionService.IsOtpSupported(userAgent))
            {
                return BadRequest("Two-factor authentication via SMS is not supported on this platform");
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

    /// <summary>
    /// Delete current user's account - requires password confirmation
    /// </summary>
    [Authorize]
    [HttpDelete("account")]
    public async Task<ActionResult> DeleteAccount([FromBody] DeleteAccountRequestDto request)
    {
        try
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            // Verify the password before deleting
            var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!passwordValid)
            {
                return BadRequest(new { error = "Invalid password" });
            }

            // Explicitly delete all user's passwords
            var userPasswordItems = await _dbContext.PasswordItems
                .Where(p => p.UserId == userId)
                .ToListAsync();
            _dbContext.PasswordItems.RemoveRange(userPasswordItems);
            
            // Explicitly delete all user's categories
            var userCategories = await _dbContext.Categories
                .Where(c => c.UserId == userId)
                .ToListAsync();
            _dbContext.Categories.RemoveRange(userCategories);
            
            // Delete all user's collections
            var userCollections = await _dbContext.Collections
                .Where(c => c.UserId == userId)
                .ToListAsync();
            _dbContext.Collections.RemoveRange(userCollections);
            
            // Delete all user's tags
            var userTags = await _dbContext.Tags
                .Where(t => t.UserId == userId)
                .ToListAsync();
            _dbContext.Tags.RemoveRange(userTags);
            
            // Save changes to ensure all related data is deleted first
            await _dbContext.SaveChangesAsync();

            // Delete the user account
            var result = await _userManager.DeleteAsync(user);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("User {UserId} deleted their account and all associated data (passwords, categories, collections, tags)", userId);
                
                // Sign out the user
                await _signInManager.SignOutAsync();
                
                return Ok(new { message = "Account deleted successfully" });
            }
            
            _logger.LogError("Failed to delete user {UserId}: {Errors}", 
                userId, 
                string.Join(", ", result.Errors.Select(e => e.Description)));
            
            return StatusCode(500, new { error = "Failed to delete account" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting account for user");
            return StatusCode(500, "An error occurred while deleting the account");
        }
    }
}
