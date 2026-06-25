using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using VaultGuard.DAL;
using VaultGuard.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using VaultGuard.Models.Configuration;
using VaultGuard.WinUi.Services;
using VaultGuard.WinUi.Helpers;
using Sentry;

namespace VaultGuard.WinUi;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private IHost _host;

    /// <summary>
    /// Initializes the singleton application object.
    /// </summary>
    public App()
    {
        this.InitializeComponent();
        _host = CreateHostBuilder().Build();
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
        catch (Exception)
        {
            // Silently fail if Sentry initialization fails
        }
    }

    /// <summary>
    /// Invoked when the application is launched normally by the end user.
    /// </summary>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        m_window = new MainWindow(_host.Services);

        ThemeHelper.Initialize(m_window, this);
        _ = LoadSavedTheme();

        m_window.Activate();

        _ = Task.Run(async () =>
        {
            try
            {
                await _host.StartAsync();

                // Check if this is first run and show database configuration dialog
                await ShowDatabaseConfigurationIfNeededAsync();

                using var scope = _host.Services.CreateScope();
                var startupService = scope.ServiceProvider.GetRequiredService<IAppStartupService>();
                await startupService.InitializeAsync();
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
            }
        });
    }

    private async Task ShowDatabaseConfigurationIfNeededAsync()
    {
        try
        {
            using var scope = _host.Services.CreateScope();
            var databaseConfigService = scope.ServiceProvider.GetRequiredService<IDatabaseConfigurationService>();
            var platformService = scope.ServiceProvider.GetRequiredService<IPlatformService>();
            
            // Check if this is first run
            var isFirstRun = await databaseConfigService.IsFirstRunAsync();
            
            if (isFirstRun)
            {
                // Show the database configuration dialog on UI thread
                await m_window.DispatcherQueue.EnqueueAsync(async () =>
                {
                    // Verify XamlRoot is available before showing dialog
                    if (m_window.Content?.XamlRoot != null)
                    {
                        var dialog = new Dialogs.DatabaseConfigurationDialog(platformService, databaseConfigService);
                        dialog.XamlRoot = m_window.Content.XamlRoot;
                        await dialog.ShowAsync();
                    }
                    else
                    {
                        // XamlRoot not available, skip dialog
                    }
                });
            }
        }
        catch (Exception)
        {
            // Continue with startup even if dialog fails
        }
    }

    private async Task LoadSavedTheme()
    {
        try
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("SelectedTheme"))
            {
                var savedTheme = localSettings.Values["SelectedTheme"]?.ToString();
                if (!string.IsNullOrEmpty(savedTheme))
                {
                    var theme = savedTheme switch
                    {
                        "Light" => VaultGuard.WinUi.Services.AppTheme.Light,
                        "Dark" => VaultGuard.WinUi.Services.AppTheme.Dark,
                        "System" => VaultGuard.WinUi.Services.AppTheme.System,
                        _ => VaultGuard.WinUi.Services.AppTheme.System
                    };

                    VaultGuard.WinUi.Services.ThemeHelper.SetTheme(theme);
                    return;
                }
            }

            using var scope = _host.Services.CreateScope();
            var secureStorage = scope.ServiceProvider.GetRequiredService<ISecureStorageService>();
            var savedThemeFromSecure = await secureStorage.GetAsync("SelectedTheme");

            if (!string.IsNullOrEmpty(savedThemeFromSecure))
            {
                var theme = savedThemeFromSecure switch
                {
                    "Light" => VaultGuard.WinUi.Services.AppTheme.Light,
                    "Dark" => VaultGuard.WinUi.Services.AppTheme.Dark,
                    "System" => VaultGuard.WinUi.Services.AppTheme.System,
                    _ => VaultGuard.WinUi.Services.AppTheme.System
                };

                VaultGuard.WinUi.Services.ThemeHelper.SetTheme(theme);

                localSettings.Values["SelectedTheme"] = savedThemeFromSecure;
            }
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            VaultGuard.WinUi.Services.ThemeHelper.SetTheme(VaultGuard.WinUi.Services.AppTheme.System);
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
                services.ConfigureServices(context.Configuration);
            });
    }

    private Window? m_window;

    public MainWindow? MainWindow => m_window as MainWindow;
    public IServiceProvider Services => _host.Services;
}