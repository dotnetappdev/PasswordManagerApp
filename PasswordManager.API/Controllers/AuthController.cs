using Microsoft.AspNetCore.Mvc;
using PasswordManager.Models.DTOs.Auth;
using PasswordManager.Crypto.Interfaces;
using PasswordManager.Models;
using PasswordManager.DAL;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PasswordManager.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace PasswordManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IPasswordCryptoService _passwordCryptoService;
    private readonly IVaultSessionService _vaultSessionService;
    private readonly IQrLoginService _qrLoginService;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IPasskeyService _passkeyService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IPasswordCryptoService passwordCryptoService,
        IVaultSessionService vaultSessionService,
        IQrLoginService qrLoginService,
        ITwoFactorService twoFactorService,
        IPasskeyService passkeyService,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<AuthController> logger)
    {
        _passwordCryptoService = passwordCryptoService;
        _vaultSessionService = vaultSessionService;
        _qrLoginService = qrLoginService;
        _twoFactorService = twoFactorService;
        _passkeyService = passkeyService;
        _userManager = userManager;
        _signInManager = signInManager;
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
}
