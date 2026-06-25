using Microsoft.AspNetCore.Identity;
using PasswordManager.Models;
using PasswordManager.Models.DTOs.Auth;

namespace PasswordManager.Services.Interfaces;

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
}
