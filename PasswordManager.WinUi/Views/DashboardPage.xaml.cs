using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Services.Interfaces;
using PasswordManager.WinUi.ViewModels;
using System;

namespace PasswordManager.WinUi.Views;

/// <summary>
/// Main dashboard page showing password items and navigation
/// </summary>
public sealed partial class DashboardPage : Page
{
    private readonly IServiceProvider _serviceProvider;
    private DashboardViewModel? _viewModel;

    public DashboardPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        if (e.Parameter is IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _viewModel = new DashboardViewModel(serviceProvider);
            this.DataContext = _viewModel;
        }
    }

    private void AddPasswordButton_Click(object sender, RoutedEventArgs e)
    {
        // Request navigation from the main window
        RequestNavigation("Passwords");
    }

    private void ViewPasswordsButton_Click(object sender, RoutedEventArgs e)
    {
        RequestNavigation("Passwords");
    }

    private void CategoriesButton_Click(object sender, RoutedEventArgs e)
    {
        RequestNavigation("Categories");
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        RequestNavigation("Settings");
    }

    private void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        RequestNavigation("Import");
    }

    private async void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            await _viewModel.LogoutAsync();
        }
        
        // Request logout from main window
        if (GetMainWindow() is MainWindow mainWindow)
        {
            mainWindow.HandleLogout();
        }
    }

    private void RequestNavigation(string pageTag)
    {
        // Find the main window and request navigation
        if (GetMainWindow() is MainWindow mainWindow)
        {
            // Use navigation method instead of directly accessing NavigationView
            mainWindow.NavigateToPage(pageTag);
        }
    }

    private Window? GetMainWindow()
    {
        // Walk up the visual tree to find the main window
        var current = this.XamlRoot?.Content as FrameworkElement;
        while (current != null)
        {
            if (current is MainWindow mainWindow)
                return mainWindow;
            
            current = current.Parent as FrameworkElement;
        }

        // Fallback: try to get the application's main window
        // Since Application doesn't have MainWindow property in WinUI, we'll try to get it from App
        if (Application.Current is App app && app != null)
        {
            // For now, return null - this would need to be implemented in the App class
            return null;
        }

        return null;
    }
}