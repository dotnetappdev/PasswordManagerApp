using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PasswordManager.DAL;
using PasswordManager.Models;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.Services.Services;

/// <summary>
/// Implementation of permission service for managing child permissions and parent-child relationships
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly PasswordManagerDbContextApp _context;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(
        UserManager<ApplicationUser> userManager,
        PasswordManagerDbContextApp context,
        ILogger<PermissionService> logger)
    {
        _userManager = userManager;
        _context = context;
        _logger = logger;
    }

    public async Task<bool> HasPermissionAsync(string userId, string permission)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var roles = await _userManager.GetRolesAsync(user);
            
            foreach (var role in roles)
            {
                if (Permissions.RolePermissions.RoleHasPermission(role, permission))
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission {Permission} for user {UserId}", permission, userId);
            return false;
        }
    }

    public async Task<bool> CanAccessResourceAsync(string userId, string resourceOwnerId, string permission)
    {
        try
        {
            // Admin can access everything
            if (await IsInRoleAsync(userId, ApplicationRoles.Admin))
            {
                return await HasPermissionAsync(userId, permission);
            }

            // User can access their own resources
            if (userId == resourceOwnerId)
            {
                return await HasPermissionAsync(userId, permission);
            }

            // Parent can access child's resources (with appropriate permissions)
            if (await IsParentOfAsync(userId, resourceOwnerId))
            {
                return await HasPermissionAsync(userId, permission);
            }

            // Child can access parent's shared resources (read-only)
            if (await IsChildOfAsync(userId, resourceOwnerId) && 
                permission == Permissions.Passwords.View)
            {
                return await ChildCanPerformPasswordOperationAsync(userId, "view");
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking resource access for user {UserId}, resource owner {ResourceOwnerId}, permission {Permission}",
                userId, resourceOwnerId, permission);
            return false;
        }
    }

    public async Task<IList<string>> GetUserRolesAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return new List<string>();

        return await _userManager.GetRolesAsync(user);
    }

    public async Task<bool> IsInRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        return await _userManager.IsInRoleAsync(user, role);
    }

    public async Task<ChildPermissionConfig?> GetChildPermissionConfigAsync(string childUserId, string parentUserId)
    {
        return await _context.ChildPermissionConfigs
            .FirstOrDefaultAsync(c => c.ChildUserId == childUserId && c.ParentUserId == parentUserId);
    }

    public async Task<ChildPermissionConfig> SetChildPermissionConfigAsync(string childUserId, string parentUserId, ChildPermissionConfig config)
    {
        var existingConfig = await GetChildPermissionConfigAsync(childUserId, parentUserId);
        
        if (existingConfig != null)
        {
            // Update existing configuration
            existingConfig.CanViewPasswords = config.CanViewPasswords;
            existingConfig.CanCreatePasswords = config.CanCreatePasswords;
            existingConfig.CanEditPasswords = config.CanEditPasswords;
            existingConfig.CanDeletePasswords = config.CanDeletePasswords;
            existingConfig.CanRevealPasswords = config.CanRevealPasswords;
            existingConfig.CanSharePasswords = config.CanSharePasswords;
            existingConfig.CanExportData = config.CanExportData;
            existingConfig.CanCreateCollections = config.CanCreateCollections;
            existingConfig.CanManageOwnCollections = config.CanManageOwnCollections;
            existingConfig.MaxPasswordItems = config.MaxPasswordItems;
            existingConfig.AccessStartTime = config.AccessStartTime;
            existingConfig.AccessEndTime = config.AccessEndTime;
            existingConfig.AllowedDaysOfWeek = config.AllowedDaysOfWeek;
            existingConfig.RequireParentApproval = config.RequireParentApproval;
            existingConfig.LogActivities = config.LogActivities;
            existingConfig.LastModified = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existingConfig;
        }
        else
        {
            // Create new configuration
            config.ChildUserId = childUserId;
            config.ParentUserId = parentUserId;
            config.CreatedAt = DateTime.UtcNow;
            config.LastModified = DateTime.UtcNow;

            _context.ChildPermissionConfigs.Add(config);
            await _context.SaveChangesAsync();
            return config;
        }
    }

    public async Task<bool> IsChildOfAsync(string childUserId, string parentUserId)
    {
        return await _context.UserRelationships
            .AnyAsync(r => r.ChildUserId == childUserId && 
                          r.ParentUserId == parentUserId && 
                          r.IsActive && 
                          r.RelationshipType == UserRelationshipTypes.ParentChild);
    }

    public async Task<bool> IsParentOfAsync(string parentUserId, string childUserId)
    {
        return await _context.UserRelationships
            .AnyAsync(r => r.ParentUserId == parentUserId && 
                          r.ChildUserId == childUserId && 
                          r.IsActive && 
                          r.RelationshipType == UserRelationshipTypes.ParentChild);
    }

    public async Task<IEnumerable<ApplicationUser>> GetChildrenAsync(string parentUserId)
    {
        var childRelationships = await _context.UserRelationships
            .Include(r => r.ChildUser)
            .Where(r => r.ParentUserId == parentUserId && 
                       r.IsActive && 
                       r.RelationshipType == UserRelationshipTypes.ParentChild)
            .ToListAsync();

        return childRelationships.Select(r => r.ChildUser!).Where(u => u != null);
    }

    public async Task<ApplicationUser?> GetParentAsync(string childUserId)
    {
        var parentRelationship = await _context.UserRelationships
            .Include(r => r.ParentUser)
            .FirstOrDefaultAsync(r => r.ChildUserId == childUserId && 
                                     r.IsActive && 
                                     r.RelationshipType == UserRelationshipTypes.ParentChild);

        return parentRelationship?.ParentUser;
    }

    public async Task<UserRelationship> CreateParentChildRelationshipAsync(string parentUserId, string childUserId, string createdBy, string? notes = null)
    {
        // Check if relationship already exists
        var existingRelationship = await _context.UserRelationships
            .FirstOrDefaultAsync(r => r.ParentUserId == parentUserId && 
                                     r.ChildUserId == childUserId && 
                                     r.RelationshipType == UserRelationshipTypes.ParentChild);

        if (existingRelationship != null)
        {
            existingRelationship.IsActive = true;
            existingRelationship.LastModified = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return existingRelationship;
        }

        var relationship = new UserRelationship
        {
            ParentUserId = parentUserId,
            ChildUserId = childUserId,
            RelationshipType = UserRelationshipTypes.ParentChild,
            CreatedBy = createdBy,
            Notes = notes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow
        };

        _context.UserRelationships.Add(relationship);
        await _context.SaveChangesAsync();

        // Create default permission configuration for the child
        var defaultConfig = GetDefaultChildPermissionConfig();
        await SetChildPermissionConfigAsync(childUserId, parentUserId, defaultConfig);

        return relationship;
    }

    public async Task<bool> RemoveParentChildRelationshipAsync(string parentUserId, string childUserId)
    {
        var relationship = await _context.UserRelationships
            .FirstOrDefaultAsync(r => r.ParentUserId == parentUserId && 
                                     r.ChildUserId == childUserId && 
                                     r.RelationshipType == UserRelationshipTypes.ParentChild);

        if (relationship == null) return false;

        relationship.IsActive = false;
        relationship.LastModified = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ChildCanPerformPasswordOperationAsync(string childUserId, string operation)
    {
        // Check if user is actually a child
        if (!await IsInRoleAsync(childUserId, ApplicationRoles.Child))
        {
            return false;
        }

        // Get parent
        var parent = await GetParentAsync(childUserId);
        if (parent == null) return false;

        // Get permission configuration
        var config = await GetChildPermissionConfigAsync(childUserId, parent.Id);
        if (config == null)
        {
            config = GetDefaultChildPermissionConfig();
        }

        // Check time and day restrictions
        if (!await IsWithinAllowedAccessTimeAsync(childUserId) || 
            !await IsAllowedDayOfWeekAsync(childUserId))
        {
            return false;
        }

        // Check specific operation permissions
        return operation.ToLower() switch
        {
            "view" => config.CanViewPasswords,
            "create" => config.CanCreatePasswords,
            "edit" => config.CanEditPasswords,
            "delete" => config.CanDeletePasswords,
            "reveal" => config.CanRevealPasswords,
            "share" => config.CanSharePasswords,
            "export" => config.CanExportData,
            _ => false
        };
    }

    public async Task<bool> IsWithinAllowedAccessTimeAsync(string childUserId)
    {
        var parent = await GetParentAsync(childUserId);
        if (parent == null) return true; // If no parent, no restrictions

        var config = await GetChildPermissionConfigAsync(childUserId, parent.Id);
        if (config == null || string.IsNullOrEmpty(config.AccessStartTime) || string.IsNullOrEmpty(config.AccessEndTime))
        {
            return true; // No time restrictions configured
        }

        var now = DateTime.Now.TimeOfDay;
        
        if (TimeSpan.TryParse(config.AccessStartTime, out var startTime) &&
            TimeSpan.TryParse(config.AccessEndTime, out var endTime))
        {
            if (startTime <= endTime)
            {
                // Same day restriction (e.g., 08:00 to 20:00)
                return now >= startTime && now <= endTime;
            }
            else
            {
                // Overnight restriction (e.g., 20:00 to 08:00 next day)
                return now >= startTime || now <= endTime;
            }
        }

        return true; // Invalid time format, allow access
    }

    public async Task<bool> IsAllowedDayOfWeekAsync(string childUserId)
    {
        var parent = await GetParentAsync(childUserId);
        if (parent == null) return true; // If no parent, no restrictions

        var config = await GetChildPermissionConfigAsync(childUserId, parent.Id);
        if (config == null || string.IsNullOrEmpty(config.AllowedDaysOfWeek))
        {
            return true; // No day restrictions configured
        }

        var today = (int)DateTime.Now.DayOfWeek; // 0 = Sunday, 1 = Monday, etc.
        var allowedDays = config.AllowedDaysOfWeek.Split(',')
            .Select(d => d.Trim())
            .Where(d => int.TryParse(d, out _))
            .Select(int.Parse)
            .ToList();

        return allowedDays.Contains(today);
    }

    public ChildPermissionConfig GetDefaultChildPermissionConfig()
    {
        return new ChildPermissionConfig
        {
            CanViewPasswords = true,          // Children can view passwords by default
            CanCreatePasswords = false,       // Children cannot create passwords
            CanEditPasswords = false,         // Children cannot edit passwords  
            CanDeletePasswords = false,       // Children cannot delete passwords
            CanRevealPasswords = false,       // Children cannot reveal sensitive data
            CanSharePasswords = false,        // Children cannot share passwords
            CanExportData = false,           // Children cannot export data
            CanCreateCollections = false,     // Children cannot create collections
            CanManageOwnCollections = true,   // Children can organize their view
            MaxPasswordItems = 0,            // No limit on password items
            RequireParentApproval = true,     // Require parent approval for changes
            LogActivities = true             // Log all child activities
        };
    }
}