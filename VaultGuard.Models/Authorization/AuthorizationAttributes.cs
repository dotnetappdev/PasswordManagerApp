using Microsoft.AspNetCore.Authorization;

namespace VaultGuard.Models.Authorization;

/// <summary>
/// Custom authorization attribute for permission-based access control
/// </summary>
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
    {
        Policy = permission;
    }
}

/// <summary>
/// Authorization attribute specifically for password-related operations
/// </summary>
public class RequirePasswordPermissionAttribute : RequirePermissionAttribute
{
    public RequirePasswordPermissionAttribute(string permission) : base(permission)
    {
    }
}

/// <summary>
/// Authorization attribute for child-safe operations (operations that children can perform)
/// </summary>
public class ChildSafeOperationAttribute : AuthorizeAttribute
{
    public ChildSafeOperationAttribute()
    {
        Policy = "ChildSafeOperation";
    }
}

/// <summary>
/// Authorization attribute for operations that require parent or admin role
/// </summary>
public class RequireParentOrAdminAttribute : AuthorizeAttribute
{
    public RequireParentOrAdminAttribute()
    {
        Roles = $"{ApplicationRoles.Parent},{ApplicationRoles.Admin}";
    }
}

/// <summary>
/// Authorization attribute for operations that require admin role only
/// </summary>
public class RequireAdminAttribute : AuthorizeAttribute
{
    public RequireAdminAttribute()
    {
        Roles = ApplicationRoles.Admin;
    }
}

/// <summary>
/// Authorization attribute for VaultGuard.Admin-only operations (customers, subscriptions, license
/// keys, tenants) — separate from <see cref="RequireAdminAttribute"/>, which gates in-app admin actions.
/// </summary>
public class RequireSuperAdminAttribute : AuthorizeAttribute
{
    public RequireSuperAdminAttribute()
    {
        Roles = ApplicationRoles.SuperAdmin;
    }
}

/// <summary>
/// Authorization attribute for operations that children cannot perform
/// </summary>
public class RestrictedFromChildrenAttribute : AuthorizeAttribute
{
    public RestrictedFromChildrenAttribute()
    {
        Policy = "RestrictedFromChildren";
    }
}