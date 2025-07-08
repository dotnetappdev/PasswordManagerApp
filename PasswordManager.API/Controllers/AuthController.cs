using Microsoft.AspNetCore.Mvc;
using PasswordManager.Models.DTOs.Auth;
using PasswordManager.Crypto.Interfaces;
using PasswordManager.Models;
using PasswordManager.DAL;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IPasswordCryptoService _passwordCryptoService;
    private readonly IVaultSessionService _vaultSessionService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IPasswordCryptoService passwordCryptoService,
        IVaultSessionService vaultSessionService,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<AuthController> logger)
    {
        _passwordCryptoService = passwordCryptoService;
        _vaultSessionService = vaultSessionService;
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
            var authHash = _passwordCryptoService.CreateAuthHashh(masterKey, loginRequest.Password);
            
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
            var authHash = _passwordCryptoService.CreateAuthHashh(masterKey, registerRequest.Password);

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
    public async Task<ActionResult> Logout()
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
}
