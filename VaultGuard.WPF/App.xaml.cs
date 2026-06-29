using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using VaultGuard.DAL;
using VaultGuard.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using VaultGuard.Models.Configuration;
using VaultGuard.WPF.Services;
using VaultGuard.WPF.Helpers;
using Sentry;

namespace VaultGuard.WPF;

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
        // Force ModernWPF dark mode before any UI is created
        ModernWpf.ThemeManager.Current.ApplicationTheme = ModernWpf.ApplicationTheme.Dark;

        this.InitializeComponent();

        // Override NavigationView resources after InitializeComponent so they sit
        // above ModernWPF's injected theme dictionary in the resource lookup chain.
        ApplyNavigationViewDarkResources();
        ApplyComboBoxDarkResources();

        _host = CreateHostBuilder().Build();

        // Route the AppLogger facade through the host's logging pipeline (Debug + durable file logs).
        VaultGuard.Services.Logging.AppLogger.Initialize(
            Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
                .GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>(_host.Services));

        InitializeSentry();

        // ModernWpf bug: ScrollBarHelper animates a frozen brush's Color when IsEnabled
        // changes, causing Storyboard.VerifyPathIsAnimatable to throw on .NET 6+.
        DispatcherUnhandledException += (_, args) =>
        {
            if (args.Exception is InvalidOperationException
                && args.Exception.StackTrace?.Contains("VerifyPathIsAnimatable") == true)
            {
                args.Handled = true;
                return;
            }

            SentrySdk.CaptureException(args.Exception);
            VaultGuard.Services.Logging.AppLogger.Error("Unhandled UI exception", args.Exception);

            // Keep the app alive and inform the user gracefully with the shared exception dialog
            // instead of a hard crash. The dialog marshals onto the UI thread itself.
            args.Handled = true;
            _ = VaultGuard.WPF.Dialogs.ExceptionDialog.ShowAsync(
                message: "Something went wrong and the action couldn't be completed. Your data is safe. You can keep using the app — if this keeps happening, the details below will help us fix it.",
                exception: args.Exception,
                title: "Something went wrong");
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                SentrySdk.CaptureException(ex);
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            SentrySdk.CaptureException(args.Exception);
            args.SetObserved();
        };
    }

    private static void ApplyNavigationViewDarkResources()
    {
        var r = Application.Current.Resources;
        static System.Windows.Media.SolidColorBrush B(byte r2, byte g, byte b)
            => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(r2, g, b));

        // Establish mutable (non-frozen) brush entries so ThemeHelper can mutate them on theme switch
        r["NavigationViewDefaultPaneBackground"]             = B(0x14, 0x14, 0x14);
        r["NavigationViewExpandedPaneBackground"]            = B(0x14, 0x14, 0x14);
        r["NavigationViewTopPaneBackground"]                 = B(0x14, 0x14, 0x14);
        r["NavigationViewContentBackground"]                 = B(0x1A, 0x1A, 0x1A);
        r["NavigationViewContentGridBackground"]             = B(0x1A, 0x1A, 0x1A);
        r["NavigationViewItemForeground"]                    = B(0xD1, 0xD5, 0xDB);
        r["NavigationViewItemForegroundSelected"]            = B(0xFF, 0xFF, 0xFF);
        r["NavigationViewItemForegroundPointerOver"]         = B(0xFF, 0xFF, 0xFF);
        r["NavigationViewItemForegroundPressed"]             = B(0xE5, 0xE7, 0xEB);
        r["NavigationViewItemForegroundDisabled"]            = B(0x4B, 0x55, 0x63);
        r["NavigationViewItemBackground"]                    = System.Windows.Media.Brushes.Transparent;
        r["NavigationViewItemBackgroundSelected"]            = B(0x2B, 0x2B, 0x2B);
        r["NavigationViewItemBackgroundPointerOver"]         = B(0x28, 0x28, 0x28);
        r["NavigationViewItemBackgroundPressed"]             = B(0x36, 0x36, 0x36);
        r["NavigationViewItemBackgroundSelectedPointerOver"] = B(0x2B, 0x2B, 0x2B);
        r["NavigationViewItemBackgroundSelectedPressed"]     = B(0x20, 0x20, 0x20);
        r["NavigationViewItemSeparatorForeground"]           = B(0x3A, 0x3A, 0x3A);
        r["NavigationViewItemHeaderForeground"]              = B(0x6B, 0x72, 0x80);
    }

    private static void ApplyComboBoxDarkResources()
    {
        var r = Application.Current.Resources;
        static System.Windows.Media.SolidColorBrush Hex(string hex)
        {
            var c = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
            return new System.Windows.Media.SolidColorBrush(c);
        }

        r["ComboBoxBackground"]                          = Hex("#2D2D2D");
        r["ComboBoxBackgroundPointerOver"]               = Hex("#3A3A3A");
        r["ComboBoxBackgroundPressed"]                   = Hex("#272727");
        r["ComboBoxBackgroundDisabled"]                  = Hex("#1C1C1C");
        r["ComboBoxBackgroundFocused"]                   = Hex("#2D2D2D");
        r["ComboBoxBackgroundOpen"]                      = Hex("#2D2D2D");
        r["ComboBoxEditableBackground"]                  = Hex("#2D2D2D");
        r["ComboBoxEditableBackgroundPointerOver"]       = Hex("#3A3A3A");
        r["ComboBoxEditableBackgroundPressed"]           = Hex("#272727");
        r["ComboBoxEditableBackgroundFocused"]           = Hex("#2D2D2D");
        r["ComboBoxForeground"]                          = Hex("#E5E5E5");
        r["ComboBoxForegroundDisabled"]                  = Hex("#6A6A6A");
        r["ComboBoxForegroundFocused"]                   = Hex("#E5E5E5");
        r["ComboBoxPlaceholderForeground"]               = Hex("#9D9D9D");
        r["ComboBoxPlaceholderForegroundPointerOver"]    = Hex("#9D9D9D");
        r["ComboBoxPlaceholderForegroundFocused"]        = Hex("#9D9D9D");
        r["ComboBoxPlaceholderForegroundDisabled"]       = Hex("#6A6A6A");
        r["ComboBoxBorderBrush"]                         = Hex("#3A3A3A");
        r["ComboBoxBorderBrushPointerOver"]              = Hex("#3A3A3A");
        r["ComboBoxBorderBrushPressed"]                  = Hex("#3A3A3A");
        r["ComboBoxBorderBrushFocused"]                  = Hex("#3A3A3A");
        r["ComboBoxBorderBrushDisabled"]                 = Hex("#333333");
        r["ComboBoxDropDownBackground"]                  = Hex("#1E1E1E");
        r["ComboBoxDropDownBorderBrush"]                 = Hex("#3A3A3A");
        r["ComboBoxDropDownGlyphForeground"]             = Hex("#9D9D9D");
        r["ComboBoxItemBackground"]                      = System.Windows.Media.Brushes.Transparent;
        r["ComboBoxItemBackgroundPointerOver"]           = Hex("#383838");
        r["ComboBoxItemBackgroundPressed"]               = Hex("#2D2D2D");
        r["ComboBoxItemBackgroundSelected"]              = Hex("#3A3A3A");
        r["ComboBoxItemBackgroundSelectedPointerOver"]   = Hex("#3A3A3A");
        r["ComboBoxItemBackgroundSelectedPressed"]       = Hex("#3A3A3A");
        r["ComboBoxItemBackgroundDisabled"]              = System.Windows.Media.Brushes.Transparent;
        r["ComboBoxItemForeground"]                      = Hex("#E5E5E5");
        r["ComboBoxItemForegroundPointerOver"]           = Hex("#FFFFFF");
        r["ComboBoxItemForegroundPressed"]               = Hex("#E5E5E5");
        r["ComboBoxItemForegroundSelected"]              = Hex("#FFFFFF");
        r["ComboBoxItemForegroundSelectedPointerOver"]   = Hex("#FFFFFF");
        r["ComboBoxItemForegroundDisabled"]              = Hex("#6A6A6A");
    }

    private void InitializeSentry()
    {
        try
        {
            var configuration = _host.Services.GetRequiredService<IConfiguration>();
            var sentryConfig = configuration.GetSection("Sentry").Get<SentryConfiguration>() ?? new VaultGuard.Models.Configuration.SentryConfiguration();

            // Local settings.json can override the DSN (configurable from the About tab in Settings).
            var localSettingsPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "VaultGuard", "settings.json");
            if (System.IO.File.Exists(localSettingsPath))
            {
                try
                {
                    var localSettings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(
                        System.IO.File.ReadAllText(localSettingsPath)) ?? new();
                    if (localSettings.TryGetValue("SentryDsn", out var localDsn) && !string.IsNullOrWhiteSpace(localDsn))
                        sentryConfig.Dsn = localDsn;
                }
                catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Failed to read local Sentry DSN override", ex); }
            }

            if (sentryConfig.IsConfigured)
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
            VaultGuard.Services.Logging.AppLogger.Error($"Failed to initialize Sentry", ex);
        }
    }

    /// <summary>
    /// Invoked when the application is launched normally by the end user.
    /// </summary>
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Any dialog window (not the main window) gets scaled to the user's "Dialogs" font setting
        // when it loads. Registered as a class handler so every current and future dialog is covered.
        EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent,
            new RoutedEventHandler((s, _) => FontScaleManager.ApplyDialogWindowScale(s as Window)));

        m_window = new MainWindow(_host.Services);

        ThemeHelper.Initialize(m_window, this);
        _ = LoadSavedTheme();

        // Load persisted toast accent colours so they're applied before any toast is shown.
        VaultGuard.WPF.Services.ToastSettings.Load();

        InitTrayIcon();
        m_window.Show();

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

                // Re-navigate to the login page now that the database is fully initialised.
                // The LoginPage that loaded at startup ran before the DB was ready, so
                // profiles and auth checks would have failed silently.
                m_window?.Dispatcher.Invoke(() => (m_window as MainWindow)?.OnDatabaseInitialized());
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
            // Ensure window is initialized
            if (m_window == null)
            {
                VaultGuard.Services.Logging.AppLogger.Debug("Main window not initialized, skipping database configuration dialog.");
                return;
            }

            using var scope = _host.Services.CreateScope();
            var databaseConfigService = scope.ServiceProvider.GetRequiredService<IDatabaseConfigurationService>();
            var platformService = scope.ServiceProvider.GetRequiredService<IPlatformService>();

            // Check if this is first run with timeout
            var isFirstRunTask = databaseConfigService.IsFirstRunAsync();
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
            var completedTask = await Task.WhenAny(isFirstRunTask, timeoutTask);

            bool isFirstRun = false;
            if (completedTask == isFirstRunTask)
            {
                isFirstRun = await isFirstRunTask;
            }
            else
            {
                VaultGuard.Services.Logging.AppLogger.Debug("IsFirstRunAsync timed out, assuming not first run.");
                return;
            }

            if (isFirstRun)
            {
                VaultGuard.Services.Logging.AppLogger.Debug("First run detected, showing database configuration dialog.");

                // Show the database configuration dialog on UI thread with error handling
                try
                {
                    await m_window.Dispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            var dialog = new Dialogs.DatabaseConfigurationDialog(databaseConfigService)
                            {
                                Owner = m_window
                            };
                            var result = dialog.ShowDialog();
                            VaultGuard.Services.Logging.AppLogger.Debug($"Database configuration dialog result: {result}");
                        }
                        catch (Exception dialogEx)
                        {
                            VaultGuard.Services.Logging.AppLogger.Debug($"Error in dialog construction/display: {dialogEx.Message}");
                            SentrySdk.CaptureException(dialogEx);
                            MessageBox.Show(
                                $"Failed to show database configuration dialog: {dialogEx.Message}\n\nThe application will continue with default settings.",
                                "Configuration Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        }
                    });
                }
                catch (Exception dispatcherEx)
                {
                    VaultGuard.Services.Logging.AppLogger.Debug($"Error invoking on dispatcher: {dispatcherEx.Message}");
                    SentrySdk.CaptureException(dispatcherEx);
                }
            }
            else
            {
                VaultGuard.Services.Logging.AppLogger.Debug("Not first run, skipping database configuration dialog.");
            }
        }
        catch (Exception ex)
        {
            // Continue with startup even if dialog fails
            VaultGuard.Services.Logging.AppLogger.Debug($"Error in ShowDatabaseConfigurationIfNeededAsync: {ex.Message}\nStackTrace: {ex.StackTrace}");
            SentrySdk.CaptureException(ex);
        }
    }

    private async Task LoadSavedTheme()
    {
        try
        {
            using var scope = _host.Services.CreateScope();
            var secureStorage = scope.ServiceProvider.GetRequiredService<ISecureStorageService>();
            var savedThemeFromSecure = await secureStorage.GetAsync("SelectedTheme");

            if (!string.IsNullOrEmpty(savedThemeFromSecure))
            {
                var theme = savedThemeFromSecure switch
                {
                    "Light" => AppTheme.Light,
                    "Dark" => AppTheme.Dark,
                    "System" => AppTheme.System,
                    _ => AppTheme.System
                };

                ThemeHelper.SetTheme(theme);
            }
            else
            {
                // Default to System theme
                ThemeHelper.SetTheme(AppTheme.System);
            }
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            ThemeHelper.SetTheme(AppTheme.System);
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
    private System.Windows.Forms.NotifyIcon? _trayIcon;

    public MainWindow? MainWindow => m_window as MainWindow;
    public IServiceProvider Services => _host.Services;

    // ── System Tray ─────────────────────────────────────────────────────────────

    private void InitTrayIcon()
    {
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Text    = "VaultGuard Vault Guard",
            Visible = true,
            Icon    = System.Drawing.SystemIcons.Shield
        };

        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("Open VaultGuard", null, (_, _) => ShowMainWindow());
        menu.Items.Add("Lock Vault",       null, (_, _) => LockVault());
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add("Settings",         null, (_, _) => OpenSettings());
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add("Exit",             null, (_, _) => ExitApp());

        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (_, _) => ShowMainWindow();

        _trayIcon.ShowBalloonTip(
            timeout: 2000,
            tipTitle: "VaultGuard",
            tipText: "VaultGuard is running in the background.",
            tipIcon: System.Windows.Forms.ToolTipIcon.Info);
    }

    private void ShowMainWindow()
    {
        if (m_window == null) return;
        m_window.Dispatcher.Invoke(() =>
        {
            m_window.Show();
            m_window.WindowState = WindowState.Normal;
            m_window.Activate();
        });
    }

    private void LockVault()
    {
        m_window?.Dispatcher.Invoke(() => (m_window as MainWindow)?.HandleLogout());
        _trayIcon?.ShowBalloonTip(1500, "VaultGuard", "Vault locked.", System.Windows.Forms.ToolTipIcon.Info);
    }

    private void OpenSettings()
    {
        ShowMainWindow();
        m_window?.Dispatcher.Invoke(() => (m_window as MainWindow)?.NavigateToPage("Settings"));
    }

    private void ExitApp()
    {
        _trayIcon?.Dispose();
        _trayIcon = null;
        Current.Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
