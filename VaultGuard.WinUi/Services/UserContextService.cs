using PasswordManager.Services.Interfaces;
using PasswordManager.Models.DTOs.Auth;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PasswordManager.WinUi.Services;

/// <summary>
/// Service to manage the current user context in the WinUI app.
/// This service helps ensure proper data isolation between users.
/// </summary>
public class UserContextService
{
    private readonly IAuthService _authService;
    private readonly IUserProfileService _userProfileService;
    private UserDto? _currentUser;

    public UserContextService(IAuthService authService, IUserProfileService userProfileService)
    {
        _authService = authService;
        _userProfileService = userProfileService;
    }

    /// <summary>
    /// Gets the current user in the application context
    /// </summary>
    public UserDto? CurrentUser => _currentUser;

    /// <summary>
    /// Gets the current user's ID, or null if no user is logged in
    /// </summary>
    public string? CurrentUserId => _currentUser?.Id;

    /// <summary>
    /// Sets the current user context after authentication
    /// </summary>
    public async Task SetCurrentUserAsync(string userId)
    {
        var user = await _userProfileService.GetUserByIdAsync(userId);
        _currentUser = user;
    }

    /// <summary>
    /// Sets the current user context directly
    /// </summary>
    public void SetCurrentUser(UserDto user)
    {
        _currentUser = user;
    }

    /// <summary>
    /// Clears the current user context (logout)
    /// </summary>
    public void ClearCurrentUser()
    {
        _currentUser = null;
    }

    /// <summary>
    /// Switches to a different user profile
    /// </summary>
    public async Task<bool> SwitchUserAsync(UserDto targetUser)
    {
        try
        {
            // In a complete implementation, this would:
            // 1. Validate the user switch is allowed
            // 2. Update the authentication context
            // 3. Clear any cached data from the previous user
            // 4. Reload data for the new user

            _currentUser = targetUser;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Ensures that data operations are filtered by the current user
    /// This is a helper method that should be used by data services
    /// </summary>
    public void ValidateUserContext()
    {
        if (_currentUser == null)
        {
            throw new InvalidOperationException("No user context available. User must be authenticated.");
        }
    }

    /// <summary>
    /// Gets a filter predicate for user-owned data
    /// This demonstrates how data services should filter by user
    /// </summary>
    public System.Linq.Expressions.Expression<Func<T, bool>> GetUserFilter<T>() where T : class
    {
        if (_currentUser == null)
        {
            throw new InvalidOperationException("No user context available");
        }

        var userId = _currentUser.Id;

        // This is a simplified example - in reality, different entity types
        // might have different properties for user association
        return entity => EF.Property<string>(entity, "UserId") == userId;
    }
}

/// <summary>
/// Static class to demonstrate how entity framework queries should be enhanced
/// for proper user data isolation
/// </summary>
public static class UserDataFilterExtensions
{
    /// <summary>
    /// Example extension method showing how to filter queries by current user
    /// </summary>
    public static IQueryable<T> ForCurrentUser<T>(this IQueryable<T> query, string userId) where T : class
    {
        // This would need to be implemented for each entity type that has user association
        // For example:
        // - PasswordItem.UserId
        // - Category.UserId
        // - Collection.UserId
        // etc.

        return query.Where(entity => EF.Property<string>(entity, "UserId") == userId);
    }
}