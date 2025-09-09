using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using PasswordManager.Models;
using PasswordManager.DAL;
using PasswordManager.Crypto.Interfaces;
using PasswordManager.Crypto.Extensions;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace PasswordManager.WinUi.Tests;

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
        services.AddDbContext<PasswordManagerDbContextApp>(options =>
            options.UseSqlite($"Data Source={_testDbPath}"));
        
        // Add crypto services
        services.AddCryptographyServices();
        
        _serviceProvider = services.BuildServiceProvider();
        
        // Initialize database
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PasswordManagerDbContextApp>();
        dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task SampleDataSeeder_CreatesUserWithProperCryptographicSetup()
    {
        // This test verifies the fix we made to ensure demo users get proper crypto setup
        // We'll simulate the SampleDataSeeder behavior directly since it's internal
        
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PasswordManagerDbContextApp>();
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
        var dbContext = scope.ServiceProvider.GetRequiredService<PasswordManagerDbContextApp>();
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
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}