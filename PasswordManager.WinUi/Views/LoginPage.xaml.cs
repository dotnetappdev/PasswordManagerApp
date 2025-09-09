using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Services.Interfaces;
using PasswordManager.WinUi.ViewModels;
using PasswordManager.Models.DTOs.Auth;
using PasswordManager.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;

namespace PasswordManager.WinUi.Views;

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
            masterPasswordBox.Focus(FocusState.Programmatic);
        }
    }

    private async void MasterPasswordBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            System.Diagnostics.Debug.WriteLine("MasterPasswordBox_KeyDown - Enter key pressed");
            await DoPrimaryActionAsync();
        }
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is IServiceProvider serviceProvider)
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

            System.Diagnostics.Debug.WriteLine($"LoginPage DataContext set - ViewModel created");
            System.Diagnostics.Debug.WriteLine($"Initial ViewModel state - PageTitle: {_viewModel.PageTitle}, PrimaryButtonText: {_viewModel.PrimaryButtonText}");

            // Check if already authenticated after a brief delay for initialization
            _ = CheckAuthenticationStatusAsync();
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("LoginPage OnNavigatedTo - No service provider passed as parameter");
        }
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
        {
            System.Diagnostics.Debug.WriteLine($"Error checking authentication status: {ex.Message}");
        }
    }

    private async Task DoPrimaryActionAsync()
    {
        if (_viewModel == null)
        {
            System.Diagnostics.Debug.WriteLine("DoPrimaryActionAsync - ViewModel is null");
            return;
        }

        System.Diagnostics.Debug.WriteLine("DoPrimaryActionAsync - Starting authentication");

        // Resolve UI elements once for this handler
        var primaryActionButton = this.FindName("PrimaryActionButton") as Button;
        var authProgressRing = this.FindName("AuthProgressRing") as ProgressRing;
        var masterPasswordBox = this.FindName("MasterPasswordBox") as PasswordBox;
        var confirmPasswordBox = this.FindName("ConfirmPasswordBox") as PasswordBox;
        var passwordHintBox = this.FindName("PasswordHintBox") as TextBox;

        try
        {
            if (primaryActionButton != null) primaryActionButton.IsEnabled = false;
            if (authProgressRing != null) authProgressRing.IsActive = true;

            // Update ViewModel with current values
            _viewModel.MasterPassword = masterPasswordBox?.Password ?? string.Empty;
            _viewModel.ConfirmMasterPassword = confirmPasswordBox?.Password ?? string.Empty;
            _viewModel.PasswordHint = passwordHintBox?.Text ?? string.Empty;

            System.Diagnostics.Debug.WriteLine($"DoPrimaryActionAsync - Password length: {_viewModel.MasterPassword.Length}");
            System.Diagnostics.Debug.WriteLine($"DoPrimaryActionAsync - IsFirstTimeSetup: {_viewModel.IsFirstTimeSetup}");

            // Attempt authentication (handles both setup and login)
            var success = await _viewModel.AuthenticateAsync();

            System.Diagnostics.Debug.WriteLine($"DoPrimaryActionAsync - Authentication result: {success}");

            if (success)
            {
                // Clear password fields for security
                if (masterPasswordBox != null) masterPasswordBox.Password = string.Empty;
                if (confirmPasswordBox != null) confirmPasswordBox.Password = string.Empty;
                if (passwordHintBox != null) passwordHintBox.Text = string.Empty;

                // Navigate to main dashboard via MainWindow
                if (GetMainWindow() is MainWindow mainWindow)
                {
                    System.Diagnostics.Debug.WriteLine("DoPrimaryActionAsync - Navigating to home via MainWindow");
                    mainWindow.NavigateToHome();
                }
                else
                {
                    // Fallback navigation
                    System.Diagnostics.Debug.WriteLine("DoPrimaryActionAsync - Using fallback navigation");
                    this.Frame?.Navigate(typeof(DashboardPage), _serviceProvider);
                }
            }
        }
        catch (Exception ex)
        {
            // Error handling is done in ViewModel
            System.Diagnostics.Debug.WriteLine($"Authentication error in UI: {ex.Message}");
        }
        finally
        {
            if (primaryActionButton != null) primaryActionButton.IsEnabled = true;
            if (authProgressRing != null) authProgressRing.IsActive = false;
        }
    }

    // Event handler remains async void for XAML Click binding
    private async void PrimaryActionButton_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("PrimaryActionButton_Click - Button clicked");
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
            {
                System.Diagnostics.Debug.WriteLine($"Could not get current user for permissions check: {ex.Message}");
            }

            // Create and show the registration dialog
            var registrationDialog = new Dialogs.UserRegistrationDialog(_serviceProvider!, currentUser);
            var result = await registrationDialog.ShowAsync();

            if (result == ContentDialogResult.Primary && registrationDialog.Result != null)
            {
                // User was created successfully
                var userResult = registrationDialog.Result;
                System.Diagnostics.Debug.WriteLine($"User created successfully: {userResult.User.Email} with role: {userResult.Role}");

                // Optionally auto-login the new user
                if (_viewModel != null)
                {
                    _viewModel.MasterPassword = userResult.MasterPassword;
                    _viewModel.SelectedUser = new UserDto
                    {
                        Id = userResult.User.Id,
                        Email = userResult.User.Email!,
                        FirstName = userResult.User.FirstName!,
                        LastName = userResult.User.LastName!,
                        IsActive = userResult.User.IsActive
                    };
                    _viewModel.ShowProfileSelection = false;

                    // Attempt to authenticate with the new user
                    var success = await _viewModel.AuthenticateAsync();
                    if (success)
                    {
                        if (GetMainWindow() is MainWindow mainWindow)
                        {
                            mainWindow.NavigateToHome();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing create profile dialog: {ex.Message}");
        }
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
            registrationDialog.XamlRoot = this.XamlRoot;

            var result = await registrationDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                // Registration was successful, refresh the login page
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
        var errorDialog = new ContentDialog
        {
            Title = "Error",
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        await errorDialog.ShowAsync();
    }

    private async Task ShowSuccessMessage(string message)
    {
        var successDialog = new ContentDialog
        {
            Title = "Success",
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        await successDialog.ShowAsync();
    }

    #endregion
}