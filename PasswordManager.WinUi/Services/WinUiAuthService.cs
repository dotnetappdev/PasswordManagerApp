using System;
using System.Threading.Tasks;
using PasswordManager.Crypto.Interfaces;
using PasswordManager.DAL;
using PasswordManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.WinUi.Services;

/// <summary>
/// WinUI-specific authentication service that uses master password authentication
/// without ASP.NET Core Identity and uses Windows secure storage instead of browser storage
/// </summary>
public class WinUiAuthService : IAuthService
{
    private readonly IPasswordCryptoService _passwordCryptoService;
    private readonly IVaultSessionService _vaultSessionService;
    private readonly PasswordManagerDbContext _dbContext;
    private readonly ISecureStorageService _secureStorageService;
    private readonly ILogger<WinUiAuthService> _logger;
    private bool _isAuthenticated = false;
    private ApplicationUser? _currentUser;

    public WinUiAuthService(
        IPasswordCryptoService passwordCryptoService,
        IVaultSessionService vaultSessionService,
        PasswordManagerDbContext dbContext,
        ISecureStorageService secureStorageService,
        ILogger<WinUiAuthService> logger)
    {
        _passwordCryptoService = passwordCryptoService;
        _vaultSessionService = vaultSessionService;
        _dbContext = dbContext;
        _secureStorageService = secureStorageService;
        _logger = logger;
    }

    public bool IsAuthenticated => _isAuthenticated;
    public ApplicationUser? CurrentUser => _currentUser;

    /// <summary>
    /// Sets up a new master password for first-time use
    /// </summary>
    public async Task<bool> SetupMasterPasswordAsync(string masterPassword, string hint = "")
    {
        try
        {
            // Generate user salt
            var userSalt = _passwordCryptoService.GenerateUserSalt();
            
            // Create master password hash for authentication
            var masterPasswordHash = _passwordCryptoService.CreateMasterPasswordHash(masterPassword, userSalt);
            
            // Create user record in database with username instead of email
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "admin", // Simple username for single-user setup
                Email = "user@passwordmanager.local", // Keep for compatibility but not used for auth
                UserSalt = Convert.ToBase64String(userSalt),
                MasterPasswordHash = masterPasswordHash,
                MasterPasswordHint = hint,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Store user salt securely in Windows secure storage
            await StoreUserSaltSecurelyAsync(user.Id.ToString(), userSalt);

            _logger.LogInformation("Master password setup completed for user {UserId}", user.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to setup master password");
            return false;
        }
    }

    /// <summary>
    /// Authenticates user with master password
    /// </summary>
    public async Task<bool> AuthenticateAsync(string masterPassword)
    {
        try
        {
            // Get user from database (for single-user setup, get the first user)
            var user = await _dbContext.Users.FirstOrDefaultAsync();
            if (user == null)
            {
                _logger.LogWarning("No user found in database");
                return false;
            }

            // Retrieve user salt from secure storage
            var userSalt = await GetUserSaltSecurelyAsync(user.Id.ToString());
            if (userSalt == null)
            {
                _logger.LogError("Failed to retrieve user salt from secure storage");
                return false;
            }

            // Verify master password
            var isValid = _passwordCryptoService.VerifyMasterPassword(
                masterPassword, 
                user.MasterPasswordHash!, 
                Convert.FromBase64String(user.UserSalt!)
            );

            if (isValid)
            {
                // Derive master key for session
                var masterKey = _passwordCryptoService.DeriveMasterKey(masterPassword, userSalt);
                
                // Initialize session with master key
                var sessionId = _vaultSessionService.InitializeSession(user.Id, masterKey);
                
                // Store session in secure storage instead of browser storage
                await _secureStorageService.SetAsync("sessionId", sessionId);
                await _secureStorageService.SetAsync("isAuthenticated", "true");
                
                _isAuthenticated = true;
                _currentUser = user;
                
                _logger.LogInformation("User {UserId} authenticated successfully", user.Id);
                return true;
            }

            _logger.LogWarning("Authentication failed for user {UserId}", user.Id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication error");
            return false;
        }
    }

    /// <summary>
    /// Checks if user is already authenticated
    /// </summary>
    public async Task<bool> CheckAuthenticationStatusAsync()
    {
        try
        {
            var isAuth = await _secureStorageService.GetAsync("isAuthenticated");
            var sessionId = await _secureStorageService.GetAsync("sessionId");
            var isSessionValid = !string.IsNullOrEmpty(isAuth) && isAuth == "true";
            
            if (isSessionValid && !string.IsNullOrEmpty(sessionId) && _vaultSessionService.IsVaultUnlocked(sessionId))
            {
                _isAuthenticated = true;
                // Restore current user if not already set
                if (_currentUser == null)
                {
                    _currentUser = await _dbContext.Users.FirstOrDefaultAsync();
                }
                return true;
            }
            
            _isAuthenticated = false;
            return false;
        }
        catch
        {
            _isAuthenticated = false;
            return false;
        }
    }

    /// <summary>
    /// Logs out the user and locks the vault
    /// </summary>
    public async Task LogoutAsync()
    {
        try
        {
            var sessionId = await _secureStorageService.GetAsync("sessionId");
            
            _isAuthenticated = false;
            _currentUser = null;
            
            if (!string.IsNullOrEmpty(sessionId))
            {
                _vaultSessionService.ClearSession(sessionId);
            }
            
            _secureStorageService.Remove("isAuthenticated");
            _secureStorageService.Remove("sessionId");
            
            _logger.LogInformation("User logged out successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
        }
    }

    /// <summary>
    /// Checks if this is the first time setup
    /// </summary>
    public async Task<bool> IsFirstTimeSetupAsync()
    {
        try
        {
            var userExists = await _dbContext.Users.AnyAsync();
            return !userExists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking first time setup");
            return true; // Default to first time setup on error
        }
    }

    /// <summary>
    /// Gets the master password hint
    /// </summary>
    public async Task<string> GetMasterPasswordHintAsync()
    {
        try
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync();
            return user?.MasterPasswordHint ?? "";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting master password hint");
            return "";
        }
    }

    /// <summary>
    /// Checks if user is already authenticated (alias for CheckAuthenticationStatusAsync)
    /// </summary>
    public async Task<bool> IsAuthenticatedAsync()
    {
        return await CheckAuthenticationStatusAsync();
    }

    /// <summary>
    /// Changes the master password for the current user
    /// </summary>
    public async Task<bool> ChangeMasterPasswordAsync(string currentPassword, string newPassword, string newPasswordHint = "")
    {
        try
        {
            // Get current user from database
            var user = await _dbContext.Users.FirstOrDefaultAsync();
            if (user == null)
            {
                _logger.LogError("No user found in database for password change");
                return false;
            }

            // Retrieve current user salt from secure storage
            var userSalt = await GetUserSaltSecurelyAsync(user.Id.ToString());
            if (userSalt == null)
            {
                _logger.LogError("Failed to retrieve user salt from secure storage for password change");
                return false;
            }

            // Verify current password
            var isCurrentPasswordValid = _passwordCryptoService.VerifyMasterPassword(
                currentPassword, 
                user.MasterPasswordHash!, 
                Convert.FromBase64String(user.UserSalt!)
            );

            if (!isCurrentPasswordValid)
            {
                _logger.LogWarning("Current password verification failed during password change");
                return false;
            }

            // Generate new user salt for enhanced security
            var newUserSalt = _passwordCryptoService.GenerateUserSalt();
            
            // Create new master password hash
            var newMasterPasswordHash = _passwordCryptoService.CreateMasterPasswordHash(newPassword, newUserSalt);

            // Get the current master key for re-encryption
            var currentMasterKey = _passwordCryptoService.DeriveMasterKey(currentPassword, userSalt);
            
            // Derive new master key
            var newMasterKey = _passwordCryptoService.DeriveMasterKey(newPassword, newUserSalt);

            // TODO: Re-encrypt all vault data with new master key
            // This would require getting all password items and re-encrypting them
            // For now, we'll update the user record and session
            
            // Update user record in database
            user.UserSalt = Convert.ToBase64String(newUserSalt);
            user.MasterPasswordHash = newMasterPasswordHash;
            user.MasterPasswordHint = newPasswordHint;
            user.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            // Update secure storage with new salt
            await StoreUserSaltSecurelyAsync(user.Id.ToString(), newUserSalt);

            // Update current session with new master key if authenticated
            if (_isAuthenticated)
            {
                var sessionId = await _secureStorageService.GetAsync("sessionId");
                if (!string.IsNullOrEmpty(sessionId))
                {
                    // Clear old session and create new one with new master key
                    _vaultSessionService.ClearSession(sessionId);
                    var newSessionId = _vaultSessionService.InitializeSession(user.Id, newMasterKey);
                    await _secureStorageService.SetAsync("sessionId", newSessionId);
                }
            }

            _logger.LogInformation("Master password changed successfully for user {UserId}", user.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change master password");
            return false;
        }
    }

    /// <summary>
    /// Logs in a user with credentials (username/password instead of email/password)
    /// </summary>
    public async Task<bool> LoginAsync(string usernameOrEmail, string password)
    {
        try
        {
            // For single-user setup, we authenticate with the master password regardless of username
            // In multi-user scenarios, this could find user by username
            var user = await _dbContext.Users.FirstOrDefaultAsync();
            if (user == null)
            {
                _logger.LogWarning("No user found in database");
                return false;
            }

            // Use master password authentication
            return await AuthenticateAsync(password);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return false;
        }
    }

    /// <summary>
    /// Registers a new user with credentials (for first-time setup)
    /// </summary>
    public async Task<bool> RegisterAsync(string username, string password)
    {
        try
        {
            // Check if any user already exists (single-user setup)
            var existingUser = await _dbContext.Users.AnyAsync();
            if (existingUser)
            {
                _logger.LogWarning("User already exists in single-user setup");
                return false;
            }

            // Set up master password with the provided credentials
            return await SetupMasterPasswordAsync(password, "");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return false;
        }
    }

    /// <summary>
    /// Stores user salt securely using Windows DPAPI through WinUiSecureStorageService
    /// </summary>
    private async Task StoreUserSaltSecurelyAsync(string userId, byte[] userSalt)
    {
        try
        {
            // Convert salt to base64 for storage
            var saltBase64 = Convert.ToBase64String(userSalt);
            await _secureStorageService.SetAsync($"userSalt_{userId}", saltBase64);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store user salt securely");
            throw;
        }
    }

    /// <summary>
    /// Retrieves user salt from Windows secure storage
    /// </summary>
    private async Task<byte[]?> GetUserSaltSecurelyAsync(string userId)
    {
        try
        {
            var saltBase64 = await _secureStorageService.GetAsync($"userSalt_{userId}");
            
            if (string.IsNullOrEmpty(saltBase64))
            {
                return null;
            }

            return Convert.FromBase64String(saltBase64);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve user salt from secure storage");
            return null;
        }
    }
}