using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Services.Interfaces;
using PasswordManager.WinUi.ViewModels;
using System;

namespace PasswordManager.WinUi.Views;

/// <summary>
/// Login page for the Password Manager application
/// </summary>
public sealed partial class LoginPage : Page
{
    private IServiceProvider? _serviceProvider;
    private LoginViewModel? _viewModel;

    public LoginPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _viewModel = new LoginViewModel(serviceProvider);
            this.DataContext = _viewModel;
        }
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;

        // Resolve UI elements once for this handler
        var loginButton = this.FindName("LoginButton") as Button;
        var loginProgressRing = this.FindName("LoginProgressRing") as ProgressRing;
        var errorText = this.FindName("ErrorText") as TextBlock;
        var emailTextBox = this.FindName("EmailTextBox") as TextBox;
        var passwordBox = this.FindName("PasswordBox") as PasswordBox;

        try
        {
            if (loginButton != null) loginButton.IsEnabled = false;
            if (loginProgressRing != null) loginProgressRing.IsActive = true;
            if (errorText != null) errorText.Visibility = Visibility.Collapsed;

            // Update ViewModel with current values
            _viewModel.Username = emailTextBox?.Text ?? string.Empty;
            _viewModel.Password = passwordBox?.Password ?? string.Empty;

            // Attempt login
            var success = await _viewModel.LoginAsync();

            if (success)
            {
                // Navigate to main dashboard via MainWindow
                if (GetMainWindow() is MainWindow mainWindow)
                {
                    mainWindow.NavigateToHome();
                }
                else
                {
                    // Fallback navigation
                    this.Frame?.Navigate(typeof(DashboardPage), _serviceProvider);
                }
            }
            else
            {
                // Show error message from ViewModel
                if (errorText != null)
                {
                    errorText.Text = _viewModel.ErrorMessage;
                    errorText.Visibility = Visibility.Visible;
                }
            }
        }
        catch (Exception ex)
        {
            if (errorText != null)
            {
                errorText.Text = $"Login failed: {ex.Message}";
                errorText.Visibility = Visibility.Visible;
            }
        }
        finally
        {
            if (loginButton != null) loginButton.IsEnabled = true;
            if (loginProgressRing != null) loginProgressRing.IsActive = false;
        }
    }

    private async void CreateAccountButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;

        // Resolve UI elements once for this handler
        var loginButton = this.FindName("LoginButton") as Button;
        var loginProgressRing = this.FindName("LoginProgressRing") as ProgressRing;
        var errorText = this.FindName("ErrorText") as TextBlock;
        var emailTextBox = this.FindName("EmailTextBox") as TextBox;
        var passwordBox = this.FindName("PasswordBox") as PasswordBox;

        try
        {
            if (loginButton != null) loginButton.IsEnabled = false;
            if (loginProgressRing != null) loginProgressRing.IsActive = true;
            if (errorText != null) errorText.Visibility = Visibility.Collapsed;

            // Update ViewModel with current values
            _viewModel.Username = emailTextBox?.Text ?? string.Empty;
            _viewModel.Password = passwordBox?.Password ?? string.Empty;

            // Attempt registration
            var success = await _viewModel.RegisterAsync();

            if (success)
            {
                // Navigate to main dashboard via MainWindow
                if (GetMainWindow() is MainWindow mainWindow)
                {
                    mainWindow.NavigateToHome();
                }
                else
                {
                    // Fallback navigation
                    this.Frame?.Navigate(typeof(DashboardPage), _serviceProvider);
                }
            }
            else
            {
                // Show error message from ViewModel
                if (errorText != null)
                {
                    errorText.Text = _viewModel.ErrorMessage;
                    errorText.Visibility = Visibility.Visible;
                }
            }
        }
        catch (Exception ex)
        {
            if (errorText != null)
            {
                errorText.Text = $"Registration failed: {ex.Message}";
                errorText.Visibility = Visibility.Visible;
            }
        }
        finally
        {
            if (loginButton != null) loginButton.IsEnabled = true;
            if (loginProgressRing != null) loginProgressRing.IsActive = false;
        }
    }

    private MainWindow? GetMainWindow()
    {
        // Use the MainWindow property exposed in App
        return (App.Current as App)?.MainWindow;
    }
}