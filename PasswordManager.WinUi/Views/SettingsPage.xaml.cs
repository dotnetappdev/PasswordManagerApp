using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.WinUi.ViewModels;
using PasswordManager.Services.Interfaces;
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

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        if (e.Parameter is IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _authService = serviceProvider.GetRequiredService<IAuthService>();
            _viewModel = new SettingsViewModel(serviceProvider);
            DataContext = _viewModel;
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

    private void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_serviceProvider != null)
        {
            Frame.Navigate(typeof(ImportPage), _serviceProvider);
        }
    }

    private async void ChooseExportFolderButton_Click(object sender, RoutedEventArgs e)
    {
        // This would normally open a folder picker
        // For now, show a message that this feature would be implemented with platform-specific code
        var dialog = new ContentDialog
        {
            Title = "Choose Export Folder",
            Content = "This feature would open a folder picker to choose the export location.",
            CloseButtonText = "OK",
            XamlRoot = XamlRoot
        };
        
        await dialog.ShowAsync();
    }

    private async void ConfigureDatabaseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Database Configuration",
            Content = "Database configuration UI would be implemented here.",
            CloseButtonText = "OK",
            XamlRoot = XamlRoot
        };
        
        await dialog.ShowAsync();
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
}