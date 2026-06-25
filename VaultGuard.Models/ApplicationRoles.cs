namespace PasswordManager.Models;

/// <summary>
/// Standard application roles with consistent naming
/// </summary>
public static class ApplicationRoles
{
    /// <summary>
    /// Administrator role - full system access
    /// </summary>
    public const string Admin = "Admin";

    /// <summary>
    /// Parent role - can manage child accounts and family data
    /// </summary>
    public const string Parent = "Parent";

    /// <summary>
    /// Child role - limited access, managed by parents
    /// </summary>
    public const string Child = "Child";

    /// <summary>
    /// Standard user role - regular user access
    /// </summary>
    public const string User = "User";

    /// <summary>
    /// Get all available roles
    /// </summary>
    public static readonly string[] AllRoles = { Admin, Parent, Child, User };

    /// <summary>
    /// Get role descriptions for seeding
    /// </summary>
    public static readonly Dictionary<string, string> RoleDescriptions = new()
    {
        { Admin, "Administrator with full system access" },
        { Parent, "Parent user who can manage child accounts and family password data" },
        { Child, "Child user with limited access, managed by parent accounts" },
        { User, "Standard user with access to personal password data" }
    };

    /// <summary>
    /// Get role display order for UI
    /// </summary>
    public static readonly Dictionary<string, int> RoleDisplayOrder = new()
    {
        { Admin, 1 },
        { Parent, 2 },
        { User, 3 },
        { Child, 4 }
    };
}