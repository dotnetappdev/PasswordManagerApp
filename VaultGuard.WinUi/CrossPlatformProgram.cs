using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using VaultGuard.DAL;
using VaultGuard.Services;
using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Services;
using VaultGuard.Imports.Interfaces;
using VaultGuard.Imports.Services;
using Microsoft.Extensions.Configuration;
using VaultGuard.Crypto.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using VaultGuard.Models;
using VaultGuard.WinUi.Services.CrossPlatform;

namespace VaultGuard.WinUi;

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
        Console.WriteLine("VaultGuard WinUI - Cross-platform build");
        Console.WriteLine("Note: This build excludes WinUI functionality for non-Windows platforms.");

        var host = CreateHostBuilder(args).Build();

        try
        {
            await host.StartAsync();

            using var scope = host.Services.CreateScope();
            var startupService = scope.ServiceProvider.GetRequiredService<IAppStartupService>();
            await startupService.InitializeAsync();

            Console.WriteLine("Core services initialized successfully.");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Error($"Error initializing services", ex);
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
                services.AddCryptographyServices();

                services.AddSingleton<IPlatformService, CrossPlatformService>();
                services.AddSingleton<ISecureStorageService, CrossPlatformSecureStorageService>();

                services.AddScoped<IDatabaseConfigurationService, DatabaseConfigurationService>();
                services.AddScoped<DynamicDatabaseContextFactory>();

                var platformService = new CrossPlatformService();
                var appDataDir = platformService.GetAppDataDirectory();
                var defaultDbPath = Path.Combine(appDataDir, "passwordmanager.db");

                // Ensure directory exists before creating database context
                if (!Directory.Exists(appDataDir))
                {
                    Directory.CreateDirectory(appDataDir);
                }

                services.AddDbContext<VaultGuardDbContext>(options =>
                    options.UseSqlite($"Data Source={defaultDbPath}"));

                services.AddIdentityCore<ApplicationUser>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = false;
                    options.Password.RequireDigit = true;
                    options.Password.RequiredLength = 8;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireLowercase = true;
                })
                .AddRoles<ApplicationRole>()
                .AddEntityFrameworkStores<VaultGuardDbContext>();

                services.AddScoped<IAuthService, SimpleAuthService>();
                services.AddScoped<IPasswordItemService, PasswordItemService>();
                services.AddScoped<ITagService, TagService>();
                services.AddScoped<ICategoryInterface, CategoryService>();
                services.AddScoped<ICollectionService, CollectionService>();
                services.AddScoped<IPasskeyService, PasskeyService>();
                services.AddScoped<IPasswordRevealService, PasswordRevealService>();
                services.AddScoped<IAppSyncService, AppSyncService>();
                services.AddScoped<IAppStartupService, AppStartupService>();
                services.AddScoped<IDatabaseMigrationService, DatabaseMigrationService>();
                services.AddScoped<IUserProfileService, UserProfileService>();
                services.AddScoped<IVaultSessionService, VaultSessionService>();
                services.AddScoped<IPasscodeService, PasscodeService>();
                services.AddScoped<VaultGuard.DAL.Seed.IdentityDataSeeder>();

                services.AddHttpClient();
                services.AddSingleton<PluginDiscoveryService>();
                services.AddScoped<IImportService, ImportService>();

                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.AddDebug();
                });
            });
    }
}
#endif
