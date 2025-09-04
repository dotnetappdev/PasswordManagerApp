using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using PasswordManager.Models;
using PasswordManager.DAL.Interfaces;

namespace PasswordManager.DAL.Seed;

/// <summary>
/// Service for seeding Identity users and roles
/// </summary>
public class IdentityDataSeeder
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILogger<IdentityDataSeeder> _logger;

    public IdentityDataSeeder(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger<IdentityDataSeeder> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Seeds roles and default users
    /// </summary>
    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        await SeedDefaultUsersAsync();
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
}