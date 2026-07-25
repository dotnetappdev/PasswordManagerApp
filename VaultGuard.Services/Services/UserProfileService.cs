using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using VaultGuard.Crypto.Interfaces;
using VaultGuard.Models;
using VaultGuard.Models.DTOs.Auth;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.Services.Services;

public class UserProfileService : IUserProfileService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPasswordCryptoService _passwordCryptoService;
    private readonly IVaultService _vaultService;
    private readonly ILogger<UserProfileService> _logger;

    public UserProfileService(
        UserManager<ApplicationUser> userManager,
        IPasswordCryptoService passwordCryptoService,
        IVaultService vaultService,
        ILogger<UserProfileService> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _passwordCryptoService = passwordCryptoService ?? throw new ArgumentNullException(nameof(passwordCryptoService));
        _vaultService = vaultService ?? throw new ArgumentNullException(nameof(vaultService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var users = _userManager.Users.ToList();
        return users.Select(u => new UserDto
        {
            Id = u.Id,
            Email = u.Email ?? string.Empty,
            FirstName = u.FirstName,
            LastName = u.LastName,
            CreatedAt = u.CreatedAt,
            LastLoginAt = u.LastLoginAt,
            IsActive = u.IsActive
        }).ToList();
    }

    public async Task<UserDto?> GetUserByIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return null;

        return new UserProfileDetailsDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            IsActive = user.IsActive,
            MasterPasswordHint = user.MasterPasswordHint,
            UpdatedAt = user.UpdatedAt,
            PhoneNumber = user.PhoneNumber,
            TwoFactorEnabled = user.TwoFactorEnabled
        };
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return null;

        return new UserProfileDetailsDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            IsActive = user.IsActive,
            MasterPasswordHint = user.MasterPasswordHint,
            UpdatedAt = user.UpdatedAt,
            PhoneNumber = user.PhoneNumber,
            TwoFactorEnabled = user.TwoFactorEnabled
        };
    }

    public async Task<(IdentityResult Result, ApplicationUser? User, string? ErrorMessage)> CreateUserAsync(CreateUserProfileDto createUserDto)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(createUserDto.Email);
            if (existingUser != null)
            {
                return (IdentityResult.Failed(new IdentityError 
                { 
                    Description = "User with this email already exists" 
                }), null, "User with this email already exists");
            }

            // Generate salt and derive master key
            var salt = _passwordCryptoService.GenerateUserSalt();
            var masterKey = _passwordCryptoService.DeriveMasterKey(createUserDto.Password, salt);

            // Create authentication hash for master password verification
            var authHash = _passwordCryptoService.CreateAuthHash(masterKey, createUserDto.Password);

            // Create master key identifier for master key login
            var masterKeyIdentifier = _passwordCryptoService.CreateMasterKeyIdentifier(createUserDto.Password, salt);

            // Create new user
            var newUser = new ApplicationUser
            {
                Email = createUserDto.Email,
                UserName = createUserDto.Email,
                FirstName = createUserDto.FirstName,
                LastName = createUserDto.LastName,
                UserSalt = Convert.ToBase64String(salt),
                MasterPasswordHash = authHash,
                MasterKeyIdentifier = masterKeyIdentifier,
                MasterPasswordIterations = 600000, // Using OWASP recommendation
                MasterPasswordHint = createUserDto.MasterPasswordHint,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(newUser, createUserDto.Password);
            
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to create user {Email}: {Errors}", createUserDto.Email, errors);
                return (result, null, errors);
            }

            // Seed default vault with categories for the new user
            try
            {
                await _vaultService.GetOrCreateDefaultVaultAsync(newUser.Id);
                _logger.LogInformation("Seeded default vault and categories for user {Email}", newUser.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed default vault for user {Email}", newUser.Email);
                // Continue even if seeding fails - user is created successfully
            }

            _logger.LogInformation("Created user {Email} successfully", newUser.Email);
            return (result, newUser, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user {Email}", createUserDto.Email);
            return (IdentityResult.Failed(new IdentityError 
            { 
                Description = $"An error occurred: {ex.Message}" 
            }), null, ex.Message);
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateUserProfileAsync(UpdateUserProfileDto updateUserDto)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(updateUserDto.Id);
            if (user == null)
                return (false, "User not found");

            if (!string.IsNullOrEmpty(updateUserDto.Email) && user.Email != updateUserDto.Email)
            {
                // Check if email is already used
                var existingUser = await _userManager.FindByEmailAsync(updateUserDto.Email);
                if (existingUser != null && existingUser.Id != user.Id)
                    return (false, "Email is already in use");

                user.Email = updateUserDto.Email;
                user.UserName = updateUserDto.Email; // Keep username and email in sync
            }

            if (updateUserDto.FirstName != null)
                user.FirstName = updateUserDto.FirstName;

            if (updateUserDto.LastName != null)
                user.LastName = updateUserDto.LastName;

            if (updateUserDto.MasterPasswordHint != null)
                user.MasterPasswordHint = updateUserDto.MasterPasswordHint;

            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to update user {UserId}: {Errors}", user.Id, errors);
                return (false, errors);
            }

            _logger.LogInformation("Updated user {UserId} profile successfully", user.Id);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", updateUserDto.Id);
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> ChangeMasterPasswordAsync(
        string userId, 
        string currentPassword, 
        string newPassword, 
        string? newPasswordHint)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return (false, "User not found");

            // Verify current password
            var checkResult = await _userManager.CheckPasswordAsync(user, currentPassword);
            if (!checkResult)
                return (false, "Current password is incorrect");

            // Generate new salt and master key
            var salt = _passwordCryptoService.GenerateUserSalt();
            var masterKey = _passwordCryptoService.DeriveMasterKey(newPassword, salt);

            // Create new authentication hash
            var authHash = _passwordCryptoService.CreateAuthHash(masterKey, newPassword);

            // Create new master key identifier for master key login
            var masterKeyIdentifier = _passwordCryptoService.CreateMasterKeyIdentifier(newPassword, salt);

            // Update user's salt and password hash
            user.UserSalt = Convert.ToBase64String(salt);
            user.MasterPasswordHash = authHash;
            user.MasterKeyIdentifier = masterKeyIdentifier;
            user.MasterPasswordHint = newPasswordHint;
            user.UpdatedAt = DateTime.UtcNow;

            // Use Identity to handle password reset
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to change password for user {UserId}: {Errors}", user.Id, errors);
                return (false, errors);
            }

            // Update user with new salt and hash
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                _logger.LogError("Failed to update user {UserId} with new password hash: {Errors}", user.Id, errors);
                return (false, errors);
            }

            _logger.LogInformation("Changed password for user {UserId} successfully", user.Id);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", userId);
            return (false, ex.Message);
        }
    }

    public async Task<bool> DeactivateUserAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("Deactivated user {UserId} successfully", user.Id);
                return true;
            }
            
            _logger.LogError("Failed to deactivate user {UserId}: {Errors}", 
                user.Id, 
                string.Join(", ", result.Errors.Select(e => e.Description)));
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> ReactivateUserAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("Reactivated user {UserId} successfully", user.Id);
                return true;
            }
            
            _logger.LogError("Failed to reactivate user {UserId}: {Errors}", 
                user.Id, 
                string.Join(", ", result.Errors.Select(e => e.Description)));
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> DeleteUserAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            var result = await _userManager.DeleteAsync(user);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("Deleted user {UserId} successfully", userId);
                return true;
            }
            
            _logger.LogError("Failed to delete user {UserId}: {Errors}", 
                userId, 
                string.Join(", ", result.Errors.Select(e => e.Description)));
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Verifies the supplied master password against the user's stored hash. Mirrors the
    /// verification used during sign-in so the non-2FA profile-switch flow re-authenticates
    /// for real instead of just swapping the displayed profile.
    /// </summary>
    public async Task<bool> VerifyMasterPasswordAsync(string userId, string masterPassword)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.MasterPasswordHash) || string.IsNullOrEmpty(user.UserSalt))
                return false;

            var salt = Convert.FromBase64String(user.UserSalt);
            return _passwordCryptoService.VerifyMasterPassword(
                masterPassword, user.MasterPasswordHash, salt, user.MasterPasswordIterations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying master password for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Returns whether the user has 2FA enabled, so callers can choose the right step-up flow.
    /// </summary>
    public async Task<bool> IsTwoFactorEnabledAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user?.TwoFactorEnabled == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading 2FA status for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Gets the current user's profile
    /// </summary>
    public async Task<UserDto?> GetCurrentUserAsync()
    {
        // For now, return the first user since we don't have a proper authentication context
        // This should be replaced with proper current user context in a real application
        try
        {
            var user = _userManager.Users.FirstOrDefault();
            if (user == null)
                return null;

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                IsActive = user.IsActive
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return null;
        }
    }

    /// <summary>
    /// Updates an existing user's profile information (alias for UpdateUserProfileAsync)
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> UpdateAsync(UpdateUserProfileDto updateUserDto)
    {
        return await UpdateUserProfileAsync(updateUserDto);
    }

    /// <summary>
    /// Changes a user's password (simplified method for current user)
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        try
        {
            // Get the current user - for now using the first user, but should use proper context
            var currentUser = _userManager.Users.FirstOrDefault();
            if (currentUser == null)
                return (false, "No user found");

            // Use the existing ChangeMasterPasswordAsync method
            return await ChangeMasterPasswordAsync(currentUser.Id, currentPassword, newPassword, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for current user");
            return (false, ex.Message);
        }
    }

    /// <summary>See IUserProfileService.FindByExternalLoginAsync's doc comment.</summary>
    public async Task<UserDto?> FindByExternalLoginAsync(string loginProvider, string providerKey)
    {
        try
        {
            var user = await _userManager.FindByLoginAsync(loginProvider, providerKey);
            if (user == null)
                return null;

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                IsActive = user.IsActive
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error looking up external login {Provider}", loginProvider);
            return null;
        }
    }

    /// <summary>See IUserProfileService.LinkExternalLoginAsync's doc comment - callers must have already verified the master password.</summary>
    public async Task<(bool Success, string? ErrorMessage)> LinkExternalLoginAsync(string userId, string loginProvider, string providerKey, string? providerDisplayName)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return (false, "User not found");

            var existingLogins = await _userManager.GetLoginsAsync(user);
            if (existingLogins.Any(l => l.LoginProvider == loginProvider && l.ProviderKey == providerKey))
                return (true, null); // already linked - idempotent

            var result = await _userManager.AddLoginAsync(user,
                new UserLoginInfo(loginProvider, providerKey, providerDisplayName ?? loginProvider));

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to link external login {Provider} for user {UserId}: {Errors}", loginProvider, userId, errors);
                return (false, errors);
            }

            _logger.LogInformation("Linked external login {Provider} for user {UserId}", loginProvider, userId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking external login {Provider} for user {UserId}", loginProvider, userId);
            return (false, ex.Message);
        }
    }

    /// <summary>See IUserProfileService.GetExternalLoginsAsync's doc comment.</summary>
    public async Task<List<ExternalLoginDto>> GetExternalLoginsAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return new List<ExternalLoginDto>();

        var logins = await _userManager.GetLoginsAsync(user);
        return logins.Select(l => new ExternalLoginDto
        {
            LoginProvider = l.LoginProvider,
            ProviderDisplayName = l.ProviderDisplayName ?? l.LoginProvider
        }).ToList();
    }

    /// <summary>See IUserProfileService.RemoveExternalLoginAsync's doc comment.</summary>
    public async Task<bool> RemoveExternalLoginAsync(string userId, string loginProvider)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return true;

            var logins = await _userManager.GetLoginsAsync(user);
            var match = logins.FirstOrDefault(l => l.LoginProvider == loginProvider);
            if (match == null)
                return true; // nothing to remove

            var result = await _userManager.RemoveLoginAsync(user, match.LoginProvider, match.ProviderKey);
            return result.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing external login {Provider} for user {UserId}", loginProvider, userId);
            return false;
        }
    }
}
