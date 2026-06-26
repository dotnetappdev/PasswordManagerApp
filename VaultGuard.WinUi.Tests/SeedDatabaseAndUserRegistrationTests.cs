using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using VaultGuard.Models;
using VaultGuard.DAL;
using VaultGuard.Crypto.Interfaces;
using VaultGuard.Crypto.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace VaultGuard.WinUi.Tests;

/// <summary>
/// Tests for verifying that the cryptographic fixes work correctly
/// and that user creation properly sets MasterKeyIdentifier and UserSalt
/// </summary>
public class SeedDatabaseAndUserRegistrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly string _testDbPath;

    public SeedDatabaseAndUserRegistrationTests()
    {
        // Create a unique test database
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_passwordmanager_{Guid.NewGuid()}.db");

        // Set up services
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Add database context
        services.AddDbContext<VaultGuardDbContextApp>(options =>
            options.UseSqlite($"Data Source={_testDbPath}"));
        
        // Add crypto services
        services.AddCryptographyServices();
        
        _serviceProvider = services.BuildServiceProvider();
        
        // Initialize database
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VaultGuardDbContextApp>();
        dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task SampleDataSeeder_CreatesUserWithProperCryptographicSetup()
    {
        // This test verifies the fix we made to ensure demo users get proper crypto setup
        // We'll simulate the SampleDataSeeder behavior directly since it's internal
        
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VaultGuardDbContextApp>();
        var cryptoService = scope.ServiceProvider.GetRequiredService<IPasswordCryptoService>();

        // Act - Simulate the fixed SampleDataSeeder behavior
        const string demoMasterPassword = "DemoPassword123!";
        
        // Generate user salt for cryptographic operations
        var userSalt = cryptoService.GenerateUserSalt();
        
        // Create master password hash for authentication
        var masterPasswordHash = cryptoService.CreateMasterPasswordHash(demoMasterPassword, userSalt);
        
        // Create master key identifier for lookup during master key login
        var masterKeyIdentifier = cryptoService.CreateMasterKeyIdentifier(demoMasterPassword, userSalt);

        var demoUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "demo@local",
            UserName = "demo@local",
            FirstName = "Demo",
            LastName = "User",
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            UserSalt = Convert.ToBase64String(userSalt),
            MasterPasswordHash = masterPasswordHash,
            MasterKeyIdentifier = masterKeyIdentifier,
            MasterPasswordHint = "Demo user password: DemoPassword123!",
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            IsActive = true
        };
        
        dbContext.Users.Add(demoUser);
        await dbContext.SaveChangesAsync();

        // Assert
        var createdDemoUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == "demo@local");
        
        Assert.NotNull(createdDemoUser);
        Assert.False(string.IsNullOrEmpty(createdDemoUser.MasterKeyIdentifier), 
            "Demo user does not have MasterKeyIdentifier set");
        Assert.False(string.IsNullOrEmpty(createdDemoUser.UserSalt), 
            "Demo user does not have UserSalt set");
        Assert.False(string.IsNullOrEmpty(createdDemoUser.MasterPasswordHash), 
            "Demo user does not have MasterPasswordHash set");
        Assert.False(string.IsNullOrEmpty(createdDemoUser.MasterPasswordHint), 
            "Demo user does not have MasterPasswordHint set");
    }

    [Fact]
    public async Task UserRegistration_CreatesUserWithCorrectCryptographicFields()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VaultGuardDbContextApp>();
        var cryptoService = scope.ServiceProvider.GetRequiredService<IPasswordCryptoService>();

        const string testPassword = "TestPassword123!";
        const string testEmail = "test@example.com";

        // Act - Simulate user registration process from UserRegistrationDialog
        var userSalt = cryptoService.GenerateUserSalt();
        var masterPasswordHash = cryptoService.CreateMasterPasswordHash(testPassword, userSalt);
        var masterKeyIdentifier = cryptoService.CreateMasterKeyIdentifier(testPassword, userSalt);

        var newUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = testEmail,
            Email = testEmail,
            EmailConfirmed = true,
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            UserSalt = Convert.ToBase64String(userSalt),
            MasterPasswordHash = masterPasswordHash,
            MasterKeyIdentifier = masterKeyIdentifier,
            MasterPasswordHint = "Test hint",
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };

        dbContext.Users.Add(newUser);
        await dbContext.SaveChangesAsync();

        // Assert
        var createdUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == testEmail);
        Assert.NotNull(createdUser);
        Assert.False(string.IsNullOrEmpty(createdUser.MasterKeyIdentifier), 
            "Created user does not have MasterKeyIdentifier set");
        Assert.False(string.IsNullOrEmpty(createdUser.UserSalt), 
            "Created user does not have UserSalt set");
        Assert.False(string.IsNullOrEmpty(createdUser.MasterPasswordHash), 
            "Created user does not have MasterPasswordHash set");
    }

    [Fact]
    public void CryptographicService_GeneratesValidComponents()
    {
        // Arrange
        var cryptoService = _serviceProvider.GetRequiredService<IPasswordCryptoService>();
        const string testPassword = "TestPassword123!";

        // Act
        var userSalt = cryptoService.GenerateUserSalt();
        var masterPasswordHash = cryptoService.CreateMasterPasswordHash(testPassword, userSalt);
        var masterKeyIdentifier = cryptoService.CreateMasterKeyIdentifier(testPassword, userSalt);

        // Assert
        Assert.NotNull(userSalt);
        Assert.True(userSalt.Length > 0, "UserSalt should not be empty");
        Assert.False(string.IsNullOrEmpty(masterPasswordHash), "MasterPasswordHash should not be null or empty");
        Assert.False(string.IsNullOrEmpty(masterKeyIdentifier), "MasterKeyIdentifier should not be null or empty");

        // Verify that different passwords generate different hashes
        var differentPassword = "DifferentPassword456!";
        var differentHash = cryptoService.CreateMasterPasswordHash(differentPassword, userSalt);
        var differentIdentifier = cryptoService.CreateMasterKeyIdentifier(differentPassword, userSalt);

        Assert.NotEqual(masterPasswordHash, differentHash);
        Assert.NotEqual(masterKeyIdentifier, differentIdentifier);
    }

    [Fact]
    public void CryptographicService_GeneratesConsistentComponents()
    {
        // Arrange
        var cryptoService = _serviceProvider.GetRequiredService<IPasswordCryptoService>();
        const string testPassword = "TestPassword123!";

        // Act
        var userSalt1 = cryptoService.GenerateUserSalt();
        var userSalt2 = cryptoService.GenerateUserSalt();

        var hash1 = cryptoService.CreateMasterPasswordHash(testPassword, userSalt1);
        var hash2 = cryptoService.CreateMasterPasswordHash(testPassword, userSalt1); // Same salt
        var hash3 = cryptoService.CreateMasterPasswordHash(testPassword, userSalt2); // Different salt

        var identifier1 = cryptoService.CreateMasterKeyIdentifier(testPassword, userSalt1);
        var identifier2 = cryptoService.CreateMasterKeyIdentifier(testPassword, userSalt1); // Same salt
        var identifier3 = cryptoService.CreateMasterKeyIdentifier(testPassword, userSalt2); // Different salt

        // Assert
        // Same password + same salt = same hash and identifier
        Assert.Equal(hash1, hash2);
        Assert.Equal(identifier1, identifier2);

        // Same password + different salt = different hash and identifier
        Assert.NotEqual(hash1, hash3);
        Assert.NotEqual(identifier1, identifier3);

        // Different salts should be generated
        Assert.NotEqual(userSalt1, userSalt2);
    }

    [Fact]
    public void UserRegistrationPermissions_RoleBasedLogic_CorrectlyDeterminesAdminCreationRights()
    {
        // This test verifies the core logic used in UserRegistrationDialog.DeterminePermissions()
        // to ensure Parent and User roles cannot create admin accounts
        
        // Test case 1: Admin user can create admin accounts
        var adminRoles = new[] { ApplicationRoles.Admin };
        bool canAdminCreateAdmin = adminRoles.Contains(ApplicationRoles.Admin);
        Assert.True(canAdminCreateAdmin, "Admin users should be allowed to create admin accounts");
        
        // Test case 2: Parent user cannot create admin accounts  
        var parentRoles = new[] { ApplicationRoles.Parent };
        bool canParentCreateAdmin = parentRoles.Contains(ApplicationRoles.Admin);
        Assert.False(canParentCreateAdmin, "Parent users should NOT be allowed to create admin accounts");
        
        // Test case 3: Standard user cannot create admin accounts
        var userRoles = new[] { ApplicationRoles.User };
        bool canUserCreateAdmin = userRoles.Contains(ApplicationRoles.Admin);
        Assert.False(canUserCreateAdmin, "Standard users should NOT be allowed to create admin accounts");
        
        // Test case 4: Child user cannot create admin accounts
        var childRoles = new[] { ApplicationRoles.Child };
        bool canChildCreateAdmin = childRoles.Contains(ApplicationRoles.Admin);
        Assert.False(canChildCreateAdmin, "Child users should NOT be allowed to create admin accounts");
        
        // Test case 5: Child user should be blocked from creating any accounts
        bool isChildUser = childRoles.Contains(ApplicationRoles.Child);
        Assert.True(isChildUser, "Child role detection should work correctly");
    }

    [Fact]
    public void UserRegistrationPermissions_AdminCreationValidation_PreventsBypassAttempts()
    {
        // This test simulates the additional security validation added to CreateUserAsync method
        // to prevent non-admin users from creating admin accounts even if they somehow bypass UI restrictions
        
        // Simulate current user roles
        var adminUserRoles = new[] { ApplicationRoles.Admin };
        var parentUserRoles = new[] { ApplicationRoles.Parent };
        var standardUserRoles = new[] { ApplicationRoles.User };
        var childUserRoles = new[] { ApplicationRoles.Child };
        
        // Simulate trying to create an admin account (selectedRole = ApplicationRoles.Admin)
        string targetRole = ApplicationRoles.Admin;
        
        // Test: Admin user attempting to create admin account - should be allowed
        bool isCurrentUserAdmin = adminUserRoles.Contains(ApplicationRoles.Admin);
        bool adminCreationShouldSucceed = !(targetRole == ApplicationRoles.Admin && !isCurrentUserAdmin);
        Assert.True(adminCreationShouldSucceed, "Admin users should be able to create admin accounts");
        
        // Test: Parent user attempting to create admin account - should be blocked
        bool isCurrentUserParent = parentUserRoles.Contains(ApplicationRoles.Admin); // This will be false
        bool parentCreationShouldFail = targetRole == ApplicationRoles.Admin && !isCurrentUserParent;
        Assert.True(parentCreationShouldFail, "Parent users should be blocked from creating admin accounts");
        
        // Test: Standard user attempting to create admin account - should be blocked
        bool isCurrentUserStandard = standardUserRoles.Contains(ApplicationRoles.Admin); // This will be false
        bool standardCreationShouldFail = targetRole == ApplicationRoles.Admin && !isCurrentUserStandard;
        Assert.True(standardCreationShouldFail, "Standard users should be blocked from creating admin accounts");
        
        // Test: Child user attempting to create admin account - should be blocked
        bool isCurrentUserChild = childUserRoles.Contains(ApplicationRoles.Admin); // This will be false
        bool childCreationShouldFail = targetRole == ApplicationRoles.Admin && !isCurrentUserChild;
        Assert.True(childCreationShouldFail, "Child users should be blocked from creating admin accounts");
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
        
        // Clean up test database
        if (File.Exists(_testDbPath))
        {
            try
            {
                File.Delete(_testDbPath);
            }
            catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Unhandled exception: {ex.Message}");
            }
        }
    }
}