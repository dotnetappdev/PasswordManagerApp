using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.WinUi.ViewModels;

namespace PasswordManager.WinUi.Views;

public sealed partial class SettingsPage : Page
{
    private SettingsViewModel? _viewModel;
    private IServiceProvider? _serviceProvider;

    public SettingsPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        if (e.Parameter is IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _viewModel = new SettingsViewModel(serviceProvider);
            this.DataContext = _viewModel;
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
                XamlRoot = this.XamlRoot
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
            XamlRoot = this.XamlRoot
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
            XamlRoot = this.XamlRoot
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
            XamlRoot = this.XamlRoot
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
                XamlRoot = this.XamlRoot
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
            Header = "Current Password",
            PlaceholderText = "Enter current password"
        };

        var newPasswordBox = new PasswordBox
        {
            Header = "New Password",
            PlaceholderText = "Enter new password"
        };

        var confirmPasswordBox = new PasswordBox
        {
            Header = "Confirm New Password",
            PlaceholderText = "Confirm new password"
        };

        var stackPanel = new StackPanel { Spacing = 16 };
        stackPanel.Children.Add(currentPasswordBox);
        stackPanel.Children.Add(newPasswordBox);
        stackPanel.Children.Add(confirmPasswordBox);

        var dialog = new ContentDialog
        {
            Title = "Change Master Password",
            Content = stackPanel,
            PrimaryButtonText = "Change Password",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && _viewModel != null)
        {
            if (string.IsNullOrEmpty(currentPasswordBox.Password) || 
                string.IsNullOrEmpty(newPasswordBox.Password) ||
                newPasswordBox.Password != confirmPasswordBox.Password)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = "Please fill in all fields and ensure new passwords match.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
                return;
            }

            var success = await _viewModel.ChangePasswordAsync(currentPasswordBox.Password, newPasswordBox.Password);
            
            var message = success ? "Password changed successfully!" : "Failed to change password. Please check your current password.";
            var resultDialog = new ContentDialog
            {
                Title = "Change Password Result",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            
            await resultDialog.ShowAsync();
        }
    }
}