using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using PasswordManager.DAL;
using PasswordManager.Services;
using PasswordManager.Services.Interfaces;
using PasswordManager.Services.Services;
using PasswordManager.Imports.Interfaces;
using PasswordManager.Imports.Services;
using Microsoft.Extensions.Configuration;
using PasswordManager.Crypto.Extensions;
using Microsoft.Extensions.Logging;
using PasswordManager.Models;

namespace PasswordManager.WinUi;

#if CROSSPLATFORM
/// <summary>
/// Cross-platform console entry point for non-Windows environments.
/// This provides a minimal console application that can initialize core services
/// without requiring WinUI dependencies.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("PasswordManager WinUI - Cross-platform build");
        Console.WriteLine("Note: This build excludes WinUI functionality for non-Windows platforms.");
        
        var host = CreateHostBuilder(args).Build();
        
        try
        {
            await host.StartAsync();
            
            // Initialize core services
            using var scope = host.Services.CreateScope();
            var startupService = scope.ServiceProvider.GetRequiredService<IAppStartupService>();
            await startupService.InitializeAsync();
            
            Console.WriteLine("Core services initialized successfully.");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing services: {ex.Message}");
        }
        finally
        {
            await host.StopAsync();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                // Register crypto services
                services.AddCryptographyServices();

                // Register cross-platform services
                services.AddSingleton<IPlatformService, CrossPlatformService>();
                services.AddSingleton<ISecureStorageService, CrossPlatformSecureStorageService>();

                // Register database configuration service
                services.AddScoped<IDatabaseConfigurationService, DatabaseConfigurationService>();
                services.AddScoped<DynamicDatabaseContextFactory>();

                // Configure database context with SQLite
                var tempPlatformService = new CrossPlatformService();
                var defaultDbPath = Path.Combine(tempPlatformService.GetAppDataDirectory(), "data", "passwordmanager.db");
                var defaultDirectory = Path.GetDirectoryName(defaultDbPath);
                if (!Directory.Exists(defaultDirectory))
                {
                    Directory.CreateDirectory(defaultDirectory!);
                }

                services.AddDbContext<PasswordManagerDbContextApp>(options =>
                    options.UseSqlite($"Data Source={defaultDbPath}"));
                
                services.AddDbContext<PasswordManagerDbContext>(options =>
                    options.UseSqlite($"Data Source={defaultDbPath}"));

                // Register business services
                services.AddScoped<IPasswordItemService, PasswordItemService>();
                services.AddScoped<ITagService, TagService>();
                services.AddScoped<ICategoryInterface, CategoryService>();
                services.AddScoped<ICollectionService, CollectionService>();
                services.AddScoped<IPasskeyService, PasskeyService>();
                // Use a simple auth service for cross-platform builds
                services.AddScoped<IAuthService, SimpleAuthService>();
                services.AddScoped<IPasswordRevealService, PasswordRevealService>();
                services.AddScoped<IAppSyncService, AppSyncService>();
                services.AddScoped<IAppStartupService, AppStartupService>();
                services.AddScoped<IDatabaseMigrationService, DatabaseMigrationService>();
                services.AddScoped<IUserProfileService, UserProfileService>();
                services.AddScoped<IVaultSessionService, VaultSessionService>();
                services.AddScoped<IPasscodeService, PasscodeService>();

                // Register HTTP client
                services.AddHttpClient();

                // Register import services
                services.AddSingleton<PluginDiscoveryService>();
                services.AddScoped<IImportService, ImportService>();

                // Add logging
                services.AddLogging(builder => 
                {
                    builder.AddConsole();
                    builder.AddDebug();
                });
            });
    }
}
#endif

/// <summary>
/// Cross-platform implementation of IPlatformService for non-Windows environments.
/// </summary>
public class CrossPlatformService : IPlatformService
{
    public string GetAppDataDirectory()
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(baseDir, ".passwordmanager");
    }

    public string GetDocumentsDirectory()
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    public string GetAppVersion()
    {
        return "1.0.0-crossplatform";
    }

    public string GetPlatformName()
    {
        return Environment.OSVersion.Platform.ToString();
    }

    public bool IsRunningOnWindows()
    {
        return Environment.OSVersion.Platform == PlatformID.Win32NT;
    }

    public bool ShouldShowDatabaseSelection()
    {
        // For cross-platform builds, always show database selection
        return true;
    }

    public string GetDeviceIdentifier()
    {
        // Generate a stable device identifier for cross-platform builds
        var machineName = Environment.MachineName;
        var userName = Environment.UserName;
        return $"{machineName}-{userName}";
    }

    public bool IsMobilePlatform()
    {
        // Cross-platform builds are typically desktop/server
        return false;
    }
}

/// <summary>
/// Cross-platform implementation of ISecureStorageService for non-Windows environments.
/// Note: This is a simplified implementation and should be enhanced for production use.
/// </summary>
public class CrossPlatformSecureStorageService : ISecureStorageService
{
    private readonly string _storageDirectory;

    public CrossPlatformSecureStorageService()
    {
        var platformService = new CrossPlatformService();
        _storageDirectory = Path.Combine(platformService.GetAppDataDirectory(), "secure");
        if (!Directory.Exists(_storageDirectory))
        {
            Directory.CreateDirectory(_storageDirectory);
        }
    }

    public Task<string?> GetAsync(string key)
    {
        try
        {
            var filePath = Path.Combine(_storageDirectory, $"{key}.dat");
            if (File.Exists(filePath))
            {
                var content = File.ReadAllText(filePath);
                return Task.FromResult<string?>(content);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading secure storage: {ex.Message}");
        }
        return Task.FromResult<string?>(null);
    }

    public Task SetAsync(string key, string value)
    {
        try
        {
            var filePath = Path.Combine(_storageDirectory, $"{key}.dat");
            File.WriteAllText(filePath, value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to secure storage: {ex.Message}");
        }
        return Task.CompletedTask;
    }

    public Task<bool> RemoveAsync(string key)
    {
        try
        {
            var filePath = Path.Combine(_storageDirectory, $"{key}.dat");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return Task.FromResult(true);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error removing from secure storage: {ex.Message}");
        }
        return Task.FromResult(false);
    }

    public bool Remove(string key)
    {
        try
        {
            var filePath = Path.Combine(_storageDirectory, $"{key}.dat");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error removing from secure storage: {ex.Message}");
        }
        return false;
    }

    public Task RemoveAllAsync()
    {
        try
        {
            if (Directory.Exists(_storageDirectory))
            {
                Directory.Delete(_storageDirectory, true);
                Directory.CreateDirectory(_storageDirectory);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clearing secure storage: {ex.Message}");
        }
        return Task.CompletedTask;
    }

    public void RemoveAll()
    {
        try
        {
            if (Directory.Exists(_storageDirectory))
            {
                Directory.Delete(_storageDirectory, true);
                Directory.CreateDirectory(_storageDirectory);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clearing secure storage: {ex.Message}");
        }
    }
}

/// <summary>
/// Simple authentication service for cross-platform builds.
/// This provides basic authentication functionality without Windows-specific dependencies.
/// </summary>
public class SimpleAuthService : IAuthService
{
    private bool _isAuthenticated = false;
    private ApplicationUser? _currentUser = null;

    public bool IsAuthenticated => _isAuthenticated;
    public ApplicationUser? CurrentUser => _currentUser;

    public Task<bool> SetupMasterPasswordAsync(string masterPassword, string hint = "")
    {
        Console.WriteLine($"Master password setup attempted with hint: {hint}");
        return Task.FromResult(true);
    }

    public Task<bool> AuthenticateAsync(string masterPassword)
    {
        Console.WriteLine("Master password authentication attempted");
        _isAuthenticated = true;
        _currentUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "demo@example.com",
            FirstName = "Demo",
            LastName = "User"
        };
        return Task.FromResult(true);
    }

    public Task<bool> CheckAuthenticationStatusAsync()
    {
        return Task.FromResult(_isAuthenticated);
    }

    public Task<bool> IsAuthenticatedAsync()
    {
        return Task.FromResult(_isAuthenticated);
    }

    public Task<bool> LoginAsync(string email, string password)
    {
        Console.WriteLine($"User login attempted: {email}");
        _isAuthenticated = true;
        _currentUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            FirstName = "Demo",
            LastName = "User"
        };
        return Task.FromResult(true);
    }

    public Task<bool> RegisterAsync(string email, string password)
    {
        Console.WriteLine($"User registration attempted: {email}");
        return Task.FromResult(true);
    }

    public Task LogoutAsync()
    {
        Console.WriteLine("User logged out");
        _isAuthenticated = false;
        _currentUser = null;
        return Task.CompletedTask;
    }

    public Task<bool> IsFirstTimeSetupAsync()
    {
        // For demo purposes, assume not first time setup
        return Task.FromResult(false);
    }

    public Task<string> GetMasterPasswordHintAsync()
    {
        return Task.FromResult("Demo password hint");
    }

    public Task<bool> ChangeMasterPasswordAsync(string currentPassword, string newPassword, string newPasswordHint = "")
    {
        Console.WriteLine("Master password change attempted");
        return Task.FromResult(true);
    }
}