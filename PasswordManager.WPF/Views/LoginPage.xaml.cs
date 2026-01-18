using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Services.Interfaces;
using PasswordManager.WPF.ViewModels;
using PasswordManager.Models.DTOs.Auth;
using PasswordManager.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;

namespace PasswordManager.WPF.Views;

/// <summary>
/// Login page for the Password Manager application with master password authentication
/// </summary>
public sealed partial class LoginPage : Page
{
    private IServiceProvider? _serviceProvider;
    private LoginViewModel? _viewModel;
    private UserProfileSelectionViewModel? _profileSelectionViewModel;

    public LoginPage()
    {
        this.InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        // Focus the master password field when the page loads
        if (this.FindName("MasterPasswordBox") is PasswordBox masterPasswordBox)
        {
            masterPasswordBox.Focus();
        }
    }

    private async void MasterPasswordBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            await DoPrimaryActionAsync();
        }
    }

    // Custom navigation handler for WPF (replacing WinUI's OnNavigatedTo)
    public void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
    {
        // Note: WPF Page doesn't have base.OnNavigatedTo
        if (e.ExtraData is IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _viewModel = new LoginViewModel(serviceProvider);
            _profileSelectionViewModel = new UserProfileSelectionViewModel(serviceProvider);

            // Set data context for the main view
            this.DataContext = _viewModel;

            // Set data context for user profiles list
            if (this.FindName("UserProfilesList") is ItemsControl userProfilesList)
            {
                userProfilesList.ItemsSource = _profileSelectionViewModel.UserProfiles;
            }


            // Check if already authenticated after a brief delay for initialization
            _ = CheckAuthenticationStatusAsync();
        }
        else
        { }
    }

    private async Task CheckAuthenticationStatusAsync()
    {
        try
        {
            // Give the ViewModel time to initialize and check authentication
            await Task.Delay(100);

            // If already authenticated, navigate to home
            if (_viewModel?.IsAuthenticated == true)
            {
                if (GetMainWindow() is MainWindow mainWindow)
                {
                    mainWindow.NavigateToHome();
                }
            }
        }
        catch (Exception ex)
        { }
    }

    private async Task DoPrimaryActionAsync()
    {
        if (_viewModel == null)
        {
            return;
        }


        // Resolve UI elements once for this handler
        var primaryActionButton = this.FindName("PrimaryActionButton") as Button;
        var authProgressRing = this.FindName("AuthProgressRing") as System.Windows.Controls.ProgressBar;
        var masterPasswordBox = this.FindName("MasterPasswordBox") as PasswordBox;
        var confirmPasswordBox = this.FindName("ConfirmPasswordBox") as PasswordBox;
        var passwordHintBox = this.FindName("PasswordHintBox") as TextBox;

        try
        {
            if (primaryActionButton != null) primaryActionButton.IsEnabled = false;
            if (authProgressRing != null) authProgressRing.IsIndeterminate = true;

            // Update ViewModel with current values
            _viewModel.MasterPassword = masterPasswordBox?.Password ?? string.Empty;
            _viewModel.ConfirmMasterPassword = confirmPasswordBox?.Password ?? string.Empty;
            _viewModel.PasswordHint = passwordHintBox?.Text ?? string.Empty;


            // Attempt authentication (handles both setup and login)
            var success = await _viewModel.AuthenticateAsync();


            if (success)
            {
                // Clear password fields for security
                if (masterPasswordBox != null) masterPasswordBox.Password = string.Empty;
                if (confirmPasswordBox != null) confirmPasswordBox.Password = string.Empty;
                if (passwordHintBox != null) passwordHintBox.Text = string.Empty;

                // Navigate to main dashboard via MainWindow
                if (GetMainWindow() is MainWindow mainWindow)
                {
                    mainWindow.NavigateToHome();
                }
            }
        }
        catch (Exception ex)
        {
            // Error handling is done in ViewModel
        }
        finally
        {
            if (primaryActionButton != null) primaryActionButton.IsEnabled = true;
            if (authProgressRing != null) authProgressRing.IsIndeterminate = false;
        }
    }

    // Event handler remains async void for XAML Click binding
    private async void PrimaryActionButton_Click(object sender, RoutedEventArgs e)
    {
        await DoPrimaryActionAsync();
    }

    private MainWindow? GetMainWindow()
    {
        // Use the MainWindow property exposed in App
        return (App.Current as App)?.MainWindow;
    }

    #region Legacy Methods for Backward Compatibility

    // Keep these methods for any existing references, but redirect to the new flow
    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        await DoPrimaryActionAsync();
    }



    private void ProfileButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is UserDto user && _viewModel != null)
        {
            _viewModel.SelectUserProfile(user);
        }
    }

    private void BackToProfilesButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel?.GoBackToProfileSelection();
    }

    private async void CreateProfileButton_Click(object sender, RoutedEventArgs e)
    {
        // Navigate to profile creation - for now, we'll implement a simple dialog
        // In a full implementation, this would open a profile creation dialog
        await ShowCreateProfileDialog();
    }

    private async Task ShowCreateProfileDialog()
    {
        try
        {
            // Get current user for permissions check
            ApplicationUser? currentUser = null;
            try
            {
                if (_serviceProvider != null)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var authService = scope.ServiceProvider.GetService<IAuthService>();
                    if (authService != null)
                    {
                        var currentUserId = await authService.GetCurrentUserIdAsync();
                        if (!string.IsNullOrEmpty(currentUserId))
                        {
                            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                            currentUser = await userManager.FindByIdAsync(currentUserId);
                        }
                    }
                }
            }
            catch (Exception ex)
            { }

            // Create and show the registration dialog
            if (_serviceProvider == null)
            {
                return;
            }

            var registrationDialog = new Dialogs.UserRegistrationDialog(_serviceProvider!, currentUser);
            
            var result = await registrationDialog.ShowAsync();
            if (result == ModernWpf.Controls.ContentDialogResult.Primary)
            {
                // Refresh the profiles list
                if (_profileSelectionViewModel != null)
                {
                    await _profileSelectionViewModel.LoadUserProfilesAsync();
                }
            }
        }
        catch (Exception ex)
        { }
    }

    private async void CreateAccountButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_serviceProvider == null)
            {
                await ShowErrorDialog("Service provider not initialized.");
                return;
            }

            var registrationDialog = new Dialogs.UserRegistrationDialog(_serviceProvider);
            
            var result = await registrationDialog.ShowAsync();
            if (result == ModernWpf.Controls.ContentDialogResult.Primary)
            {
                // Refresh the login page
                if (_viewModel != null)
                {
                    await _viewModel.RefreshAsync();
                }
                await ShowSuccessMessage("Account created successfully! You can now log in.");
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialog($"Error creating account: {ex.Message}");
        }
    }

    private async Task ShowErrorDialog(string message)
    {
        var errorDialog = new ModernWpf.Controls.ContentDialog
        {
            Title = "Error",
            Content = message,
            CloseButtonText = "OK"};
        await errorDialog.ShowAsync();
    }

    private async Task ShowSuccessMessage(string message)
    {
        var successDialog = new ModernWpf.Controls.ContentDialog
        {
            Title = "Success",
            Content = message,
            CloseButtonText = "OK"};
        await successDialog.ShowAsync();
    }

    #endregion
}
