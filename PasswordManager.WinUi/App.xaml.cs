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
using PasswordManager.Models.Configuration;
using PasswordManager.WinUi.Services;
using Sentry;

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

        // Initialize Sentry.io
        InitializeSentry();
    }

    private void InitializeSentry()
    {
        try
        {
            var configuration = _host.Services.GetRequiredService<IConfiguration>();
            var sentryConfig = configuration.GetSection("Sentry").Get<SentryConfiguration>();

            if (sentryConfig?.IsConfigured == true)
            {
                SentrySdk.Init(options =>
                {
                    options.Dsn = sentryConfig.Dsn;
                    options.Environment = sentryConfig.Environment;
                    options.TracesSampleRate = sentryConfig.TracesSampleRate;
                    options.SendDefaultPii = sentryConfig.SendDefaultPii;
                    options.AttachStacktrace = sentryConfig.AttachStacktrace;
                    options.Debug = sentryConfig.Debug;
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize Sentry: {ex.Message}");
        }
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

                // Ensure junction table exists as a final safety net for WinUI/SQLite scenarios
                try
                {
                    var migrationService = scope.ServiceProvider.GetService<IDatabaseMigrationService>();
                    if (migrationService != null)
                    {
                        await migrationService.EnsurePasswordItemTagsTableExistsAsync();
                        System.Diagnostics.Debug.WriteLine("Ensured PasswordItemTags table exists via migration service fallback (WinUI)");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("IDatabaseMigrationService not registered in WinUI host - cannot ensure PasswordItemTags table");
                    }
                }
                catch (Exception exEnsure)
                {
                    System.Diagnostics.Debug.WriteLine($"Warning ensuring PasswordItemTags table in WinUI: {exEnsure.Message}");
                    SentrySdk.CaptureException(exEnsure);
                }

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
                SentrySdk.CaptureException(ex);
            }
        });
    }

    private async Task LoadSavedTheme()
    {
        try
        {
            // First, try to load from ApplicationData (persisted settings)
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("SelectedTheme"))
            {
                var savedTheme = localSettings.Values["SelectedTheme"]?.ToString();
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
                    return;
                }
            }

            // Fallback to secure storage (legacy)
            using var scope = _host.Services.CreateScope();
            var secureStorage = scope.ServiceProvider.GetRequiredService<ISecureStorageService>();
            var savedThemeFromSecure = await secureStorage.GetAsync("SelectedTheme");

            if (!string.IsNullOrEmpty(savedThemeFromSecure))
            {
                var theme = savedThemeFromSecure switch
                {
                    "Light" => PasswordManager.WinUi.Services.AppTheme.Light,
                    "Dark" => PasswordManager.WinUi.Services.AppTheme.Dark,
                    "System" => PasswordManager.WinUi.Services.AppTheme.System,
                    _ => PasswordManager.WinUi.Services.AppTheme.System
                };

                PasswordManager.WinUi.Services.ThemeHelper.SetTheme(theme);

                // Migrate to ApplicationData for future use
                localSettings.Values["SelectedTheme"] = savedThemeFromSecure;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading saved theme: {ex.Message}");
            SentrySdk.CaptureException(ex);
            // Apply default system theme on error
            PasswordManager.WinUi.Services.ThemeHelper.SetTheme(PasswordManager.WinUi.Services.AppTheme.System);
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
                services.AddScoped<IDatabaseResetService, DatabaseResetService>();
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
                services.AddScoped<INetworkLocationBackupService, NetworkLocationBackupService>();
                services.AddScoped<CloudBackupManager>();
                services.AddScoped<IBackupSettingsService, BackupSettingsService>();
                services.AddSingleton<IScheduledBackupService, ScheduledBackupService>();
                services.AddHostedService<ScheduledBackupService>();

                // Add logging (Debug + file logger)
                var tempLogPlatform = new WinUiPlatformService();
                var logBase = Path.Combine(tempLogPlatform.GetAppDataDirectory(), "log");

                services.AddLogging(builder =>
                {
                    builder.AddDebug();
                    // Register file logger provider that appends logs into \log\{year}\{month}\{day}.txt
                    builder.AddProvider(new PasswordManager.WinUi.Services.FileLogging.FileLoggerProvider(logBase, LogLevel.Debug));
                });
            });
    }

    private Window? m_window;

    public MainWindow? MainWindow => m_window as MainWindow;
    public IServiceProvider Services => _host.Services;
}