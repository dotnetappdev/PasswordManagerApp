using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using PasswordManager.Crypto.Interfaces;
using PasswordManager.DAL;
using PasswordManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.Services.Services;

/// <summary>
/// Service for user authentication and session management
/// </summary>
public class AuthService : IAuthService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IPasswordCryptoService _passwordCryptoService;
    private readonly IVaultSessionService _vaultSessionService;
    private readonly PasswordManagerDbContext _dbContext;
    private readonly ILogger<AuthService> _logger;
    private bool _isAuthenticated = false;
    private ApplicationUser? _currentUser;

    public AuthService(
        IJSRuntime jsRuntime,
        IPasswordCryptoService passwordCryptoService,
        IVaultSessionService vaultSessionService,
        PasswordManagerDbContext dbContext,
        ILogger<AuthService> logger)
    {
        _jsRuntime = jsRuntime;
        _passwordCryptoService = passwordCryptoService;
        _vaultSessionService = vaultSessionService;
        _dbContext = dbContext;
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
            
            // Create user record in database
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = "user@passwordmanager.local", // Default email for single-user setup
                UserSalt = Convert.ToBase64String(userSalt),
                MasterPasswordHash = masterPasswordHash,
                MasterPasswordHint = hint,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Store user salt securely in Windows Credential Manager (for desktop) or secure storage
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
                
                // Store session ID for later use
                await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "sessionId", sessionId);
                
                _isAuthenticated = true;
                _currentUser = user;
                await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "isAuthenticated", "true");
                
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
            var isAuth = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", "isAuthenticated");
            var sessionId = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", "sessionId");
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
            var sessionId = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", "sessionId");
            
            _isAuthenticated = false;
            _currentUser = null;
            
            if (!string.IsNullOrEmpty(sessionId))
            {
                _vaultSessionService.ClearSession(sessionId);
            }
            
            await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "isAuthenticated");
            await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "sessionId");
            
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
    /// Logs in a user with credentials
    /// </summary>
    public async Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            // Find user by email
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                _logger.LogWarning("Login attempt with invalid email: {Email}", email);
                return false;
            }

            // Verify password using the stored hash
            if (!string.IsNullOrEmpty(user.MasterPasswordHash) && !string.IsNullOrEmpty(user.UserSalt))
            {
                var userSalt = Convert.FromBase64String(user.UserSalt);
                var isValidPassword = _passwordCryptoService.VerifyMasterPassword(password, user.MasterPasswordHash, userSalt, user.MasterPasswordIterations);
                
                if (isValidPassword)
                {
                    _isAuthenticated = true;
                    _currentUser = user;
                    
                    // Update last login
                    user.LastLoginAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();
                    
                    return true;
                }
            }
            
            _logger.LogWarning("Login attempt with invalid password for email: {Email}", email);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email: {Email}", email);
            return false;
        }
    }

    /// <summary>
    /// Registers a new user with credentials
    /// </summary>
    public async Task<bool> RegisterAsync(string email, string password)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existingUser != null)
            {
                _logger.LogWarning("Registration attempt with existing email: {Email}", email);
                return false;
            }

            // Generate user GUID
            var userId = Guid.NewGuid().ToString();
            
            // Generate user salt
            var userSalt = _passwordCryptoService.GenerateUserSalt();
            
            // Create master password hash for authentication
            var masterPasswordHash = _passwordCryptoService.CreateMasterPasswordHash(password, userSalt);
            
            // Create user record
            var user = new ApplicationUser
            {
                Id = userId,
                Email = email,
                UserName = email,
                UserSalt = Convert.ToBase64String(userSalt),
                MasterPasswordHash = masterPasswordHash,
                MasterPasswordHint = "",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                IsActive = true
            };

            // Save to database
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Store user salt securely in platform-specific storage
            await StoreUserSaltSecurelyAsync(user.Id, userSalt);

            // Auto-login after successful registration
            _isAuthenticated = true;
            _currentUser = user;

            _logger.LogInformation("User registration completed for email: {Email}", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for email: {Email}", email);
            return false;
        }
    }

    /// <summary>
    /// Stores user salt securely in Windows Credential Manager or platform-specific secure storage
    /// </summary>
    private async Task StoreUserSaltSecurelyAsync(string userId, byte[] userSalt)
    {
        try
        {
            // Convert salt to base64 for storage
            var saltBase64 = Convert.ToBase64String(userSalt);
            
            // For now, store in localStorage (should be replaced with proper credential storage)
            // In production, this should use:
            // - Windows Credential Manager on Windows
            // - Keychain on macOS
            // - libsecret on Linux
            // - Android Keystore on Android
            // - iOS Keychain on iOS
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", $"userSalt_{userId}", saltBase64);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store user salt securely");
            throw;
        }
    }

    /// <summary>
    /// Retrieves user salt from secure storage
    /// </summary>
    private async Task<byte[]?> GetUserSaltSecurelyAsync(string userId)
    {
        try
        {
            // For now, retrieve from localStorage (should be replaced with proper credential storage)
            var saltBase64 = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", $"userSalt_{userId}");
            
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
