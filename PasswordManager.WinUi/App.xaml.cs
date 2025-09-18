using Microsoft.UI.Xaml;
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
using Microsoft.AspNetCore.Identity;
using PasswordManager.Models;
using PasswordManager.WinUi.Services;

namespace PasswordManager.WinUi;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private IHost _host;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
        _host = CreateHostBuilder().Build();
    }

    /// <summary>
    /// Invoked when the application is launched normally by the end user.  Other entry points
    /// will be used such as when the application is launched to open a specific file.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        m_window = new MainWindow(_host.Services);

        // Initialize theme system
        ThemeHelper.Initialize(m_window, this);

        // Load saved theme setting
        _ = LoadSavedTheme();

        m_window.Activate();

        // Initialize services
        _ = Task.Run(async () =>
        {
            try
            {
                await _host.StartAsync();
                // Initialize database and services
                using var scope = _host.Services.CreateScope();

                // DEBUG: quick DI self-check to confirm Identity/UserManager and UserProfileService are registered
#if DEBUG
                try
                {
                    var dbgUserManager = scope.ServiceProvider.GetService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
                    var dbgUserProfile = scope.ServiceProvider.GetService<IUserProfileService>();
                    System.Diagnostics.Debug.WriteLine($"DEBUG DI check: UserManager {(dbgUserManager != null ? "RESOLVED" : "MISSING")}, UserProfileService {(dbgUserProfile != null ? "RESOLVED" : "MISSING")} ");
                }
                catch (Exception dbgEx)
                {
                    System.Diagnostics.Debug.WriteLine($"DEBUG DI check exception: {dbgEx}");
                }
#endif

                var startupService = scope.ServiceProvider.GetRequiredService<IAppStartupService>();
                await startupService.InitializeAsync();

                // Test the identity seeder with common master key (DEBUG only)
#if DEBUG
                try
                {
                    System.Diagnostics.Debug.WriteLine("Running Identity Seeder test...");
                    var testResult = await Tests.IdentitySeederTests.TestCommonMasterKeySetupAsync(_host.Services);
                    System.Diagnostics.Debug.WriteLine($"Identity Seeder test result: {(testResult ? "PASSED" : "FAILED")}");
                }
                catch (Exception testEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Identity Seeder test exception: {testEx.Message}");
                }
#endif
            }
            catch (Exception ex)
            {
                // Log error but don't crash the app
                System.Diagnostics.Debug.WriteLine($"Service initialization error: {ex}");
            }
        });
    }

    private async Task LoadSavedTheme()
    {
        try
        {
            using var scope = _host.Services.CreateScope();
            var secureStorage = scope.ServiceProvider.GetRequiredService<ISecureStorageService>();
            var savedTheme = await secureStorage.GetAsync("SelectedTheme");

            if (!string.IsNullOrEmpty(savedTheme))
            {
                var theme = savedTheme switch
                {
                    "Light" => PasswordManager.WinUi.Services.AppTheme.Light,
                    "Dark" => PasswordManager.WinUi.Services.AppTheme.Dark,
                    "System" => PasswordManager.WinUi.Services.AppTheme.System,
                    _ => PasswordManager.WinUi.Services.AppTheme.System
                };

                PasswordManager.WinUi.Services.ThemeHelper.SetTheme(theme);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading saved theme: {ex.Message}");
        }
    }

    private static IHostBuilder CreateHostBuilder()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                // Register crypto services (needed for database configuration)
                services.AddCryptographyServices();

                // Register platform service
                services.AddSingleton<IPlatformService, WinUiPlatformService>();
                services.AddSingleton<ISecureStorageService, WinUiSecureStorageService>();

                // Register database configuration service
                services.AddScoped<IDatabaseConfigurationService, DatabaseConfigurationService>();
                services.AddScoped<DynamicDatabaseContextFactory>();

                // Configure database context with default SQLite
                var tempPlatformService = new WinUiPlatformService();
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

                // Add Identity services with roles so all Identity tables are created
                services.AddIdentityCore<ApplicationUser>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = false;
                    options.Password.RequireDigit = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireLowercase = true;
                })
                .AddRoles<ApplicationRole>()
                .AddEntityFrameworkStores<PasswordManagerDbContextApp>();

                // Register the interface mapping for dependency injection
                services.AddScoped<DAL.Interfaces.IPasswordManagerDbContext>(provider =>
                    provider.GetRequiredService<PasswordManagerDbContext>());

                // Register business services
                services.AddScoped<IPasswordItemService, PasswordItemService>();
                services.AddScoped<ITagService, TagService>();
                services.AddScoped<ICategoryInterface, CategoryService>();
                services.AddScoped<ICollectionService, CollectionService>();
                services.AddScoped<ICustomFieldService, CustomFieldService>();
                services.AddScoped<IPasswordEncryptionService, PasswordEncryptionService>(); // Fix: Add missing PasswordEncryptionService registration
                services.AddScoped<IPasskeyService, PasskeyService>(); // Fix: Add missing PasskeyService registration
                services.AddScoped<WinUiAuthService>(); // Register the local auth service
                services.AddScoped<IAuthService, ConfigurableAuthService>(); // Use configurable auth service
                services.AddScoped<IPasswordRevealService, PasswordRevealService>();
                services.AddScoped<IAppSyncService, AppSyncService>();
                services.AddScoped<IAppStartupService, AppStartupService>();
                services.AddScoped<IDatabaseMigrationService, DatabaseMigrationService>();
                services.AddScoped<IDatabaseHealthService, DatabaseHealthService>();
                services.AddScoped<IUserProfileService, UserProfileService>();
                services.AddScoped<IVaultSessionService, VaultSessionService>();
                services.AddScoped<IPasscodeService, PasscodeService>();

                // Register Identity data seeder for proper Identity table initialization
                services.AddScoped<PasswordManager.DAL.Seed.IdentityDataSeeder>();

                // Register Fido2 service for passkeys
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

                // Register HTTP client
                services.AddHttpClient();

                // Register import services
                services.AddSingleton<PluginDiscoveryService>();
                services.AddScoped<IImportService, ImportService>();

                // Register cloud backup services
                services.AddScoped<IDatabaseBackupService, DatabaseBackupService>();
                services.AddScoped<IOneDriveBackupService, OneDriveBackupService>();
                services.AddScoped<IiCloudBackupService, iCloudBackupService>();
                services.AddScoped<CloudBackupManager>();

                // Add logging
                services.AddLogging(builder => builder.AddDebug());
            });
    }

    private Window? m_window;

    public MainWindow? MainWindow => m_window as MainWindow;
    public IServiceProvider Services => _host.Services;
}