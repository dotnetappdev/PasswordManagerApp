using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PasswordManager.Crypto.Extensions;
using PasswordManager.DAL;
using PasswordManager.Imports.Interfaces;
using PasswordManager.Imports.Services;
using PasswordManager.Models;
using PasswordManager.Services;
using PasswordManager.Services.Interfaces;
using PasswordManager.Services.Services;

#if WINDOWS
using PasswordManager.WinUi.Services.FileLogging;
#endif

namespace PasswordManager.WinUi.Services;

/// <summary>
/// Configures dependency injection for the WinUI application.
/// </summary>
public static class ServiceConfiguration
{
    /// <summary>
    /// Configures all required services for the WinUI application.
    /// </summary>
    public static IServiceCollection ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
#if WINDOWS
        // Crypto services
        services.AddCryptographyServices();

        // Platform services
        services.AddSingleton<IPlatformService, WinUiPlatformService>();
        services.AddSingleton<ISecureStorageService, WinUiSecureStorageService>();

        // Database services
        ConfigureDatabaseServices(services);

        // Business services
        ConfigureBusinessServices(services);

        // Import/export services
        services.AddSingleton<PluginDiscoveryService>();
        services.AddScoped<IImportService, ImportService>();

        // Backup services
        ConfigureBackupServices(services);

        // Logging
        ConfigureLogging(services);

        // HTTP client
        services.AddHttpClient();
#endif
        return services;
    }

#if WINDOWS
    private static void ConfigureDatabaseServices(IServiceCollection services)
    {
        services.AddScoped<IDatabaseConfigurationService, DatabaseConfigurationService>();
        services.AddScoped<DynamicDatabaseContextFactory>();

        // Get the database path from saved configuration or use default
        var platformService = new WinUiPlatformService();
        var dbPath = GetConfiguredDatabasePath(platformService);

        services.AddDbContext<PasswordManagerDbContextApp>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        services.AddDbContext<PasswordManagerDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
        })
        .AddRoles<ApplicationRole>()
        .AddEntityFrameworkStores<PasswordManagerDbContextApp>();

        services.AddScoped<DAL.Interfaces.IPasswordManagerDbContext>(provider =>
            provider.GetRequiredService<PasswordManagerDbContext>());

        services.AddScoped<PasswordManager.DAL.Seed.IdentityDataSeeder>();
    }

    private static string GetConfiguredDatabasePath(IPlatformService platformService)
    {
        try
        {
            var configFilePath = Path.Combine(platformService.GetAppDataDirectory(), "appsettings.json");
            
            // Check if configuration file exists
            if (File.Exists(configFilePath))
            {
                var jsonContent = File.ReadAllText(configFilePath);
                var config = System.Text.Json.JsonSerializer.Deserialize<PasswordManager.Models.Configuration.DatabaseConfiguration>(
                    jsonContent, 
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (config?.Sqlite?.DatabasePath != null && !string.IsNullOrWhiteSpace(config.Sqlite.DatabasePath))
                {
                    // If path is absolute, use it directly; otherwise, make it relative to app data directory
                    var dbPath = config.Sqlite.DatabasePath;
                    if (!Path.IsPathRooted(dbPath))
                    {
                        dbPath = Path.Combine(platformService.GetAppDataDirectory(), dbPath);
                    }
                    
                    // Ensure the directory exists
                    var directory = Path.GetDirectoryName(dbPath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    
                    return dbPath;
                }
            }
        }
        catch
        {
            // If we can't read the config, fall back to default
        }
        
        // Default path based on app data directory
        var appDataDir = platformService.GetAppDataDirectory();
        
        // Ensure directory exists for the default path
        if (!Directory.Exists(appDataDir))
        {
            Directory.CreateDirectory(appDataDir);
        }
        
        return Path.Combine(appDataDir, "passwordmanager.db");
    }

    private static void ConfigureBusinessServices(IServiceCollection services)
    {
        services.AddScoped<IPasswordItemService, PasswordItemService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<ICategoryInterface, CategoryService>();
        services.AddScoped<ICollectionService, CollectionService>();
        services.AddScoped<ICustomFieldService, CustomFieldService>();
        services.AddScoped<IPasswordEncryptionService, PasswordEncryptionService>();
        services.AddScoped<IPasskeyService, PasskeyService>();
        services.AddScoped<WinUiAuthService>();
        services.AddScoped<IAuthService, ConfigurableAuthService>();
        services.AddScoped<IPasswordRevealService, PasswordRevealService>();
        services.AddScoped<IAppSyncService, AppSyncService>();
        services.AddScoped<IAppStartupService, AppStartupService>();
        services.AddScoped<IDatabaseMigrationService, DatabaseMigrationService>();
        services.AddScoped<IDatabaseHealthService, DatabaseHealthService>();
        services.AddScoped<IDatabaseResetService, DatabaseResetService>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IVaultSessionService, VaultSessionService>();
        services.AddScoped<IPasscodeService, PasscodeService>();

        services.AddScoped<Fido2NetLib.IFido2>(provider =>
        {
            var config = new Fido2NetLib.Fido2Configuration
            {
                ServerDomain = "localhost",
                ServerName = "PasswordManager WinUI",
                Origins = new HashSet<string> { "https://localhost", "http://localhost" },
                TimestampDriftTolerance = 300000
            };
            return new Fido2NetLib.Fido2(config);
        });
    }

    private static void ConfigureBackupServices(IServiceCollection services)
    {
        services.AddScoped<IDatabaseBackupService, DatabaseBackupService>();
        services.AddScoped<IOneDriveBackupService, OneDriveBackupService>();
        services.AddScoped<IiCloudBackupService, iCloudBackupService>();
        services.AddScoped<INetworkLocationBackupService, NetworkLocationBackupService>();
        services.AddScoped<CloudBackupManager>();
        services.AddScoped<IBackupSettingsService, BackupSettingsService>();
        
        // Register ScheduledBackupService as singleton and use the same instance for hosted service
        services.AddSingleton<ScheduledBackupService>();
        services.AddSingleton<IScheduledBackupService>(provider => provider.GetRequiredService<ScheduledBackupService>());
        services.AddSingleton<IHostedService>(provider => provider.GetRequiredService<ScheduledBackupService>());
    }

    private static void ConfigureLogging(IServiceCollection services)
    {
        var platformService = new WinUiPlatformService();
        var logBase = Path.Combine(platformService.GetAppDataDirectory(), "log");

        services.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.AddProvider(new FileLoggerProvider(logBase, LogLevel.Debug));
        });
    }
#endif
}
