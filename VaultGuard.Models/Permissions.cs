namespace VaultGuard.Models;

/// <summary>
/// Application permissions for role-based authorization
/// </summary>
public static class Permissions
{
    /// <summary>
    /// Password management permissions
    /// </summary>
    public static class Passwords
    {
        public const string View = "Permissions.Passwords.View";
        public const string Create = "Permissions.Passwords.Create";
        public const string Edit = "Permissions.Passwords.Edit";
        public const string Delete = "Permissions.Passwords.Delete";
        public const string Share = "Permissions.Passwords.Share";
        public const string Export = "Permissions.Passwords.Export";
        public const string Import = "Permissions.Passwords.Import";
        public const string ViewSensitive = "Permissions.Passwords.ViewSensitive"; // For revealing passwords
    }

    /// <summary>
    /// User management permissions
    /// </summary>
    public static class Users
    {
        public const string View = "Permissions.Users.View";
        public const string Create = "Permissions.Users.Create";
        public const string Edit = "Permissions.Users.Edit";
        public const string Delete = "Permissions.Users.Delete";
        public const string ManageRoles = "Permissions.Users.ManageRoles";
        public const string ViewChildren = "Permissions.Users.ViewChildren";
        public const string ManageChildren = "Permissions.Users.ManageChildren";
        public const string AssignParent = "Permissions.Users.AssignParent";
    }

    /// <summary>
    /// System administration permissions
    /// </summary>
    public static class Administration
    {
        public const string ViewSystem = "Permissions.Administration.ViewSystem";
        public const string ManageSystem = "Permissions.Administration.ManageSystem";
        public const string ViewLogs = "Permissions.Administration.ViewLogs";
        public const string ManageBackups = "Permissions.Administration.ManageBackups";
        public const string ManageSettings = "Permissions.Administration.ManageSettings";
    }

    /// <summary>
    /// Collections and organization permissions
    /// </summary>
    public static class Collections
    {
        public const string View = "Permissions.Collections.View";
        public const string Create = "Permissions.Collections.Create";
        public const string Edit = "Permissions.Collections.Edit";
        public const string Delete = "Permissions.Collections.Delete";
        public const string Share = "Permissions.Collections.Share";
    }

    /// <summary>
    /// Get all permissions for a role
    /// </summary>
    public static class RolePermissions
    {
        /// <summary>
        /// Admin role permissions - full access to everything
        /// </summary>
        public static readonly string[] Admin = 
        {
            // Password permissions
            Passwords.View, Passwords.Create, Passwords.Edit, Passwords.Delete,
            Passwords.Share, Passwords.Export, Passwords.Import, Passwords.ViewSensitive,
            
            // User management
            Users.View, Users.Create, Users.Edit, Users.Delete, Users.ManageRoles,
            Users.ViewChildren, Users.ManageChildren, Users.AssignParent,
            
            // Administration
            Administration.ViewSystem, Administration.ManageSystem, Administration.ViewLogs,
            Administration.ManageBackups, Administration.ManageSettings,
            
            // Collections
            Collections.View, Collections.Create, Collections.Edit, Collections.Delete, Collections.Share
        };

        /// <summary>
        /// Parent role permissions - can manage their own data and child accounts
        /// </summary>
        public static readonly string[] Parent = 
        {
            // Password permissions (for own passwords)
            Passwords.View, Passwords.Create, Passwords.Edit, Passwords.Delete,
            Passwords.Share, Passwords.Export, Passwords.ViewSensitive,
            
            // Child management
            Users.ViewChildren, Users.ManageChildren, Users.AssignParent,
            
            // Collections
            Collections.View, Collections.Create, Collections.Edit, Collections.Delete, Collections.Share
        };

        /// <summary>
        /// User role permissions - standard user access
        /// </summary>
        public static readonly string[] User = 
        {
            // Password permissions (for own passwords only)
            Passwords.View, Passwords.Create, Passwords.Edit, Passwords.Delete,
            Passwords.ViewSensitive,
            
            // Collections
            Collections.View, Collections.Create, Collections.Edit, Collections.Delete
        };

        /// <summary>
        /// Child role permissions - read-only access, managed by parent
        /// </summary>
        public static readonly string[] Child = 
        {
            // Password permissions (read-only)
            Passwords.View,
            
            // Collections (read-only)
            Collections.View
        };

        /// <summary>
        /// Get permissions for a specific role
        /// </summary>
        public static string[] GetPermissionsForRole(string role)
        {
            return role switch
            {
                ApplicationRoles.Admin => Admin,
                ApplicationRoles.Parent => Parent,
                ApplicationRoles.User => User,
                ApplicationRoles.Child => Child,
                _ => Array.Empty<string>()
            };
        }

        /// <summary>
        /// Check if a role has a specific permission
        /// </summary>
        public static bool RoleHasPermission(string role, string permission)
        {
            var rolePermissions = GetPermissionsForRole(role);
            return rolePermissions.Contains(permission);
        }
    }
}