using System;
using System.Threading.Tasks;
using PasswordManager.Crypto.Interfaces;
using PasswordManager.DAL;
using PasswordManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PasswordManager.Services.Interfaces;
using Microsoft.Data.Sqlite;

namespace PasswordManager.WinUi.Services;

/// <summary>
/// WinUI-specific authentication service that uses master password authentication
/// without ASP.NET Core Identity and uses Windows secure storage instead of browser storage
/// </summary>
public class WinUiAuthService : IAuthService
{
    private readonly IPasswordCryptoService _passwordCryptoService;
    private readonly IVaultSessionService _vaultSessionService;
    private readonly PasswordManagerDbContextApp _dbContext;
    private readonly ISecureStorageService _secureStorageService;
    private readonly ILogger<WinUiAuthService> _logger;
    private bool _isAuthenticated = false;
    private ApplicationUser? _currentUser;

    public WinUiAuthService(
        IPasswordCryptoService passwordCryptoService,
        IVaultSessionService vaultSessionService,
        PasswordManagerDbContextApp dbContext,
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
            
            // Create master key identifier for lookup during master key login
            var masterKeyIdentifier = _passwordCryptoService.CreateMasterKeyIdentifier(masterPassword, userSalt);
            
            // Create user record in database with simple setup that doesn't require full Identity registration
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = $"user_{DateTime.UtcNow.Ticks}", // Unique username for master-key-only setup
                NormalizedUserName = $"USER_{DateTime.UtcNow.Ticks}",
                Email = "user@passwordmanager.local", // Default email for compatibility
                NormalizedEmail = "USER@PASSWORDMANAGER.LOCAL",
                EmailConfirmed = true, // Skip email confirmation for master-key-only setup
                UserSalt = Convert.ToBase64String(userSalt),
                MasterPasswordHash = masterPasswordHash,
                MasterKeyIdentifier = masterKeyIdentifier,
                MasterPasswordHint = hint,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true,
                SecurityStamp = Guid.NewGuid().ToString(), // Required by Identity
                ConcurrencyStamp = Guid.NewGuid().ToString() // Required by Identity
            };

            _dbContext.Users.Add(user);
            
            try
            {
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("User created successfully for master-key-only authentication");
            }
            catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.Message.Contains("no such table: AspNetUsers"))
            {
                _logger.LogWarning("AspNetUsers table not found, attempting to create Identity tables");
                
                // Try to ensure Identity tables are created
                try
                {
                    await _dbContext.Database.MigrateAsync();
                    _logger.LogInformation("Identity tables created successfully, retrying user creation");
                    
                    // Retry saving the user after migration
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation("User created successfully after Identity table creation");
                }
                catch (Exception migrationEx)
                {
                    _logger.LogError(migrationEx, "Failed to create Identity tables");
                    throw new InvalidOperationException("Cannot create user: Identity tables are missing and could not be created. Please ensure the database is properly initialized.", migrationEx);
                }
            }
            catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.Message.Contains("no column named MasterKeyIdentifier"))
            {
                _logger.LogWarning("MasterKeyIdentifier column not found, attempting to apply pending migrations");
                
                // Try to apply pending migrations that might include the MasterKeyIdentifier column
                try
                {
                    await _dbContext.Database.MigrateAsync();
                    _logger.LogInformation("Migrations applied successfully, retrying user creation");
                    
                    // Retry saving the user after migration
                    await _dbContext.SaveChangesAsync();
                }
                catch (Exception migrationEx)
                {
                    _logger.LogError(migrationEx, "Failed to apply migrations for MasterKeyIdentifier column");
                    
                    // Fallback: create user without MasterKeyIdentifier for now
                    user.MasterKeyIdentifier = null;
                    await _dbContext.SaveChangesAsync();
                    
                    _logger.LogWarning("User created without MasterKeyIdentifier due to migration failure");
                }
            }

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
    /// Authenticates user with master password using master key identifier lookup
    /// </summary>
    public async Task<bool> AuthenticateAsync(string masterPassword)
    {
        try
        {
            // Try to find user by master key identifier
            var user = await FindUserByMasterKeyAsync(masterPassword);
            if (user == null)
            {
                _logger.LogWarning("No user found with matching master key");
                return false;
            }

            // Retrieve user salt
            byte[] userSalt;
            if (!string.IsNullOrEmpty(user.UserSalt))
            {
                userSalt = Convert.FromBase64String(user.UserSalt);
            }
            else
            {
                // Fallback: try secure storage for user salt
                userSalt = await GetUserSaltSecurelyAsync(user.Id.ToString());
                if (userSalt == null)
                {
                    _logger.LogError("Failed to retrieve user salt for user {UserId}", user.Id);
                    return false;
                }
            }

            // Verify master password
            var isValid = _passwordCryptoService.VerifyMasterPassword(
                masterPassword, 
                user.MasterPasswordHash!, 
                userSalt
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
                await _secureStorageService.SetAsync("currentUserId", user.Id);
                
                _isAuthenticated = true;
                _currentUser = user;
                
                _logger.LogInformation("User {UserId} ({Email}) authenticated successfully with master key", user.Id, user.Email);
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
    /// Finds a user by their master key using master key identifier lookup
    /// </summary>
    private async Task<ApplicationUser?> FindUserByMasterKeyAsync(string masterPassword)
    {
        try
        {
            // Get all users and check their master key identifiers
            var users = await _dbContext.Users.ToListAsync();
            
            foreach (var user in users)
            {
                if (string.IsNullOrEmpty(user.UserSalt) || string.IsNullOrEmpty(user.MasterKeyIdentifier))
                    continue;

                var userSalt = Convert.FromBase64String(user.UserSalt);
                
                // Check if the master password matches this user's master key identifier
                if (_passwordCryptoService.VerifyMasterKeyIdentifier(masterPassword, userSalt, user.MasterKeyIdentifier))
                {
                    _logger.LogInformation("Found user {Email} matching master key", user.Email);
                    return user;
                }
            }

            // Fallback for backwards compatibility: try first user with valid data
            var firstUser = users.FirstOrDefault(u => !string.IsNullOrEmpty(u.UserSalt) && !string.IsNullOrEmpty(u.MasterPasswordHash));
            if (firstUser != null)
            {
                _logger.LogInformation("Using fallback authentication for user {Email}", firstUser.Email);
                return firstUser;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding user by master key");
            return null;
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
            
            // Create new master key identifier for lookup during master key login
            var newMasterKeyIdentifier = _passwordCryptoService.CreateMasterKeyIdentifier(newPassword, newUserSalt);

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
            user.MasterKeyIdentifier = newMasterKeyIdentifier;
            user.MasterPasswordHint = newPasswordHint;
            user.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.Message.Contains("no column named MasterKeyIdentifier"))
            {
                _logger.LogWarning("MasterKeyIdentifier column not found during password change, attempting to apply pending migrations");
                
                // Try to apply pending migrations that might include the MasterKeyIdentifier column
                try
                {
                    await _dbContext.Database.MigrateAsync();
                    _logger.LogInformation("Migrations applied successfully, retrying password change");
                    
                    // Retry saving the user after migration
                    await _dbContext.SaveChangesAsync();
                }
                catch (Exception migrationEx)
                {
                    _logger.LogError(migrationEx, "Failed to apply migrations for MasterKeyIdentifier column during password change");
                    
                    // Fallback: update user without MasterKeyIdentifier for now
                    user.MasterKeyIdentifier = null;
                    await _dbContext.SaveChangesAsync();
                    
                    _logger.LogWarning("Password changed without updating MasterKeyIdentifier due to migration failure");
                }
            }

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

    /// <summary>
    /// Authenticates user with master password for a specific user email
    /// Useful when multiple users share the same master key but have different roles
    /// </summary>
    public async Task<bool> AuthenticateAsUserAsync(string masterPassword, string userEmail)
    {
        try
        {
            // Find specific user by email
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null)
            {
                _logger.LogWarning("User {Email} not found in database", userEmail);
                return false;
            }

            // Verify master password for this specific user
            if (string.IsNullOrEmpty(user.UserSalt) || string.IsNullOrEmpty(user.MasterPasswordHash))
            {
                _logger.LogWarning("User {Email} missing required authentication data", userEmail);
                return false;
            }

            var userSalt = Convert.FromBase64String(user.UserSalt);
            var isValid = _passwordCryptoService.VerifyMasterPassword(
                masterPassword, 
                user.MasterPasswordHash, 
                userSalt
            );

            if (isValid)
            {
                // Derive master key for session
                var masterKey = _passwordCryptoService.DeriveMasterKey(masterPassword, userSalt);
                
                // Initialize session with master key
                var sessionId = _vaultSessionService.InitializeSession(user.Id, masterKey);
                
                // Store session in secure storage
                await _secureStorageService.SetAsync("sessionId", sessionId);
                await _secureStorageService.SetAsync("isAuthenticated", "true");
                await _secureStorageService.SetAsync("currentUserId", user.Id);
                await _secureStorageService.SetAsync("currentUserEmail", user.Email);
                
                _isAuthenticated = true;
                _currentUser = user;
                
                _logger.LogInformation("User {UserId} ({Email}) authenticated successfully as specific user", user.Id, user.Email);
                return true;
            }

            _logger.LogWarning("Authentication failed for user {Email}", userEmail);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication error for user {Email}", userEmail);
            return false;
        }
    }

    /// <summary>
    /// Gets all available users that can be authenticated with the master key
    /// </summary>
    public async Task<List<ApplicationUser>> GetAvailableUsersAsync()
    {
        try
        {
            var users = await _dbContext.Users
                .Where(u => !string.IsNullOrEmpty(u.UserSalt) && !string.IsNullOrEmpty(u.MasterPasswordHash))
                .Select(u => new ApplicationUser 
                { 
                    Id = u.Id, 
                    Email = u.Email, 
                    FirstName = u.FirstName, 
                    LastName = u.LastName 
                })
                .ToListAsync();

            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available users");
            return new List<ApplicationUser>();
        }
    }
}