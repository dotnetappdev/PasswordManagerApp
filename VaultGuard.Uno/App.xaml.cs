using Uno.Resizetizer;
using Sentry;
using VaultGuard.Models.Configuration;

namespace VaultGuard.Mobile;

public partial class App : Application
{
    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
    }

    protected Window? MainWindow { get; private set; }
    protected IHost? Host { get; private set; }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            // Add navigation support for toolkit controls such as TabBar and NavigationView
            .UseToolkitNavigation()
            .Configure(host => host
#if DEBUG
                // Switch to Development environment when running in DEBUG
                .UseEnvironment(Environments.Development)
#endif
                .UseLogging(configure: (context, logBuilder) =>
                {
                    // Configure log levels for different categories of logging
                    logBuilder
                        .SetMinimumLevel(
                            context.HostingEnvironment.IsDevelopment() ?
                                LogLevel.Information :
                                LogLevel.Warning)

                        // Default filters for core Uno Platform namespaces
                        .CoreLogLevel(LogLevel.Warning);

                    // Uno Platform namespace filter groups
                    // Uncomment individual methods to see more detailed logging
                    //// Generic Xaml events
                    //logBuilder.XamlLogLevel(LogLevel.Debug);
                    //// Layout specific messages
                    //logBuilder.XamlLayoutLogLevel(LogLevel.Debug);
                    //// Storage messages
                    //logBuilder.StorageLogLevel(LogLevel.Debug);
                    //// Binding related messages
                    //logBuilder.XamlBindingLogLevel(LogLevel.Debug);
                    //// Binder memory references tracking
                    //logBuilder.BinderMemoryReferenceLogLevel(LogLevel.Debug);
                    //// DevServer and HotReload related
                    //logBuilder.HotReloadCoreLogLevel(LogLevel.Information);
                    //// Debug JS interop
                    //logBuilder.WebAssemblyLogLevel(LogLevel.Debug);

                }, enableUnoLogging: true)
                .UseConfiguration(configure: configBuilder =>
                    configBuilder
                        .Section<AppConfig>()
                )
                // Enable localization (see appsettings.json for supported languages)
                .UseLocalization()
                .UseHttp((context, services) => {
#if DEBUG
                // DelegatingHandler will be automatically injected
                services.AddTransient<DelegatingHandler, DebugHttpHandler>();
#endif

})
                .ConfigureServices((context, services) =>
                {
                    // Initialize Sentry.io
                    InitializeSentry(context.Configuration);
                    
                    // Register database service
                    var dbPath = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "passwordmanager.db3");
                    services.AddSingleton(new VaultGuard.Uno.Services.LocalDatabase.LocalDatabaseService(dbPath));
                    
                    // Register HTTP client for API
                    services.AddHttpClient("VaultGuardApi", (sp, client) =>
                    {
                        var config = sp.GetService<IConfiguration>();
                        var apiBaseUrl = config?["ApiBaseUrl"] ?? "https://localhost:5001";
                        client.BaseAddress = new Uri(apiBaseUrl);
                        client.Timeout = TimeSpan.FromSeconds(30);
                    });
                    
                    // Register sync service
                    services.AddSingleton<VaultGuard.Uno.Services.Sync.SyncService>();
                    
                    // Register biometric authentication service
                    services.AddSingleton<VaultGuard.Uno.Services.Biometric.IBiometricAuthService, VaultGuard.Uno.Services.Biometric.BiometricAuthService>();
                    
                    // Register theme service
                    services.AddSingleton<VaultGuard.Uno.Services.Theme.IThemeService, VaultGuard.Uno.Services.Theme.ThemeService>();
                    
                    // Register backup service
                    services.AddSingleton<VaultGuard.Uno.Services.Backup.IBackupService>(sp => 
                        new VaultGuard.Uno.Services.Backup.BackupService(
                            sp.GetRequiredService<ILogger<VaultGuard.Uno.Services.Backup.BackupService>>(),
                            dbPath));
                    
                    // Register AutoFill service
                    services.AddSingleton<VaultGuard.Uno.Services.AutoFill.IAutoFillService, VaultGuard.Uno.Services.AutoFill.AutoFillService>();
                    
                    // Register QR Code service
                    services.AddSingleton<VaultGuard.Uno.Services.QRCode.IQRCodeService, VaultGuard.Uno.Services.QRCode.QRCodeService>();
                    
                    // Register ViewModels
                    services.AddTransient<VaultGuard.Mobile.Presentation.Pages.Login.LoginModel>();
                    services.AddTransient<VaultGuard.Mobile.Presentation.Pages.Passwords.PasswordsModel>();
                    services.AddTransient<VaultGuard.Mobile.Presentation.Pages.Categories.CategoriesModel>();
                    services.AddTransient<VaultGuard.Mobile.Presentation.Pages.Settings.SettingsModel>();
                })
                .UseNavigation(RegisterRoutes)
            );
        MainWindow = builder.Window;

        #if DEBUG
        MainWindow.UseStudio();
#endif

        Host = await builder.NavigateAsync<Shell>();
    }

    private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
    {
        views.Register(
            new ViewMap(ViewModel: typeof(ShellModel)),
            new ViewMap<MainPage, MainModel>(),
            new DataViewMap<SecondPage, SecondModel, Entity>(),
            new ViewMap<VaultGuard.Mobile.Presentation.Pages.Login.LoginPage, VaultGuard.Mobile.Presentation.Pages.Login.LoginModel>(),
            new ViewMap<VaultGuard.Mobile.Presentation.Pages.Passwords.PasswordsPage, VaultGuard.Mobile.Presentation.Pages.Passwords.PasswordsModel>(),
            new ViewMap<VaultGuard.Mobile.Presentation.Pages.Categories.CategoriesPage, VaultGuard.Mobile.Presentation.Pages.Categories.CategoriesModel>(),
            new ViewMap<VaultGuard.Mobile.Presentation.Pages.Settings.SettingsPage, VaultGuard.Mobile.Presentation.Pages.Settings.SettingsModel>()
        );

        routes.Register(
            new RouteMap("", View: views.FindByViewModel<ShellModel>(),
                Nested:
                [
                    new ("Login", View: views.FindByViewModel<VaultGuard.Mobile.Presentation.Pages.Login.LoginModel>(), IsDefault:true),
                    new ("Passwords", View: views.FindByViewModel<VaultGuard.Mobile.Presentation.Pages.Passwords.PasswordsModel>()),
                    new ("Categories", View: views.FindByViewModel<VaultGuard.Mobile.Presentation.Pages.Categories.CategoriesModel>()),
                    new ("Settings", View: views.FindByViewModel<VaultGuard.Mobile.Presentation.Pages.Settings.SettingsModel>()),
                    new ("Main", View: views.FindByViewModel<MainModel>()),
                    new ("Second", View: views.FindByViewModel<SecondModel>()),
                ]
            )
        );
    }
    
    private void InitializeSentry(IConfiguration configuration)
    {
        try
        {
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
            // Log error but don't crash the app
            VaultGuard.Uno.Logging.UnoLogger.Error("Failed to initialize Sentry", ex);
        }
    }
}
