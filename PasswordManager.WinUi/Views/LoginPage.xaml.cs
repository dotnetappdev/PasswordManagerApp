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
    private readonly IServiceProvider _serviceProvider;
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

        try
        {
            LoginButton.IsEnabled = false;
            LoginProgressRing.IsActive = true;
            ErrorText.Visibility = Visibility.Collapsed;

            // Update ViewModel with current values
            _viewModel.Email = EmailTextBox.Text;
            _viewModel.Password = PasswordBox.Password;

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
                    Frame.Navigate(typeof(DashboardPage), _serviceProvider);
                }
            }
            else
            {
                // Show error message from ViewModel
                ErrorText.Text = _viewModel.ErrorMessage;
                ErrorText.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"Login failed: {ex.Message}";
            ErrorText.Visibility = Visibility.Visible;
        }
        finally
        {
            LoginButton.IsEnabled = true;
            LoginProgressRing.IsActive = false;
        }
    }

    private async void CreateAccountButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;

        try
        {
            LoginButton.IsEnabled = false;
            LoginProgressRing.IsActive = true;
            ErrorText.Visibility = Visibility.Collapsed;

            // Update ViewModel with current values
            _viewModel.Email = EmailTextBox.Text;
            _viewModel.Password = PasswordBox.Password;

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
                    Frame.Navigate(typeof(DashboardPage), _serviceProvider);
                }
            }
            else
            {
                // Show error message from ViewModel
                ErrorText.Text = _viewModel.ErrorMessage;
                ErrorText.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"Registration failed: {ex.Message}";
            ErrorText.Visibility = Visibility.Visible;
        }
        finally
        {
            LoginButton.IsEnabled = true;
            LoginProgressRing.IsActive = false;
        }
    }

    private Window? GetMainWindow()
    {
        // Walk up the visual tree to find the main window
        var current = this.XamlRoot?.Content;
        while (current != null)
        {
            if (current is MainWindow mainWindow)
                return mainWindow;
            
            if (current is FrameworkElement element)
                current = element.Parent;
            else
                break;
        }

        // Fallback: try to get from App
        return App.Current?.MainWindow;
    }
}