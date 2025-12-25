using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Services.Interfaces;
using PasswordManager.Models.DTOs;
using PasswordManager.WinUi.ViewModels;
using PasswordManager.Services.Utilities;
using System.Linq;

namespace PasswordManager.WinUi.Views;

public sealed partial class SettingsPage : Page
{
    private SettingsViewModel? _viewModel;
    private IServiceProvider? _serviceProvider;
    private IAuthService? _authService;
    private readonly FileLogger _logger;

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
            _viewModel = new SettingsViewModel(serviceProvider);
            DataContext = _viewModel;

            // Update network location visibility based on initial provider selection
            UpdateNetworkLocationVisibility();

            // Preload import providers to ensure they're available when needed
            try
            {
                await _logger.LogAsync("SettingsPage", "Starting provider preload");
                var importService = serviceProvider.GetService<PasswordManager.Imports.Interfaces.IImportService>();
                if (importService != null)
                {
                    // Force load all PasswordManagerImports.* assemblies and register providers
                    var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    var importDlls = System.IO.Directory.GetFiles(baseDirectory, "PasswordManagerImports.*.dll");
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
                                    System.Diagnostics.Debug.WriteLine($"Successfully registered {provider.DisplayName} v{provider.Version}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            var fileName = System.IO.Path.GetFileName(dllPath);
                            await _logger.LogErrorAsync("SettingsPage", $"Failed to load provider from {fileName}", ex);
                            System.Diagnostics.Debug.WriteLine($"Failed to load provider from {fileName}: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"Error during provider preload: {ex.Message}");
            }
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
        {
            System.Diagnostics.Debug.WriteLine($"Failed to populate import types: {ex.Message}");
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
            var importService = _serviceProvider.GetRequiredService<PasswordManager.Imports.Interfaces.IImportService>();
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
                var importDlls = System.IO.Directory.GetFiles(baseDirectory, "PasswordManagerImports.*.dll");
                foreach (var dllPath in importDlls)
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

            // Explicit fallback: if provider still not found, try to instantiate it directly
            if (provider == null && providerName == "1Password")
            {
                try
                {
                    var onePasswordProviderType = Type.GetType("PasswordManagerImports.OnePassword.Providers.OnePasswordImportProvider, PasswordManagerImports.OnePassword");
                    if (onePasswordProviderType != null)
                    {
                        var instance = Activator.CreateInstance(onePasswordProviderType) as PasswordManager.Imports.Interfaces.IPasswordImportProvider;
                        if (instance != null)
                        {
                            importService.RegisterProvider(instance);
                            provider = instance;
                            System.Diagnostics.Debug.WriteLine("Registered 1Password provider via direct instantiation");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to directly instantiate 1Password provider: {ex.Message}");
                }
            }

            // Perform import
            using var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            var fileName = System.IO.Path.GetFileName(filePath);

            ImportProgressText.Text = "Processing items...";
            // Determine current user id to attach imported items to the logged-in tenant/user by default
            string? currentUserId = null;
            try
            {
                if (_authService?.CurrentUser != null)
                    currentUserId = _authService.CurrentUser.Id;
                else if (_authService != null)
                    currentUserId = await _authService.GetCurrentUserIdAsync();
            }
            catch { }

            var result = await importService.ImportPasswordsAsync(providerName, fileStream, fileName, currentUserId);

            // Show result
            ImportProgressRing.IsActive = false;
            ImportProgressBar.IsIndeterminate = false;
            ImportProgressBar.Value = 100;
            ImportProgressPanel.Visibility = Visibility.Collapsed;

            if (result.Success)
            {
                ImportStatusText.Text = "✓ Import completed successfully!";
                ImportResultText.Text = $"Imported: {result.SuccessfulImports} items, Failed: {result.FailedImports}, Total processed: {result.TotalItemsProcessed}";
            }
            else
            {
                ImportStatusText.Text = "✗ Import failed";
                ImportResultText.Text = result.ErrorMessage ?? "Unknown error occurred";
            }
        }
        catch (Exception ex)
        {
            ImportProgressRing.IsActive = false;
            ImportProgressPanel.Visibility = Visibility.Collapsed;
            ImportStatusText.Text = "✗ Import failed";
            ImportResultText.Text = $"Error: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Import error: {ex}");
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
        catch { }
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
                var resetService = _serviceProvider?.GetService<PasswordManager.Services.Interfaces.IDatabaseResetService>();
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
                var resetService = _serviceProvider?.GetService<PasswordManager.Services.Interfaces.IDatabaseResetService>();
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
            var resetService = _serviceProvider?.GetService<PasswordManager.Services.Interfaces.IDatabaseResetService>();
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