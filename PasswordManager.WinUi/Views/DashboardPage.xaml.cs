using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Services.Interfaces;
using PasswordManager.WinUi.ViewModels;
using PasswordManager.Models;
using System;
using System.Diagnostics;

namespace PasswordManager.WinUi.Views;

/// <summary>
/// Main dashboard page showing password items and navigation
/// </summary>
public sealed partial class DashboardPage : Page
{
    private IServiceProvider? _serviceProvider;
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

    private MainWindow? GetMainWindow()
    {
        // Use the MainWindow property exposed in App
        return (App.Current as App)?.MainWindow;
    }

    // New event handlers for 3-column layout
    
    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        // TODO: Implement search functionality
        if (_viewModel != null && !string.IsNullOrWhiteSpace(sender.Text))
        {
            // Filter items based on search text
        }
    }
    
    private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        // TODO: Handle search query submission
    }
    
    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            if (_viewModel.IsEditing)
            {
                _viewModel.StopEditing();
            }
            else
            {
                _viewModel.StartEditing();
            }
        }
    }
    
    private void NavigationButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string navItem)
        {
            if (_viewModel != null)
            {
                _viewModel.SelectedNavItem = navItem;
            }
        }
    }
    
    private void PasswordItemsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView listView && listView.SelectedItem is PasswordItem selectedItem)
        {
            _viewModel?.SelectPasswordItem(selectedItem);
        }
    }
    
    // Action buttons for password detail form
    
    private void CopyUsernameButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel?.SelectedPasswordItem?.Username is string username)
        {
            CopyToClipboard(username);
        }
    }
    
    private void CopyPasswordButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel?.SelectedPasswordItem?.Password is string password)
        {
            CopyToClipboard(password);
        }
    }
    
    private void TogglePasswordVisibilityButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement password visibility toggle
        // This would need custom implementation to show/hide password text
    }
    
    private void OpenWebsiteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel?.SelectedPasswordItem?.Website is string website)
        {
            try
            {
                var uri = new Uri(website);
                Process.Start(new ProcessStartInfo(uri.ToString()) { UseShellExecute = true });
            }
            catch
            {
                // Fallback for URLs without protocol
                try
                {
                    var uri = new Uri($"https://{website}");
                    Process.Start(new ProcessStartInfo(uri.ToString()) { UseShellExecute = true });
                }
                catch
                {
                    // Handle error - could show a message to user
                }
            }
        }
    }
    
    private void CopyToClipboard(string text)
    {
        try
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(text);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }
        catch
        {
            // Handle clipboard error
        }
    }
}