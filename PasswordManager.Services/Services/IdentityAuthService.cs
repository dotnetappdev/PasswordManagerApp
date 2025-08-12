using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using PasswordManager.Crypto.Interfaces;
using PasswordManager.DAL;
using PasswordManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PasswordManager.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Linq;

namespace PasswordManager.Services.Services;

/// <summary>
/// Service for user authentication and session management using ASP.NET Core Identity
/// </summary>
public class IdentityAuthService : IAuthService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IPasswordCryptoService _passwordCryptoService;
    private readonly ICryptographyService _cryptographyService;
    private readonly IVaultSessionService _vaultSessionService;
    private readonly PasswordManagerDbContextApp _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<IdentityAuthService> _logger;
    private bool _isAuthenticated = false;
    private ApplicationUser? _currentUser;

    public IdentityAuthService(
        IJSRuntime jsRuntime,
        IPasswordCryptoService passwordCryptoService,
        ICryptographyService cryptographyService,
        IVaultSessionService vaultSessionService,
        PasswordManagerDbContextApp dbContext,
        UserManager<ApplicationUser> userManager,
        ILogger<IdentityAuthService> logger)
    {
        _jsRuntime = jsRuntime;
        _passwordCryptoService = passwordCryptoService;
        _cryptographyService = cryptographyService;
        _vaultSessionService = vaultSessionService;
        _dbContext = dbContext;
        _userManager = userManager;
        _logger = logger;
    }

    public bool IsAuthenticated => _isAuthenticated;
    public ApplicationUser? CurrentUser => _currentUser;

    /// <summary>
    /// Sets up a new master password for first-time use with a new user
    /// </summary>
    public async Task<bool> SetupMasterPasswordAsync(string masterPassword, string hint = "")
    {
        try
        {
            // Generate user GUID for multi-user support
            var userId = Guid.NewGuid().ToString();
            
            // Generate user salt
            var userSalt = _passwordCryptoService.GenerateUserSalt();
            
            // Create master password hash for authentication
            var masterPasswordHash = _passwordCryptoService.CreateMasterPasswordHash(masterPassword, userSalt);
            
            // Generate backup codes for recovery
            var backupCodes = GenerateBackupCodes(10);
            var encryptedBackupCodes = await EncryptBackupCodesAsync(backupCodes, masterPassword, userSalt);
            
            // Create user record using Identity
            var user = new ApplicationUser
            {
                Id = userId,
                Email = $"user-{userId.Substring(0, 8)}@passwordmanager.local",
                UserName = $"user-{userId.Substring(0, 8)}@passwordmanager.local",
                UserSalt = Convert.ToBase64String(userSalt),
                MasterPasswordHash = masterPasswordHash,
                MasterPasswordHint = hint,
                BackupCodes = encryptedBackupCodes,
                BackupCodesUsed = 0,
                TwoFactorBackupCodesRemaining = backupCodes.Count,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                IsActive = true
            };

            // Use Identity to create the user
            var result = await _userManager.CreateAsync(user);
            
            if (result.Succeeded)
            {
                // Store user salt securely in platform-specific storage
                await StoreUserSaltSecurelyAsync(user.Id, userSalt);

                _logger.LogInformation("Master password setup completed for user {UserId}", user.Id);
                return true;
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to create user: {Errors}", errors);
                return false;
            }
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
            // Get the currently set user (for single-device use, we take the first active user)
            // In a multi-user scenario, this would require user selection first
            var user = await _dbContext.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.CreatedAt)
                .FirstOrDefaultAsync();
                
            if (user == null)
            {
                _logger.LogWarning("No active user found in database");
                return false;
            }

            // Retrieve user salt from secure storage
            var userSalt = await GetUserSaltSecurelyAsync(user.Id);
            if (userSalt == null)
            {
                _logger.LogError("Failed to retrieve user salt from secure storage for user {UserId}", user.Id);
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
                
                // Update last login
                user.LastLoginAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                
                await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "isAuthenticated", "true");
                await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "currentUserId", user.Id);
                
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
            var currentUserId = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", "currentUserId");
            var isSessionValid = !string.IsNullOrEmpty(isAuth) && isAuth == "true";
            
            if (isSessionValid && !string.IsNullOrEmpty(sessionId) && _vaultSessionService.IsVaultUnlocked(sessionId))
            {
                _isAuthenticated = true;
                // Restore current user if not already set
                if (_currentUser == null && !string.IsNullOrEmpty(currentUserId))
                {
                    _currentUser = await _dbContext.Users.FindAsync(currentUserId);
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
            await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "currentUserId");
            
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
            var user = await _dbContext.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.CreatedAt)
                .FirstOrDefaultAsync();
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
    /// Logs in a user with credentials (for multi-user scenarios)
    /// </summary>
    public async Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            // Find user by email
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
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
                    
                    await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "isAuthenticated", "true");
                    await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "currentUserId", user.Id);
                    
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
            
            // Generate backup codes for recovery
            var backupCodes = GenerateBackupCodes(10);
            var encryptedBackupCodes = await EncryptBackupCodesAsync(backupCodes, password, userSalt);
            
            // Create user record using Identity
            var user = new ApplicationUser
            {
                Id = userId,
                Email = email,
                UserName = email,
                UserSalt = Convert.ToBase64String(userSalt),
                MasterPasswordHash = masterPasswordHash,
                MasterPasswordHint = "",
                BackupCodes = encryptedBackupCodes,
                BackupCodesUsed = 0,
                TwoFactorBackupCodesRemaining = backupCodes.Count,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                IsActive = true
            };

            // Use Identity to create the user
            var result = await _userManager.CreateAsync(user);
            
            if (result.Succeeded)
            {
                // Store user salt securely in platform-specific storage
                await StoreUserSaltSecurelyAsync(user.Id, userSalt);

                // Auto-login after successful registration
                _isAuthenticated = true;
                _currentUser = user;
                
                await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "isAuthenticated", "true");
                await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "currentUserId", user.Id);

                _logger.LogInformation("User registration completed for email: {Email}", email);
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to register user {Email}: {Errors}", email, string.Join(", ", result.Errors.Select(e => e.Description)));
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for email: {Email}", email);
            return false;
        }
    }

    /// <summary>
    /// Generates backup codes for account recovery
    /// </summary>
    private List<string> GenerateBackupCodes(int count = 10)
    {
        var codes = new List<string>();
        using var rng = RandomNumberGenerator.Create();
        
        for (int i = 0; i < count; i++)
        {
            var bytes = new byte[6];
            rng.GetBytes(bytes);
            
            // Convert to 12-character alphanumeric code
            var code = Convert.ToHexString(bytes).ToUpper();
            
            // Format as XXX-XXX-XXX-XXX
            var formattedCode = $"{code.Substring(0, 3)}-{code.Substring(3, 3)}-{code.Substring(6, 3)}-{code.Substring(9, 3)}";
            codes.Add(formattedCode);
        }
        
        return codes;
    }

    /// <summary>
    /// Encrypts backup codes for secure storage
    /// </summary>
    private async Task<string> EncryptBackupCodesAsync(List<string> backupCodes, string masterPassword, byte[] userSalt)
    {
        try
        {
            var json = JsonSerializer.Serialize(backupCodes);
            var key = _passwordCryptoService.DeriveMasterKey(masterPassword, userSalt);
            var encryptedData = _cryptographyService.EncryptAes256Gcm(json, key);
            
            // Combine nonce + auth tag + ciphertext and encode as base64
            var combined = new byte[encryptedData.Nonce.Length + encryptedData.AuthenticationTag.Length + encryptedData.Ciphertext.Length];
            Array.Copy(encryptedData.Nonce, 0, combined, 0, encryptedData.Nonce.Length);
            Array.Copy(encryptedData.AuthenticationTag, 0, combined, encryptedData.Nonce.Length, encryptedData.AuthenticationTag.Length);
            Array.Copy(encryptedData.Ciphertext, 0, combined, encryptedData.Nonce.Length + encryptedData.AuthenticationTag.Length, encryptedData.Ciphertext.Length);
            
            return Convert.ToBase64String(combined);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt backup codes");
            return "";
        }
    }

    /// <summary>
    /// Stores user salt securely in platform-specific storage
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