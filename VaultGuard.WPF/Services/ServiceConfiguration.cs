using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VaultGuard.Crypto.Extensions;
using VaultGuard.DAL;
using VaultGuard.Imports.Interfaces;
using VaultGuard.Imports.Services;
using VaultGuard.Models;
using VaultGuard.Services;
using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Services;
using System.IO;
using VaultGuard.Services.Logging;
using VaultGuard.ExceptionReporting.Sentry;

namespace VaultGuard.WPF.Services;

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
        services.AddSingleton<IPlatformService, WpfPlatformService>();
        services.AddSingleton<ISecureStorageService, WpfSecureStorageService>();
        services.AddSingleton<IMasterPasswordCacheService, MasterPasswordCacheService>();
        services.AddSingleton<IWindowsHelloService, WindowsHelloService>();

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

        // Exception reporting (Sentry-backed, swappable via IExceptionReporter)
        services.AddSentryExceptionReporting(configuration["ExceptionReporting:SentryDsn"], "WPF");

        // HTTP client
        services.AddHttpClient();
        services.AddSingleton<UpdateService>();

        // Shared machine-local settings (%LocalAppData%\VaultGuard\settings.json) — same file the
        // Blazor Web app reads/writes (ApiBaseUrl, theme, and now the cached license certificate).
        services.AddSingleton<IAppSettingsService, AppSettingsService>();

        // Licensing (CD keys / Pro feature unlock) — see docs/LICENSING.md. Only the public key + AES
        // key belong in this app's appsettings.json "Licensing" section; never the private key.
        services.Configure<VaultGuard.Models.Configuration.LicensingConfiguration>(
            configuration.GetSection(VaultGuard.Models.Configuration.LicensingConfiguration.SectionName));
        services.AddScoped<ILicenseClientService, LicenseClientService>();
#endif
        return services;
    }

#if WINDOWS
    private static void ConfigureDatabaseServices(IServiceCollection services)
    {
        services.AddScoped<IDatabaseConfigurationService, DatabaseConfigurationService>();
        services.AddScoped<DynamicDatabaseContextFactory>();

        // Get the database path from saved configuration or use default
        var platformService = new WpfPlatformService();
        var dbPath = GetConfiguredDatabasePath(platformService);

        services.AddDbContext<VaultGuardDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // Backup/restore services depend on IDatabaseContextFactory. The shared DatabaseContextFactory is
        // connection-string driven and would point at the wrong file, so bind a SQLite factory to the
        // configured app-data database path.
        services.AddScoped<IDatabaseContextFactory>(_ => new WpfDatabaseContextFactory(dbPath));

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
        })
        .AddRoles<ApplicationRole>()
        .AddEntityFrameworkStores<VaultGuardDbContext>();

        services.AddScoped<DAL.Interfaces.IVaultGuardDbContext>(provider =>
            provider.GetRequiredService<VaultGuardDbContext>());

        services.AddScoped<VaultGuard.DAL.Seed.IdentityDataSeeder>();
    }

    private static string GetConfiguredDatabasePath(IPlatformService platformService)
    {
        var configFilePath = Path.Combine(platformService.GetAppDataDirectory(), "appsettings.json");
        
        // Try to read configuration file
        try
        {
            var jsonContent = File.ReadAllText(configFilePath);
            var config = System.Text.Json.JsonSerializer.Deserialize<VaultGuard.Models.Configuration.DatabaseConfiguration>(
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
                
                // Validate path security before creating directories
                if (IsPathSecure(dbPath))
                {
                    // Ensure the directory exists
                    var directory = Path.GetDirectoryName(dbPath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    
                    return dbPath;
                }
                // If path is not secure, fall through to default
            }
        }
        catch (FileNotFoundException ex)
        {
            VaultGuard.Services.Logging.AppLogger.Error($"Configuration file doesn't exist, using default", ex);
        }
        catch (System.Text.Json.JsonException ex)
        {
            VaultGuard.Services.Logging.AppLogger.Error($"JSON deserialization failed, falling back to default", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            VaultGuard.Services.Logging.AppLogger.Error($"Cannot access the file or create directory, falling back to default", ex);
        }
        catch (IOException ex)
        {
            VaultGuard.Services.Logging.AppLogger.Error($"File I/O error, falling back to default", ex);
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Error($"Error accessing config, falling back to default", ex);
        }
        
        // Default path - GetAppDataDirectory already ensures directory exists
        return Path.Combine(platformService.GetAppDataDirectory(), "passwordmanager.db");
    }

    private static bool IsPathSecure(string path)
    {
        try
        {
            // Get the full path to resolve any relative paths
            // Path.GetFullPath already normalizes and resolves path traversal attempts
            var fullPath = Path.GetFullPath(path);
            
            // Disallow system directories
            var systemDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
            var windowsDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var programFilesDir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesX86Dir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            
            if (fullPath.StartsWith(systemDir, StringComparison.OrdinalIgnoreCase) ||
                fullPath.StartsWith(windowsDir, StringComparison.OrdinalIgnoreCase) ||
                fullPath.StartsWith(programFilesDir, StringComparison.OrdinalIgnoreCase) ||
                fullPath.StartsWith(programFilesX86Dir, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            
            // Disallow UNC paths (network paths)
            if (fullPath.StartsWith(@"\\"))
            {
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Error($"Path validation failed, treating as insecure", ex);
            return false;
        }
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
        services.AddScoped<WpfAuthService>();
        services.AddScoped<IAuthService, WpfAuthService>();
        services.AddSingleton<IPasswordRevealService, PasswordRevealService>();
        services.AddScoped<IAppSyncService, AppSyncService>();
        services.AddScoped<IAppStartupService, AppStartupService>();
        services.AddScoped<IDatabaseMigrationService, DatabaseMigrationService>();
        services.AddScoped<IDatabaseHealthService, DatabaseHealthService>();
        services.AddScoped<IDatabaseResetService, DatabaseResetService>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddSingleton<IVaultSessionService, VaultSessionService>();
        services.AddScoped<IPasscodeService, PasscodeService>();
        services.AddScoped<IVaultService, VaultService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<ITwoFactorService, TwoFactorService>();
        services.AddScoped<IDeviceService, DeviceService>();

        // Shared, stateless feature services (strength meter + TOTP authenticator codes + QR)
        services.AddSingleton<IPasswordStrengthService, PasswordStrengthService>();
        services.AddSingleton<ITotpService, TotpService>();
        services.AddSingleton<IQrCodeService, QrCodeService>();
        services.AddSingleton<ISecurityAuditService, SecurityAuditService>();
        services.AddSingleton<IPassphraseGenerator, PassphraseGenerator>();
        services.AddSingleton<VaultGuard.Services.Interfaces.IBreachCheckService, VaultGuard.Services.Services.HibpBreachCheckService>();

        services.AddScoped<Fido2NetLib.IFido2>(provider =>
        {
            var config = new Fido2NetLib.Fido2Configuration
            {
                ServerDomain = "localhost",
                ServerName = "VaultGuard WinUI",
                Origins = new HashSet<string> { "https://localhost", "http://localhost" },
                TimestampDriftTolerance = 300000
            };
            return new Fido2NetLib.Fido2(config);
        });
    }

    private static void ConfigureBackupServices(IServiceCollection services)
    {
        services.AddScoped<IBackupEncryptionService, BackupEncryptionService>();
        services.AddScoped<IDatabaseBackupService, DatabaseBackupService>();
        services.AddScoped<IOneDriveBackupService, OneDriveBackupService>();
        services.AddScoped<IiCloudBackupService, iCloudBackupService>();
        services.AddScoped<INetworkLocationBackupService, NetworkLocationBackupService>();
        services.AddSingleton<IGoogleDriveBackupService, GoogleDriveBackupService>();
        services.AddSingleton<IFtpBackupService, FtpBackupService>();
        services.AddScoped<CloudBackupManager>();
        services.AddScoped<IBackupSettingsService, BackupSettingsService>();
        
        // Register ScheduledBackupService as singleton and use the same instance for hosted service
        services.AddSingleton<ScheduledBackupService>();
        services.AddSingleton<IScheduledBackupService>(provider => provider.GetRequiredService<ScheduledBackupService>());
        services.AddSingleton<IHostedService>(provider => provider.GetRequiredService<ScheduledBackupService>());
    }

    private static void ConfigureLogging(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.AddProvider(new FileLoggerProvider(minLevel: LogLevel.Debug));
        });
    }
#endif
}
