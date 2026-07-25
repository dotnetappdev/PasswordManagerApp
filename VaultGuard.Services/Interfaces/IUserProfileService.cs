using Microsoft.AspNetCore.Identity;
using VaultGuard.Models;
using VaultGuard.Models.DTOs.Auth;

namespace VaultGuard.Services.Interfaces;

public interface IUserProfileService
{
    /// <summary>
    /// Gets all users in the system
    /// </summary>
    Task<List<UserDto>> GetAllUsersAsync();
    
    /// <summary>
    /// Gets a user by their ID
    /// </summary>
    Task<UserDto?> GetUserByIdAsync(string userId);
    
    /// <summary>
    /// Gets a user by their email
    /// </summary>
    Task<UserDto?> GetUserByEmailAsync(string email);
    
    /// <summary>
    /// Gets the current user's profile
    /// </summary>
    Task<UserDto?> GetCurrentUserAsync();
    
    /// <summary>
    /// Creates a new user with proper salt generation
    /// </summary>
    Task<(IdentityResult Result, ApplicationUser? User, string? ErrorMessage)> CreateUserAsync(CreateUserProfileDto createUserDto);
    
    /// <summary>
    /// Updates an existing user's profile information
    /// </summary>
    Task<(bool Success, string? ErrorMessage)> UpdateUserProfileAsync(UpdateUserProfileDto updateUserDto);
    
    /// <summary>
    /// Updates an existing user's profile information (alias for UpdateUserProfileAsync)
    /// </summary>
    Task<(bool Success, string? ErrorMessage)> UpdateAsync(UpdateUserProfileDto updateUserDto);
    
    /// <summary>
    /// Changes a user's master password
    /// </summary>
    Task<(bool Success, string? ErrorMessage)> ChangeMasterPasswordAsync(
        string userId, 
        string currentPassword, 
        string newPassword, 
        string? newPasswordHint);
    
    /// <summary>
    /// Changes a user's password (simplified method for current user)
    /// </summary>
    Task<(bool Success, string? ErrorMessage)> ChangePasswordAsync(
        string currentPassword, 
        string newPassword);
    
    /// <summary>
    /// Deactivates a user account (soft delete)
    /// </summary>
    Task<bool> DeactivateUserAsync(string userId);
    
    /// <summary>
    /// Reactivates a previously deactivated user account
    /// </summary>
    Task<bool> ReactivateUserAsync(string userId);
    
    /// <summary>
    /// Permanently deletes a user account
    /// </summary>
    Task<bool> DeleteUserAsync(string userId);

    /// <summary>
    /// Verifies that the supplied master password matches the stored hash for the given user.
    /// Used by the normal (non-2FA) profile-switch / step-up flow.
    /// </summary>
    Task<bool> VerifyMasterPasswordAsync(string userId, string masterPassword);

    /// <summary>
    /// Returns true if the given user has two-factor authentication enabled. Lets callers
    /// pick the correct step-up workflow (authenticator code vs. master password) without
    /// loading the full user entity.
    /// </summary>
    Task<bool> IsTwoFactorEnabledAsync(string userId);

    /// <summary>
    /// Finds the local user linked to the given external identity provider + provider-specific
    /// subject ("sub") claim, via ASP.NET Identity's built-in AspNetUserLogins table. This is the
    /// robust SSO lookup: unlike matching on email (which can change at either the IdP or locally),
    /// a persisted login link keeps resolving correctly even if the two diverge. Returns null the
    /// first time a given provider+subject signs in, before any link has been created - see
    /// <see cref="LinkExternalLoginAsync"/>.
    /// </summary>
    Task<UserDto?> FindByExternalLoginAsync(string loginProvider, string providerKey);

    /// <summary>
    /// Persists that the given local user is the same person as the given external identity
    /// (LoginProvider + ProviderKey, e.g. "google" + the IdP's "sub" claim) via AspNetUserLogins.
    /// Call this ONLY after the caller has separately verified the user holds this account's master
    /// password - SSO identity alone must never be sufficient to create the link, or anyone who
    /// merely knows a victim's email could preempt it with their own SSO account and silently
    /// redirect future SSO sign-ins to themselves. Idempotent: re-linking the same provider+key to
    /// the same user is a no-op.
    /// </summary>
    Task<(bool Success, string? ErrorMessage)> LinkExternalLoginAsync(string userId, string loginProvider, string providerKey, string? providerDisplayName);

    /// <summary>Lists the external identity providers currently linked to a user, e.g. for a "Linked accounts" settings page.</summary>
    Task<List<ExternalLoginDto>> GetExternalLoginsAsync(string userId);

    /// <summary>Removes a previously linked external identity. Returns true even if no such link existed.</summary>
    Task<bool> RemoveExternalLoginAsync(string userId, string loginProvider);
}
