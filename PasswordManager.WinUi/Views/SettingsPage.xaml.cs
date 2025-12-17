using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Services.Interfaces;
using PasswordManager.Models.DTOs;
using PasswordManager.WinUi.ViewModels;
using System.Linq;

namespace PasswordManager.WinUi.Views;

public sealed partial class SettingsPage : Page
{
    private SettingsViewModel? _viewModel;
    private IServiceProvider? _serviceProvider;
    private IAuthService? _authService;

    public SettingsPage()
    {
        InitializeComponent();
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
                var importService = serviceProvider.GetService<PasswordManager.Imports.Interfaces.IImportService>();
                if (importService != null)
                {
                    // Force load all PasswordManagerImports.* assemblies and register providers
                    var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    var importDlls = System.IO.Directory.GetFiles(baseDirectory, "PasswordManagerImports.*.dll");

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
                                    System.Diagnostics.Debug.WriteLine($"Successfully registered {provider.DisplayName} v{provider.Version}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to load provider from {System.IO.Path.GetFileName(dllPath)}: {ex.Message}");
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
            await importService.GetAvailableProvidersAsync();

            // Verify provider exists
            var providers = await importService.GetAvailableProvidersAsync();
            System.Diagnostics.Debug.WriteLine($"Available providers: {string.Join(", ", providers.Select(p => p.ProviderName))}");

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

            var provider = providers.FirstOrDefault(p => p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));
            if (provider == null)
            {
                ImportProgressRing.IsActive = false;
                ImportProgressPanel.Visibility = Visibility.Collapsed;
                ImportStatusText.Text = "Import failed";
                ImportResultText.Text = $"Import provider '{providerName}' not found. Available providers: {string.Join(", ", providers.Select(p => p.ProviderName))}";
                StartImportButton.IsEnabled = true;
                return;
            }

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
            var result = await importService.ImportPasswordsAsync(providerName, fileStream, fileName);

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
}