using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Services.Interfaces;
using PasswordManager.WinUi.ViewModels;
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
            this.DataContext = _viewModel;

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

    private async void CreateAccountButton_Click(object sender, RoutedEventArgs e)
    {
        await DoPrimaryActionAsync();
    }

    #endregion
}