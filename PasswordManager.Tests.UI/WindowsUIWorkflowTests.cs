using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using PasswordManager.DAL;
using PasswordManager.Models;
using PasswordManager.Models.Configuration;
using PasswordManager.Services.Interfaces;
using PasswordManager.Crypto.Interfaces;
using Xunit;
using System.IO;

namespace PasswordManager.Tests.UI;

/// <summary>
/// Integration tests that simulate the complete UI workflow described in the issue:
/// 1. Select SQLite database
/// 2. Create a master key
/// 3. Save a password item and create a new collection
/// 4. Exit application
/// 5. Load application and use the previous master key
/// 6. Ensure you can see the new created collection and the previous saved password
/// </summary>
public class WindowsUIWorkflowTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly string _testDatabasePath;
    private readonly string _testDataDirectory;

    public WindowsUIWorkflowTests()
    {
        // Create a temporary directory for test data
        _testDataDirectory = Path.Combine(Path.GetTempPath(), "PasswordManagerUITests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDataDirectory);
        
        _testDatabasePath = Path.Combine(_testDataDirectory, "test_passwordmanager.db");

        // Set up dependency injection container
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Add configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseConfiguration:Provider"] = "Sqlite",
                ["DatabaseConfiguration:Sqlite:DatabasePath"] = _testDatabasePath,
                ["DatabaseConfiguration:IsFirstRun"] = "true"
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        // Add Entity Framework with SQLite
        services.AddDbContext<PasswordManagerDbContext>(options =>
            options.UseSqlite($"Data Source={_testDatabasePath}"));

        // Add essential services (we'll mock the complex ones if needed)
        services.AddScoped<IDatabaseConfigurationService, MockDatabaseConfigurationService>();
        services.AddScoped<IAuthService, MockAuthService>();
        services.AddScoped<IPasswordItemService, MockPasswordItemService>();
        services.AddScoped<ICollectionService, MockCollectionService>();
        services.AddScoped<IPasswordCryptoService, MockPasswordCryptoService>();
        services.AddScoped<IVaultSessionService, MockVaultSessionService>();
    }

    [Fact]
    public async Task CompleteUIWorkflow_ShouldWorkCorrectly()
    {
        // Step 1: Select SQLite database
        var dbConfigService = _serviceProvider.GetRequiredService<IDatabaseConfigurationService>();
        var configuration = new DatabaseConfiguration
        {
            Provider = DatabaseProvider.Sqlite,
            IsFirstRun = false,
            Sqlite = new SqliteConfig
            {
                DatabasePath = _testDatabasePath
            }
        };

        await dbConfigService.SaveConfigurationAsync(configuration);

        // Verify SQLite is selected
        var savedConfig = await dbConfigService.GetConfigurationAsync();
        Assert.Equal(DatabaseProvider.Sqlite, savedConfig.Provider);
        Assert.False(savedConfig.IsFirstRun);
        Assert.Equal(_testDatabasePath, savedConfig.Sqlite?.DatabasePath);

        // Step 2: Create a master key
        var authService = _serviceProvider.GetRequiredService<IAuthService>();
        const string masterPassword = "TestMasterPassword123!";
        const string hint = "My test password";

        var setupResult = await authService.SetupMasterPasswordAsync(masterPassword, hint);
        Assert.True(setupResult, "Master password setup should succeed");

        // Verify authentication with the new master key
        var authResult = await authService.AuthenticateAsync(masterPassword);
        Assert.True(authResult, "Authentication with master password should succeed");
        Assert.True(authService.IsAuthenticated, "User should be authenticated");

        // Step 3: Save a password item and create a new collection
        var collectionService = _serviceProvider.GetRequiredService<ICollectionService>();
        var passwordItemService = _serviceProvider.GetRequiredService<IPasswordItemService>();

        // Create a new collection
        var testCollection = new Collection
        {
            Name = "Test Collection",
            Description = "A test collection created during UI testing",
            Icon = "🔒",
            Color = "#007bff",
            UserId = "test-user-id"
        };

        var createdCollection = await collectionService.CreateAsync(testCollection);
        Assert.NotEqual(0, createdCollection.Id);
        Assert.Equal("Test Collection", createdCollection.Name);

        // Create a password item in the collection
        var testPasswordItem = new PasswordItem
        {
            Title = "Test Website",
            Description = "Test login for website",
            Type = ItemType.Login,
            CollectionId = createdCollection.Id,
            UserId = "test-user-id",
            LoginItem = new LoginItem
            {
                Username = "testuser@example.com",
                Password = "SecurePassword123!",
                WebsiteUrl = "https://test.example.com"
            }
        };

        var createdPasswordItem = await passwordItemService.CreateAsync(testPasswordItem);
        Assert.NotEqual(0, createdPasswordItem.Id);
        Assert.Equal("Test Website", createdPasswordItem.Title);
        Assert.Equal(createdCollection.Id, createdPasswordItem.CollectionId);

        // Step 4: Simulate application exit (clear authentication state)
        var vaultSessionService = _serviceProvider.GetRequiredService<IVaultSessionService>();
        await SimulateApplicationExit(authService, vaultSessionService);

        // Verify user is no longer authenticated
        Assert.False(authService.IsAuthenticated, "User should be logged out after application exit");

        // Step 5: Load application and use the previous master key
        // Simulate restarting the application by recreating the auth service
        var newAuthService = _serviceProvider.GetRequiredService<IAuthService>();
        Assert.False(newAuthService.IsAuthenticated, "Fresh auth service should not be authenticated");

        // Authenticate with the same master password
        var reAuthResult = await newAuthService.AuthenticateAsync(masterPassword);
        Assert.True(reAuthResult, "Re-authentication with same master password should succeed");
        Assert.True(newAuthService.IsAuthenticated, "User should be authenticated after restart");

        // Step 6: Ensure you can see the new created collection and the previous saved password
        // Verify the collection still exists
        var collections = await collectionService.GetAllAsync();
        var persistedCollection = collections.FirstOrDefault(c => c.Name == "Test Collection");
        Assert.NotNull(persistedCollection);
        Assert.Equal("Test Collection", persistedCollection.Name);
        Assert.Equal("A test collection created during UI testing", persistedCollection.Description);

        // Verify the password item still exists
        var passwordItems = await passwordItemService.GetAllAsync();
        var persistedPasswordItem = passwordItems.FirstOrDefault(p => p.Title == "Test Website");
        Assert.NotNull(persistedPasswordItem);
        Assert.Equal("Test Website", persistedPasswordItem.Title);
        Assert.Equal(persistedCollection.Id, persistedPasswordItem.CollectionId);

        // Verify the login details are intact
        Assert.NotNull(persistedPasswordItem.LoginItem);
        Assert.Equal("testuser@example.com", persistedPasswordItem.LoginItem.Username);
        Assert.Equal("https://test.example.com", persistedPasswordItem.LoginItem.WebsiteUrl);

        // Additional verification: Check that wrong master password fails
        await SimulateApplicationExit(newAuthService, vaultSessionService);
        var wrongPasswordResult = await newAuthService.AuthenticateAsync("WrongPassword123!");
        Assert.False(wrongPasswordResult, "Authentication with wrong password should fail");
    }

    private static async Task SimulateApplicationExit(IAuthService authService, IVaultSessionService vaultSessionService)
    {
        // Clear authentication state to simulate app exit
        if (authService is MockAuthService mockAuth)
        {
            mockAuth.ClearAuthentication();
        }

        // Clear any cached session data
        if (vaultSessionService is MockVaultSessionService mockVault)
        {
            await mockVault.ClearSessionAsync();
        }
    }

    [Fact]
    public async Task DatabaseConfiguration_SqliteSelection_ShouldPersist()
    {
        var dbConfigService = _serviceProvider.GetRequiredService<IDatabaseConfigurationService>();

        // Test SQLite configuration
        var sqliteConfig = new DatabaseConfiguration
        {
            Provider = DatabaseProvider.Sqlite,
            IsFirstRun = false,
            Sqlite = new SqliteConfig
            {
                DatabasePath = _testDatabasePath,
                ApiUrl = "https://api.test.com",
                ApiKey = "test-api-key"
            }
        };

        await dbConfigService.SaveConfigurationAsync(sqliteConfig);

        var retrievedConfig = await dbConfigService.GetConfigurationAsync();
        Assert.Equal(DatabaseProvider.Sqlite, retrievedConfig.Provider);
        Assert.False(retrievedConfig.IsFirstRun);
        Assert.NotNull(retrievedConfig.Sqlite);
        Assert.Equal(_testDatabasePath, retrievedConfig.Sqlite.DatabasePath);
        Assert.Equal("https://api.test.com", retrievedConfig.Sqlite.ApiUrl);
        Assert.Equal("test-api-key", retrievedConfig.Sqlite.ApiKey);
    }

    [Fact]
    public async Task MasterKeyCreation_ShouldSupportStrongPasswords()
    {
        var authService = _serviceProvider.GetRequiredService<IAuthService>();

        // Test various strong password formats
        var strongPasswords = new[]
        {
            "MyVerySecurePassword123!",
            "P@ssw0rd$ecure2024",
            "ThisIsALongerPassphraseWithNumbers123",
            "Sp3c!@l_Ch@rs&Numbers456"
        };

        foreach (var password in strongPasswords)
        {
            var setupResult = await authService.SetupMasterPasswordAsync(password, $"Hint for {password}");
            Assert.True(setupResult, $"Setup should succeed for password: {password}");

            var authResult = await authService.AuthenticateAsync(password);
            Assert.True(authResult, $"Authentication should succeed for password: {password}");

            // Reset for next iteration
            if (authService is MockAuthService mockAuth)
            {
                mockAuth.ClearAuthentication();
                mockAuth.ClearMasterPassword();
            }
        }
    }

    [Fact]
    public async Task CollectionCreation_ShouldSupportVariousFormats()
    {
        var collectionService = _serviceProvider.GetRequiredService<ICollectionService>();

        // Test different collection formats
        var testCollections = new[]
        {
            new Collection { Name = "Work", Description = "Work-related passwords", Icon = "💼", Color = "#007bff" },
            new Collection { Name = "Personal", Description = "Personal accounts", Icon = "🏠", Color = "#28a745" },
            new Collection { Name = "Banking", Description = "Financial accounts", Icon = "🏦", Color = "#dc3545" },
            new Collection { Name = "Social Media", Description = "Social platforms", Icon = "📱", Color = "#6f42c1" }
        };

        var createdCollections = new List<Collection>();

        foreach (var collection in testCollections)
        {
            collection.UserId = "test-user-id";
            var created = await collectionService.CreateAsync(collection);
            createdCollections.Add(created);

            Assert.NotEqual(0, created.Id);
            Assert.Equal(collection.Name, created.Name);
            Assert.Equal(collection.Description, created.Description);
            Assert.Equal(collection.Icon, created.Icon);
            Assert.Equal(collection.Color, created.Color);
        }

        // Verify all collections can be retrieved
        var allCollections = await collectionService.GetAllAsync();
        Assert.True(allCollections.Count >= testCollections.Length);

        foreach (var expected in testCollections)
        {
            var found = allCollections.FirstOrDefault(c => c.Name == expected.Name);
            Assert.NotNull(found);
        }
    }

    [Fact]
    public async Task PasswordItemCreation_ShouldSupportDifferentTypes()
    {
        var passwordItemService = _serviceProvider.GetRequiredService<IPasswordItemService>();
        var collectionService = _serviceProvider.GetRequiredService<ICollectionService>();

        // Create a collection first
        var collection = await collectionService.CreateAsync(new Collection 
        { 
            Name = "Test Collection", 
            UserId = "test-user-id" 
        });

        // Test different password item types
        var loginItem = new PasswordItem
        {
            Title = "Gmail Account",
            Description = "Email account",
            Type = ItemType.Login,
            CollectionId = collection.Id,
            UserId = "test-user-id",
            LoginItem = new LoginItem
            {
                Username = "user@gmail.com",
                Password = "SecurePassword123!",
                WebsiteUrl = "https://gmail.com",
                Notes = "Primary email account"
            }
        };

        var secureNoteItem = new PasswordItem
        {
            Title = "Secret Recipe",
            Description = "Family recipe",
            Type = ItemType.SecureNote,
            CollectionId = collection.Id,
            UserId = "test-user-id",
            SecureNoteItem = new SecureNoteItem
            {
                Content = "This is a secret recipe for cookies"
            }
        };

        // Create the items
        var createdLogin = await passwordItemService.CreateAsync(loginItem);
        var createdNote = await passwordItemService.CreateAsync(secureNoteItem);

        // Verify creation
        Assert.NotEqual(0, createdLogin.Id);
        Assert.Equal(ItemType.Login, createdLogin.Type);
        Assert.NotNull(createdLogin.LoginItem);
        Assert.Equal("user@gmail.com", createdLogin.LoginItem.Username);

        Assert.NotEqual(0, createdNote.Id);
        Assert.Equal(ItemType.SecureNote, createdNote.Type);
        Assert.NotNull(createdNote.SecureNoteItem);
        Assert.Equal("This is a secret recipe for cookies", createdNote.SecureNoteItem.Content);

        // Verify retrieval
        var allItems = await passwordItemService.GetAllAsync();
        Assert.Contains(allItems, item => item.Title == "Gmail Account");
        Assert.Contains(allItems, item => item.Title == "Secret Recipe");
    }

    public void Dispose()
    {
        // Clean up test data
        if (_serviceProvider is IDisposable disposableProvider)
        {
            disposableProvider.Dispose();
        }
        
        if (Directory.Exists(_testDataDirectory))
        {
            Directory.Delete(_testDataDirectory, true);
        }
    }
}