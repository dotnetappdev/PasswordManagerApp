using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using PasswordManager.Models;
using PasswordManager.DAL.Interfaces;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.DAL.Seed;

/// <summary>
/// Service for seeding Identity users and roles
/// </summary>
public class IdentityDataSeeder
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<IdentityDataSeeder> _logger;

    public IdentityDataSeeder(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IPermissionService permissionService,
        ILogger<IdentityDataSeeder> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Seeds roles and default users
    /// </summary>
    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        await SeedDefaultUsersAsync();
        await SeedParentChildRelationshipsAsync();
    }

    /// <summary>
    /// Seeds application roles
    /// </summary>
    private async Task SeedRolesAsync()
    {
        foreach (var roleName in ApplicationRoles.AllRoles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var role = new ApplicationRole
                {
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant(),
                    Description = ApplicationRoles.RoleDescriptions[roleName],
                    DisplayOrder = ApplicationRoles.RoleDisplayOrder[roleName],
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    IsActive = true
                };

                var result = await _roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Created role: {RoleName}", roleName);
                }
                else
                {
                    _logger.LogError("Failed to create role {RoleName}: {Errors}", 
                        roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    /// <summary>
    /// Seeds default users for each role
    /// </summary>
    private async Task SeedDefaultUsersAsync()
    {
        // Create default admin user
        await CreateDefaultUserAsync(
            "admin@passwordmanager.local",
            "Administrator",
            "User",
            ApplicationRoles.Admin,
            "Admin123!"
        );

        // Create default parent user
        await CreateDefaultUserAsync(
            "parent@passwordmanager.local",
            "Parent",
            "User",
            ApplicationRoles.Parent,
            "Parent123!"
        );

        // Create default standard user
        await CreateDefaultUserAsync(
            "user@passwordmanager.local",
            "Regular",
            "User",
            ApplicationRoles.User,
            "User123!"
        );

        // Create default child user
        await CreateDefaultUserAsync(
            "child@passwordmanager.local",
            "Child",
            "User",
            ApplicationRoles.Child,
            "Child123!"
        );
    }

    /// <summary>
    /// Creates a default user with specified role
    /// </summary>
    private async Task CreateDefaultUserAsync(
        string email,
        string firstName,
        string lastName,
        string roleName,
        string password)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            _logger.LogInformation("User {Email} already exists", email);
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, roleName);
            _logger.LogInformation("Created default user {Email} with role {Role}", email, roleName);
        }
        else
        {
            _logger.LogError("Failed to create user {Email}: {Errors}", 
                email, string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    /// <summary>
    /// Seeds parent-child relationships and permission configurations
    /// </summary>
    private async Task SeedParentChildRelationshipsAsync()
    {
        try
        {
            // Get the parent and child users
            var parentUser = await _userManager.FindByEmailAsync("parent@passwordmanager.local");
            var childUser = await _userManager.FindByEmailAsync("child@passwordmanager.local");

            if (parentUser == null || childUser == null)
            {
                _logger.LogWarning("Parent or child user not found, skipping relationship seeding");
                return;
            }

            // Check if relationship already exists
            if (await _permissionService.IsParentOfAsync(parentUser.Id, childUser.Id))
            {
                _logger.LogInformation("Parent-child relationship already exists");
                return;
            }

            // Create parent-child relationship
            await _permissionService.CreateParentChildRelationshipAsync(
                parentUser.Id, 
                childUser.Id, 
                "system", 
                "Default parent-child relationship created during seeding");

            _logger.LogInformation("Created parent-child relationship between {ParentEmail} and {ChildEmail}", 
                parentUser.Email, childUser.Email);

            // Set up default child permissions (restrictive but allow viewing)
            var childPermissionConfig = new ChildPermissionConfig
            {
                ChildUserId = childUser.Id,
                ParentUserId = parentUser.Id,
                CanViewPasswords = true,          // Child can view passwords
                CanCreatePasswords = false,       // Child cannot create passwords
                CanEditPasswords = false,         // Child cannot edit passwords
                CanDeletePasswords = false,       // Child cannot delete passwords
                CanRevealPasswords = false,       // Child cannot reveal sensitive data
                CanSharePasswords = false,        // Child cannot share passwords
                CanExportData = false,           // Child cannot export data
                CanCreateCollections = false,     // Child cannot create collections
                CanManageOwnCollections = true,   // Child can organize their view
                MaxPasswordItems = 0,            // No limit on viewing
                AccessStartTime = "08:00",       // Access allowed from 8 AM
                AccessEndTime = "20:00",         // Access allowed until 8 PM
                AllowedDaysOfWeek = "1,2,3,4,5,6,0", // All days allowed (0=Sunday, 1=Monday, etc.)
                RequireParentApproval = true,     // Require parent approval for changes
                LogActivities = true             // Log all child activities
            };

            await _permissionService.SetChildPermissionConfigAsync(childUser.Id, parentUser.Id, childPermissionConfig);

            _logger.LogInformation("Created default permission configuration for child {ChildEmail}", childUser.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding parent-child relationships");
        }
    }
}