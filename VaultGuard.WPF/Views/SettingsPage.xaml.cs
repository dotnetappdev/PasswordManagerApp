using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using VaultGuard.Services.Interfaces;
using VaultGuard.Models.DTOs;
using VaultGuard.Models.DTOs.Auth;
using VaultGuard.WPF.ViewModels;
using VaultGuard.Services.Utilities;
using VaultGuard.Imports.Interfaces;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using VaultGuard.DAL;
using VaultGuard.DAL.Seed;
using Microsoft.EntityFrameworkCore;
using Sentry;
using VaultGuard.ExceptionReporting;

namespace VaultGuard.WPF.Views;

public sealed partial class SettingsPage : Page
{
    private SettingsViewModel? _viewModel;
    private IServiceProvider? _serviceProvider;
    private IAuthService? _authService;
    private IUserProfileService? _userProfileService;
    private readonly FileLogger _logger;
    private List<UserDto> _availableUsers = new();

    // Guards the cloud-backup dialogs (Create/Restore/Browse/Delete) against double-clicks.
    // These handlers are "async void" and each shows a ModernWpf ContentDialog; a second click
    // before the first dialog finishes can open two ContentDialogs over the same visual root at
    // once, which ModernWpf doesn't support — it crashes deep in WPF's layout pass with
    // "Specified Visual is already a child of another Visual" (AddVisualChild). One flag for the
    // whole flow is enough since the various backup actions are mutually exclusive anyway.
    private bool _cloudBackupDialogBusy;

    // Periodically pings OneDrive while this page is open so the online/offline dot updates live
    // instead of only on page load — no app restart needed to see a dropped connection recover.
    private System.Windows.Threading.DispatcherTimer? _oneDriveStatusTimer;

    public SettingsPage()
    {
        InitializeComponent();
        _logger = new FileLogger();
        BuildToastRows();
        PopulateAboutInfo();
        InitAccessibilityControls();
        Unloaded += SettingsPage_Unloaded;
    }

    // Called by MainWindow's Closing handler so edits aren't lost if the app is closed directly
    // from the Settings page, before SettingsPage_Unloaded would normally have a chance to fire.
    public void FlushPendingSettings()
    {
        if (_viewModel == null) return;
        try { _viewModel.SaveSettingsAsync().GetAwaiter().GetResult(); }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Failed to persist settings on app exit", ex); }
    }

    private T? GetElement<T>(string name) where T : class
    {
        try
        {
            return this.FindName(name) as T;
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Error($"Failed to resolve named element '{name}'", ex);
            return null;
        }
    }

    // ── Accessibility tab ────────────────────────────────────────────────────
    private bool _initingAccessibility;

    private void InitAccessibilityControls()
    {
        try
        {
            _initingAccessibility = true;
            if (ZoomSlider != null) ZoomSlider.Value = Helpers.AccessibilityManager.UiZoom;
            if (FontSizeSlider != null) FontSizeSlider.Value = Helpers.AccessibilityManager.BaseFontSize;
            if (ReduceMotionToggle != null) ReduceMotionToggle.IsOn = Helpers.AccessibilityManager.ReduceMotion;
            if (HighContrastToggle != null) HighContrastToggle.IsOn = Helpers.AccessibilityManager.HighContrast;
            UpdateZoomText();
            UpdateFontText();
            InitSectionFontCombos();
        }
        finally { _initingAccessibility = false; }
    }

    // Per-section font presets: label → scale multiplier (stacks on top of the global font size).
    private static readonly (string Label, double Scale)[] FontPresets =
    {
        ("Smaller", 0.85),
        ("Default", 1.00),
        ("Larger", 1.15),
        ("Extra Large", 1.30),
        ("Huge", 1.50),
    };

    // Icon regions (Sidebar icons / Card icons) use a much wider range than text — a 16-18px glyph
    // needs real headroom to reach something like 64px, and there's no global multiplier to stack
    // on top (icons are exempt from the "Text size" slider; see FontScaleManager.Apply).
    private static readonly (string Label, double Scale)[] IconPresets =
    {
        ("Smaller", 0.75),
        ("Default", 1.00),
        ("Larger", 1.75),
        ("Extra Large", 2.75),
        ("Huge", 4.00),
    };

    private static bool IsIconRegion(string region) =>
        region == Helpers.FontScaleManager.MenuIcons || region == Helpers.FontScaleManager.CardIcons;

    private static (string Label, double Scale)[] PresetsFor(string region) =>
        IsIconRegion(region) ? IconPresets : FontPresets;

    private void InitSectionFontCombos()
    {
        InitOneSectionCombo(MenuFontCombo, Helpers.FontScaleManager.Menu);
        InitOneSectionCombo(QuickActionsFontCombo, Helpers.FontScaleManager.QuickActions);
        InitOneSectionCombo(DetailsFontCombo, Helpers.FontScaleManager.Details);
        InitOneSectionCombo(DialogsFontCombo, Helpers.FontScaleManager.Dialogs);
        InitOneSectionCombo(GlobalFontCombo, Helpers.FontScaleManager.Global);
        InitOneSectionCombo(CardIconsFontCombo, Helpers.FontScaleManager.CardIcons);
    }

    private void InitOneSectionCombo(System.Windows.Controls.ComboBox? combo, string region)
    {
        if (combo == null) return;
        var presets = PresetsFor(region);
        if (combo.Items.Count == 0)
            foreach (var p in presets) combo.Items.Add(p.Label);

        // Select the preset closest to the saved scale for this region.
        var scale = Helpers.FontScaleManager.GetRegionScale(region);
        int best = 1; // Default
        double bestDelta = double.MaxValue;
        for (int i = 0; i < presets.Length; i++)
        {
            var d = Math.Abs(presets[i].Scale - scale);
            if (d < bestDelta) { bestDelta = d; best = i; }
        }
        combo.SelectedIndex = best;
    }

    private void SectionFontCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_initingAccessibility) return;
        if (sender is not System.Windows.Controls.ComboBox combo) return;
        if (combo.Tag is not string region) return;
        var presets = PresetsFor(region);
        var idx = combo.SelectedIndex;
        if (idx < 0 || idx >= presets.Length) return;
        Helpers.FontScaleManager.SetRegionScale(region, presets[idx].Scale);
    }

    private void ResetSectionFontsButton_Click(object sender, RoutedEventArgs e)
    {
        Helpers.FontScaleManager.ResetAll();
        try { _initingAccessibility = true; InitSectionFontCombos(); }
        finally { _initingAccessibility = false; }
    }

    private void UpdateZoomText()
    {
        if (ZoomValueText != null)
            ZoomValueText.Text = $"{Math.Round((ZoomSlider?.Value ?? 1.0) * 100)}%";
    }

    private void UpdateFontText()
    {
        if (FontSizeValueText != null)
            FontSizeValueText.Text = $"{Math.Round(FontSizeSlider?.Value ?? 14)} pt";
    }

    private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdateZoomText();
        if (_initingAccessibility) return;
        Helpers.AccessibilityManager.SetZoom(e.NewValue);
    }

    private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdateFontText();
        if (_initingAccessibility) return;
        Helpers.AccessibilityManager.SetFontSize(e.NewValue);
    }

    private void ResetZoomButton_Click(object sender, RoutedEventArgs e)
    {
        if (ZoomSlider != null) ZoomSlider.Value = Helpers.AccessibilityManager.DefaultZoom;
    }

    private void ResetFontButton_Click(object sender, RoutedEventArgs e)
    {
        if (FontSizeSlider != null) FontSizeSlider.Value = Helpers.AccessibilityManager.DefaultFontSize;
    }

    private void ReduceMotionToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_initingAccessibility) return;
        if (sender is ModernWpf.Controls.ToggleSwitch t)
            Helpers.AccessibilityManager.SetReduceMotion(t.IsOn);
    }

    private void HighContrastToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_initingAccessibility) return;
        if (sender is ModernWpf.Controls.ToggleSwitch t)
            Helpers.AccessibilityManager.SetHighContrast(t.IsOn);
    }

    private void SettingsPage_Unloaded(object sender, RoutedEventArgs e)
    {
        _oneDriveStatusTimer?.Stop();
        _oneDriveStatusTimer = null;

        // Persist any in-memory edits (database path, connection string, export path, DSNs, etc.)
        // when leaving the page. Each navigation to Settings recreates the view model and reloads
        // from disk, so without this, text-field edits are lost simply by switching away and back.
        if (_viewModel != null)
        {
            try { _ = _viewModel.SaveSettingsAsync(); }
            catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Failed to persist settings on unload", ex); }
        }
    }

    private void StartOneDriveStatusTimer()
    {
        _oneDriveStatusTimer?.Stop();
        _oneDriveStatusTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _oneDriveStatusTimer.Tick += async (_, _) =>
        {
            if (_viewModel != null)
                await _viewModel.RefreshOneDriveStatusAsync();
        };
        _oneDriveStatusTimer.Start();
    }

    // Fills the About panel with the real assembly version, build date and the live .NET runtime
    // (RuntimeInformation.FrameworkDescription is e.g. ".NET 10.0.1") rather than hardcoded text.
    private void PopulateAboutInfo()
    {
        try
        {
            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            var version = asm.GetName().Version?.ToString(3) ?? "1.0.0";
            var framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription; // ".NET 10.0.x"

            if (AppVersionText != null) AppVersionText.Text = $"Version {version} · Windows Desktop";
            if (RuntimeText != null) RuntimeText.Text = framework;
            if (BuiltWithText != null) BuiltWithText.Text = $" using {framework}";

            var buildDate = File.GetLastWriteTime(asm.Location);
            if (BuildDateText != null) BuildDateText.Text = buildDate.ToString("MMMM yyyy");
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"About info load failed", ex); }
    }

    public async void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
    {
        // Note: WPF Page doesn't have base.OnNavigatedTo
        if (e.ExtraData is IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _authService = serviceProvider.GetRequiredService<IAuthService>();
            _userProfileService = serviceProvider.GetService<IUserProfileService>();
            _viewModel = new SettingsViewModel(serviceProvider);
            DataContext = _viewModel;
            StartOneDriveStatusTimer();
            RefreshLicenseStatus();

            // Theme loads asynchronously in the view model's constructor, so reflect the tile
            // selection both now (in case it's already loaded) and whenever it changes.
            UpdateThemeTileSelection(_viewModel.SelectedTheme);
            _viewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(SettingsViewModel.SelectedTheme))
                    Dispatcher.Invoke(() => UpdateThemeTileSelection(_viewModel?.SelectedTheme));
            };

            // Load all users for multi-user selection
            await LoadUsersAsync();

            await RefreshTwoFactorStatusAsync();

            // Update network location visibility based on initial provider selection
            UpdateNetworkLocationVisibility();

            // Display current database path
            await DisplayCurrentDatabasePathAsync();

            // Preload import providers to ensure they're available when needed
            try
            {
                await _logger.LogAsync("SettingsPage", "Starting provider preload");
                var importService = serviceProvider.GetService<VaultGuard.Imports.Interfaces.IImportService>();
                if (importService != null)
                {
                    // Force load all VaultGuardImports.* assemblies and register providers
                    var importDllsList = new List<string>();

                    // Common candidate directories to search for import provider assemblies. This covers
                    // both per-user and machine-wide install locations so the provider is found when
                    // running as a normal user or as an elevated/admin process.
                    var candidateDirs = new List<string>();

                    // App base and AppContext
                    try { candidateDirs.Add(AppDomain.CurrentDomain.BaseDirectory); } catch (Exception ex) { await _logger.LogErrorAsync("SettingsPage", "Failed to add BaseDirectory candidate", ex); }
                    try { candidateDirs.Add(AppContext.BaseDirectory); } catch (Exception ex) { await _logger.LogErrorAsync("SettingsPage", "Failed to add AppContext.BaseDirectory candidate", ex); }

                    // Executing assembly location
                    try
                    {
                        var execPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                        if (!string.IsNullOrEmpty(execPath)) candidateDirs.Add(System.IO.Path.GetDirectoryName(execPath)!);
                    }
                    catch (Exception ex) { await _logger.LogErrorAsync("SettingsPage", "Failed to add executing assembly directory candidate", ex); }

                    // Current directory
                    try { candidateDirs.Add(Environment.CurrentDirectory); } catch (Exception ex) { await _logger.LogErrorAsync("SettingsPage", "Failed to add current directory candidate", ex); }

                    // Per-user imports folder (LocalAppData)
                    try { candidateDirs.Add(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VaultGuard", "imports")); } catch (Exception ex) { await _logger.LogErrorAsync("SettingsPage", "Failed to add LocalApplicationData imports candidate", ex); }

                    // Machine-wide imports folder (ProgramData)
                    try { candidateDirs.Add(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "VaultGuard", "imports")); } catch (Exception ex) { await _logger.LogErrorAsync("SettingsPage", "Failed to add CommonApplicationData imports candidate", ex); }

                    // Standard plugin discovery folder used by PluginDiscoveryService
                    try { candidateDirs.Add(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "imports", "otherpasswordmanagers")); } catch (Exception ex) { await _logger.LogErrorAsync("SettingsPage", "Failed to add plugin discovery folder candidate", ex); }

                    // Collect DLLs from all candidate directories
                    foreach (var dir in candidateDirs.Where(d => !string.IsNullOrEmpty(d)).Distinct())
                    {
                        try
                        {
                            if (Directory.Exists(dir))
                            {
                                importDllsList.AddRange(Directory.GetFiles(dir, "VaultGuardImports.*.dll", SearchOption.AllDirectories));
                                importDllsList.AddRange(Directory.GetFiles(dir, "VaultGuardImports.*.dll", SearchOption.TopDirectoryOnly));
                            }
                        }
                        catch (Exception ex) { await _logger.LogErrorAsync("SettingsPage", $"Failed to enumerate import DLLs in {dir}", ex); }
                    }
                    var importDlls = importDllsList.ToArray();
                    await _logger.LogAsync("SettingsPage", $"Found {importDlls.Length} import DLLs");

                    foreach (var dllPath in importDlls)
                    {
                        try
                        {
                            var assembly = System.Reflection.Assembly.LoadFrom(dllPath);
                            var providerTypes = assembly.GetTypes()
                                .Where(t => typeof(VaultGuard.Imports.Interfaces.IPasswordImportProvider).IsAssignableFrom(t)
                                         && !t.IsInterface && !t.IsAbstract);

                            foreach (var providerType in providerTypes)
                            {
                                var provider = Activator.CreateInstance(providerType) as VaultGuard.Imports.Interfaces.IPasswordImportProvider;
                                if (provider != null)
                                {
                                    importService.RegisterProvider(provider);
                                    await _logger.LogAsync("SettingsPage", $"Registered {provider.DisplayName} v{provider.Version}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            var fileName = System.IO.Path.GetFileName(dllPath);
                            await _logger.LogErrorAsync("SettingsPage", $"Failed to load provider from {fileName}", ex);
                        }
                    }

                    // Also try async loading in background for plugin-based providers
                    await importService.GetAvailableProvidersAsync();

                    // Populate import types after providers are loaded
                    await PopulateImportTypesAsync(importService);
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("SettingsPage", "Error during provider preload", ex);
            }
        }
    }

    private async System.Threading.Tasks.Task DisplayCurrentDatabasePathAsync()
    {
        try
        {
            if (_serviceProvider != null)
            {
                var platformService = _serviceProvider.GetService<IPlatformService>();
                if (platformService != null)
                {
                    var appDataDir = platformService.GetAppDataDirectory();
                    // Fixed: Match the actual database path used in ServiceConfiguration.cs
                    var dbPath = System.IO.Path.Combine(appDataDir, "passwordmanager.db");
                    CurrentDbPathTextBox.Text = dbPath;

                    // Ensure the directory exists
                    var dbDirectory = System.IO.Path.GetDirectoryName(dbPath);
                    if (!string.IsNullOrEmpty(dbDirectory) && !System.IO.Directory.Exists(dbDirectory))
                    {
                        System.IO.Directory.CreateDirectory(dbDirectory);
                        await _logger.LogAsync("SettingsPage", $"Created database directory: {dbDirectory}");
                    }

                    // Check if the database file actually exists
                    if (System.IO.File.Exists(dbPath))
                    {
                        await _logger.LogAsync("SettingsPage", $"Database file found at: {dbPath}");
                    }
                    else
                    {
                        await _logger.LogAsync("SettingsPage", $"Database file does not exist yet at: {dbPath}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("SettingsPage", "Error displaying database path", ex);
        }
    }

    private async void OpenDatabaseFolderButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_serviceProvider != null)
            {
                var platformService = _serviceProvider.GetService<IPlatformService>();
                if (platformService != null)
                {
                    var appDataDir = platformService.GetAppDataDirectory();
                    // Fixed: Open the actual folder where the database is stored
                    var dataFolder = appDataDir;

                    // Ensure directory exists before opening
                    if (!System.IO.Directory.Exists(dataFolder))
                    {
                        System.IO.Directory.CreateDirectory(dataFolder);
                    }

                    // Open in File Explorer
                    System.Diagnostics.Process.Start("explorer.exe", dataFolder);
                    await _logger.LogAsync("SettingsPage", $"Opened database folder: {dataFolder}");
                }
            }
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("SettingsPage", "Error opening database folder", ex);
            await ShowErrorDialog("Failed to open database folder: " + ex.Message);
        }
    }

    private async void CopyDatabasePathButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = CurrentDbPathTextBox.Text;
            if (!string.IsNullOrEmpty(path))
            {
                System.Windows.Clipboard.SetText(path);
                VaultGuard.WPF.Services.ToastService.Instance.Show("Database path copied to clipboard.", VaultGuard.WPF.Services.ToastType.Success);

                await _logger.LogAsync("SettingsPage", "Copied database path to clipboard");
            }
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("SettingsPage", "Error copying database path", ex);
            await ShowErrorDialog("Failed to copy path: " + ex.Message);
        }
    }

    private async System.Threading.Tasks.Task CleanupUnwantedCategoriesAsync()
    {
        try
        {
            if (_serviceProvider != null)
            {
                var categoryService = _serviceProvider.GetService<ICategoryInterface>();
                if (categoryService != null)
                {
                    var allCategories = await categoryService.GetAllAsync();

                    // Keep only Logins and Credit Cards
                    var allowedCategories = new[] { "Logins", "Credit Cards" };
                    var categoriesToDelete = allCategories
                        .Where(c => !allowedCategories.Contains(c.Name))
                        .ToList();

                    if (categoriesToDelete.Any())
                    {
                        await _logger.LogAsync("SettingsPage", $"Cleaning up {categoriesToDelete.Count} unwanted categories");

                        foreach (var category in categoriesToDelete)
                        {
                            try
                            {
                                // Check if category has password items before deleting
                                var hasItems = await categoryService.HasPasswordItemsAsync(category.Id);
                                if (!hasItems)
                                {
                                    await categoryService.DeleteAsync(category.Id);
                                    await _logger.LogAsync("SettingsPage", $"Deleted category: {category.Name}");
                                }
                                else
                                {
                                    await _logger.LogAsync("SettingsPage", $"Skipped category {category.Name} (has password items)");
                                }
                            }
                            catch (Exception ex)
                            {
                                await _logger.LogErrorAsync("SettingsPage", $"Error deleting category {category.Name}", ex);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("SettingsPage", "Error cleaning up categories", ex);
        }
    }

    private async System.Threading.Tasks.Task PopulateImportTypesAsync(VaultGuard.Imports.Interfaces.IImportService importService)
    {
        try
        {
            var providers = await importService.GetAvailableProvidersAsync();
            ImportTypeComboBox.Items.Clear();

            foreach (var provider in providers.OrderBy(p => p.DisplayName))
            {
                var displayText = $"{provider.DisplayName} (v{provider.Version})";
                ImportTypeComboBox.Items.Add(new ComboBoxItem { Content = displayText, Tag = provider.ProviderName });
            }

            if (ImportTypeComboBox.Items.Count > 0)
            {
                ImportTypeComboBox.SelectedIndex = 0;
            }
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", ex); }
    }

    private async void TwoFactorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_serviceProvider == null) return;
        try
        {
            var userId = _authService?.CurrentUser?.Id ?? await _authService?.GetCurrentUserIdAsync()!;
            var userEmail = _authService?.CurrentUser?.Email;

            await Helpers.TwoFactorDialogHelper.OpenManageDialogAsync(_serviceProvider, userId, userEmail);

            await RefreshTwoFactorStatusAsync();
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("SettingsPage", "Two-factor setup failed", ex);
        }
    }

    // "Forget this device" — wipes the DPAPI-cached master password for the current profile so
    // the next sign-in (even with 2FA enabled) requires the master password again.
    private async void ForgetDeviceButton_Click(object sender, RoutedEventArgs e)
    {
        if (_serviceProvider == null) return;
        try
        {
            var userId = _authService?.CurrentUser?.Id ?? await _authService?.GetCurrentUserIdAsync()!;
            if (string.IsNullOrEmpty(userId)) return;

            await VaultGuard.WPF.ViewModels.LoginViewModel.ForgetDeviceAsync(_serviceProvider, userId);

            var dialog = new ModernWpf.Controls.ContentDialog
            {
                Title = "Device forgotten",
                Content = "Your saved sign-in was cleared. You'll need your master password next time you sign in.",
                CloseButtonText = "OK"
            };
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("SettingsPage", "Forget device failed", ex);
        }
    }

    private async System.Threading.Tasks.Task RefreshTwoFactorStatusAsync()
    {
        if (_serviceProvider == null || TwoFactorStatusText == null) return;
        try
        {
            var twoFactorService = _serviceProvider.GetService<ITwoFactorService>();
            var userId = _authService?.CurrentUser?.Id ?? await _authService?.GetCurrentUserIdAsync()!;
            if (twoFactorService == null || string.IsNullOrEmpty(userId)) return;

            var status = await twoFactorService.GetTwoFactorStatusAsync(userId);
            TwoFactorStatusText.Text = status.IsEnabled ? "Enabled" : "Not enabled";
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("SettingsPage", "Error loading two-factor status", ex);
        }
    }

    private async void PasscodeToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            await _viewModel.SaveSettingsAsync();
        }
    }

    private async void SyncToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            await _viewModel.SaveSettingsAsync();
        }
    }

    // ── Theme tile picker ────────────────────────────────────────────────────
    private static readonly (string Name, string ElementName)[] ThemeTiles =
    {
        ("Light", "ThemeTile_Light"),
        ("Dark", "ThemeTile_Dark"),
        ("System", "ThemeTile_System"),
        ("High Contrast", "ThemeTile_HighContrast"),
        ("Windows", "ThemeTile_Windows"),
    };

    private async void ThemeTile_Click(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel == null || sender is not Border tile || tile.Tag is not string themeName) return;

        _viewModel.SelectedTheme = themeName;
        UpdateThemeTileSelection(themeName);
        await _viewModel.SaveSettingsAsync();
    }

    /// <summary>Highlights the tile matching the current theme and paints the "Windows" tile's
    /// swatch with the user's actual Windows accent colour, so the picker itself reflects their
    /// custom theme rather than a fixed blue.</summary>
    private void UpdateThemeTileSelection(string? selectedTheme)
    {
        try
        {
            var accent = VaultGuard.WPF.Services.ThemeHelper.GetWindowsAccentColor();
            if (accent.HasValue)
            {
                var swatch = GetElement<TextBlock>("WindowsTileSwatch");
                if (swatch != null) swatch.Foreground = new SolidColorBrush(accent.Value);
            }
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", ex); }

        foreach (var (name, elementName) in ThemeTiles)
        {
            var border = GetElement<Border>(elementName);
            if (border == null) continue;
            bool selected = string.Equals(name, selectedTheme, StringComparison.OrdinalIgnoreCase);
            border.BorderBrush = selected
                ? (Brush)FindResource("ModernPrimaryBrush")
                : Brushes.Transparent;
        }
    }

    private async void SessionTimeoutComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel != null && SessionTimeoutComboBox.SelectedItem != null)
        {
            await _viewModel.SaveSettingsAsync();
        }
    }

    private async void AuthModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel != null && AuthModeComboBox.SelectedItem != null)
        {
            await _viewModel.SaveSettingsAsync();
        }
    }

    private async void TestApiConnectionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null)
            return;

        if (string.IsNullOrWhiteSpace(_viewModel.ApiBaseUrl))
        {
            await ShowErrorDialog("API Connection", "Enter the API base URL first.");
            return;
        }

        try
        {
            await _viewModel.SaveSettingsAsync();

            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(8)
            };

            if (!string.IsNullOrWhiteSpace(_viewModel.ApiKey))
                client.DefaultRequestHeaders.Add("X-Api-Key", _viewModel.ApiKey);
            if (!string.IsNullOrWhiteSpace(_viewModel.ApiClientId))
                client.DefaultRequestHeaders.Add("X-Client-Id", _viewModel.ApiClientId);

            var baseUri = _viewModel.ApiBaseUrl.TrimEnd('/');
            var response = await client.GetAsync($"{baseUri}/health");
            if (response.IsSuccessStatusCode)
            {
                VaultGuard.WPF.Services.ToastService.Instance.Show("API connection succeeded.", VaultGuard.WPF.Services.ToastType.Success);
            }
            else
            {
                await ShowErrorDialog("API Connection", $"API responded with {(int)response.StatusCode} {response.ReasonPhrase}.");
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialog("API Connection", $"Could not reach the API: {ex.Message}");
        }
    }

    // Ask the CONFIGURED API server to issue a REAL key into its own database, authenticated by the
    // account's email + master password. This is the key that will pass "Test API Connection" — unlike a
    // key generated locally (which lives in this app's DB and gets a 401/403 from the remote API).
    private async void GenerateApiKeyFromServerButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null)
            return;

        var baseUrl = _viewModel.ApiBaseUrl?.Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            await ShowErrorDialog("Generate API Key", "Enter the API base URL first.");
            return;
        }

        var email = IssueEmailTextBox.Text?.Trim();
        var master = IssueMasterPasswordBox.Password;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(master))
        {
            await ShowErrorDialog("Generate API Key", "Enter your account email and master password.");
            return;
        }

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            var baseUri = baseUrl.TrimEnd('/');
            var payload = new { email, masterPassword = master, name = $"WPF app ({DateTime.Now:yyyy-MM-dd HH:mm})" };
            var resp = await client.PostAsJsonAsync($"{baseUri}/api/apikeys/issue", payload);

            if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await ShowErrorDialog("Generate API Key", "The API rejected those credentials. Check the email and master password for this server's account.");
                return;
            }
            if (!resp.IsSuccessStatusCode)
            {
                await ShowErrorDialog("Generate API Key", $"Couldn't issue a key: {(int)resp.StatusCode} {resp.ReasonPhrase}.");
                return;
            }

            using var doc = System.Text.Json.JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var key = doc.RootElement.TryGetProperty("keyValue", out var kv) ? kv.GetString() : null;
            if (string.IsNullOrWhiteSpace(key))
            {
                await ShowErrorDialog("Generate API Key", "The server did not return a key value.");
                return;
            }

            _viewModel.ApiKey = key;
            IssueMasterPasswordBox.Clear();
            await _viewModel.SaveSettingsAsync();
            VaultGuard.WPF.Services.ToastService.Instance.Show("Real API key issued by the server and saved. Use Test API Connection to confirm.", VaultGuard.WPF.Services.ToastType.Success);
        }
        catch (Exception ex)
        {
            await ShowErrorDialog("Generate API Key", $"Could not reach the API: {ex.Message}");
        }
    }

    // ── Licensing (CD keys / Pro feature unlock) — see docs/LICENSING.md ───────────────────────

    private void RefreshLicenseStatus()
    {
        if (_serviceProvider == null || LicenseStatusText == null) return;
        try
        {
            var licenseClient = _serviceProvider.GetService<ILicenseClientService>();
            licenseClient?.LoadCached();
            LicenseStatusText.Text = DescribeLicenseStatus(licenseClient);
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Error("Failed to refresh license status", ex);
        }
    }

    private static string DescribeLicenseStatus(ILicenseClientService? licenseClient)
    {
        if (licenseClient == null) return "Licensing is unavailable.";
        if (!licenseClient.IsProUnlocked) return "Free plan — no license key activated.";
        var expiry = licenseClient.ExpiresAt.HasValue
            ? $" — expires {licenseClient.ExpiresAt:yyyy-MM-dd}"
            : " (perpetual license)";
        return $"{licenseClient.CurrentPlan} plan active{expiry}";
    }

    private async void ActivateLicenseButton_Click(object sender, RoutedEventArgs e)
    {
        if (_serviceProvider == null) return;

        var key = LicenseKeyTextBox?.Text?.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            await ShowErrorDialog("Activate License", "Enter a license key first.");
            return;
        }

        try
        {
            var licenseClient = _serviceProvider.GetRequiredService<ILicenseClientService>();
            await licenseClient.ActivateAsync(key);
            LicenseStatusText.Text = DescribeLicenseStatus(licenseClient);
            LicenseKeyTextBox!.Text = "";
            VaultGuard.WPF.Services.ToastService.Instance.Show("License activated.", VaultGuard.WPF.Services.ToastType.Success);
        }
        catch (VaultGuard.Services.Interfaces.LicenseActivationException ex)
        {
            await ShowErrorDialog("Activate License", ex.Message);
        }
        catch (Exception ex)
        {
            await ShowErrorDialog("Activate License", $"Activation failed: {ex.Message}");
        }
    }

    private async void DeactivateLicenseButton_Click(object sender, RoutedEventArgs e)
    {
        if (_serviceProvider == null) return;
        try
        {
            var licenseClient = _serviceProvider.GetRequiredService<ILicenseClientService>();
            licenseClient.ClearCached();
            LicenseStatusText.Text = DescribeLicenseStatus(licenseClient);
            VaultGuard.WPF.Services.ToastService.Instance.Show("License removed from this device.", VaultGuard.WPF.Services.ToastType.Info);
        }
        catch (Exception ex)
        {
            await ShowErrorDialog("Deactivate License", $"Failed to remove license: {ex.Message}");
        }
    }

    private async void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
    {
        await ShowChangePasswordDialog();
    }

    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            var success = await _viewModel.ExportDataAsync();

            if (success)
                VaultGuard.WPF.Services.ToastService.Instance.Show("Export completed successfully!", VaultGuard.WPF.Services.ToastType.Success);
            else
                VaultGuard.WPF.Services.ToastService.Instance.Show("Export failed. Please try again.", VaultGuard.WPF.Services.ToastType.Error);
        }
    }

    private void ImportTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateImportButtonState();
    }

    private async System.Threading.Tasks.Task LoadUsersAsync()
    {
        try
        {
            if (_userProfileService != null)
            {
                _availableUsers = await _userProfileService.GetAllUsersAsync();

                // Populate the ListView with users
                UserSelectionListView.Items.Clear();
                foreach (var user in _availableUsers)
                {
                    var checkBox = new CheckBox
                    {
                        Content = $"{user.Email} ({user.FirstName} {user.LastName})".Trim(),
                        Tag = user.Id,
                        Margin = new System.Windows.Thickness(4)
                    };
                    UserSelectionListView.Items.Add(checkBox);
                }
            }
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("SettingsPage", "Failed to load users for multi-select", ex);
        }
    }

    private void ImportUserModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ImportUserHintText != null && ImportUserModeComboBox != null)
        {
            var selectedItem = ImportUserModeComboBox.SelectedItem as ComboBoxItem;
            var tag = selectedItem?.Tag?.ToString() ?? "current";

            // Show/hide multi-user selection panel
            if (MultiUserSelectionPanel != null)
            {
                MultiUserSelectionPanel.Visibility = tag == "multiple" ? Visibility.Visible : Visibility.Collapsed;
            }

            if (tag == "all")
            {
                ImportUserHintText.Text = "Passwords will be imported as accessible to all users in the system";
            }
            else if (tag == "multiple")
            {
                ImportUserHintText.Text = "Passwords will be imported for each selected user";
            }
            else
            {
                ImportUserHintText.Text = "Passwords will be imported to your current account";
            }
        }
    }

    private async void BrowseImportFileButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var filePicker = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Import File",
                Filter = "All Supported Files|*.csv;*.1pux;*.1pu;*.json;*.txt|CSV Files (*.csv)|*.csv|1Password Files (*.1pux)|*.1pux|JSON Files (*.json)|*.json|Text Files (*.txt)|*.txt",
                FilterIndex = 1,
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (filePicker.ShowDialog() == true)
            {
                ImportFilePathTextBox.Text = filePicker.FileName;
                UpdateImportButtonState();
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialog("File Selection Error", $"Failed to open file picker: {ex.Message}");
        }
    }

    private async void StartImportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_serviceProvider == null) return;

        // Create a dedicated scope so the import gets a fresh DbContext isolated from the
        // rest of the app. Using the root provider directly caused EF tracking conflicts
        // (stale entities from login/load operations) that silently failed all item saves.
        using var importScope = _serviceProvider.CreateScope();
        try
        {
            var importService = importScope.ServiceProvider.GetRequiredService<VaultGuard.Imports.Interfaces.IImportService>();
            var selectedItem = ImportTypeComboBox.SelectedItem as ComboBoxItem;
            var filePath = ImportFilePathTextBox.Text;

            if (selectedItem == null || string.IsNullOrEmpty(filePath))
                return;

            // Show progress UI
            ImportStatusBorder.Visibility = Visibility.Visible;
            ImportProgressRing.IsActive = true;
            ImportProgressPanel.Visibility = Visibility.Visible;
            ImportProgressBar.IsIndeterminate = true;
            ImportStatusText.Text = "Loading import providers...";
            ImportProgressText.Text = "Initializing...";
            ImportResultText.Text = "";
            StartImportButton.IsEnabled = false;

            // Force load providers asynchronously first
            // Ensure any import plugin assemblies in the app folder are loaded and registered (fallback)
            try
            {
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var importDllsList = new List<string>();
                importDllsList.AddRange(System.IO.Directory.GetFiles(baseDirectory, "VaultGuardImports.*.dll"));
                var importsFolder = System.IO.Path.Combine(baseDirectory, "imports", "otherpasswordmanagers");
                if (System.IO.Directory.Exists(importsFolder))
                {
                    importDllsList.AddRange(System.IO.Directory.GetFiles(importsFolder, "VaultGuardImports.*.dll", System.IO.SearchOption.AllDirectories));
                }
                foreach (var dllPath in importDllsList)
                {
                    try
                    {
                        var asm = System.Reflection.Assembly.LoadFrom(dllPath);
                        var providerTypes = asm.GetTypes()
                            .Where(t => typeof(VaultGuard.Imports.Interfaces.IPasswordImportProvider).IsAssignableFrom(t)
                                     && !t.IsInterface && !t.IsAbstract);

                        foreach (var providerType in providerTypes)
                        {
                            try
                            {
                                var providerInstance = Activator.CreateInstance(providerType) as VaultGuard.Imports.Interfaces.IPasswordImportProvider;
                                if (providerInstance != null)
                                {
                                    importService.RegisterProvider(providerInstance);
                                    await _logger.LogAsync("SettingsPage", $"Fallback registered provider {providerInstance.DisplayName} v{providerInstance.Version}");
                                }
                            }
                            catch (Exception ex)
                            {
                                await _logger.LogErrorAsync("SettingsPage", $"Failed to instantiate provider type {providerType.FullName}", ex);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        await _logger.LogErrorAsync("SettingsPage", $"Failed to load plugin assembly {dllPath}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("SettingsPage", "Error while fallback-loading import plugins", ex);
            }

            await importService.GetAvailableProvidersAsync();

            // Verify provider exists
            var providers = (await importService.GetAvailableProvidersAsync()).ToList();

            // Log available providers to help diagnose provider discovery issues
            try
            {
                await _logger.LogAsync("SettingsPage", $"Available providers: {string.Join(", ", providers.Select(p => p.ProviderName + " (" + p.DisplayName + ")"))}");
            }
            catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Failed to log available providers", ex); }

            // Get provider name from ComboBoxItem Tag
            string providerName = selectedItem.Tag?.ToString() ?? "";
            var selectedType = selectedItem.Content?.ToString() ?? "";

            if (string.IsNullOrEmpty(providerName))
            {
                ImportProgressRing.IsActive = false;
                ImportProgressPanel.Visibility = Visibility.Collapsed;
                ImportStatusText.Text = "Import failed";
                ImportResultText.Text = "Invalid provider selection";
                StartImportButton.IsEnabled = true;
                return;
            }

            // Try several tolerant resolution strategies: exact provider name, display name contains, normalized match
            IPasswordImportProvider? provider = providers.FirstOrDefault(p => string.Equals(p.ProviderName, providerName, StringComparison.OrdinalIgnoreCase));

            if (provider == null)
            {
                provider = providers.FirstOrDefault(p => !string.IsNullOrEmpty(p.DisplayName) && p.DisplayName.IndexOf(providerName, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (provider == null)
            {
                // Normalize names (remove non-alphanumeric) and compare
                static string Normalize(string s) => new string(s?.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
                var target = Normalize(providerName);
                provider = providers.FirstOrDefault(p => Normalize(p.ProviderName) == target || (!string.IsNullOrEmpty(p.DisplayName) && Normalize(p.DisplayName) == target));
            }

            if (provider == null)
            {
                ImportProgressRing.IsActive = false;
                ImportProgressPanel.Visibility = Visibility.Collapsed;
                ImportStatusText.Text = "Import failed";
                ImportResultText.Text = $"Import provider '{providerName}' not found. Available providers: {string.Join(", ", providers.Select(p => p.ProviderName))}";
                // Also log the failure for diagnostics
                try { await _logger.LogAsync("SettingsPage", $"Provider resolution failed for '{providerName}'. Available: {string.Join(",", providers.Select(p => p.ProviderName))}"); } catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Failed to log provider resolution failure", ex); }
                StartImportButton.IsEnabled = true;
                return;
            }

            // Log the chosen provider
            try { await _logger.LogAsync("SettingsPage", $"Resolved provider '{providerName}' to '{provider.ProviderName}' ({provider.DisplayName})"); } catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Failed to log resolved provider", ex); }

            // Update progress
            ImportStatusText.Text = $"Importing from {selectedType}...";
            ImportProgressText.Text = "Reading file...";

            // Explicit fallback: if provider still not found, try multiple resolution strategies
            if (provider == null && providerName == "1Password")
            {
                try
                {
                    // Try common assembly name variants
                    var assemblyNames = new[] { "VaultGuardImports.OnePassword", "VaultGuardImports.1Password", "VaultGuardImports.OnePassword.dll" };
                    foreach (var asmName in assemblyNames)
                    {
                        var typeName = $"VaultGuardImports.OnePassword.Providers.OnePasswordImportProvider, {asmName}";
                        var onePasswordProviderType = Type.GetType(typeName, false);
                        if (onePasswordProviderType != null)
                        {
                            var instance = Activator.CreateInstance(onePasswordProviderType) as VaultGuard.Imports.Interfaces.IPasswordImportProvider;
                            if (instance != null)
                            {
                                importService.RegisterProvider(instance);
                                provider = instance;
                                break;
                            }
                        }
                    }

                    // As a last resort, scan already loaded assemblies for a matching provider type
                    if (provider == null)
                    {
                        var loaded = AppDomain.CurrentDomain.GetAssemblies()
                            .Where(a => !a.IsDynamic)
                            .ToList();

                        foreach (var asm in loaded)
                        {
                            try
                            {
                                var candidateTypes = asm.GetTypes()
                                    .Where(t => typeof(VaultGuard.Imports.Interfaces.IPasswordImportProvider).IsAssignableFrom(t)
                                                && !t.IsInterface && !t.IsAbstract)
                                    .ToList();

                                foreach (var ct in candidateTypes)
                                {
                                    // Try to instantiate and check ProviderName
                                    try
                                    {
                                        var inst = Activator.CreateInstance(ct) as VaultGuard.Imports.Interfaces.IPasswordImportProvider;
                                        if (inst != null && string.Equals(inst.ProviderName, "1Password", StringComparison.OrdinalIgnoreCase))
                                        {
                                            importService.RegisterProvider(inst);
                                            provider = inst;
                                            break;
                                        }
                                    }
                                    catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Failed to instantiate candidate provider type", ex); }
                                }

                                if (provider != null) break;
                            }
                            catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Failed to load assembly for 1Password fallback resolution", ex); }
                        }
                    }
                }
                catch (Exception ex)
                { VaultGuard.Services.Logging.AppLogger.Error($"1Password provider fallback resolution failed", ex); }
            }

            // Perform import
            using var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            var fileName = System.IO.Path.GetFileName(filePath);

            ImportProgressText.Text = "Processing items...";

            // Determine user ID(s) based on selection
            var userSelection = ImportUserModeComboBox?.SelectedItem as ComboBoxItem;
            var userSelectionTag = userSelection?.Tag?.ToString() ?? "current";

            List<string?> targetUserIds = new();

            if (userSelectionTag == "current")
            {
                // Prefer the already-authenticated user from the page-level auth service (no DB hit).
                // Fall back to the scoped auth service only if needed.
                try
                {
                    string? currentUserId = _authService?.CurrentUser?.Id;
                    if (string.IsNullOrEmpty(currentUserId))
                    {
                        var scopedAuth = importScope.ServiceProvider.GetService<VaultGuard.Services.Interfaces.IAuthService>();
                        currentUserId = scopedAuth != null ? await scopedAuth.GetCurrentUserIdAsync() : null;
                    }
                    targetUserIds.Add(currentUserId);
                }
                catch (Exception ex)
                {
                    await _logger.LogErrorAsync("SettingsPage", "Failed to get current user ID for import", ex);
                    targetUserIds.Add(_authService?.CurrentUser?.Id); // best-effort fallback
                }
            }
            else if (userSelectionTag == "multiple")
            {
                // Import for multiple selected users
                foreach (var item in UserSelectionListView.Items)
                {
                    if (item is CheckBox checkBox && checkBox.IsChecked == true)
                    {
                        targetUserIds.Add(checkBox.Tag?.ToString());
                    }
                }

                if (targetUserIds.Count == 0)
                {
                    ImportProgressRing.IsActive = false;
                    ImportProgressPanel.Visibility = Visibility.Collapsed;
                    ImportStatusText.Text = "✗ Import failed";
                    ImportResultText.Text = "Please select at least one user";
                    StartImportButton.IsEnabled = true;
                    return;
                }
            }
            else // "all"
            {
                // If userSelectionTag == "all", targetUserId remains null, which will make items accessible to all users
                targetUserIds.Add(null);
            }

            // Perform import for each target user
            int totalSuccessful = 0;
            int totalFailed = 0;
            int totalProcessed = 0;
            var collectedErrors = new List<string>();
            var collectedWarnings = new List<string>();

            for (int i = 0; i < targetUserIds.Count; i++)
            {
                var targetUserId = targetUserIds[i];

                // Reset stream position for each import
                fileStream.Position = 0;

                ImportProgressText.Text = $"Processing items for user {i + 1} of {targetUserIds.Count}...";

                // Live percentage on the progress bar.
                ImportProgressBar.IsIndeterminate = false;
                var progress = new Progress<int>(p =>
                {
                    ImportProgressBar.Value = p;
                    ImportProgressText.Text = p >= 100 ? "Finishing up… 100%" : $"Importing… {p}%";
                });
                var result = await importService.ImportPasswordsAsync(providerName, fileStream, fileName, targetUserId, progress);

                if (result.Success)
                {
                    totalSuccessful += result.SuccessfulImports;
                    totalFailed += result.FailedImports;
                    totalProcessed += result.TotalItemsProcessed;
                    // Capture per-item failures for display
                    collectedWarnings.AddRange(result.Warnings);
                }
                else
                {
                    var errMsg = result.ErrorMessage ?? "Unknown error";
                    await _logger.LogErrorAsync("SettingsPage", $"Import failed for user {targetUserId}: {errMsg}", null);
                    collectedErrors.Add(errMsg);
                    SentrySdk.CaptureMessage($"Import failed [{providerName}]: {errMsg}", SentryLevel.Error);
                }
            }

            // Show combined result — leave the bar at a visible 100%.
            ImportProgressRing.IsActive = false;
            ImportProgressBar.IsIndeterminate = false;
            ImportProgressBar.Value = 100;
            ImportProgressText.Text = "100% — import complete";
            ImportProgressPanel.Visibility = Visibility.Visible;

            if (totalSuccessful > 0 || totalProcessed > 0)
            {
                ImportStatusText.Text = "✓ Import completed successfully!";
                var summary = targetUserIds.Count > 1
                    ? $"Imported for {targetUserIds.Count} user(s): {totalSuccessful} items, {totalFailed} failed, {totalProcessed} processed."
                    : $"Imported: {totalSuccessful} items, Failed: {totalFailed}, Processed: {totalProcessed}.";
                if (collectedWarnings.Count > 0)
                    summary += $"\n\nPer-item errors ({collectedWarnings.Count}):\n" + string.Join("\n", collectedWarnings.Take(10));
                if (collectedWarnings.Count > 10)
                    summary += $"\n…and {collectedWarnings.Count - 10} more (see log for details)";
                ImportResultText.Text = summary;
            }
            else
            {
                ImportStatusText.Text = "✗ Import failed";
                if (collectedErrors.Count > 0)
                    ImportResultText.Text = string.Join("\n\n", collectedErrors);
                else if (collectedWarnings.Count > 0)
                    ImportResultText.Text = $"0 items saved. Errors:\n{string.Join("\n", collectedWarnings.Take(10))}";
                else
                    ImportResultText.Text = "No items were successfully imported. Check the log file for details.";
            }
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            ImportProgressRing.IsActive = false;
            ImportProgressPanel.Visibility = Visibility.Collapsed;
            ImportStatusText.Text = "✗ Import failed";
            var msg = ex.InnerException != null ? $"{ex.Message}\nCause: {ex.InnerException.Message}" : ex.Message;
            ImportResultText.Text = $"Error: {msg}";
            await _logger.LogErrorAsync("SettingsPage", "Unhandled exception during import", ex);
        }
        finally
        {
            StartImportButton.IsEnabled = true;
        }
    }

    private void ClearImportButton_Click(object sender, RoutedEventArgs e)
    {
        ImportFilePathTextBox.Text = string.Empty;
        ImportTypeComboBox.SelectedIndex = -1;
        ImportStatusBorder.Visibility = Visibility.Collapsed;
        ImportProgressPanel.Visibility = Visibility.Collapsed;
        ImportProgressBar.Value = 0;
        ImportProgressBar.IsIndeterminate = false;
        ImportStatusText.Text = "Ready to import";
        ImportResultText.Text = "";
        ImportProgressText.Text = "";
        UpdateImportButtonState();
    }

    private void UpdateImportButtonState()
    {
        StartImportButton.IsEnabled =
            ImportTypeComboBox.SelectedIndex >= 0 &&
            !string.IsNullOrEmpty(ImportFilePathTextBox.Text) &&
            System.IO.File.Exists(ImportFilePathTextBox.Text);
    }

    private async System.Threading.Tasks.Task ShowErrorDialog(string title, string message)
    {
        var dialog = new ModernWpf.Controls.ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            // WPF: XamlRoot not needed
        };
        await dialog.ShowAsync();
    }

    private async void ChooseExportFolderButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select Export Folder",
                ShowNewFolderButton = true
            };

            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK && _viewModel != null)
            {
                _viewModel.ExportPath = folderDialog.SelectedPath;

                VaultGuard.WPF.Services.ToastService.Instance.Show($"Export folder: {folderDialog.SelectedPath}", VaultGuard.WPF.Services.ToastType.Info);
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialog($"Failed to open folder picker: {ex.Message}");
        }
    }

    private async void ConfigureDatabaseButton_Click(object sender, RoutedEventArgs e)
    {
        // Create database configuration dialog
        var connectionStringBox = new TextBox
        {
            // PlaceholderText = "Enter database connection string...",
            TextWrapping = System.Windows.TextWrapping.Wrap,
            AcceptsReturn = true,
            MinHeight = 80,
            Text = _viewModel?.DatabaseConnectionString ?? ""
        };

        var providerComboBox = new ComboBox
        {
            MinWidth = 200,
            ItemsSource = new string[] { "SqlServer", "MySQL", "PostgreSQL", "SQLite" },
            SelectedItem = _viewModel?.DatabaseProvider ?? "SQLite"
        };
        var connectionLabel = new TextBlock { Text = "Connection String", Margin = new System.Windows.Thickness(0, 8, 0, 2) };
        var providerLabel = new TextBlock { Text = "Database Provider", Margin = new System.Windows.Thickness(0, 0, 0, 2) };

        var testButton = new Button
        {
            Content = "Test Connection",
            Margin = new System.Windows.Thickness(0, 8, 0, 0)
        };

        var stackPanel = new StackPanel { };
        stackPanel.Children.Add(providerLabel);
        stackPanel.Children.Add(providerComboBox);
        stackPanel.Children.Add(connectionLabel);
        stackPanel.Children.Add(connectionStringBox);
        stackPanel.Children.Add(testButton);

        var dialog = new ModernWpf.Controls.ContentDialog
        {
            Title = "Database Configuration",
            Content = stackPanel,
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            // WPF: XamlRoot not needed
        };

        // Handle test connection button
        testButton.Click += async (s, e) =>
        {
            try
            {
                var testDialog = new ModernWpf.Controls.ContentDialog
                {
                    Title = "Connection Test",
                    Content = "Connection test functionality would be implemented here.\nFor now, simulating successful connection.",
                    CloseButtonText = "OK",
                    // WPF: XamlRoot not needed
                };
                await testDialog.ShowAsync();
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"Connection test failed: {ex.Message}");
            }
        };

        var result = await dialog.ShowAsync();
        if (result == ModernWpf.Controls.ContentDialogResult.Primary && _viewModel != null)
        {
            // Save database configuration
            _viewModel.DatabaseProvider = providerComboBox.SelectedItem?.ToString() ?? "SQLite";
            _viewModel.DatabaseConnectionString = connectionStringBox.Text;

            var success = await _viewModel.SaveSettingsAsync();

            var resultDialog = new ModernWpf.Controls.ContentDialog
            {
                Title = success ? "Success" : "Error",
                Content = success ? "Database configuration saved successfully." : "Failed to save database configuration.",
                CloseButtonText = "OK",
                // WPF: XamlRoot not needed
            };

            await resultDialog.ShowAsync();
        }
    }

    private async void SeedDataButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var confirmDialog = new ModernWpf.Controls.ContentDialog
            {
                Title = "Seed Sample Data",
                Content = "This will seed categories, collections, tags, and sample password items for your account.\n\nExisting data is not duplicated.\n\nContinue?",
                PrimaryButtonText = "Yes, Seed Data",
                CloseButtonText = "Cancel",
                DefaultButton = ModernWpf.Controls.ContentDialogButton.Primary,
            };

            var result = await confirmDialog.ShowAsync();
            if (result != ModernWpf.Controls.ContentDialogResult.Primary || _serviceProvider == null)
                return;

            var progressDialog = new ModernWpf.Controls.ContentDialog
            {
                Title = "Seeding Data",
                Content = new ModernWpf.Controls.ProgressRing { IsActive = true, Width = 50, Height = 50 },
            };
            var progressTask = progressDialog.ShowAsync();

            try
            {
                // Seed for the currently logged-in user so items appear in the lists
                string? currentUserId = null;
                if (_authService != null)
                {
                    currentUserId = _authService.CurrentUser?.Id ?? await _authService.GetCurrentUserIdAsync();
                }

                var scopeFactory = _serviceProvider.GetService<IServiceScopeFactory>();
                if (scopeFactory != null && !string.IsNullOrEmpty(currentUserId))
                {
                    await System.Threading.Tasks.Task.Run(() =>
                    {
                        using var scope = scopeFactory.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<VaultGuardDbContext>();
                        TestDataSeeder.SeedTestData(db, currentUserId);
                    });
                    await _logger.LogAsync("SettingsPage", $"Seeded data for user {currentUserId}");
                }
                else
                {
                    // Fallback: run general database initialisation (seeds for test user)
                    var startupService = _serviceProvider.GetService<IAppStartupService>();
                    if (startupService != null)
                        await startupService.InitializeDatabaseAsync();
                }

                progressDialog.Hide();

                VaultGuard.WPF.Services.ToastService.Instance.Show("Sample data seeded — categories, collections, tags and items added.", VaultGuard.WPF.Services.ToastType.Success, "Seed Complete");
            }
            catch (Exception ex)
            {
                progressDialog.Hide();
                await _logger.LogErrorAsync("SettingsPage", "Error seeding data", ex);
                await ShowErrorDialog($"Failed to seed data: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("SettingsPage", "Error in SeedDataButton_Click", ex);
            await ShowErrorDialog($"An error occurred: {ex.Message}");
        }
    }

    private async void ClearSeedDataButton_Click(object sender, RoutedEventArgs e) => await RunClearSeedDataAsync();

    private async void DeleteSeedDataButton_Click(object sender, RoutedEventArgs e) => await RunClearSeedDataAsync();

    // Removes the demo/seed data for the current user and the built-in test user. Password items are
    // always deleted; when "Keep categories" is ticked the categories, collections and tags survive.
    // User accounts are NEVER touched.
    private async System.Threading.Tasks.Task RunClearSeedDataAsync()
    {
        try
        {
            // Honour the "keep categories" option (only present in the Maintenance panel). Defaults to a
            // full seed wipe when the checkbox isn't shown (e.g. the Storage-tab "Clear Seed Data" button).
            var keepCategories = KeepCategoriesCheckBox?.IsChecked == true;

            var confirmDialog = new ModernWpf.Controls.ContentDialog
            {
                Title = "Delete Seed Data",
                Content = keepCategories
                    ? "This will permanently remove all password items (including the built-in demo data).\n\nYour categories, collections, tags and user accounts are kept. This cannot be undone. Continue?"
                    : "This will permanently remove all categories, collections, tags, and password items (including the built-in demo data).\n\nYour user accounts are kept. This cannot be undone. Continue?",
                PrimaryButtonText = "Yes, Delete Seed Data",
                CloseButtonText = "Cancel",
                DefaultButton = ModernWpf.Controls.ContentDialogButton.Close,
            };

            var result = await confirmDialog.ShowAsync();
            if (result != ModernWpf.Controls.ContentDialogResult.Primary || _serviceProvider == null)
                return;

            // STEP 1 — delete every password item through the *same* service the items page uses. This
            // is guaranteed to remove exactly what the user sees and correctly handles the child item
            // rows (login/card/note/wifi/passkey), which the old EF RemoveRange path failed on.
            var itemsDeleted = 0;
            var scopeFactory = _serviceProvider.GetService<IServiceScopeFactory>();
            if (scopeFactory != null)
            {
                using var scope = scopeFactory.CreateScope();
                var itemService = scope.ServiceProvider.GetService<VaultGuard.Services.Interfaces.IPasswordItemService>();
                if (itemService != null)
                {
                    var items = (await itemService.GetAllAsync()).ToList();
                    foreach (var item in items)
                    {
                        try { await itemService.DeleteAsync(item.Id); itemsDeleted++; }
                        catch (Exception delEx) { await _logger.LogErrorAsync("SettingsPage", $"Failed to delete item {item.Id}", delEx); }
                    }
                }
            }

            // STEP 2 — unless the user opted to keep categories, clear categories, collections, tags and
            // any leftover rows (FK constraints off). Either way, mark the database as seeded so startup
            // never re-adds the demo data.
            VaultGuard.Services.Interfaces.DatabaseResetResult? reset = null;
            if (!keepCategories)
            {
                var resetService = _serviceProvider.GetService<VaultGuard.Services.Interfaces.IDatabaseResetService>();
                if (resetService != null)
                    reset = await resetService.ResetDataTablesAsync();
            }

            try
            {
                if (scopeFactory != null)
                {
                    await System.Threading.Tasks.Task.Run(() =>
                    {
                        using var scope = scopeFactory.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<VaultGuardDbContext>();
                        TestDataSeeder.ClearSeedData(db, TestDataSeeder.TestUserId, keepCategories);
                        TryMarkSeedComplete(db);
                    });
                }
            }
            catch (Exception inner)
            {
                await _logger.LogErrorAsync("SettingsPage", "Per-user seed cleanup failed (items already deleted)", inner);
            }

            await _logger.LogAsync("SettingsPage", $"Delete seed data: {itemsDeleted} items deleted; keepCategories={keepCategories}; reset: {reset?.Message}");

            // Tell the live items / dashboard views to reload so the cleared data disappears immediately.
            VaultGuard.WPF.Services.AppEvents.RaiseVaultDataChanged();

            VaultGuard.WPF.Services.ToastService.Instance.Success(
                keepCategories
                    ? $"Removed {itemsDeleted} item(s). Your categories, collections, tags and accounts were kept."
                    : $"Removed {itemsDeleted} item(s) plus their categories, collections & tags. Your accounts were kept.",
                "Seed data deleted");
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("SettingsPage", "Error clearing seed data", ex);
            await ShowErrorDialog($"Failed to clear seed data: {ex.Message}");
        }
    }

    // Writes a "<db>.seeded" marker next to the SQLite database (matching AppStartupService) so the
    // startup demo-data seeder treats the now-empty vault as intentionally cleared, not brand new.
    private static void TryMarkSeedComplete(VaultGuardDbContext db)
    {
        try
        {
            var providerName = db.Database.ProviderName ?? string.Empty;
            if (!providerName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
                return;

            var connectionString = db.Database.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString)) return;

            foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("DataSource=", StringComparison.OrdinalIgnoreCase))
                {
                    var path = trimmed[(trimmed.IndexOf('=') + 1)..].Trim();
                    if (string.IsNullOrEmpty(path) || path.Equals(":memory:", StringComparison.OrdinalIgnoreCase))
                        return;

                    var marker = path + ".seeded";
                    if (!File.Exists(marker))
                        File.WriteAllText(marker, DateTime.UtcNow.ToString("o"));
                    return;
                }
            }
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Failed to write seed marker", ex); }
    }

    private async void ClearDataButton_Click(object sender, RoutedEventArgs e)
    {
        var confirmDialog = new ModernWpf.Controls.ContentDialog
        {
            Title = "⚠️ Clear All Data",
            Content = "Are you absolutely sure you want to delete ALL your passwords and settings? This action cannot be undone.",
            PrimaryButtonText = "Yes, Delete Everything",
            CloseButtonText = "Cancel",
            DefaultButton = ModernWpf.Controls.ContentDialogButton.Close,
            // WPF: XamlRoot not needed
        };

        var result = await confirmDialog.ShowAsync();
        if (result == ModernWpf.Controls.ContentDialogResult.Primary && _viewModel != null)
        {
            var success = await _viewModel.ClearAllDataAsync();

            if (success)
                VaultGuard.WPF.Services.ToastService.Instance.Success("All data has been cleared.", "Done");
            else
                VaultGuard.WPF.Services.ToastService.Instance.Error("Failed to clear data. Please try again.");

            if (success)
            {
                // Navigate back to login
                (App.Current as App)?.MainWindow?.HandleLogout();
            }
        }
    }

    private async Task ShowChangePasswordDialog()
    {
        var pwStyle = Application.Current.Resources["ModernPasswordBoxStyle"] as Style;
        var tbStyle = Application.Current.Resources["ModernTextBoxStyle"] as Style;
        var labelBrush = (System.Windows.Media.Brush?)Application.Current.Resources["ModernTextSecondaryBrush"]
                         ?? System.Windows.Media.Brushes.Gray;

        TextBlock MakeLabel(string text, double topMargin = 12) => new()
        {
            Text = text, FontSize = 12, FontWeight = System.Windows.FontWeights.SemiBold,
            Foreground = labelBrush, Margin = new System.Windows.Thickness(0, topMargin, 0, 4)
        };

        PasswordBox MakePw(string placeholder)
        {
            var box = new PasswordBox { Style = pwStyle };
            ModernWpf.Controls.Primitives.ControlHelper.SetPlaceholderText(box, placeholder);
            return box;
        }

        var currentPasswordBox = MakePw("Enter your current master password");
        var newPasswordBox = MakePw("Choose a strong new password");
        var confirmPasswordBox = MakePw("Re-enter the new password");
        var passwordHintBox = new TextBox { Style = tbStyle };
        ModernWpf.Controls.Primitives.ControlHelper.SetPlaceholderText(passwordHintBox, "Optional reminder — never store the password itself");

        // Live strength meter under the new-password field.
        var strengthTrack = new Border
        {
            Height = 6, Width = 360, CornerRadius = new System.Windows.CornerRadius(3),
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x33, 0x33, 0x33)),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
            Margin = new System.Windows.Thickness(0, 8, 0, 0)
        };
        var strengthFill = new Border
        {
            Height = 6, Width = 0, CornerRadius = new System.Windows.CornerRadius(3),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left
        };
        strengthTrack.Child = strengthFill;
        var strengthLabel = new TextBlock { FontSize = 11, Margin = new System.Windows.Thickness(0, 4, 0, 0), Foreground = labelBrush };

        newPasswordBox.PasswordChanged += (_, _) =>
        {
            var (score, text, color) = EstimatePasswordStrength(newPasswordBox.Password);
            strengthFill.Width = 360 * score / 4.0;
            strengthFill.Background = new System.Windows.Media.SolidColorBrush(color);
            strengthLabel.Text = string.IsNullOrEmpty(newPasswordBox.Password) ? "" : $"Strength: {text}";
            strengthLabel.Foreground = string.IsNullOrEmpty(newPasswordBox.Password)
                ? labelBrush : new System.Windows.Media.SolidColorBrush(color);
        };

        var errorTextBlock = new TextBlock
        {
            Foreground = (System.Windows.Media.Brush?)Application.Current.Resources["ModernErrorBrush"]
                         ?? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red),
            TextWrapping = System.Windows.TextWrapping.Wrap,
            FontSize = 12,
            Margin = new System.Windows.Thickness(0, 12, 0, 0),
            Visibility = System.Windows.Visibility.Collapsed
        };

        var stackPanel = new StackPanel { MinWidth = 380, Margin = new System.Windows.Thickness(4) };
        stackPanel.Children.Add(MakeLabel("Current master password", 0));
        stackPanel.Children.Add(currentPasswordBox);
        stackPanel.Children.Add(MakeLabel("New master password"));
        stackPanel.Children.Add(newPasswordBox);
        stackPanel.Children.Add(strengthTrack);
        stackPanel.Children.Add(strengthLabel);
        stackPanel.Children.Add(MakeLabel("Confirm new password"));
        stackPanel.Children.Add(confirmPasswordBox);
        stackPanel.Children.Add(MakeLabel("Password hint (optional)"));
        stackPanel.Children.Add(passwordHintBox);
        stackPanel.Children.Add(errorTextBlock);

        var dialog = new ModernWpf.Controls.ContentDialog
        {
            Title = "Change Master Password",
            Content = stackPanel,
            PrimaryButtonText = "Change Password",
            CloseButtonText = "Cancel",
            DefaultButton = ModernWpf.Controls.ContentDialogButton.Primary
        };
        dialog.Style = dialog.TryFindResource("Modern1PasswordDialogStyle") as Style;

        // Validate (and perform the change) inside the primary-button handler with a deferral so the
        // dialog stays open and shows inline errors instead of closing on every mistake.
        dialog.PrimaryButtonClick += async (_, args) =>
        {
            var deferral = args.GetDeferral();
            try
            {
                void Fail(string msg)
                {
                    errorTextBlock.Text = msg;
                    errorTextBlock.Visibility = System.Windows.Visibility.Visible;
                    args.Cancel = true;
                }

                if (string.IsNullOrEmpty(currentPasswordBox.Password)) { Fail("Enter your current master password."); return; }
                if (string.IsNullOrEmpty(newPasswordBox.Password)) { Fail("Enter a new master password."); return; }
                if (newPasswordBox.Password != confirmPasswordBox.Password) { Fail("The new passwords don't match."); return; }

                var validation = ValidatePasswordStrength(newPasswordBox.Password);
                if (!validation.IsValid) { Fail(validation.ErrorMessage); return; }

                if (_authService == null) { Fail("Authentication service unavailable."); return; }

                if (_serviceProvider != null &&
                    !await Helpers.SecurityGateHelper.RequireCodeForActionAsync(
                        _serviceProvider, Helpers.SecurityGateHelper.GateAction.MasterPasswordChange))
                {
                    args.Cancel = true;
                    return;
                }

                var ok = await _authService.ChangeMasterPasswordAsync(
                    currentPasswordBox.Password, newPasswordBox.Password, passwordHintBox.Text);

                if (!ok) { Fail("Couldn't change the password — check your current password and try again."); return; }
            }
            finally { deferral.Complete(); }
        };

        var result = await Helpers.DialogManager.ShowAsync(dialog);
        if (result == ModernWpf.Controls.ContentDialogResult.Primary)
        {
            VaultGuard.WPF.Services.ToastService.Instance.Show(
                "Master password changed. You'll use the new password next time the app starts.",
                VaultGuard.WPF.Services.ToastType.Success, "Done");
        }
    }

    // Lightweight local strength estimate for the change-password meter (0-4 + label + colour).
    private static (int Score, string Text, System.Windows.Media.Color Color) EstimatePasswordStrength(string pw)
    {
        if (string.IsNullOrEmpty(pw)) return (0, "", System.Windows.Media.Colors.Gray);
        int score = 0;
        if (pw.Length >= 8) score++;
        if (pw.Length >= 12) score++;
        if (pw.Any(char.IsUpper) && pw.Any(char.IsLower)) score++;
        if (pw.Any(char.IsDigit) && pw.Any(c => !char.IsLetterOrDigit(c))) score++;
        score = System.Math.Min(score, 4);
        return score switch
        {
            <= 1 => (1, "Weak", System.Windows.Media.Color.FromRgb(0xEF, 0x44, 0x44)),
            2 => (2, "Fair", System.Windows.Media.Color.FromRgb(0xF5, 0x9E, 0x0B)),
            3 => (3, "Good", System.Windows.Media.Color.FromRgb(0x3B, 0x82, 0xF6)),
            _ => (4, "Strong", System.Windows.Media.Color.FromRgb(0x10, 0xB9, 0x81)),
        };
    }

    private (bool IsValid, string ErrorMessage) ValidatePasswordStrength(string password)
    {
        if (password.Length < 8)
        {
            return (false, "Master password must be at least 8 characters long.");
        }

        if (!password.Any(char.IsUpper))
        {
            return (false, "Master password must contain at least one uppercase letter.");
        }

        if (!password.Any(char.IsLower))
        {
            return (false, "Master password must contain at least one lowercase letter.");
        }

        if (!password.Any(char.IsDigit))
        {
            return (false, "Master password must contain at least one number.");
        }

        return (true, string.Empty);
    }

    private Task ShowErrorDialog(string message)
    {
        VaultGuard.WPF.Services.ToastService.Instance.Show(message, VaultGuard.WPF.Services.ToastType.Error, "Error");
        return Task.CompletedTask;
    }

    // ─── Google Drive handlers ────────────────────────────────────────────────

    private async void ConnectGoogleDriveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;
        if (string.IsNullOrWhiteSpace(_viewModel.GoogleDriveClientId) ||
            string.IsNullOrWhiteSpace(_viewModel.GoogleDriveClientSecret))
        {
            await ShowErrorDialog("Please enter your Google Drive OAuth Client ID and Client Secret first.\n\nCreate them at console.cloud.google.com → APIs & Services → Credentials.");
            return;
        }

        _viewModel.IsLoading = true;
        var ok = await _viewModel.ConnectGoogleDriveAsync();
        if (ok)
            VaultGuard.WPF.Services.ToastService.Instance.Show("Connected to Google Drive!", VaultGuard.WPF.Services.ToastType.Success);
        else
            await ShowErrorDialog("Google Drive connection failed. Make sure your Client ID and Secret are correct and that you allowed the authorisation in the browser.");
    }

    private async void DisconnectGoogleDriveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;
        await _viewModel.DisconnectGoogleDriveAsync();
        VaultGuard.WPF.Services.ToastService.Instance.Show("Disconnected from Google Drive.", VaultGuard.WPF.Services.ToastType.Info);
    }

    private void GDriveClientSecretBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null && sender is PasswordBox pb)
            _viewModel.GoogleDriveClientSecret = pb.Password;
    }

    // ─── OneDrive handlers ──────────────────────────────────────────────────────

    private async void ConnectOneDriveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;
        _viewModel.IsLoading = true;
        var ok = await _viewModel.ConnectOneDriveAsync();
        if (ok)
            VaultGuard.WPF.Services.ToastService.Instance.Show("Signed in with Microsoft!", VaultGuard.WPF.Services.ToastType.Success);
        else
            await ShowErrorDialog($"OneDrive sign-in failed.\n\n{_viewModel.LastOneDriveError ?? "Unknown error — check the app log for details."}");
    }

    private async void DisconnectOneDriveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;
        await _viewModel.DisconnectOneDriveAsync();
        VaultGuard.WPF.Services.ToastService.Instance.Show("Signed out of OneDrive.", VaultGuard.WPF.Services.ToastType.Info);
    }

    // ─── FTP / NAS handlers ─────────────────────────────────────────────────────

    private void FtpPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null && sender is PasswordBox pb)
            _viewModel.FtpPassword = pb.Password;
    }

    private async void TestFtpConnectionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;
        if (string.IsNullOrWhiteSpace(_viewModel.FtpHost))
        {
            await ShowErrorDialog("Please enter the FTP host first.");
            return;
        }

        var ok = await _viewModel.TestFtpConnectionAsync();
        if (ok)
            VaultGuard.WPF.Services.ToastService.Instance.Show("FTP connection successful!", VaultGuard.WPF.Services.ToastType.Success);
        else
            VaultGuard.WPF.Services.ToastService.Instance.Show(_viewModel.FtpStatus, VaultGuard.WPF.Services.ToastType.Error);
    }

    // ─── NAS / network-drive handlers ───────────────────────────────────────────

    private void NasPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null && sender is PasswordBox pb)
            _viewModel.NasPassword = pb.Password;
    }

    private void MappedDriveComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel != null && sender is ComboBox cb &&
            cb.SelectedItem is VaultGuard.Models.DTOs.MappedDriveInfo drive)
        {
            // Prefer the UNC path (works without the drive being mapped on the target machine);
            // fall back to the drive letter root.
            _viewModel.NetworkPath = string.IsNullOrEmpty(drive.UncPath) ? drive.Root : drive.UncPath;
        }
    }

    private void RefreshMappedDrivesButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel?.RefreshMappedDrives();
    }

    private async void TestNasConnectionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;
        var ok = await _viewModel.TestNasConnectionAsync();
        if (ok)
            VaultGuard.WPF.Services.ToastService.Instance.Show("Network location reachable!", VaultGuard.WPF.Services.ToastType.Success);
        else
            VaultGuard.WPF.Services.ToastService.Instance.Show(_viewModel.NasStatus, VaultGuard.WPF.Services.ToastType.Error);
    }

    // Cloud Backup Event Handlers
    private async void CloudBackupToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            await _viewModel.SaveSettingsAsync();
            if (_viewModel.EnableCloudBackup)
            {
                await _viewModel.LoadAvailableBackupsAsync();
            }
        }
    }

    private void CloudProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // The provider sub-panels' visibility is now driven entirely by data binding
        // (IsGoogleDriveSelected / IsNetworkLocationSelected), so there is nothing to toggle here.
        // Mutating Visibility from this handler ran inside the layout pass and crashed the app.
    }

    // Kept (called once on navigation) but now a no-op — binding handles panel visibility.
    private void UpdateNetworkLocationVisibility() { }

    private async void CreateBackupButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null || _cloudBackupDialogBusy) return;
        _cloudBackupDialogBusy = true;
        try
        {
            // Show password dialog
            var passwordDialog = new ModernWpf.Controls.ContentDialog
            {
                Title = "Create Cloud Backup",
                Content = await CreateMasterPasswordInput(),
                PrimaryButtonText = "Create Backup",
                CloseButtonText = "Cancel",
                // WPF: XamlRoot not needed
            };

            var result = await passwordDialog.ShowAsync();
            if (result != ModernWpf.Controls.ContentDialogResult.Primary) return;

            var panel = passwordDialog.Content as StackPanel;
            var passwordBox = panel?.Children.OfType<PasswordBox>().FirstOrDefault();
            if (passwordBox == null) return;

            if (string.IsNullOrEmpty(passwordBox.Password))
            {
                await new ModernWpf.Controls.ContentDialog
                {
                    Title = "Master password required",
                    Content = "Please enter your master password so the backup can be encrypted.",
                    CloseButtonText = "OK"
                }.ShowAsync();
                return;
            }

            // If 2FA is enabled, require and verify a valid authenticator (or backup) code first.
            if (await IsTwoFactorEnabledAsync())
            {
                var codeBox = panel?.Children.OfType<TextBox>().FirstOrDefault();
                var code = codeBox?.Text?.Trim() ?? string.Empty;
                if (!await VerifyBackupTwoFactorAsync(code))
                {
                    await new ModernWpf.Controls.ContentDialog
                    {
                        Title = "Verification failed",
                        Content = "That two-factor code wasn't valid. Please check your authenticator app and try again.",
                        CloseButtonText = "OK"
                    }.ShowAsync();
                    return;
                }
            }

            var success = await _viewModel.CreateCloudBackupAsync(passwordBox.Password);

            var message = success ? "Backup created successfully!" : "Backup creation failed. Please try again.";
            var dialog = new VaultGuard.WPF.Dialogs.BackupResultDialog(
                success,
                message,
                _viewModel.LastBackupException,
                _viewModel.LastBackupErrorMessage);

            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            await ReportBackupErrorAsync("CreateCloudBackup", ex);
        }
        finally
        {
            _cloudBackupDialogBusy = false;
        }
    }

    private async void RefreshBackupsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;
        try
        {
            await _viewModel.LoadAvailableBackupsAsync();
        }
        catch (Exception ex)
        {
            await ReportBackupErrorAsync("RefreshBackups", ex);
        }
    }

    private async void ChooseNetworkLocationButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            await _viewModel.ChooseNetworkLocationAsync();
        }
    }

    private async void RestoreFromFileButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null || _cloudBackupDialogBusy) return;
        _cloudBackupDialogBusy = true;
        try
        {
            // 1. Pick the backup file.
            var filePicker = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Backup File",
                Filter = "VaultGuard backups (*.pwmbackup)|*.pwmbackup|All files (*.*)|*.*",
                FilterIndex = 1,
                CheckFileExists = true,
                CheckPathExists = true
            };
            if (filePicker.ShowDialog() != true) return;
            var filePath = filePicker.FileName;

            // 2. Confirm — restoring replaces everything — and collect the master password (+2FA if on).
            var warningPanel = new StackPanel { MinWidth = 360 };
            var warningBorder = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(30, 220, 50, 50)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(100, 220, 50, 50)),
                BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10), Margin = new Thickness(0, 0, 0, 14)
            };
            warningBorder.Child = new TextBlock
            {
                Text = "This will permanently remove all your current passwords, notes, credit cards, and Wi-Fi entries and replace them with the backup.\n\nThis cannot be undone.",
                TextWrapping = TextWrapping.Wrap, FontSize = 13, LineHeight = 20
            };
            warningPanel.Children.Add(warningBorder);
            warningPanel.Children.Add(new TextBlock { Text = $"File:  {System.IO.Path.GetFileName(filePath)}", FontSize = 12, Opacity = 0.7, Margin = new Thickness(0, 0, 0, 12) });

            warningPanel.Children.Add(new TextBlock { Text = "Master password", FontSize = 12, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
            var pwBox = new PasswordBox();
            ModernWpf.Controls.Primitives.ControlHelper.SetPlaceholderText(pwBox, "Enter the backup's master password");
            warningPanel.Children.Add(pwBox);

            var twoFaEnabled = await IsTwoFactorEnabledAsync();
            if (twoFaEnabled)
            {
                warningPanel.Children.Add(new TextBlock { Text = "Two-factor code", FontSize = 12, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 16, 0, 6) });
                var codeBox = new TextBox { MaxLength = 8 };
                ModernWpf.Controls.Primitives.ControlHelper.SetPlaceholderText(codeBox, "6-digit code");
                warningPanel.Children.Add(codeBox);
            }

            var confirmDialog = new ModernWpf.Controls.ContentDialog
            {
                Title = "Restore from File",
                Content = warningPanel,
                PrimaryButtonText = "Restore All",
                CloseButtonText = "Cancel",
            };
            if (await confirmDialog.ShowAsync() != ModernWpf.Controls.ContentDialogResult.Primary) return;

            if (string.IsNullOrEmpty(pwBox.Password))
            {
                await new ModernWpf.Controls.ContentDialog
                {
                    Title = "Master password required",
                    Content = "Please enter the master password the backup was encrypted with.",
                    CloseButtonText = "OK"
                }.ShowAsync();
                return;
            }

            // 3. Verify 2FA when enabled.
            if (twoFaEnabled)
            {
                var code = warningPanel.Children.OfType<TextBox>().FirstOrDefault()?.Text?.Trim() ?? string.Empty;
                if (!await VerifyBackupTwoFactorAsync(code))
                {
                    await new ModernWpf.Controls.ContentDialog
                    {
                        Title = "Verification failed",
                        Content = "That two-factor code wasn't valid. Please check your authenticator app and try again.",
                        CloseButtonText = "OK"
                    }.ShowAsync();
                    return;
                }
            }

            // 4. Restore.
            var success = await _viewModel.RestoreFromFileAsync(filePath, pwBox.Password);

            var message = success ?
                "Database restored successfully from file. Please restart the app." :
                "Failed to restore database from file. Please check the file and master password.";

            await new ModernWpf.Controls.ContentDialog
            {
                Title = success ? "Restored" : "Error",
                Content = message,
                CloseButtonText = "OK",
            }.ShowAsync();
        }
        catch (Exception ex)
        {
            await ReportBackupErrorAsync("RestoreFromFile", ex);
        }
        finally
        {
            _cloudBackupDialogBusy = false;
        }
    }

    private async void RestoreBackupButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null || sender is not Button button || button.Tag is not CloudBackupInfo backup) return;
        if (_cloudBackupDialogBusy) return;
        _cloudBackupDialogBusy = true;
        try
        {
        // iPhone-style warning — red destructive action dialog
        var warningPanel = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
        var warningBorder = new Border
        {
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(30, 220, 50, 50)),
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(100, 220, 50, 50)),
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 10, 12, 10), Margin = new Thickness(0, 0, 0, 12)
        };
        var warnStack = new StackPanel { Orientation = Orientation.Horizontal };
        warnStack.Children.Add(new TextBlock
        {
            Text = "", FontFamily = new System.Windows.Media.FontFamily("Segoe MDL2 Assets"),
            FontSize = 16, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 80, 80)),
            Margin = new Thickness(0, 0, 10, 0), VerticalAlignment = VerticalAlignment.Top
        });
        warnStack.Children.Add(new TextBlock
        {
            Text = "This will permanently remove all your current passwords, notes, credit cards, and Wi-Fi entries and replace them with the backup.\n\nThis cannot be undone.",
            TextWrapping = TextWrapping.Wrap, FontSize = 13, LineHeight = 20
        });
        warningBorder.Child = warnStack;
        warningPanel.Children.Add(warningBorder);
        warningPanel.Children.Add(new TextBlock { Text = $"Backup date:  {backup.CreatedAtFormatted}", FontSize = 13, Margin = new Thickness(0, 0, 0, 8) });
        var pwBox = new PasswordBox { Margin = new Thickness(0, 4, 0, 0) };
        warningPanel.Children.Add(new TextBlock { Text = "Master password", FontSize = 12, Opacity = 0.7, Margin = new Thickness(0, 0, 0, 4) });
        warningPanel.Children.Add(pwBox);

        var confirmDialog = new ModernWpf.Controls.ContentDialog
        {
            Title = "Restore Backup",
            Content = warningPanel,
            PrimaryButtonText = "Restore All",
            CloseButtonText = "Cancel",
        };

        if (await confirmDialog.ShowAsync() == ModernWpf.Controls.ContentDialogResult.Primary)
        {
            var success = await _viewModel.RestoreCloudBackupAsync(backup, pwBox.Password);
            var resultDlg = new ModernWpf.Controls.ContentDialog
            {
                Title = success ? "Restored" : "Error",
                Content = success ? "Backup restored successfully. Please restart the app." : "Restore failed — check your master password and try again.",
                CloseButtonText = "OK",
            };
            await resultDlg.ShowAsync();
        }
        }
        catch (Exception ex)
        {
            await ReportBackupErrorAsync("RestoreCloudBackup", ex);
        }
        finally
        {
            _cloudBackupDialogBusy = false;
        }
    }

    private async void BrowseBackupButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null || sender is not Button button || button.Tag is not CloudBackupInfo backup) return;
        if (_cloudBackupDialogBusy) return;
        _cloudBackupDialogBusy = true;
        try
        {
        // Ask for master password first
        var pwBox = new PasswordBox { };
        var pwDialog = new ModernWpf.Controls.ContentDialog
        {
            Title = "Enter Master Password",
            Content = pwBox,
            PrimaryButtonText = "Open Backup",
            CloseButtonText = "Cancel",
        };
        if (await pwDialog.ShowAsync() != ModernWpf.Controls.ContentDialogResult.Primary) return;

        var masterPassword = pwBox.Password;
        if (string.IsNullOrEmpty(masterPassword)) return;

        // Download + decrypt + parse
        var backupService = _serviceProvider?.GetService<IDatabaseBackupService>();
        var cloudManager = _serviceProvider?.GetService<VaultGuard.Services.Services.CloudBackupManager>();
        if (backupService == null || cloudManager == null) return;

        var downloadResult = await cloudManager.DownloadBackupDataAsync(backup);
        if (downloadResult == null)
        {
            await new ModernWpf.Controls.ContentDialog { Title = "Error", Content = "Could not download backup.", CloseButtonText = "OK" }.ShowAsync();
            return;
        }

        var contents = await backupService.BrowseBackupAsync(downloadResult, masterPassword);
        if (contents == null || contents.TotalCount == 0)
        {
            await new ModernWpf.Controls.ContentDialog { Title = "Error", Content = "Could not read backup — check your master password.", CloseButtonText = "OK" }.ShowAsync();
            return;
        }

        // Open browse dialog
        var browseDialog = new VaultGuard.WPF.Dialogs.CloudBackupBrowseDialog(contents);
        if (await browseDialog.ShowAsync() == ModernWpf.Controls.ContentDialogResult.Primary)
        {
            var imported = await backupService.ImportSelectedItemsAsync(contents, browseDialog.SelectedIds);
            await new ModernWpf.Controls.ContentDialog
            {
                Title = "Import Complete",
                Content = $"{imported} item(s) added to your vault.",
                CloseButtonText = "OK"
            }.ShowAsync();
        }
        }
        catch (Exception ex)
        {
            await ReportBackupErrorAsync("BrowseBackup", ex);
        }
        finally
        {
            _cloudBackupDialogBusy = false;
        }
    }

    private async void DeleteBackupButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null || sender is not Button button || button.Tag is not CloudBackupInfo backup) return;
        if (_cloudBackupDialogBusy) return;
        _cloudBackupDialogBusy = true;
        try
        {
            var confirmDialog = new ModernWpf.Controls.ContentDialog
            {
                Title = "Delete Backup",
                Content = $"Are you sure you want to delete the backup '{backup.FileName}'? This action cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                // WPF: XamlRoot not needed
            };

            if (await confirmDialog.ShowAsync() == ModernWpf.Controls.ContentDialogResult.Primary)
            {
                var success = await _viewModel.DeleteCloudBackupAsync(backup);

                if (!success)
                {
                    var errorDialog = new ModernWpf.Controls.ContentDialog
                    {
                        Title = "Error",
                        Content = "Failed to delete backup. Please try again.",
                        CloseButtonText = "OK",
                        // WPF: XamlRoot not needed
                    };

                    await errorDialog.ShowAsync();
                }
            }
        }
        catch (Exception ex)
        {
            await ReportBackupErrorAsync("DeleteBackup", ex);
        }
        finally
        {
            _cloudBackupDialogBusy = false;
        }
    }

    private async void ExportToBrowserButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null && sender is Button button)
        {
            var browserName = button.Tag?.ToString();
            if (!string.IsNullOrEmpty(browserName))
            {
                var result = await _viewModel.ExportToBrowserAsync(browserName);

                var message = result?.Success == true
                    ? $"Successfully exported {result.ExportedCount} passwords to {browserName} format."
                    : $"Export to {browserName} failed: {result?.ErrorMessage}";

                var dialog = new ModernWpf.Controls.ContentDialog
                {
                    Title = result?.Success == true ? "Export Success" : "Export Failed",
                    Content = message,
                    CloseButtonText = "OK",
                    // WPF: XamlRoot not needed
                };

                await dialog.ShowAsync();
            }
        }
    }

    // Returns the wrapping panel (label + PasswordBox) — the PasswordBox is already parented inside
    // it, so it must never be returned/assigned as Content on its own (WPF throws "AddVisualChild" if
    // a Visual already has a parent when you try to give it a second one).
    private async Task<StackPanel> CreateMasterPasswordInput()
    {
        var panel = new StackPanel { MinWidth = 360 };

        panel.Children.Add(new TextBlock
        {
            Text = "Your backup is encrypted with your master password. You'll need the same password to restore it, so keep it safe.",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            Opacity = 0.75,
            Margin = new Thickness(0, 0, 0, 18)
        });

        panel.Children.Add(new TextBlock
        {
            Text = "Master password",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 6)
        });

        var passwordBox = new PasswordBox();
        ModernWpf.Controls.Primitives.ControlHelper.SetPlaceholderText(passwordBox, "Enter your master password");
        panel.Children.Add(passwordBox);

        // Only surface the 2FA field when the account actually has two-factor enabled.
        if (await IsTwoFactorEnabledAsync())
        {
            panel.Children.Add(new TextBlock
            {
                Text = "Two-factor code",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 16, 0, 6)
            });

            var codeBox = new TextBox { MaxLength = 8 };
            ModernWpf.Controls.Primitives.ControlHelper.SetPlaceholderText(codeBox, "6-digit code");
            panel.Children.Add(codeBox);

            panel.Children.Add(new TextBlock
            {
                Text = "Enter the code from your authenticator app, or one of your backup codes.",
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.6,
                Margin = new Thickness(0, 6, 0, 0)
            });
        }

        return panel;
    }

    // Central place to log + Sentry-report a backup-related failure and tell the user about it.
    private async Task ReportBackupErrorAsync(string operation, Exception ex)
    {
        await _logger.LogErrorAsync("SettingsPage", $"Backup operation '{operation}' failed", ex);
        _serviceProvider?.GetService<IExceptionReporter>()?.CaptureException(ex,
            new Dictionary<string, string> { ["operation"] = operation });
        try
        {
            VaultGuard.WPF.Services.ToastService.Instance.Show(
                $"Something went wrong: {ex.Message}", VaultGuard.WPF.Services.ToastType.Error);
        }
        catch (Exception toastEx) { VaultGuard.Services.Logging.AppLogger.Debug($"Failed to show backup error toast: {toastEx.Message}"); }
    }

    // True when the signed-in user has two-factor authentication enabled.
    private async Task<bool> IsTwoFactorEnabledAsync()
    {
        try
        {
            if (_serviceProvider == null) return false;
            var twoFactorService = _serviceProvider.GetService<ITwoFactorService>();
            var userId = _authService?.CurrentUser?.Id ?? await _authService?.GetCurrentUserIdAsync()!;
            if (twoFactorService == null || string.IsNullOrEmpty(userId)) return false;

            var status = await twoFactorService.GetTwoFactorStatusAsync(userId);
            return status.IsEnabled;
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("SettingsPage", "Failed to check two-factor status for backup", ex);
            _serviceProvider?.GetService<IExceptionReporter>()?.CaptureException(ex,
                new Dictionary<string, string> { ["operation"] = "BackupTwoFactorStatus" });
            // Fail safe: if we can't determine 2FA state, don't block the backup behind a code we can't verify.
            return false;
        }
    }

    // Verifies a TOTP / backup code for the current user before a backup is allowed.
    private async Task<bool> VerifyBackupTwoFactorAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        try
        {
            if (_serviceProvider == null) return false;
            var twoFactorService = _serviceProvider.GetService<ITwoFactorService>();
            var userId = _authService?.CurrentUser?.Id ?? await _authService?.GetCurrentUserIdAsync()!;
            if (twoFactorService == null || string.IsNullOrEmpty(userId)) return false;

            // A 6-digit numeric code — including one entered with the "123-456" mask or stray
            // spaces — is a TOTP code; anything else is treated as a backup/recovery code.
            var trimmed = code.Trim();
            var digits = new string(trimmed.Where(char.IsDigit).ToArray());
            var looksLikeTotp = digits.Length == 6 && trimmed.All(c => char.IsDigit(c) || c == '-' || c == ' ');
            return looksLikeTotp
                ? await twoFactorService.VerifyTwoFactorCodeAsync(userId, digits, false)
                : await twoFactorService.VerifyTwoFactorCodeAsync(userId, trimmed, true);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("SettingsPage", "Failed to verify two-factor code for backup", ex);
            _serviceProvider?.GetService<IExceptionReporter>()?.CaptureException(ex,
                new Dictionary<string, string> { ["operation"] = "BackupTwoFactorVerify" });
            return false;
        }
    }

    // Handle changes to the selected database provider (show/hide sqlite path)
    private void DatabaseProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            var combo = sender as ComboBox;
            var selected = combo?.SelectedItem as ComboBoxItem;
            var provider = selected?.Content?.ToString() ?? combo?.SelectedItem?.ToString() ?? string.Empty;

            if (SqlitePathPanel != null)
            {
                SqlitePathPanel.Visibility = string.Equals(provider, "SQLite", StringComparison.OrdinalIgnoreCase)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Failed to update SQLite path panel visibility", ex); }
    }

    private async void SecureWipeButton_Click(object sender, RoutedEventArgs e)
    {
        var confirm = new ModernWpf.Controls.ContentDialog
        {
            Title = "Secure Wipe",
            Content = "This will securely delete your database (SQLite) or clear all tables (SQL Server). This cannot be undone. Continue?",
            PrimaryButtonText = "Yes, Wipe",
            CloseButtonText = "Cancel",
            DefaultButton = ModernWpf.Controls.ContentDialogButton.Close,
            // WPF: XamlRoot not needed
        };

        if (await confirm.ShowAsync() == ModernWpf.Controls.ContentDialogResult.Primary)
        {
            try
            {
                var resetService = _serviceProvider?.GetService<VaultGuard.Services.Interfaces.IDatabaseResetService>();
                if (resetService == null)
                {
                    await ShowErrorDialog("Database reset service is not available.");
                    return;
                }

                var result = await resetService.SecureWipeDatabaseAsync();
                await ShowErrorDialog(result.Success ? "Secure wipe completed." : $"Secure wipe failed: {result.Message}");
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"Secure wipe failed: {ex.Message}");
            }
        }
    }

    private async void ResetDataButton_Click(object sender, RoutedEventArgs e)
    {
        var confirm = new ModernWpf.Controls.ContentDialog
        {
            Title = "Reset Password Data",
            Content = "This will clear all password items, collections, categories and tags but keep your user account. Continue?",
            PrimaryButtonText = "Yes, Reset",
            CloseButtonText = "Cancel",
            DefaultButton = ModernWpf.Controls.ContentDialogButton.Close,
            // WPF: XamlRoot not needed
        };

        if (await confirm.ShowAsync() == ModernWpf.Controls.ContentDialogResult.Primary)
        {
            try
            {
                var resetService = _serviceProvider?.GetService<VaultGuard.Services.Interfaces.IDatabaseResetService>();
                if (resetService == null)
                {
                    await ShowErrorDialog("Database reset service is not available.");
                    return;
                }

                var result = await resetService.ResetDataTablesAsync();
                if (result.Success)
                    VaultGuard.WPF.Services.ToastService.Instance.Show("Password data has been reset.", VaultGuard.WPF.Services.ToastType.Success, "Reset Complete");
                else
                    VaultGuard.WPF.Services.ToastService.Instance.Show($"Reset failed: {result.Message}", VaultGuard.WPF.Services.ToastType.Error, "Reset Failed");
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"Reset failed: {ex.Message}");
            }
        }
    }

    private async void DeleteUsersButton_Click(object sender, RoutedEventArgs e)
    {
        var confirm = new ModernWpf.Controls.ContentDialog
        {
            Title = "Delete User Accounts",
            Content = "This permanently deletes all non-admin user accounts (including the built-in demo/test user) and the data they own. Admin accounts are kept. Continue?",
            PrimaryButtonText = "Yes, Delete Users",
            CloseButtonText = "Cancel",
            DefaultButton = ModernWpf.Controls.ContentDialogButton.Close,
        };

        if (await confirm.ShowAsync() != ModernWpf.Controls.ContentDialogResult.Primary)
            return;

        try
        {
            var resetService = _serviceProvider?.GetService<VaultGuard.Services.Interfaces.IDatabaseResetService>();
            if (resetService == null)
            {
                await ShowErrorDialog("Database reset service is not available.");
                return;
            }

            var result = await resetService.ClearNonAdminUsersAsync();
            if (result.Success)
                VaultGuard.WPF.Services.ToastService.Instance.Success(result.Message, "Users deleted");
            else
                VaultGuard.WPF.Services.ToastService.Instance.Warning(result.Message, "Completed with errors");
        }
        catch (Exception ex)
        {
            VaultGuard.WPF.Services.ToastService.Instance.Error($"Failed to delete users: {ex.Message}", "Error");
        }
    }

    private async void ResetAllButton_Click(object sender, RoutedEventArgs e)
    {
        var confirm = new ModernWpf.Controls.ContentDialog
        {
            Title = "Reset All Database Tables",
            Content = "DANGER: This will clear all database tables including user accounts and log you out. Continue?",
            PrimaryButtonText = "Yes, Reset All",
            CloseButtonText = "Cancel",
            DefaultButton = ModernWpf.Controls.ContentDialogButton.Close,
            // WPF: XamlRoot not needed
        };

        if (await confirm.ShowAsync() == ModernWpf.Controls.ContentDialogResult.Primary)
        {
            try
            {
                var resetService = _serviceProvider?.GetService<VaultGuard.Services.Interfaces.IDatabaseResetService>();
                if (resetService == null)
                {
                    await ShowErrorDialog("Database reset service is not available.");
                    return;
                }

                var result = await resetService.ResetAllTablesAsync(reseedData: false);
                if (result.Success)
                    VaultGuard.WPF.Services.ToastService.Instance.Show("All database tables have been reset.", VaultGuard.WPF.Services.ToastType.Success, "Reset Complete");
                else
                    VaultGuard.WPF.Services.ToastService.Instance.Show($"Reset failed: {result.Message}", VaultGuard.WPF.Services.ToastType.Error, "Reset Failed");

                if (result.Success)
                {
                    (App.Current as App)?.MainWindow?.HandleLogout();
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"Reset failed: {ex.Message}");
            }
        }
    }

    private async void ReseedSampleButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var resetService = _serviceProvider?.GetService<VaultGuard.Services.Interfaces.IDatabaseResetService>();
            if (resetService == null)
            {
                await ShowErrorDialog("Database reset service is not available.");
                return;
            }

            var result = await resetService.ReseedSampleDataAsync();
            var dlg = new ModernWpf.Controls.ContentDialog
            {
                Title = result.Success ? "Reseeded" : "Reseed Failed",
                Content = result.Success ? "Sample data has been reseeded." : $"Failed to reseed sample data: {result.Message}",
                CloseButtonText = "OK",
                // WPF: XamlRoot not needed
            };
            await dlg.ShowAsync();
        }
        catch (Exception ex)
        {
            await ShowErrorDialog($"Reseed failed: {ex.Message}");
        }
    }

    // ── New settings handlers ───────────────────────────────────────────────────

    private void ManageVaultsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var mainWindow = (Application.Current as App)?.MainWindow;
            mainWindow?.NavigateToPage("Vaults");
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Failed to navigate to Vaults page", ex); }
    }

    private void ManagePasskeysButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var mainWindow = (Application.Current as App)?.MainWindow;
            mainWindow?.NavigateToPage("Passkeys");
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Failed to navigate to Passkeys page", ex); }
    }

    private async void PasskeysToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
            await _viewModel.SaveSettingsAsync();
    }

    // WhatsApp-style "Linked Devices": shows a QR the mobile (MAUI) app scans to sign in.
    private async void LinkedDevicesButton_Click(object sender, RoutedEventArgs e)
    {
        if (_serviceProvider == null) return;
        try
        {
            var email = _authService?.CurrentUser?.Email;
            var userId = _authService?.CurrentUser?.Id;
            await Helpers.QrSignInDialog.ShowAsync(_serviceProvider, email, userId);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("SettingsPage", "Linked Devices QR failed", ex);
        }
    }

    private async void SeedVaultsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_serviceProvider == null) return;
        try
        {
            var authService = _serviceProvider.GetService<IAuthService>();
            var vaultService = _serviceProvider.GetService<IVaultService>();
            var userId = authService?.CurrentUser?.Id;
            if (string.IsNullOrEmpty(userId) || vaultService == null)
            {
                await ShowErrorDialog("Not signed in. Please sign in first.");
                return;
            }
            await vaultService.SeedDefaultVaultsAsync(userId);
            var dlg = new ModernWpf.Controls.ContentDialog
            {
                Title = "Vaults Created",
                Content = "Default vaults (Personal, Work, Finance) have been created for your account.",
                CloseButtonText = "OK"
            };
            await dlg.ShowAsync();
        }
        catch (Exception ex) { await ShowErrorDialog($"Failed to seed vaults: {ex.Message}"); }
    }

    private void PasswordLengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (PasswordLengthLabel != null)
            PasswordLengthLabel.Text = ((int)e.NewValue).ToString();
    }

    private void GeneratePreviewPassword_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            int length = (int)(PasswordLengthSlider?.Value ?? 20);
            bool upper   = PwdUppercase?.IsChecked == true;
            bool lower   = PwdLowercase?.IsChecked == true;
            bool numbers = PwdNumbers?.IsChecked  == true;
            bool symbols = PwdSymbols?.IsChecked  == true;

            var chars = new System.Text.StringBuilder();
            if (upper)   chars.Append("ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            if (lower)   chars.Append("abcdefghijklmnopqrstuvwxyz");
            if (numbers) chars.Append("0123456789");
            if (symbols) chars.Append("!@#$%^&*()-_=+[]{}|;:,.<>?");
            if (chars.Length == 0) chars.Append("abcdefghijklmnopqrstuvwxyz");

            var charSet = chars.ToString();
            var bytes = new byte[length];
            System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
            var pwd = new string(bytes.Select(b => charSet[b % charSet.Length]).ToArray());
            if (GeneratedPasswordPreview != null) GeneratedPasswordPreview.Text = pwd;
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Failed to generate preview password", ex); }
    }

    private void OpenImportLogButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var logDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            var monthFolder = System.IO.Path.Combine(logDir, DateTime.Now.ToString("yyyy-MM"));
            var logFile = System.IO.Path.Combine(monthFolder, $"{DateTime.Now:yyyy-MM-dd}.log");

            if (System.IO.File.Exists(logFile))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = logFile, UseShellExecute = true });
            else if (System.IO.Directory.Exists(logDir))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = logDir, UseShellExecute = true });
            else
                _ = ShowErrorDialog("No log file found yet.", "Logs");
        }
        catch (Exception ex)
        {
            _ = ShowErrorDialog($"Could not open log: {ex.Message}", "Error");
        }
    }

    private async void SaveSentrySettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;
        await _viewModel.SaveSettingsAsync();

        var dsn = _viewModel.SentryDsn?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(dsn))
        {
            try
            {
                SentrySdk.Init(o =>
                {
                    o.Dsn = dsn;
                    o.AttachStacktrace = true;
                    o.SendDefaultPii = false;
                    o.Environment = "production";
                });
                VaultGuard.WPF.Services.ToastService.Instance.Success("Sentry error reporting enabled.", "Saved");
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("Error", $"Invalid Sentry DSN: {ex.Message}");
            }
        }
        else
        {
            VaultGuard.WPF.Services.ToastService.Instance.Info("Sentry DSN cleared. Error reporting disabled.", "Saved");
        }
    }

    private VaultGuard.WPF.Services.UpdateInfo? _pendingUpdate;
    private CancellationTokenSource? _updateCts;

    private async void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
    {
        var updateService = _serviceProvider?.GetService<VaultGuard.WPF.Services.UpdateService>();
        if (updateService is null) return;

        CheckUpdatesButton.IsEnabled = false;
        CheckUpdatesButtonText.Text = "Checking…";
        UpdateStatusPanel.Visibility = System.Windows.Visibility.Visible;
        UpdateProgressBar.Visibility = System.Windows.Visibility.Collapsed;
        UpdateFeaturesText.Visibility = System.Windows.Visibility.Collapsed;
        DownloadUpdateButton.Visibility = System.Windows.Visibility.Collapsed;
        OpenReleasesButton.Visibility = System.Windows.Visibility.Collapsed;

        try
        {
            _updateCts?.Cancel();
            _updateCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            // Drive the check from the configured JSON manifest URL (falls back to GitHub releases).
            _pendingUpdate = await updateService.CheckManifestAsync(_viewModel?.UpdateManifestUrl, _updateCts.Token);

            if (_pendingUpdate is null)
            {
                UpdateStatusText.Text = $"You're up to date! (v{updateService.GetCurrentVersion()})";
                UpdateStatusPanel.Background = new SolidColorBrush(Color.FromRgb(0x16, 0x61, 0x34));
            }
            else
            {
                var appName = string.IsNullOrWhiteSpace(_pendingUpdate.AppName) ? "Vault Guard" : _pendingUpdate.AppName;
                UpdateStatusText.Text =
                    $"{appName} v{_pendingUpdate.Version} is available (you have v{updateService.GetCurrentVersion()})"
                    + (_pendingUpdate.RequiresRestart ? "  ·  a restart is required to apply it." : string.Empty);
                UpdateStatusPanel.Background = new SolidColorBrush(Color.FromRgb(0x1D, 0x4E, 0xD8));

                // Show the "what's new" feature list from the manifest.
                if (_pendingUpdate.Features is { Count: > 0 })
                {
                    UpdateFeaturesText.Text = "What's new:\n" +
                        string.Join("\n", _pendingUpdate.Features.Select(f => $"  •  {f}"));
                    UpdateFeaturesText.Visibility = System.Windows.Visibility.Visible;
                }

                if (_pendingUpdate.InstallerDownloadUrl is not null)
                    DownloadUpdateButton.Visibility = System.Windows.Visibility.Visible;
                OpenReleasesButton.Visibility = System.Windows.Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            UpdateStatusText.Text = $"Could not check for updates: {ex.Message}";
            UpdateStatusPanel.Background = new SolidColorBrush(Color.FromRgb(0x7F, 0x1D, 0x1D));
        }
        finally
        {
            CheckUpdatesButton.IsEnabled = true;
            CheckUpdatesButtonText.Text = "Check for Updates";
        }
    }

    private async void DownloadUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        var updateService = _serviceProvider?.GetService<VaultGuard.WPF.Services.UpdateService>();
        if (updateService is null || _pendingUpdate?.InstallerDownloadUrl is null) return;

        DownloadUpdateButton.IsEnabled = false;
        CheckUpdatesButton.IsEnabled = false;
        UpdateProgressBar.Visibility = System.Windows.Visibility.Visible;
        UpdateStatusText.Text = "Downloading update…";

        var savePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            $"VaultGuardSetup-{_pendingUpdate.Version}.exe");

        try
        {
            _updateCts?.Cancel();
            _updateCts = new CancellationTokenSource();
            var progress = new Progress<int>(p =>
            {
                UpdateProgressBar.Value = p;
                UpdateStatusText.Text = $"Downloading update… {p}%";
            });

            await updateService.DownloadInstallerAsync(
                _pendingUpdate.InstallerDownloadUrl, savePath, progress, _updateCts.Token);

            UpdateProgressBar.Visibility = System.Windows.Visibility.Collapsed;

            // Visual-Studio-style: prompt the user to restart to apply the update.
            var restartDialog = new ModernWpf.Controls.ContentDialog
            {
                Title = "Update ready to install",
                Content = _pendingUpdate.RequiresRestart
                    ? $"Vault Guard {_pendingUpdate.Version} has been downloaded. The app needs to restart to finish installing. Restart now?"
                    : $"Vault Guard {_pendingUpdate.Version} has been downloaded. Install it now?",
                PrimaryButtonText = "Restart & Install",
                CloseButtonText = "Later",
                DefaultButton = ModernWpf.Controls.ContentDialogButton.Primary
            };
            restartDialog.Style = restartDialog.TryFindResource("Modern1PasswordDialogStyle") as Style;

            if (await Helpers.DialogManager.ShowAsync(restartDialog) == ModernWpf.Controls.ContentDialogResult.Primary)
            {
                UpdateStatusText.Text = "Launching installer…";
                await Task.Delay(400);
                updateService.LaunchInstallerAndExit(savePath);
            }
            else
            {
                UpdateStatusText.Text = "Update downloaded. It will install when you restart Vault Guard.";
                DownloadUpdateButton.IsEnabled = true;
                CheckUpdatesButton.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            UpdateStatusText.Text = $"Download failed: {ex.Message}";
            DownloadUpdateButton.IsEnabled = true;
            CheckUpdatesButton.IsEnabled = true;
            UpdateProgressBar.Visibility = System.Windows.Visibility.Collapsed;
        }
    }

    private void OpenReleasesButton_Click(object sender, RoutedEventArgs e)
    {
        var updateService = _serviceProvider?.GetService<VaultGuard.WPF.Services.UpdateService>();
        updateService?.OpenReleasesPage();
    }

    private void OpenDocumentationButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/dotnetappdev/vaultguard/wiki",
                UseShellExecute = true
            });
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Failed to open documentation link", ex); }
    }

    // ── Toast notification appearance ────────────────────────────────────────────

    private readonly Dictionary<VaultGuard.WPF.Services.ToastType, Border> _toastChips = new();

    private static readonly (VaultGuard.WPF.Services.ToastType Type, string Label)[] ToastRowDefs =
    {
        (VaultGuard.WPF.Services.ToastType.Success, "Success"),
        (VaultGuard.WPF.Services.ToastType.Error,   "Error"),
        (VaultGuard.WPF.Services.ToastType.Warning, "Warning"),
        (VaultGuard.WPF.Services.ToastType.Info,    "Info"),
    };

    private void BuildToastRows()
    {
        if (ToastRowsHost == null) return;
        ToastRowsHost.Children.Clear();
        _toastChips.Clear();

        foreach (var (type, label) in ToastRowDefs)
        {
            var grid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var chip = new Border { VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(chip, 0);
            _toastChips[type] = chip;

            var nameBlock = new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 0, 0),
                Foreground = (Brush)FindResource("ModernTextSecondaryBrush")
            };
            Grid.SetColumn(nameBlock, 1);

            var btnRow = new StackPanel { Orientation = Orientation.Horizontal };
            Grid.SetColumn(btnRow, 2);

            var customizeBtn = new Button
            {
                Content = "Customize…",
                Style = (Style)FindResource("ModernSecondaryButtonStyle"),
                Padding = new Thickness(10, 4, 10, 4),
                Tag = type
            };
            customizeBtn.Click += CustomizeToast_Click;

            var testBtn = new Button
            {
                Content = "Test",
                Style = (Style)FindResource("ModernSecondaryButtonStyle"),
                Margin = new Thickness(6, 0, 0, 0),
                Padding = new Thickness(10, 4, 10, 4),
                Tag = type
            };
            testBtn.Click += TestToast_Click;

            btnRow.Children.Add(customizeBtn);
            btnRow.Children.Add(testBtn);

            grid.Children.Add(chip);
            grid.Children.Add(nameBlock);
            grid.Children.Add(btnRow);
            ToastRowsHost.Children.Add(grid);

            RefreshToastChip(type);
        }
    }

    private void RefreshToastChip(VaultGuard.WPF.Services.ToastType type)
    {
        if (!_toastChips.TryGetValue(type, out var chip)) return;
        var theme = VaultGuard.WPF.Services.ToastSettings.For(type);

        Brush Safe(string hex)
        {
            try { return (Brush)new BrushConverter().ConvertFrom(hex)!; }
            catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", logEx); return Brushes.Gray; }
        }

        var accent = Safe(theme.Accent);

        var icon = new TextBlock
        {
            Text = char.ConvertFromUtf32(theme.IconGlyph),
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 15,
            Foreground = accent,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        var swatch = new TextBlock
        {
            Text = theme.Accent.ToUpperInvariant(),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)),
            VerticalAlignment = VerticalAlignment.Center
        };
        var inner = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10, 6, 12, 6) };
        inner.Children.Add(icon);
        inner.Children.Add(swatch);

        chip.Background = Safe(theme.Background);
        chip.CornerRadius = new CornerRadius(6);
        chip.BorderBrush = accent;
        chip.BorderThickness = new Thickness(1);
        chip.Child = inner;
    }

    private void CustomizeToast_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not VaultGuard.WPF.Services.ToastType type) return;
        var theme = VaultGuard.WPF.Services.ToastSettings.For(type);
        var dialog = new VaultGuard.WPF.Dialogs.ToastColorPickerDialog($"Customize \"{type}\" toast", theme)
        {
            Owner = Window.GetWindow(this)
        };
        if (dialog.ShowDialog() == true)
        {
            theme.CopyFrom(dialog.Result);
            RefreshToastChip(type);
        }
    }

    private void TestToast_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not VaultGuard.WPF.Services.ToastType type) return;
        VaultGuard.WPF.Services.ToastService.Instance.Show(
            "This is a sample notification.", type, $"{type} preview");
    }

    private void SaveToastColors_Click(object sender, RoutedEventArgs e)
    {
        VaultGuard.WPF.Services.ToastSettings.Save();
        VaultGuard.WPF.Services.ToastService.Instance.Success(
            "Toast notification colours saved.", "Saved");
    }

    private void ResetToastColors_Click(object sender, RoutedEventArgs e)
    {
        VaultGuard.WPF.Services.ToastSettings.ResetToDefaults();
        VaultGuard.WPF.Services.ToastSettings.Save();
        foreach (var (type, _) in ToastRowDefs) RefreshToastChip(type);
        VaultGuard.WPF.Services.ToastService.Instance.Info(
            "Toast colours reset to defaults.", "Reset");
    }
}
