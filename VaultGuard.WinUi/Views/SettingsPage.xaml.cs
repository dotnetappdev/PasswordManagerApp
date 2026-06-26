using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using VaultGuard.Services.Interfaces;
using VaultGuard.Models.DTOs;
using VaultGuard.Models.DTOs.Auth;
using VaultGuard.WinUi.ViewModels;
using VaultGuard.Services.Utilities;
using VaultGuard.Imports.Interfaces;
using System.Linq;
using System.Collections.Generic;

namespace VaultGuard.WinUi.Views;

public sealed partial class SettingsPage : Page
{
    private SettingsViewModel? _viewModel;
    private IServiceProvider? _serviceProvider;
    private IAuthService? _authService;
    private IUserProfileService? _userProfileService;
    private readonly FileLogger _logger;
    private List<UserDto> _availableUsers = new();

    public SettingsPage()
    {
        InitializeComponent();
        _logger = new FileLogger();
    }

    protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _authService = serviceProvider.GetRequiredService<IAuthService>();
            _userProfileService = serviceProvider.GetService<IUserProfileService>();
            _viewModel = new SettingsViewModel(serviceProvider);
            DataContext = _viewModel;

            // Load all users for multi-user selection
            await LoadUsersAsync();

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
                    try { candidateDirs.Add(AppDomain.CurrentDomain.BaseDirectory); } catch (Exception ex) { await _logger.LogErrorAsync("SettingsPage", "Failed to add base directory candidate", ex); }
                    try { candidateDirs.Add(AppContext.BaseDirectory); } catch (Exception ex) { await _logger.LogErrorAsync("SettingsPage", "Failed to add AppContext base directory candidate", ex); }

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
                    try { candidateDirs.Add(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VaultGuard", "imports")); } catch (Exception ex) { await _logger.LogErrorAsync("SettingsPage", "Failed to add per-user imports folder candidate", ex); }

                    // Machine-wide imports folder (ProgramData)
                    try { candidateDirs.Add(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "VaultGuard", "imports")); } catch (Exception ex) { await _logger.LogErrorAsync("SettingsPage", "Failed to add machine-wide imports folder candidate", ex); }

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
                        catch (Exception ex)
                        {
                            await _logger.LogErrorAsync("SettingsPage", $"Failed to scan directory {dir} for import DLLs", ex);
                        }
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
                var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                dataPackage.SetText(path);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);

                // Show success notification
                var dialog = new ContentDialog
                {
                    Title = "Success",
                    Content = "Database path copied to clipboard",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                _ = dialog.ShowAsync(); // Fire and forget

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
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("SettingsPage", "Error populating import types", ex);
        }
    }

    private async void TwoFactorToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            await _viewModel.SaveSettingsAsync();
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

    private async void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel != null && ThemeComboBox.SelectedItem != null)
        {
            await _viewModel.SaveSettingsAsync();
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

    private async void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
    {
        await ShowChangePasswordDialog();
    }

    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            var success = await _viewModel.ExportDataAsync();

            var message = success ? "Export completed successfully!" : "Export failed. Please try again.";
            var dialog = new ContentDialog
            {
                Title = "Export Result",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = XamlRoot
            };

            await dialog.ShowAsync();
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
                        Margin = new Microsoft.UI.Xaml.Thickness(4)
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
            var filePicker = new Windows.Storage.Pickers.FileOpenPicker();
            var app = App.Current as App;
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(app?.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(filePicker, hWnd);

            filePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            filePicker.FileTypeFilter.Add(".csv");
            filePicker.FileTypeFilter.Add(".1pux");
            // Some 1Password exports use the .1pu extension — include it so files are selectable
            filePicker.FileTypeFilter.Add(".1pu");
            filePicker.FileTypeFilter.Add(".json");
            filePicker.FileTypeFilter.Add(".txt");

            var file = await filePicker.PickSingleFileAsync();
            if (file != null)
            {
                ImportFilePathTextBox.Text = file.Path;
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

        try
        {
            var importService = _serviceProvider.GetRequiredService<VaultGuard.Imports.Interfaces.IImportService>();
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
            catch (Exception ex)
            {
                VaultGuard.Services.Logging.AppLogger.Error($"Failed to log available providers", ex);
            }

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
                                    catch (Exception ex)
                                    {
                                        VaultGuard.Services.Logging.AppLogger.Error($"Failed to instantiate candidate provider type {ct.FullName}", ex);
                                    }
                                }

                                if (provider != null) break;
                            }
                            catch (Exception ex)
                            {
                                VaultGuard.Services.Logging.AppLogger.Error($"Failed to scan assembly {asm.FullName} for provider types", ex);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    VaultGuard.Services.Logging.AppLogger.Error($"Failed fallback 1Password provider resolution", ex);
                }
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
                // Import for current user - determine current user id to attach imported items
                try
                {
                    string? currentUserId = null;
                    if (_authService?.CurrentUser != null)
                        currentUserId = _authService.CurrentUser.Id;
                    else if (_authService != null)
                        currentUserId = await _authService.GetCurrentUserIdAsync();
                    targetUserIds.Add(currentUserId);
                }
                catch (Exception ex)
                {
                    // Log error but continue with null userId (will import for all users as fallback)
                    await _logger.LogErrorAsync("SettingsPage", "Failed to get current user ID for import", ex);
                    targetUserIds.Add(null);
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

            for (int i = 0; i < targetUserIds.Count; i++)
            {
                var targetUserId = targetUserIds[i];

                // Reset stream position for each import
                fileStream.Position = 0;

                ImportProgressText.Text = $"Processing items for user {i + 1} of {targetUserIds.Count}...";

                var result = await importService.ImportPasswordsAsync(providerName, fileStream, fileName, targetUserId);

                if (result.Success)
                {
                    totalSuccessful += result.SuccessfulImports;
                    totalFailed += result.FailedImports;
                    totalProcessed += result.TotalItemsProcessed;
                }
                else
                {
                    await _logger.LogErrorAsync("SettingsPage", $"Import failed for user {targetUserId}: {result.ErrorMessage}", null);
                }
            }

            // Show combined result
            ImportProgressRing.IsActive = false;
            ImportProgressBar.IsIndeterminate = false;
            ImportProgressBar.Value = 100;
            ImportProgressPanel.Visibility = Visibility.Collapsed;

            if (totalSuccessful > 0 || totalProcessed > 0)
            {
                ImportStatusText.Text = "✓ Import completed successfully!";
                if (targetUserIds.Count > 1)
                {
                    ImportResultText.Text = $"Imported for {targetUserIds.Count} user(s): {totalSuccessful} items successful, {totalFailed} failed, Total processed: {totalProcessed}";
                }
                else
                {
                    ImportResultText.Text = $"Imported: {totalSuccessful} items, Failed: {totalFailed}, Total processed: {totalProcessed}";
                }
            }
            else
            {
                ImportStatusText.Text = "✗ Import failed";
                ImportResultText.Text = "No items were successfully imported";
            }
        }
        catch (Exception ex)
        {
            ImportProgressRing.IsActive = false;
            ImportProgressPanel.Visibility = Visibility.Collapsed;
            ImportStatusText.Text = "✗ Import failed";
            ImportResultText.Text = $"Error: {ex.Message}";
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
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }

    private async void ChooseExportFolderButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();

            // Get the current window's HWND
            var app = App.Current as App;
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(app?.MainWindow);

            // Initialize the folder picker with the window handle
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hWnd);

            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            folderPicker.FileTypeFilter.Add("*");

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null && _viewModel != null)
            {
                _viewModel.ExportPath = folder.Path;

                var dialog = new ContentDialog
                {
                    Title = "Export Folder Selected",
                    Content = $"Export folder set to: {folder.Path}",
                    CloseButtonText = "OK",
                    XamlRoot = XamlRoot
                };

                await dialog.ShowAsync();
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
            Header = "Connection String",
            PlaceholderText = "Enter database connection string...",
            TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
            AcceptsReturn = true,
            MinHeight = 80,
            Text = _viewModel?.DatabaseConnectionString ?? ""
        };

        var providerComboBox = new ComboBox
        {
            Header = "Database Provider",
            MinWidth = 200,
            ItemsSource = new string[] { "SqlServer", "MySQL", "PostgreSQL", "SQLite" },
            SelectedItem = _viewModel?.DatabaseProvider ?? "SQLite"
        };

        var testButton = new Button
        {
            Content = "Test Connection",
            Margin = new Microsoft.UI.Xaml.Thickness(0, 8, 0, 0)
        };

        var stackPanel = new StackPanel { Spacing = 16 };
        stackPanel.Children.Add(providerComboBox);
        stackPanel.Children.Add(connectionStringBox);
        stackPanel.Children.Add(testButton);

        var dialog = new ContentDialog
        {
            Title = "Database Configuration",
            Content = stackPanel,
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            XamlRoot = XamlRoot
        };

        // Handle test connection button
        testButton.Click += async (s, e) =>
        {
            try
            {
                var testDialog = new ContentDialog
                {
                    Title = "Connection Test",
                    Content = "Connection test functionality would be implemented here.\nFor now, simulating successful connection.",
                    CloseButtonText = "OK",
                    XamlRoot = XamlRoot
                };
                await testDialog.ShowAsync();
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"Connection test failed: {ex.Message}");
            }
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && _viewModel != null)
        {
            // Save database configuration
            _viewModel.DatabaseProvider = providerComboBox.SelectedItem?.ToString() ?? "SQLite";
            _viewModel.DatabaseConnectionString = connectionStringBox.Text;

            var success = await _viewModel.SaveSettingsAsync();

            var resultDialog = new ContentDialog
            {
                Title = success ? "Success" : "Error",
                Content = success ? "Database configuration saved successfully." : "Failed to save database configuration.",
                CloseButtonText = "OK",
                XamlRoot = XamlRoot
            };

            await resultDialog.ShowAsync();
        }
    }

    private async void SeedDataButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Show confirmation dialog
            var confirmDialog = new ContentDialog
            {
                Title = "Seed Essential Data",
                Content = "This will seed categories, collections, and tags into the database if they are missing.\n\nThis is useful if the category dropdown is empty or you need to restore default data.\n\nContinue?",
                PrimaryButtonText = "Yes, Seed Data",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary && _serviceProvider != null)
            {
                // Show progress indicator
                var progressDialog = new ContentDialog
                {
                    Title = "Seeding Data",
                    Content = new ProgressRing { IsActive = true, Width = 50, Height = 50 },
                    XamlRoot = XamlRoot
                };

                // Show progress dialog (fire and forget)
                var progressTask = progressDialog.ShowAsync();

                try
                {
                    // Get the AppStartupService to trigger seeding
                    var startupService = _serviceProvider.GetService<IAppStartupService>();
                    if (startupService != null)
                    {
                        await _logger.LogAsync("SettingsPage", "Manually triggering essential data seeding");
                        
                        // Call the database initialization which includes essential data seeding
                        await startupService.InitializeDatabaseAsync();
                        
                        await _logger.LogAsync("SettingsPage", "Essential data seeding completed");

                        // Close progress dialog
                        progressDialog.Hide();

                        // Show success message
                        var successDialog = new ContentDialog
                        {
                            Title = "✓ Success",
                            Content = "Essential data has been seeded successfully!\n\n• Categories\n• Collections\n• Tags\n\nYou can now add new items with the category dropdown populated.",
                            CloseButtonText = "OK",
                            XamlRoot = XamlRoot
                        };
                        await successDialog.ShowAsync();
                    }
                    else
                    {
                        progressDialog.Hide();
                        await ShowErrorDialog("Startup service not available. Please restart the application.");
                    }
                }
                catch (Exception ex)
                {
                    progressDialog.Hide();
                    await _logger.LogErrorAsync("SettingsPage", "Error seeding data", ex);
                    await ShowErrorDialog($"Failed to seed data: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("SettingsPage", "Error in SeedDataButton_Click", ex);
            await ShowErrorDialog($"An error occurred: {ex.Message}");
        }
    }

    private async void ClearDataButton_Click(object sender, RoutedEventArgs e)
    {
        var confirmDialog = new ContentDialog
        {
            Title = "⚠️ Clear All Data",
            Content = "Are you absolutely sure you want to delete ALL your passwords and settings? This action cannot be undone.",
            PrimaryButtonText = "Yes, Delete Everything",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        var result = await confirmDialog.ShowAsync();
        if (result == ContentDialogResult.Primary && _viewModel != null)
        {
            var success = await _viewModel.ClearAllDataAsync();

            var message = success ? "All data has been cleared." : "Failed to clear data. Please try again.";
            var resultDialog = new ContentDialog
            {
                Title = "Clear Data Result",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = XamlRoot
            };

            await resultDialog.ShowAsync();

            if (success)
            {
                // Navigate back to login
                Frame.Navigate(typeof(LoginPage), _serviceProvider);
            }
        }
    }

    private async Task ShowChangePasswordDialog()
    {
        var currentPasswordBox = new PasswordBox
        {
            Header = "Current Master Password",
            PlaceholderText = "Enter current master password"
        };

        var newPasswordBox = new PasswordBox
        {
            Header = "New Master Password",
            PlaceholderText = "Enter new master password (min 8 chars, with uppercase, lowercase, and numbers)"
        };

        var confirmPasswordBox = new PasswordBox
        {
            Header = "Confirm New Master Password",
            PlaceholderText = "Confirm new master password"
        };

        var passwordHintBox = new TextBox
        {
            Header = "Password Hint (Optional)",
            PlaceholderText = "Enter a hint to help you remember your password"
        };

        var errorTextBlock = new TextBlock
        {
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red),
            TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
            Visibility = Microsoft.UI.Xaml.Visibility.Collapsed
        };

        var stackPanel = new StackPanel { Spacing = 16 };
        stackPanel.Children.Add(errorTextBlock);
        stackPanel.Children.Add(currentPasswordBox);
        stackPanel.Children.Add(newPasswordBox);
        stackPanel.Children.Add(confirmPasswordBox);
        stackPanel.Children.Add(passwordHintBox);

        var dialog = new ContentDialog
        {
            Title = "Change Master Password",
            Content = stackPanel,
            PrimaryButtonText = "Change Password",
            CloseButtonText = "Cancel",
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && _viewModel != null)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(currentPasswordBox.Password))
            {
                await ShowErrorDialog("Please enter your current master password.");
                return;
            }

            if (string.IsNullOrEmpty(newPasswordBox.Password))
            {
                await ShowErrorDialog("Please enter a new master password.");
                return;
            }

            if (newPasswordBox.Password != confirmPasswordBox.Password)
            {
                await ShowErrorDialog("New passwords do not match. Please try again.");
                return;
            }

            // Validate password strength
            var validationResult = ValidatePasswordStrength(newPasswordBox.Password);
            if (!validationResult.IsValid)
            {
                await ShowErrorDialog(validationResult.ErrorMessage);
                return;
            }

            // Use the auth service to change the master password with hint
            bool success;
            if (_authService != null)
            {
                success = await _authService.ChangeMasterPasswordAsync(
                    currentPasswordBox.Password,
                    newPasswordBox.Password,
                    passwordHintBox.Text);
            }
            else
            {
                success = false;
            }

            var message = success ?
                "Master password changed successfully! Your new password will be required on next app startup." :
                "Failed to change master password. Please check your current password and try again.";

            var resultDialog = new ContentDialog
            {
                Title = success ? "Success" : "Error",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = XamlRoot
            };

            await resultDialog.ShowAsync();
        }
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

    private async Task ShowErrorDialog(string message)
    {
        var errorDialog = new ContentDialog
        {
            Title = "Error",
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = XamlRoot
        };
        await errorDialog.ShowAsync();
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
        UpdateNetworkLocationVisibility();
    }

    private void UpdateNetworkLocationVisibility()
    {
        if (_viewModel != null && NetworkLocationPanel != null)
        {
            NetworkLocationPanel.Visibility = _viewModel.SelectedCloudProvider == "NetworkLocation"
                ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private async void CreateBackupButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            // Show password dialog
            var passwordDialog = new ContentDialog
            {
                Title = "Create Cloud Backup",
                Content = await CreateMasterPasswordInput(),
                PrimaryButtonText = "Create Backup",
                CloseButtonText = "Cancel",
                XamlRoot = XamlRoot
            };

            var result = await passwordDialog.ShowAsync();
            if (result == ContentDialogResult.Primary && passwordDialog.Content is PasswordBox passwordBox)
            {
                var success = await _viewModel.CreateCloudBackupAsync(passwordBox.Password);

                var message = success ? "Backup created successfully!" : "Backup creation failed. Please try again.";
                var dialog = new ContentDialog
                {
                    Title = success ? "Success" : "Error",
                    Content = message,
                    CloseButtonText = "OK",
                    XamlRoot = XamlRoot
                };

                await dialog.ShowAsync();
            }
        }
    }

    private async void RefreshBackupsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            await _viewModel.LoadAvailableBackupsAsync();
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
        if (_viewModel != null)
        {
            var success = await _viewModel.RestoreFromFileAsync();

            var message = success ?
                "Database restored successfully from file!" :
                "Failed to restore database from file. Please check the file and master password.";

            var dialog = new ContentDialog
            {
                Title = success ? "Success" : "Error",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = XamlRoot
            };

            await dialog.ShowAsync();
        }
    }

    private async void RestoreBackupButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null && sender is Button button && button.Tag is CloudBackupInfo backup)
        {
            // Show confirmation dialog
            var confirmDialog = new ContentDialog
            {
                Title = "Restore Backup",
                Content = $"This will replace all current data with the backup from {backup.CreatedAt:MMM dd, yyyy HH:mm}. This action cannot be undone.\n\nAre you sure you want to continue?",
                PrimaryButtonText = "Yes, Restore",
                CloseButtonText = "Cancel",
                XamlRoot = XamlRoot
            };

            if (await confirmDialog.ShowAsync() == ContentDialogResult.Primary)
            {
                // Show password dialog
                var passwordDialog = new ContentDialog
                {
                    Title = "Enter Master Password",
                    Content = await CreateMasterPasswordInput(),
                    PrimaryButtonText = "Restore",
                    CloseButtonText = "Cancel",
                    XamlRoot = XamlRoot
                };

                if (await passwordDialog.ShowAsync() == ContentDialogResult.Primary && passwordDialog.Content is PasswordBox passwordBox)
                {
                    var success = await _viewModel.RestoreCloudBackupAsync(backup, passwordBox.Password);

                    var message = success ? "Backup restored successfully!" : "Backup restoration failed. Please check your master password and try again.";
                    var dialog = new ContentDialog
                    {
                        Title = success ? "Success" : "Error",
                        Content = message,
                        CloseButtonText = "OK",
                        XamlRoot = XamlRoot
                    };

                    await dialog.ShowAsync();
                }
            }
        }
    }

    private async void DeleteBackupButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null && sender is Button button && button.Tag is CloudBackupInfo backup)
        {
            var confirmDialog = new ContentDialog
            {
                Title = "Delete Backup",
                Content = $"Are you sure you want to delete the backup '{backup.FileName}'? This action cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                XamlRoot = XamlRoot
            };

            if (await confirmDialog.ShowAsync() == ContentDialogResult.Primary)
            {
                var success = await _viewModel.DeleteCloudBackupAsync(backup);

                if (!success)
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "Error",
                        Content = "Failed to delete backup. Please try again.",
                        CloseButtonText = "OK",
                        XamlRoot = XamlRoot
                    };

                    await errorDialog.ShowAsync();
                }
            }
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

                var dialog = new ContentDialog
                {
                    Title = result?.Success == true ? "Export Success" : "Export Failed",
                    Content = message,
                    CloseButtonText = "OK",
                    XamlRoot = XamlRoot
                };

                await dialog.ShowAsync();
            }
        }
    }

    private async Task<PasswordBox> CreateMasterPasswordInput()
    {
        var stackPanel = new StackPanel { Spacing = 8 };

        stackPanel.Children.Add(new TextBlock
        {
            Text = "Enter your master password:",
            Style = Application.Current.Resources["ModernBodyStyle"] as Style
        });

        var passwordBox = new PasswordBox
        {
            PlaceholderText = "Master Password",
            Width = 300
        };
        stackPanel.Children.Add(passwordBox);

        return passwordBox;
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
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Error($"Failed to update SQLite path panel visibility", ex);
        }
    }

    private async void SecureWipeButton_Click(object sender, RoutedEventArgs e)
    {
        var confirm = new ContentDialog
        {
            Title = "Secure Wipe",
            Content = "This will securely delete your database (SQLite) or clear all tables (SQL Server). This cannot be undone. Continue?",
            PrimaryButtonText = "Yes, Wipe",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        if (await confirm.ShowAsync() == ContentDialogResult.Primary)
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
        var confirm = new ContentDialog
        {
            Title = "Reset Password Data",
            Content = "This will clear all password items, collections, categories and tags but keep your user account. Continue?",
            PrimaryButtonText = "Yes, Reset",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        if (await confirm.ShowAsync() == ContentDialogResult.Primary)
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
                var dlg = new ContentDialog
                {
                    Title = result.Success ? "Reset Complete" : "Reset Failed",
                    Content = result.Success ? "Password data has been reset." : $"Failed to reset password data: {result.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = XamlRoot
                };
                await dlg.ShowAsync();
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"Reset failed: {ex.Message}");
            }
        }
    }

    private async void ResetAllButton_Click(object sender, RoutedEventArgs e)
    {
        var confirm = new ContentDialog
        {
            Title = "Reset All Database Tables",
            Content = "DANGER: This will clear all database tables including user accounts and log you out. Continue?",
            PrimaryButtonText = "Yes, Reset All",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        if (await confirm.ShowAsync() == ContentDialogResult.Primary)
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
                var dlg = new ContentDialog
                {
                    Title = result.Success ? "Reset Complete" : "Reset Failed",
                    Content = result.Success ? "All database tables have been reset." : $"Failed to reset database: {result.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = XamlRoot
                };

                await dlg.ShowAsync();

                if (result.Success)
                {
                    Frame.Navigate(typeof(LoginPage), _serviceProvider);
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
            var dlg = new ContentDialog
            {
                Title = result.Success ? "Reseeded" : "Reseed Failed",
                Content = result.Success ? "Sample data has been reseeded." : $"Failed to reseed sample data: {result.Message}",
                CloseButtonText = "OK",
                XamlRoot = XamlRoot
            };
            await dlg.ShowAsync();
        }
        catch (Exception ex)
        {
            await ShowErrorDialog($"Reseed failed: {ex.Message}");
        }
    }
}