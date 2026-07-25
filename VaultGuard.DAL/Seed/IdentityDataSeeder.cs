using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using VaultGuard.Models;
using VaultGuard.DAL.Interfaces;
using VaultGuard.Crypto.Interfaces;

namespace VaultGuard.DAL.Seed;

/// <summary>
/// Service for seeding Identity users and roles
/// </summary>
public class IdentityDataSeeder
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IPasswordCryptoService _passwordCryptoService;
    private readonly VaultGuardDbContext _dbContext;
    // private readonly IPermissionService _permissionService;
    private readonly ILogger<IdentityDataSeeder> _logger;
    
    // Common master key for all seeded users to enable master-key-only login
    private const string CommonMasterKey = "7hm3Z!Csu:Y64nm";

    public IdentityDataSeeder(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IPasswordCryptoService passwordCryptoService,
        VaultGuardDbContext dbContext,
        // IPermissionService permissionService,
        ILogger<IdentityDataSeeder> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        _passwordCryptoService = passwordCryptoService ?? throw new ArgumentNullException(nameof(passwordCryptoService));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        // _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Seeds roles and default users
    /// </summary>
    public async Task SeedAsync(bool includeDefaultUsers = true)
    {
        // Roles are always needed. Default demo accounts are optional so a fresh install can show the
        // first-run "Create Master Key" setup instead of pre-created users (the web app seeds roles only
        // at startup and creates default accounts on demand via the login button).
        await SeedRolesAsync();
        if (includeDefaultUsers)
            await SeedDefaultUsersAsync();
        // await SeedParentChildRelationshipsAsync(); // TODO: Re-enable after fixing circular dependency
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
    /// Seeds default users for each role with a common master key
    /// </summary>
    private async Task SeedDefaultUsersAsync()
    {
        // Bootstrap super admin for VaultGuard.Admin (customers/subscriptions/license keys/tenants).
        // Unlike the other seeded accounts, VaultGuard.Admin authenticates via ASP.NET Identity's own
        // email+password login (POST /login), not the master-key flow — so this account needs a REAL,
        // known Identity password, not the usual random discarded one. CHANGE THIS PASSWORD IMMEDIATELY
        // after first login — see docs/ADMIN_MULTITENANCY.md.
        await CreateDefaultUserWithMasterKeyAsync(
            "superadmin@passwordmanager.local",
            "Super",
            "Admin",
            ApplicationRoles.SuperAdmin,
            CommonMasterKey,
            identityPassword: CommonMasterKey
        );

        // Create default admin user with common master key
        await CreateDefaultUserWithMasterKeyAsync(
            "admin@passwordmanager.local",
            "Administrator",
            "User",
            ApplicationRoles.Admin,
            CommonMasterKey
        );

        // Create default parent user with common master key
        await CreateDefaultUserWithMasterKeyAsync(
            "parent@passwordmanager.local",
            "Parent",
            "User",
            ApplicationRoles.Parent,
            CommonMasterKey
        );

        // Create default standard user with common master key
        await CreateDefaultUserWithMasterKeyAsync(
            "user@passwordmanager.local",
            "Regular",
            "User",
            ApplicationRoles.User,
            CommonMasterKey
        );

        // Create default child user with common master key
        await CreateDefaultUserWithMasterKeyAsync(
            "child@passwordmanager.local",
            "Child",
            "User",
            ApplicationRoles.Child,
            CommonMasterKey
        );
        
        _logger.LogInformation("All default users created with common master key: {MasterKey}", CommonMasterKey);
    }

    /// <summary>
    /// Creates a default user with specified role and master key
    /// </summary>
    private async Task CreateDefaultUserWithMasterKeyAsync(
        string email,
        string firstName,
        string lastName,
        string roleName,
        string masterPassword,
        string? identityPassword = null)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            // Account already exists — refresh its master-key hash/identifier so it matches the current
            // CommonMasterKey (keeps the existing salt so nothing else needs re-deriving).
            try
            {
                byte[] existingSalt = !string.IsNullOrWhiteSpace(existingUser.UserSalt)
                    ? Convert.FromBase64String(existingUser.UserSalt)
                    : _passwordCryptoService.GenerateUserSalt();
                existingUser.UserSalt = Convert.ToBase64String(existingSalt);
                existingUser.MasterPasswordHash = _passwordCryptoService.CreateMasterPasswordHash(masterPassword, existingSalt);
                existingUser.MasterKeyIdentifier = _passwordCryptoService.CreateMasterKeyIdentifier(masterPassword, existingSalt);
                existingUser.LastModified = DateTime.UtcNow;
                await _userManager.UpdateAsync(existingUser);

                // Keep the real Identity password in sync too, for accounts that log in that way
                // (VaultGuard.Admin's SuperAdmin bootstrap account) rather than via master key.
                if (identityPassword is not null)
                {
                    var resetToken = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
                    await _userManager.ResetPasswordAsync(existingUser, resetToken, identityPassword);
                }

                _logger.LogInformation("Refreshed master key for existing user {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not refresh master key for existing user {Email}", email);
            }
            return;
        }

        // Generate user salt for cryptographic operations
        var userSalt = _passwordCryptoService.GenerateUserSalt();
        
        // Create master password hash for authentication
        var masterPasswordHash = _passwordCryptoService.CreateMasterPasswordHash(masterPassword, userSalt);
        
        // Create master key identifier for lookup during master key login
        var masterKeyIdentifier = _passwordCryptoService.CreateMasterKeyIdentifier(masterPassword, userSalt);

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            UserSalt = Convert.ToBase64String(userSalt),
            MasterPasswordHash = masterPasswordHash,
            MasterKeyIdentifier = masterKeyIdentifier,
            MasterPasswordHint = $"Common master key for all {roleName} users",
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };

        // Regular seeded accounts log in via master key, so a random discarded password is fine here.
        // identityPassword overrides this for accounts (SuperAdmin) that must log in with a real,
        // known Identity password instead — see the SuperAdmin seeding call above.
        var tempPassword = identityPassword ?? $"TempPass_{DateTime.UtcNow.Ticks}!";
        var result = await _userManager.CreateAsync(user, tempPassword);
        
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, roleName);
            _logger.LogInformation("Created default user {Email} with role {Role} and master key support", email, roleName);
            
            // Seed default vault and categories for the new user
            try
            {
                await SeedDefaultVaultAndCategoriesAsync(user.Id);
                _logger.LogInformation("Seeded default vault and categories for user {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed default vault for user {Email}", email);
            }
        }
        else
        {
            _logger.LogError("Failed to create user {Email}: {Errors}", 
                email, string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    /// <summary>
    /// Seeds default "Personal" vault with standard categories for a user
    /// </summary>
    private async Task SeedDefaultVaultAndCategoriesAsync(string userId)
    {
        // Check if user already has a vault
        var existingVault = await _dbContext.Vaults
            .FirstOrDefaultAsync(v => v.UserId == userId);
        
        if (existingVault != null)
        {
            _logger.LogInformation("User {UserId} already has a vault, skipping seed", userId);
            return;
        }

        // Create default "Personal" vault
        var vault = new Vault
        {
            Name = "Personal",
            Description = "Your personal password vault",
            IsDefault = true,
            Icon = "🔐",
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Vaults.Add(vault);
        await _dbContext.SaveChangesAsync();

        // Seed default categories
        var defaultCategories = new[]
        {
            new Category 
            { 
                Name = "Logins", 
                Description = "Login credentials for websites and apps",
                Icon = "🔑",
                Color = "#4A90E2",
                VaultId = vault.Id,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Category 
            { 
                Name = "Credit Cards", 
                Description = "Credit and debit card information",
                Icon = "💳",
                Color = "#E94B3C",
                VaultId = vault.Id,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Category 
            { 
                Name = "Secure Notes", 
                Description = "Encrypted notes and documents",
                Icon = "📝",
                Color = "#F5A623",
                VaultId = vault.Id,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Category 
            { 
                Name = "WiFi Networks", 
                Description = "WiFi network passwords",
                Icon = "📶",
                Color = "#7ED321",
                VaultId = vault.Id,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Category 
            { 
                Name = "Passkeys", 
                Description = "Passkey credentials for passwordless authentication",
                Icon = "🔐",
                Color = "#9013FE",
                VaultId = vault.Id,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Category 
            { 
                Name = "Identities", 
                Description = "Personal identification information",
                Icon = "👤",
                Color = "#50E3C2",
                VaultId = vault.Id,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _dbContext.Categories.AddRange(defaultCategories);
        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds parent-child relationships and permission configurations
    /// TODO: Re-enable after fixing circular dependency with Services
    /// </summary>
    /*
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
    */
}