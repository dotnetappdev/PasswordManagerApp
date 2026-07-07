using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using VaultGuard.Crypto.Interfaces;
using VaultGuard.DAL;
using VaultGuard.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VaultGuard.Services.Interfaces;
using VaultGuard.Models.Configuration;

namespace VaultGuard.Services.Services;

/// <summary>
/// Service for user authentication and session management
/// </summary>
public class AuthService : IAuthService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IPasswordCryptoService _passwordCryptoService;
    private readonly IVaultSessionService _vaultSessionService;
    private readonly VaultGuardDbContext _dbContext;
    private readonly IDatabaseConfigurationService _databaseConfigurationService;
    private readonly ILogger<AuthService> _logger;
    private readonly HttpClient _httpClient;
    private bool _isAuthenticated = false;
    private ApplicationUser? _currentUser;

    public AuthService(
        IJSRuntime jsRuntime,
        IPasswordCryptoService passwordCryptoService,
        IVaultSessionService vaultSessionService,
        VaultGuardDbContext dbContext,
        IDatabaseConfigurationService databaseConfigurationService,
        HttpClient httpClient,
        ILogger<AuthService> logger)
    {
        _jsRuntime = jsRuntime;
        _passwordCryptoService = passwordCryptoService;
        _vaultSessionService = vaultSessionService;
        _dbContext = dbContext;
        _databaseConfigurationService = databaseConfigurationService;
        _httpClient = httpClient;
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
    /// Authenticates user with master password using the configured authentication mode
    /// </summary>
    public async Task<bool> AuthenticateAsync(string masterPassword)
    {
        try
        {
            // Get database configuration to determine authentication mode
            var dbConfig = await _databaseConfigurationService.GetConfigurationAsync();
            
            if (dbConfig.AuthenticationMode == AuthenticationMode.ApiEndpoint)
            {
                return await AuthenticateViaApiAsync(masterPassword, dbConfig);
            }
            else
            {
                return await AuthenticateViaLocalDatabaseAsync(masterPassword);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication error");
            return false;
        }
    }

    /// <summary>
    /// Authenticates user with master password using local database
    /// </summary>
    private async Task<bool> AuthenticateViaLocalDatabaseAsync(string masterPassword)
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

            if (string.IsNullOrEmpty(user.MasterPasswordHash) || string.IsNullOrEmpty(user.UserSalt))
            {
                _logger.LogError("User {UserId} is missing a master password hash or salt", user.Id);
                return false;
            }

            // The salt lives on the user record itself — the same value LoginViaLocalDatabaseAsync
            // uses. It previously also had to round-trip through browser localStorage (keyed by user
            // id) before it would authenticate, which meant a fresh browser profile, a cleared
            // localStorage, or a freshly seeded account (no browser context at seed time) could never
            // log in even with the correct password.
            var userSalt = Convert.FromBase64String(user.UserSalt);

            // Verify master password
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
                
                // Store session ID for later use
                await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "sessionId", sessionId);
                
                _isAuthenticated = true;
                _currentUser = user;
                await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "isAuthenticated", "true");
                
                _logger.LogInformation("User {UserId} authenticated successfully via local database", user.Id);
                return true;
            }

            _logger.LogWarning("Authentication failed for user {UserId} via local database", user.Id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Local database authentication error");
            return false;
        }
    }

    /// <summary>
    /// Authenticates user with master password using API endpoint
    /// </summary>
    private async Task<bool> AuthenticateViaApiAsync(string masterPassword, DatabaseConfiguration dbConfig)
    {
        try
        {
            if (string.IsNullOrEmpty(dbConfig.ApiUrl))
            {
                _logger.LogError("API URL is not configured for API authentication");
                return false;
            }

            // Get user email (for single-user setup, use default email)
            var user = await _dbContext.Users.FirstOrDefaultAsync();
            var email = user?.Email ?? "user@passwordmanager.local";

            // Create login request for .NET 9 Identity API endpoints
            var loginRequest = new
            {
                email = email,
                password = masterPassword
            };

            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(loginRequest),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            // Call the .NET 9 Identity API login endpoint
            var response = await _httpClient.PostAsync($"{dbConfig.ApiUrl.TrimEnd('/')}/login", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var loginResponse = System.Text.Json.JsonSerializer.Deserialize<ApiLoginResponse>(responseContent);

                if (loginResponse?.AccessToken != null)
                {
                    // Store the access token for API calls
                    await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "apiToken", loginResponse.AccessToken);
                    await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "sessionId", loginResponse.AccessToken);
                    
                    _isAuthenticated = true;
                    _currentUser = user;
                    await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "isAuthenticated", "true");
                    await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "authMode", "api");
                    
                    _logger.LogInformation("User authenticated successfully via API endpoint");
                    return true;
                }
            }
            
            _logger.LogWarning("API authentication failed. Status: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API authentication error");
            return false;
        }
    }

    // Helper class for API response
    private class ApiLoginResponse
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
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
    /// Logs in a user with credentials using the configured authentication mode
    /// </summary>
    public async Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            // Get database configuration to determine authentication mode
            var dbConfig = await _databaseConfigurationService.GetConfigurationAsync();
            
            if (dbConfig.AuthenticationMode == AuthenticationMode.ApiEndpoint)
            {
                return await LoginViaApiAsync(email, password, dbConfig);
            }
            else
            {
                return await LoginViaLocalDatabaseAsync(email, password);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error");
            return false;
        }
    }

    /// <summary>
    /// Logs in a user with credentials using local database
    /// </summary>
    private async Task<bool> LoginViaLocalDatabaseAsync(string email, string password)
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
                    // Derive the master key and initialize a vault session, exactly like
                    // AuthenticateViaLocalDatabaseAsync — without a sessionId in sessionStorage,
                    // CheckAuthenticationStatusAsync() (the post-login gate MainLayout runs) always
                    // finds no unlocked vault session and immediately bounces back to /login, even
                    // though the password was correct.
                    var masterKey = _passwordCryptoService.DeriveMasterKey(password, userSalt);
                    var sessionId = _vaultSessionService.InitializeSession(user.Id, masterKey);
                    await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "sessionId", sessionId);

                    _isAuthenticated = true;
                    _currentUser = user;

                    // Update last login
                    user.LastLoginAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();

                    await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "isAuthenticated", "true");
                    await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "authMode", "local");

                    return true;
                }
            }

            _logger.LogWarning("Login attempt with invalid password for email: {Email}", email);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during local database login for email: {Email}", email);
            return false;
        }
    }

    /// <summary>
    /// Logs in a user with credentials using API endpoint
    /// </summary>
    private async Task<bool> LoginViaApiAsync(string email, string password, DatabaseConfiguration dbConfig)
    {
        try
        {
            if (string.IsNullOrEmpty(dbConfig.ApiUrl))
            {
                _logger.LogError("API URL is not configured for API login");
                return false;
            }

            // Create login request for .NET 9 Identity API endpoints
            var loginRequest = new
            {
                email = email,
                password = password
            };

            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(loginRequest),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            // Call the .NET 9 Identity API login endpoint
            var response = await _httpClient.PostAsync($"{dbConfig.ApiUrl.TrimEnd('/')}/login", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var loginResponse = System.Text.Json.JsonSerializer.Deserialize<ApiLoginResponse>(responseContent);

                if (loginResponse?.AccessToken != null)
                {
                    // Store the access token for API calls
                    await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "apiToken", loginResponse.AccessToken);
                    
                    _isAuthenticated = true;
                    await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "isAuthenticated", "true");
                    await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "authMode", "api");
                    
                    _logger.LogInformation("User {Email} logged in successfully via API endpoint", email);
                    return true;
                }
            }
            
            _logger.LogWarning("API login failed for {Email}. Status: {StatusCode}", email, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during API login for email: {Email}", email);
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

            // Seed default vault and categories for the new user
            try
            {
                await SeedDefaultVaultAndCategoriesAsync(user.Id);
                _logger.LogInformation("Seeded default vault and categories for user {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed default vault for user {Email}", email);
                // Continue even if seeding fails - user is created successfully
            }

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

    /// <summary>
    /// Changes the master password for the current user (not implemented for web-based auth service)
    /// </summary>
    public async Task<bool> ChangeMasterPasswordAsync(string currentPassword, string newPassword, string newPasswordHint = "")
    {
        // This method would need to be implemented for web-based authentication
        // For now, return false to indicate it's not implemented
        _logger.LogWarning("ChangeMasterPasswordAsync not implemented for web-based AuthService");
        await Task.CompletedTask;
        return false;
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

            // Try to get from session storage
            var currentUserId = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", "currentUserId");
            if (!string.IsNullOrEmpty(currentUserId))
            {
                return currentUserId;
            }

            // If authenticated but no specific user ID, get the first user (for backward compatibility)
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

    public async Task<(bool Success, string? ErrorMessage)> DeleteAccountAsync(string password)
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                return (false, "User not authenticated");
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return (false, "User not found");
            }

            // Verify the password using the crypto service
            var userSalt = Convert.FromBase64String(user.UserSalt ?? "");
            var passwordValid = _passwordCryptoService.VerifyMasterPassword(
                password, 
                user.MasterPasswordHash ?? "", 
                userSalt, 
                user.MasterPasswordIterations);
            
            if (!passwordValid)
            {
                return (false, "Invalid password");
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
            
            // Delete the user account
            _dbContext.Users.Remove(user);
            await _dbContext.SaveChangesAsync();
            
            // Clear authentication state
            _isAuthenticated = false;
            _currentUser = null;
            
            _logger.LogInformation("User {UserId} deleted their account and all associated data (passwords, categories, collections, tags)", userId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting account");
            return (false, "An error occurred while deleting the account");
        }
    }

    /// <summary>
    /// Seeds default "Personal" vault with standard categories for a user
    /// </summary>
    private async Task SeedDefaultVaultAndCategoriesAsync(string userId)
    {
        // Check if user already has a vault
        var existingVault = await _dbContext.Vaults
            .FirstOrDefaultAsync(v => v.UserId == userId);
        
        if (existingVault != null)
        {
            _logger.LogInformation("User {UserId} already has a vault, skipping seed", userId);
            return;
        }

        // Create default "Personal" vault
        var vault = new Vault
        {
            Name = "Personal",
            Description = "Your personal password vault",
            IsDefault = true,
            Icon = "🔐",
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Vaults.Add(vault);
        await _dbContext.SaveChangesAsync();

        // Seed default categories
        var defaultCategories = new[]
        {
            new Category 
            { 
                Name = "Logins", 
                Description = "Login credentials for websites and apps",
                Icon = "🔑",
                Color = "#4A90E2",
                VaultId = vault.Id,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Category 
            { 
                Name = "Credit Cards", 
                Description = "Credit and debit card information",
                Icon = "💳",
                Color = "#E94B3C",
                VaultId = vault.Id,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Category 
            { 
                Name = "Secure Notes", 
                Description = "Encrypted notes and documents",
                Icon = "📝",
                Color = "#F5A623",
                VaultId = vault.Id,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Category 
            { 
                Name = "WiFi Networks", 
                Description = "WiFi network passwords",
                Icon = "📶",
                Color = "#7ED321",
                VaultId = vault.Id,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Category 
            { 
                Name = "Passkeys", 
                Description = "Passkey credentials for passwordless authentication",
                Icon = "🔐",
                Color = "#9013FE",
                VaultId = vault.Id,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Category 
            { 
                Name = "Identities", 
                Description = "Personal identification information",
                Icon = "👤",
                Color = "#50E3C2",
                VaultId = vault.Id,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _dbContext.Categories.AddRange(defaultCategories);
        await _dbContext.SaveChangesAsync();
    }
}
