using System;
using System.Threading.Tasks;
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
/// Server-side implementation of IAuthService that doesn't require JSRuntime.
/// Suitable for API contexts, console applications, and other non-Blazor environments.
/// </summary>
public class ServerAuthService : IAuthService
{
    private readonly IPasswordCryptoService _passwordCryptoService;
    private readonly ICryptographyService _cryptographyService;
    private readonly IVaultSessionService _vaultSessionService;
    private readonly PasswordManagerDbContextApp _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ServerAuthService> _logger;
    private readonly ISecureStorageService? _secureStorageService;
    private bool _isAuthenticated = false;
    private ApplicationUser? _currentUser;

    public ServerAuthService(
        IPasswordCryptoService passwordCryptoService,
        ICryptographyService cryptographyService,
        IVaultSessionService vaultSessionService,
        PasswordManagerDbContextApp dbContext,
        UserManager<ApplicationUser> userManager,
        ILogger<ServerAuthService> logger,
        ISecureStorageService? secureStorageService = null)
    {
        _passwordCryptoService = passwordCryptoService;
        _cryptographyService = cryptographyService;
        _vaultSessionService = vaultSessionService;
        _dbContext = dbContext;
        _userManager = userManager;
        _logger = logger;
        _secureStorageService = secureStorageService;
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
                
                // Store session context (without JavaScript)
                await StoreAuthenticationStateAsync(sessionId, user.Id);
                
                _isAuthenticated = true;
                _currentUser = user;
                
                // Update last login
                user.LastLoginAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                
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
    /// Checks if user is already authenticated (server-side session)
    /// </summary>
    public async Task<bool> CheckAuthenticationStatusAsync()
    {
        try
        {
            // For server-side contexts, check if we have an active session
            if (_isAuthenticated && _currentUser != null)
            {
                return true;
            }

            // Try to restore authentication state from secure storage or session
            var sessionInfo = await GetStoredAuthenticationStateAsync();
            if (sessionInfo.HasValue && _vaultSessionService.IsVaultUnlocked(sessionInfo.Value.SessionId))
            {
                _isAuthenticated = true;
                if (_currentUser == null && !string.IsNullOrEmpty(sessionInfo.Value.UserId))
                {
                    _currentUser = await _dbContext.Users.FindAsync(sessionInfo.Value.UserId);
                }
                return true;
            }
            
            _isAuthenticated = false;
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking authentication status");
            _isAuthenticated = false;
            return false;
        }
    }

    /// <summary>
    /// Logs out the user and clears the session
    /// </summary>
    public async Task LogoutAsync()
    {
        try
        {
            var sessionInfo = await GetStoredAuthenticationStateAsync();
            
            _isAuthenticated = false;
            _currentUser = null;
            
            if (sessionInfo != null)
            {
                _vaultSessionService.ClearSession(sessionInfo.Value.SessionId);
            }
            
            await ClearStoredAuthenticationStateAsync();
            
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
                    // Derive master key and create session
                    var masterKey = _passwordCryptoService.DeriveMasterKey(password, userSalt);
                    var sessionId = _vaultSessionService.InitializeSession(user.Id, masterKey);
                    
                    _isAuthenticated = true;
                    _currentUser = user;
                    
                    // Update last login
                    user.LastLoginAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();
                    
                    // Store authentication state
                    await StoreAuthenticationStateAsync(sessionId, user.Id);
                    
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
                var masterKey = _passwordCryptoService.DeriveMasterKey(password, userSalt);
                var sessionId = _vaultSessionService.InitializeSession(user.Id, masterKey);
                
                _isAuthenticated = true;
                _currentUser = user;
                
                await StoreAuthenticationStateAsync(sessionId, user.Id);

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
    /// Stores user salt securely using platform-specific secure storage or fallback
    /// </summary>
    private async Task StoreUserSaltSecurelyAsync(string userId, byte[] userSalt)
    {
        try
        {
            var saltBase64 = Convert.ToBase64String(userSalt);
            var key = $"userSalt_{userId}";
            
            if (_secureStorageService != null)
            {
                await _secureStorageService.SetAsync(key, saltBase64);
            }
            else
            {
                // Fallback: Store in a temporary in-memory cache or session
                // In production, this should use proper server-side session storage
                _logger.LogWarning("No secure storage service available, using fallback storage for user salt");
                // For API/server contexts, you might want to store this in Redis, database, or other secure storage
            }
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
            var key = $"userSalt_{userId}";
            
            if (_secureStorageService != null)
            {
                var saltBase64 = await _secureStorageService.GetAsync(key);
                if (!string.IsNullOrEmpty(saltBase64))
                {
                    return Convert.FromBase64String(saltBase64);
                }
            }
            else
            {
                // Fallback: Try to get from database UserSalt field if available
                var user = await _dbContext.Users.FindAsync(userId);
                if (user != null && !string.IsNullOrEmpty(user.UserSalt))
                {
                    return Convert.FromBase64String(user.UserSalt);
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve user salt from secure storage");
            return null;
        }
    }

    /// <summary>
    /// Stores authentication state without JavaScript dependency
    /// </summary>
    private async Task StoreAuthenticationStateAsync(string sessionId, string userId)
    {
        try
        {
            var sessionInfo = new AuthSessionInfo 
            { 
                SessionId = sessionId, 
                UserId = userId, 
                Timestamp = DateTime.UtcNow 
            };
            var json = JsonSerializer.Serialize(sessionInfo);
            
            if (_secureStorageService != null)
            {
                await _secureStorageService.SetAsync("auth_session", json);
            }
            // For server contexts, you might store this in HttpContext.Session, Redis, or in-memory cache
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store authentication state");
        }
    }

    /// <summary>
    /// Gets stored authentication state
    /// </summary>
    private async Task<(string SessionId, string UserId)?> GetStoredAuthenticationStateAsync()
    {
        try
        {
            if (_secureStorageService != null)
            {
                var json = await _secureStorageService.GetAsync("auth_session");
                if (!string.IsNullOrEmpty(json))
                {
                    var sessionInfo = JsonSerializer.Deserialize<AuthSessionInfo>(json);
                    if (sessionInfo != null && !string.IsNullOrEmpty(sessionInfo.SessionId) && !string.IsNullOrEmpty(sessionInfo.UserId))
                    {
                        return (sessionInfo.SessionId, sessionInfo.UserId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get stored authentication state");
        }
        
        return null;
    }

    private class AuthSessionInfo
    {
        public string SessionId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Clears stored authentication state
    /// </summary>
    private Task ClearStoredAuthenticationStateAsync()
    {
        try
        {
            if (_secureStorageService != null)
            {
                _secureStorageService.Remove("auth_session");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear stored authentication state");
        }
        
        return Task.CompletedTask;
    }
}