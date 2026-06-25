using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PasswordManager.Crypto.Interfaces;
using PasswordManager.DAL;
using PasswordManager.Models;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using Microsoft.Extensions.Logging;
using PasswordManager.Services.Interfaces;
using Microsoft.Data.Sqlite;

namespace PasswordManager.WPF.Services;

/// <summary>
/// WinUI-specific authentication service that uses master password authentication
/// without ASP.NET Core Identity and uses Windows secure storage instead of browser storage
/// </summary>
public class WpfAuthService : IAuthService
{
    private readonly IPasswordCryptoService _passwordCryptoService;
    private readonly IVaultSessionService _vaultSessionService;
    private readonly PasswordManagerDbContextApp _dbContext;
    private readonly ISecureStorageService _secureStorageService;
    private readonly ILogger<WpfAuthService> _logger;
    private bool _isAuthenticated = false;
    private ApplicationUser? _currentUser;

    public WpfAuthService(
        IPasswordCryptoService passwordCryptoService,
        IVaultSessionService vaultSessionService,
        PasswordManagerDbContextApp dbContext,
        ISecureStorageService secureStorageService,
        ILogger<WpfAuthService> logger)
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
                _logger.LogInformation("Attempting to save new user to database (UserId={UserId})", user.Id);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("User created successfully for master-key-only authentication (UserId={UserId})", user.Id);
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
                    // Add diagnostic info about database and tables to help debugging
                    try
                    {
                        var canConnect = await _dbContext.Database.CanConnectAsync();
                        _logger.LogError("Database connectivity: {CanConnect}", canConnect);

                        // Inspect sqlite_master for AspNetUsers presence
                        try
                        {
                            using var conn = _dbContext.Database.GetDbConnection();
                            await conn.OpenAsync();
                            using var cmd = conn.CreateCommand();
                            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='AspNetUsers';";
                            var result = await cmd.ExecuteScalarAsync();
                            _logger.LogError("AspNetUsers table presence check result: {Result}", result ?? "<null>");
                        }
                        catch (Exception tblEx)
                        {
                            _logger.LogError(tblEx, "Error while checking sqlite_master for AspNetUsers");
                        }
                    }
                    catch (Exception diagEx)
                    {
                        _logger.LogError(diagEx, "Error gathering diagnostic database information");
                    }

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

                    // Attempt a diagnostic check of table/column presence
                    try
                    {
                        using var conn = _dbContext.Database.GetDbConnection();
                        await conn.OpenAsync();
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = "PRAGMA table_info('AspNetUsers');";
                        using var reader = await cmd.ExecuteReaderAsync();
                        var columns = new List<string>();
                        while (await reader.ReadAsync())
                        {
                            columns.Add(reader[1]?.ToString() ?? "");
                        }
                        _logger.LogError("AspNetUsers columns: {Columns}", string.Join(',', columns));
                    }
                    catch (Exception diagEx)
                    {
                        _logger.LogError(diagEx, "Error while checking AspNetUsers columns");
                    }

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
    /// Authenticates user with master password - tries to find matching user by master key
    /// </summary>
    public async Task<bool> AuthenticateAsync(string masterPassword)
    {
        try
        {
            // Get all active users from database to support common master key
            var users = await _dbContext.Users.Where(u => u.IsActive).ToListAsync();
            if (!users.Any())
            {
                _logger.LogWarning("No users found in database");
                return false;
            }

            _logger.LogInformation("Attempting authentication with {UserCount} users in database", users.Count);

            // Try to authenticate against each user until we find a match
            foreach (var user in users)
            {
                try
                {
                    _logger.LogDebug("Trying authentication for user {UserId} ({Email})", user.Id, user.Email);

                    // Skip users without proper crypto setup
                    if (string.IsNullOrEmpty(user.UserSalt) || string.IsNullOrEmpty(user.MasterPasswordHash))
                    {
                        _logger.LogWarning("User {UserId} missing crypto setup, skipping", user.Id);
                        continue;
                    }

                    // Retrieve user salt from secure storage, with fallback to database
                    var userSalt = await GetUserSaltSecurelyAsync(user.Id.ToString());
                    if (userSalt == null && !string.IsNullOrEmpty(user.UserSalt))
                    {
                        // Fallback: if salt not in secure storage, get it from database and store it securely
                        _logger.LogDebug("User salt not found in secure storage, using salt from database for user {UserId}", user.Id);
                        try
                        {
                            userSalt = Convert.FromBase64String(user.UserSalt);
                            // Store it in secure storage for future use
                            await StoreUserSaltSecurelyAsync(user.Id.ToString(), userSalt);
                            _logger.LogDebug("User salt stored securely for user {UserId}", user.Id);
                        }
                        catch (Exception saltEx)
                        {
                            _logger.LogError(saltEx, "Failed to convert user salt from database for user {UserId}", user.Id);
                            continue; // Try next user
                        }
                    }
                    
                    if (userSalt == null)
                    {
                        _logger.LogWarning("Failed to retrieve user salt for user {UserId}, skipping", user.Id);
                        continue; // Try next user
                    }

                    // Verify master password against this user
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

                        // Store session in secure storage
                        await _secureStorageService.SetAsync("sessionId", sessionId);
                        await _secureStorageService.SetAsync("isAuthenticated", "true");

                        _isAuthenticated = true;
                        _currentUser = user;

                        _logger.LogInformation("User {UserId} ({Email}) authenticated successfully with master password", user.Id, user.Email);
                        return true;
                    }
                    else
                    {
                        _logger.LogDebug("Master password verification failed for user {UserId}", user.Id);
                    }
                }
                catch (Exception userEx)
                {
                    _logger.LogWarning(userEx, "Error authenticating user {UserId}, trying next user", user.Id);
                    continue; // Try next user
                }
            }

            _logger.LogWarning("Master password authentication failed for all {UserCount} users", users.Count);
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

            // Create new master key identifier for lookup during master key login
            var newMasterKeyIdentifier = _passwordCryptoService.CreateMasterKeyIdentifier(newPassword, newUserSalt);

            // Get the current master key for re-encryption
            var currentMasterKey = _passwordCryptoService.DeriveMasterKey(currentPassword, userSalt);

            // Derive new master key
            var newMasterKey = _passwordCryptoService.DeriveMasterKey(newPassword, newUserSalt);

            // LIMITATION: Re-encryption of all vault data with new master key is not implemented
            // When master password changes, existing encrypted vault data remains encrypted with old key
            // This would require:
            // 1. Decrypt all password items with current master key
            // 2. Re-encrypt them with new master key
            // 3. Update all encrypted fields in database
            // For now, we'll update the user record and session
            // Users may need to re-enter vault data after master password change

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
    /// Stores user salt securely using Windows DPAPI through WpfSecureStorageService
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
    /// Gets the current authenticated user's ID
    /// </summary>
    public async Task<string?> GetCurrentUserIdAsync()
    {
        try
        {
            // If we have a current user in memory, return their ID
            if (_currentUser != null)
            {
                return _currentUser.Id;
            }

            // If authenticated but no current user, try to get from database
            if (_isAuthenticated)
            {
                var user = await _dbContext.Users.FirstOrDefaultAsync();
                return user?.Id;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user ID");
            return null;
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
    /// Gets all available users in the system
    /// </summary>
    /// <returns>List of available users</returns>
    public async Task<List<ApplicationUser>> GetAvailableUsersAsync()
    {
        try
        {
            var users = await _dbContext.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.Email)
                .ToListAsync();

            _logger.LogInformation("Retrieved {UserCount} available users", users.Count);
            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available users");
            return new List<ApplicationUser>();
        }
    }

    /// <summary>
    /// Authenticates a specific user by email with master password
    /// </summary>
    /// <param name="masterPassword">The master password to verify</param>
    /// <param name="email">The email of the user to authenticate</param>
    /// <returns>True if authentication was successful</returns>
    public async Task<bool> AuthenticateAsUserAsync(string masterPassword, string email)
    {
        try
        {
            // Find the specific user by email
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
            if (user == null)
            {
                _logger.LogWarning("User with email {Email} not found or inactive", email);
                return false;
            }

            // Retrieve user salt from secure storage
            var userSalt = await GetUserSaltSecurelyAsync(user.Id.ToString());
            if (userSalt == null)
            {
                _logger.LogError("Failed to retrieve user salt from secure storage for user {Email}", email);
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

                _logger.LogInformation("User {Email} (ID: {UserId}) authenticated successfully", user.Email, user.Id);
                return true;
            }

            _logger.LogWarning("Authentication failed for user {Email}", email);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication error for user {Email}", email);
            return false;
        }
    }

    /// <summary>
    /// Deletes the current user's account
    /// </summary>
    /// <param name="password">The user's master password for confirmation</param>
    /// <returns>Result with success status and error message if any</returns>
    public async Task<(bool Success, string? ErrorMessage)> DeleteAccountAsync(string password)
    {
        try
        {
            // Get current user from database
            var user = await _dbContext.Users.FirstOrDefaultAsync();
            if (user == null)
            {
                _logger.LogError("No user found in database for account deletion");
                return (false, "User not found");
            }

            // Verify password before deletion
            var userSalt = await GetUserSaltSecurelyAsync(user.Id.ToString());
            if (userSalt == null)
            {
                _logger.LogError("Failed to retrieve user salt from secure storage for account deletion");
                return (false, "Failed to verify user credentials");
            }

            var isPasswordValid = _passwordCryptoService.VerifyMasterPassword(
                password,
                user.MasterPasswordHash!,
                Convert.FromBase64String(user.UserSalt!)
            );

            if (!isPasswordValid)
            {
                _logger.LogWarning("Password verification failed during account deletion");
                return (false, "Invalid password");
            }

            // Delete all user's password items
            var passwordItems = await _dbContext.PasswordItems.Where(pi => pi.UserId == user.Id).ToListAsync();
            _dbContext.PasswordItems.RemoveRange(passwordItems);

            // Delete user account
            _dbContext.Users.Remove(user);
            await _dbContext.SaveChangesAsync();

            // Clear session and secure storage
            var sessionId = await _secureStorageService.GetAsync("sessionId");
            if (!string.IsNullOrEmpty(sessionId))
            {
                _vaultSessionService.ClearSession(sessionId);
            }

            _secureStorageService.Remove("isAuthenticated");
            _secureStorageService.Remove("sessionId");
            _secureStorageService.Remove($"userSalt_{user.Id}");

            _isAuthenticated = false;
            _currentUser = null;

            _logger.LogInformation("User account {UserId} deleted successfully", user.Id);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete user account");
            return (false, $"Failed to delete account: {ex.Message}");
        }
    }
}