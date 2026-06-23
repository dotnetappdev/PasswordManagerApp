using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Services.Interfaces;
using PasswordManager.Models.DTOs;
using PasswordManager.Models.DTOs.Auth;
using PasswordManager.WPF.ViewModels;
using PasswordManager.Services.Utilities;
using PasswordManager.Imports.Interfaces;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using PasswordManager.DAL;
using PasswordManager.DAL.Seed;
using Microsoft.EntityFrameworkCore;
using Sentry;

namespace PasswordManager.WPF.Views;

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
        BuildToastRows();
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
                var importService = serviceProvider.GetService<PasswordManager.Imports.Interfaces.IImportService>();
                if (importService != null)
                {
                    // Force load all PasswordManagerImports.* assemblies and register providers
                    var importDllsList = new List<string>();

                    // Common candidate directories to search for import provider assemblies. This covers
                    // both per-user and machine-wide install locations so the provider is found when
                    // running as a normal user or as an elevated/admin process.
                    var candidateDirs = new List<string>();

                    // App base and AppContext
                    try { candidateDirs.Add(AppDomain.CurrentDomain.BaseDirectory); } catch { }
                    try { candidateDirs.Add(AppContext.BaseDirectory); } catch { }

                    // Executing assembly location
                    try
                    {
                        var execPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                        if (!string.IsNullOrEmpty(execPath)) candidateDirs.Add(System.IO.Path.GetDirectoryName(execPath)!);
                    }
                    catch { }

                    // Current directory
                    try { candidateDirs.Add(Environment.CurrentDirectory); } catch { }

                    // Per-user imports folder (LocalAppData)
                    try { candidateDirs.Add(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PasswordManager", "imports")); } catch { }

                    // Machine-wide imports folder (ProgramData)
                    try { candidateDirs.Add(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "PasswordManager", "imports")); } catch { }

                    // Standard plugin discovery folder used by PluginDiscoveryService
                    try { candidateDirs.Add(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "imports", "otherpasswordmanagers")); } catch { }

                    // Collect DLLs from all candidate directories
                    foreach (var dir in candidateDirs.Where(d => !string.IsNullOrEmpty(d)).Distinct())
                    {
                        try
                        {
                            if (Directory.Exists(dir))
                            {
                                importDllsList.AddRange(Directory.GetFiles(dir, "PasswordManagerImports.*.dll", SearchOption.AllDirectories));
                                importDllsList.AddRange(Directory.GetFiles(dir, "PasswordManagerImports.*.dll", SearchOption.TopDirectoryOnly));
                            }
                        }
                        catch { }
                    }
                    var importDlls = importDllsList.ToArray();
                    await _logger.LogAsync("SettingsPage", $"Found {importDlls.Length} import DLLs");

                    foreach (var dllPath in importDlls)
                    {
                        try
                        {
                            var assembly = System.Reflection.Assembly.LoadFrom(dllPath);
                            var providerTypes = assembly.GetTypes()
                                .Where(t => typeof(PasswordManager.Imports.Interfaces.IPasswordImportProvider).IsAssignableFrom(t)
                                         && !t.IsInterface && !t.IsAbstract);

                            foreach (var providerType in providerTypes)
                            {
                                var provider = Activator.CreateInstance(providerType) as PasswordManager.Imports.Interfaces.IPasswordImportProvider;
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
                PasswordManager.WPF.Services.ToastService.Instance.Show("Database path copied to clipboard.", PasswordManager.WPF.Services.ToastType.Success);

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

    private async System.Threading.Tasks.Task PopulateImportTypesAsync(PasswordManager.Imports.Interfaces.IImportService importService)
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
        { }
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

            if (success)
                PasswordManager.WPF.Services.ToastService.Instance.Show("Export completed successfully!", PasswordManager.WPF.Services.ToastType.Success);
            else
                PasswordManager.WPF.Services.ToastService.Instance.Show("Export failed. Please try again.", PasswordManager.WPF.Services.ToastType.Error);
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
            var importService = importScope.ServiceProvider.GetRequiredService<PasswordManager.Imports.Interfaces.IImportService>();
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
                importDllsList.AddRange(System.IO.Directory.GetFiles(baseDirectory, "PasswordManagerImports.*.dll"));
                var importsFolder = System.IO.Path.Combine(baseDirectory, "imports", "otherpasswordmanagers");
                if (System.IO.Directory.Exists(importsFolder))
                {
                    importDllsList.AddRange(System.IO.Directory.GetFiles(importsFolder, "PasswordManagerImports.*.dll", System.IO.SearchOption.AllDirectories));
                }
                foreach (var dllPath in importDllsList)
                {
                    try
                    {
                        var asm = System.Reflection.Assembly.LoadFrom(dllPath);
                        var providerTypes = asm.GetTypes()
                            .Where(t => typeof(PasswordManager.Imports.Interfaces.IPasswordImportProvider).IsAssignableFrom(t)
                                     && !t.IsInterface && !t.IsAbstract);

                        foreach (var providerType in providerTypes)
                        {
                            try
                            {
                                var providerInstance = Activator.CreateInstance(providerType) as PasswordManager.Imports.Interfaces.IPasswordImportProvider;
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
            catch { }

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
                try { await _logger.LogAsync("SettingsPage", $"Provider resolution failed for '{providerName}'. Available: {string.Join(",", providers.Select(p => p.ProviderName))}"); } catch { }
                StartImportButton.IsEnabled = true;
                return;
            }

            // Log the chosen provider
            try { await _logger.LogAsync("SettingsPage", $"Resolved provider '{providerName}' to '{provider.ProviderName}' ({provider.DisplayName})"); } catch { }

            // Update progress
            ImportStatusText.Text = $"Importing from {selectedType}...";
            ImportProgressText.Text = "Reading file...";

            // Explicit fallback: if provider still not found, try multiple resolution strategies
            if (provider == null && providerName == "1Password")
            {
                try
                {
                    // Try common assembly name variants
                    var assemblyNames = new[] { "PasswordManagerImports.OnePassword", "PasswordManagerImports.1Password", "PasswordManagerImports.OnePassword.dll" };
                    foreach (var asmName in assemblyNames)
                    {
                        var typeName = $"PasswordManagerImports.OnePassword.Providers.OnePasswordImportProvider, {asmName}";
                        var onePasswordProviderType = Type.GetType(typeName, false);
                        if (onePasswordProviderType != null)
                        {
                            var instance = Activator.CreateInstance(onePasswordProviderType) as PasswordManager.Imports.Interfaces.IPasswordImportProvider;
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
                                    .Where(t => typeof(PasswordManager.Imports.Interfaces.IPasswordImportProvider).IsAssignableFrom(t)
                                                && !t.IsInterface && !t.IsAbstract)
                                    .ToList();

                                foreach (var ct in candidateTypes)
                                {
                                    // Try to instantiate and check ProviderName
                                    try
                                    {
                                        var inst = Activator.CreateInstance(ct) as PasswordManager.Imports.Interfaces.IPasswordImportProvider;
                                        if (inst != null && string.Equals(inst.ProviderName, "1Password", StringComparison.OrdinalIgnoreCase))
                                        {
                                            importService.RegisterProvider(inst);
                                            provider = inst;
                                            break;
                                        }
                                    }
                                    catch { }
                                }

                                if (provider != null) break;
                            }
                            catch { }
                        }
                    }
                }
                catch (Exception ex)
                { }
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
                        var scopedAuth = importScope.ServiceProvider.GetService<PasswordManager.Services.Interfaces.IAuthService>();
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

                PasswordManager.WPF.Services.ToastService.Instance.Show($"Export folder: {folderDialog.SelectedPath}", PasswordManager.WPF.Services.ToastType.Info);
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
                        var db = scope.ServiceProvider.GetRequiredService<PasswordManagerDbContext>();
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

                PasswordManager.WPF.Services.ToastService.Instance.Show("Sample data seeded — categories, collections, tags and items added.", PasswordManager.WPF.Services.ToastType.Success, "Seed Complete");
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

    // Removes the demo/seed data (categories, collections, tags, password items) for the current user
    // and the built-in test user. User accounts are NOT touched.
    private async System.Threading.Tasks.Task RunClearSeedDataAsync()
    {
        try
        {
            var confirmDialog = new ModernWpf.Controls.ContentDialog
            {
                Title = "Delete Seed Data",
                Content = "This will permanently remove all categories, collections, tags, and password items (including the built-in demo data).\n\nYour user accounts are kept. This cannot be undone. Continue?",
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
                var itemService = scope.ServiceProvider.GetService<PasswordManager.Services.Interfaces.IPasswordItemService>();
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

            // STEP 2 — clear categories, collections, tags and any leftover rows (FK constraints off),
            // then mark the database as seeded so startup never re-adds the demo data.
            var resetService = _serviceProvider.GetService<PasswordManager.Services.Interfaces.IDatabaseResetService>();
            PasswordManager.Services.Interfaces.DatabaseResetResult? reset = null;
            if (resetService != null)
                reset = await resetService.ResetDataTablesAsync();

            try
            {
                if (scopeFactory != null)
                {
                    await System.Threading.Tasks.Task.Run(() =>
                    {
                        using var scope = scopeFactory.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<PasswordManagerDbContext>();
                        TestDataSeeder.ClearSeedData(db, TestDataSeeder.TestUserId);
                        TryMarkSeedComplete(db);
                    });
                }
            }
            catch (Exception inner)
            {
                await _logger.LogErrorAsync("SettingsPage", "Per-user seed cleanup failed (items already deleted)", inner);
            }

            await _logger.LogAsync("SettingsPage", $"Delete seed data: {itemsDeleted} items deleted; reset: {reset?.Message}");

            // Tell the live items / dashboard views to reload so the cleared data disappears immediately.
            PasswordManager.WPF.Services.AppEvents.RaiseVaultDataChanged();

            PasswordManager.WPF.Services.ToastService.Instance.Success(
                $"Removed {itemsDeleted} item(s) plus their categories, collections & tags. Your accounts were kept.",
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
    private static void TryMarkSeedComplete(PasswordManagerDbContext db)
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
        catch { /* best-effort */ }
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
                PasswordManager.WPF.Services.ToastService.Instance.Success("All data has been cleared.", "Done");
            else
                PasswordManager.WPF.Services.ToastService.Instance.Error("Failed to clear data. Please try again.");

            if (success)
            {
                // Navigate back to login
                (App.Current as App)?.MainWindow?.HandleLogout();
            }
        }
    }

    private async Task ShowChangePasswordDialog()
    {
        var currentPasswordBox = new PasswordBox();
        var newPasswordBox = new PasswordBox();
        var confirmPasswordBox = new PasswordBox();
        var passwordHintBox = new TextBox();

        var errorTextBlock = new TextBlock
        {
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red),
            TextWrapping = System.Windows.TextWrapping.Wrap,
            Visibility = System.Windows.Visibility.Collapsed
        };

        var stackPanel = new StackPanel { };
        stackPanel.Children.Add(errorTextBlock);
        stackPanel.Children.Add(currentPasswordBox);
        stackPanel.Children.Add(newPasswordBox);
        stackPanel.Children.Add(confirmPasswordBox);
        stackPanel.Children.Add(passwordHintBox);

        var dialog = new ModernWpf.Controls.ContentDialog
        {
            Title = "Change Master Password",
            Content = stackPanel,
            PrimaryButtonText = "Change Password",
            CloseButtonText = "Cancel",
            // WPF: XamlRoot not needed
        };

        var result = await dialog.ShowAsync();
        if (result == ModernWpf.Controls.ContentDialogResult.Primary && _viewModel != null)
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

            var resultDialog = new ModernWpf.Controls.ContentDialog
            {
                Title = success ? "Success" : "Error",
                Content = message,
                CloseButtonText = "OK",
                // WPF: XamlRoot not needed
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

    private Task ShowErrorDialog(string message)
    {
        PasswordManager.WPF.Services.ToastService.Instance.Show(message, PasswordManager.WPF.Services.ToastType.Error, "Error");
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
            PasswordManager.WPF.Services.ToastService.Instance.Show("Connected to Google Drive!", PasswordManager.WPF.Services.ToastType.Success);
        else
            await ShowErrorDialog("Google Drive connection failed. Make sure your Client ID and Secret are correct and that you allowed the authorisation in the browser.");
    }

    private async void DisconnectGoogleDriveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;
        await _viewModel.DisconnectGoogleDriveAsync();
        PasswordManager.WPF.Services.ToastService.Instance.Show("Disconnected from Google Drive.", PasswordManager.WPF.Services.ToastType.Info);
    }

    private void GDriveClientSecretBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null && sender is PasswordBox pb)
            _viewModel.GoogleDriveClientSecret = pb.Password;
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
        if (_viewModel != null)
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
            if (result == ModernWpf.Controls.ContentDialogResult.Primary && passwordDialog.Content is PasswordBox passwordBox)
            {
                var success = await _viewModel.CreateCloudBackupAsync(passwordBox.Password);

                var message = success ? "Backup created successfully!" : "Backup creation failed. Please try again.";
                var dialog = new ModernWpf.Controls.ContentDialog
                {
                    Title = success ? "Success" : "Error",
                    Content = message,
                    CloseButtonText = "OK",
                    // WPF: XamlRoot not needed
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

            var dialog = new ModernWpf.Controls.ContentDialog
            {
                Title = success ? "Success" : "Error",
                Content = message,
                CloseButtonText = "OK",
                // WPF: XamlRoot not needed
            };

            await dialog.ShowAsync();
        }
    }

    private async void RestoreBackupButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null || sender is not Button button || button.Tag is not CloudBackupInfo backup) return;

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

    private async void BrowseBackupButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null || sender is not Button button || button.Tag is not CloudBackupInfo backup) return;

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
        var cloudManager = _serviceProvider?.GetService<PasswordManager.Services.Services.CloudBackupManager>();
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
        var browseDialog = new PasswordManager.WPF.Dialogs.CloudBackupBrowseDialog(contents);
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

    private async void DeleteBackupButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null && sender is Button button && button.Tag is CloudBackupInfo backup)
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

    private async Task<PasswordBox> CreateMasterPasswordInput()
    {
        var stackPanel = new StackPanel { };

        stackPanel.Children.Add(new TextBlock
        {
            Text = "Enter your master password:",
            Style = Application.Current.Resources["ModernBodyStyle"] as Style
        });

        var passwordBox = new PasswordBox
        {
            // PlaceholderText = "Master Password",
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
        catch { }
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
                var resetService = _serviceProvider?.GetService<PasswordManager.Services.Interfaces.IDatabaseResetService>();
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
                var resetService = _serviceProvider?.GetService<PasswordManager.Services.Interfaces.IDatabaseResetService>();
                if (resetService == null)
                {
                    await ShowErrorDialog("Database reset service is not available.");
                    return;
                }

                var result = await resetService.ResetDataTablesAsync();
                if (result.Success)
                    PasswordManager.WPF.Services.ToastService.Instance.Show("Password data has been reset.", PasswordManager.WPF.Services.ToastType.Success, "Reset Complete");
                else
                    PasswordManager.WPF.Services.ToastService.Instance.Show($"Reset failed: {result.Message}", PasswordManager.WPF.Services.ToastType.Error, "Reset Failed");
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
            var resetService = _serviceProvider?.GetService<PasswordManager.Services.Interfaces.IDatabaseResetService>();
            if (resetService == null)
            {
                await ShowErrorDialog("Database reset service is not available.");
                return;
            }

            var result = await resetService.ClearNonAdminUsersAsync();
            if (result.Success)
                PasswordManager.WPF.Services.ToastService.Instance.Success(result.Message, "Users deleted");
            else
                PasswordManager.WPF.Services.ToastService.Instance.Warning(result.Message, "Completed with errors");
        }
        catch (Exception ex)
        {
            PasswordManager.WPF.Services.ToastService.Instance.Error($"Failed to delete users: {ex.Message}", "Error");
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
                var resetService = _serviceProvider?.GetService<PasswordManager.Services.Interfaces.IDatabaseResetService>();
                if (resetService == null)
                {
                    await ShowErrorDialog("Database reset service is not available.");
                    return;
                }

                var result = await resetService.ResetAllTablesAsync(reseedData: false);
                if (result.Success)
                    PasswordManager.WPF.Services.ToastService.Instance.Show("All database tables have been reset.", PasswordManager.WPF.Services.ToastType.Success, "Reset Complete");
                else
                    PasswordManager.WPF.Services.ToastService.Instance.Show($"Reset failed: {result.Message}", PasswordManager.WPF.Services.ToastType.Error, "Reset Failed");

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
            var resetService = _serviceProvider?.GetService<PasswordManager.Services.Interfaces.IDatabaseResetService>();
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
        catch { }
    }

    private void ManagePasskeysButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var mainWindow = (Application.Current as App)?.MainWindow;
            mainWindow?.NavigateToPage("Passkeys");
        }
        catch { }
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
            await Helpers.QrSignInDialog.ShowAsync(_serviceProvider, email);
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
        catch { }
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
                PasswordManager.WPF.Services.ToastService.Instance.Success("Sentry error reporting enabled.", "Saved");
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("Error", $"Invalid Sentry DSN: {ex.Message}");
            }
        }
        else
        {
            PasswordManager.WPF.Services.ToastService.Instance.Info("Sentry DSN cleared. Error reporting disabled.", "Saved");
        }
    }

    private PasswordManager.WPF.Services.UpdateInfo? _pendingUpdate;
    private CancellationTokenSource? _updateCts;

    private async void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
    {
        var updateService = _serviceProvider?.GetService<PasswordManager.WPF.Services.UpdateService>();
        if (updateService is null) return;

        CheckUpdatesButton.IsEnabled = false;
        CheckUpdatesButtonText.Text = "Checking…";
        UpdateStatusPanel.Visibility = System.Windows.Visibility.Visible;
        UpdateProgressBar.Visibility = System.Windows.Visibility.Collapsed;
        DownloadUpdateButton.Visibility = System.Windows.Visibility.Collapsed;
        OpenReleasesButton.Visibility = System.Windows.Visibility.Collapsed;

        try
        {
            _updateCts?.Cancel();
            _updateCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            _pendingUpdate = await updateService.CheckForUpdateAsync(_updateCts.Token);

            if (_pendingUpdate is null)
            {
                UpdateStatusText.Text = $"You're up to date! (v{updateService.GetCurrentVersion()})";
                UpdateStatusPanel.Background = new SolidColorBrush(Color.FromRgb(0x16, 0x61, 0x34));
            }
            else
            {
                UpdateStatusText.Text =
                    $"Update available: v{_pendingUpdate.Version}  (you have v{updateService.GetCurrentVersion()})";
                UpdateStatusPanel.Background = new SolidColorBrush(Color.FromRgb(0x1D, 0x4E, 0xD8));
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
        var updateService = _serviceProvider?.GetService<PasswordManager.WPF.Services.UpdateService>();
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

            UpdateStatusText.Text = "Download complete. Launching installer…";
            await Task.Delay(800);
            updateService.LaunchInstallerAndExit(savePath);
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
        var updateService = _serviceProvider?.GetService<PasswordManager.WPF.Services.UpdateService>();
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
        catch { }
    }

    // ── Toast notification appearance ────────────────────────────────────────────

    private readonly Dictionary<PasswordManager.WPF.Services.ToastType, Border> _toastChips = new();

    private static readonly (PasswordManager.WPF.Services.ToastType Type, string Label)[] ToastRowDefs =
    {
        (PasswordManager.WPF.Services.ToastType.Success, "Success"),
        (PasswordManager.WPF.Services.ToastType.Error,   "Error"),
        (PasswordManager.WPF.Services.ToastType.Warning, "Warning"),
        (PasswordManager.WPF.Services.ToastType.Info,    "Info"),
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

    private void RefreshToastChip(PasswordManager.WPF.Services.ToastType type)
    {
        if (!_toastChips.TryGetValue(type, out var chip)) return;
        var theme = PasswordManager.WPF.Services.ToastSettings.For(type);

        Brush Safe(string hex)
        {
            try { return (Brush)new BrushConverter().ConvertFrom(hex)!; }
            catch { return Brushes.Gray; }
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
        if (sender is not Button btn || btn.Tag is not PasswordManager.WPF.Services.ToastType type) return;
        var theme = PasswordManager.WPF.Services.ToastSettings.For(type);
        var dialog = new PasswordManager.WPF.Dialogs.ToastColorPickerDialog($"Customize \"{type}\" toast", theme)
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
        if (sender is not Button btn || btn.Tag is not PasswordManager.WPF.Services.ToastType type) return;
        PasswordManager.WPF.Services.ToastService.Instance.Show(
            "This is a sample notification.", type, $"{type} preview");
    }

    private void SaveToastColors_Click(object sender, RoutedEventArgs e)
    {
        PasswordManager.WPF.Services.ToastSettings.Save();
        PasswordManager.WPF.Services.ToastService.Instance.Success(
            "Toast notification colours saved.", "Saved");
    }

    private void ResetToastColors_Click(object sender, RoutedEventArgs e)
    {
        PasswordManager.WPF.Services.ToastSettings.ResetToDefaults();
        PasswordManager.WPF.Services.ToastSettings.Save();
        foreach (var (type, _) in ToastRowDefs) RefreshToastChip(type);
        PasswordManager.WPF.Services.ToastService.Instance.Info(
            "Toast colours reset to defaults.", "Reset");
    }
}
