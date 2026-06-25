using VaultGuard.Models;

namespace VaultGuard.Services.Interfaces;

/// <summary>
/// Service for managing child permissions and parent-child relationships
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Check if a user has a specific permission
    /// </summary>
    Task<bool> HasPermissionAsync(string userId, string permission);

    /// <summary>
    /// Check if a user can perform an operation on a specific resource
    /// </summary>
    Task<bool> CanAccessResourceAsync(string userId, string resourceOwnerId, string permission);

    /// <summary>
    /// Get user roles
    /// </summary>
    Task<IList<string>> GetUserRolesAsync(string userId);

    /// <summary>
    /// Check if user is in a specific role
    /// </summary>
    Task<bool> IsInRoleAsync(string userId, string role);

    /// <summary>
    /// Get child permission configuration for a user
    /// </summary>
    Task<ChildPermissionConfig?> GetChildPermissionConfigAsync(string childUserId, string parentUserId);

    /// <summary>
    /// Create or update child permission configuration
    /// </summary>
    Task<ChildPermissionConfig> SetChildPermissionConfigAsync(string childUserId, string parentUserId, ChildPermissionConfig config);

    /// <summary>
    /// Check if a user is a child of another user
    /// </summary>
    Task<bool> IsChildOfAsync(string childUserId, string parentUserId);

    /// <summary>
    /// Check if a user is a parent of another user
    /// </summary>
    Task<bool> IsParentOfAsync(string parentUserId, string childUserId);

    /// <summary>
    /// Get all children of a parent user
    /// </summary>
    Task<IEnumerable<ApplicationUser>> GetChildrenAsync(string parentUserId);

    /// <summary>
    /// Get the parent of a child user
    /// </summary>
    Task<ApplicationUser?> GetParentAsync(string childUserId);

    /// <summary>
    /// Create a parent-child relationship
    /// </summary>
    Task<UserRelationship> CreateParentChildRelationshipAsync(string parentUserId, string childUserId, string createdBy, string? notes = null);

    /// <summary>
    /// Remove a parent-child relationship
    /// </summary>
    Task<bool> RemoveParentChildRelationshipAsync(string parentUserId, string childUserId);

    /// <summary>
    /// Check if a child user has permission to perform a specific password operation
    /// </summary>
    Task<bool> ChildCanPerformPasswordOperationAsync(string childUserId, string operation);

    /// <summary>
    /// Check if current time is within allowed access hours for a child
    /// </summary>
    Task<bool> IsWithinAllowedAccessTimeAsync(string childUserId);

    /// <summary>
    /// Check if today is an allowed day for child access
    /// </summary>
    Task<bool> IsAllowedDayOfWeekAsync(string childUserId);

    /// <summary>
    /// Get default child permission configuration
    /// </summary>
    ChildPermissionConfig GetDefaultChildPermissionConfig();
}

/// <summary>
/// Service for managing child activities and logging
/// </summary>
public interface IChildActivityService
{
    /// <summary>
    /// Log child activity
    /// </summary>
    Task LogChildActivityAsync(string childUserId, string activity, string? details = null);

    /// <summary>
    /// Get child activities for a parent
    /// </summary>
    Task<IEnumerable<ChildActivity>> GetChildActivitiesAsync(string parentUserId, string? childUserId = null, DateTime? fromDate = null, DateTime? toDate = null);
}

/// <summary>
/// Child activity log entry
/// </summary>
public class ChildActivity
{
    public int Id { get; set; }
    public string ChildUserId { get; set; } = string.Empty;
    public string ParentUserId { get; set; } = string.Empty;
    public string Activity { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}