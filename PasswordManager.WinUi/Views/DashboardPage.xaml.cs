using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Services.Interfaces;
using PasswordManager.WinUi.ViewModels;
using PasswordManager.Models;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

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

    private async void AddPasswordButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Create service provider from main window
            var mainWindow = GetMainWindow();
            if (mainWindow?.Content is FrameworkElement element)
            {
                var serviceProvider = (element.DataContext as IServiceProvider) ?? 
                    ((App.Current as App)?.Services);
                
                if (serviceProvider != null)
                {
                    var dialog = new Dialogs.AddPasswordDialog(serviceProvider);
                    dialog.XamlRoot = this.XamlRoot;
                    
                    var result = await dialog.ShowAsync();
                    
                    if (result == ContentDialogResult.Primary && dialog.Result != null)
                    {
                        // Refresh the dashboard to show the new item
                        await _viewModel?.RefreshAsync();
                        
                        // Select the new item if it was added
                        _viewModel?.SelectPasswordItem(dialog.Result);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error opening add password dialog: {ex.Message}");
            // Fallback to navigation
            RequestNavigation("Passwords");
        }
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
    
    private async void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (_viewModel != null)
        {
            // Debounce search to avoid too many calls
            await Task.Delay(300);
            
            // Check if the search text is still the same (user might have continued typing)
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                await _viewModel.FilterPasswordItemsAsync(sender.Text);
            }
        }
    }
    
    private async void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (_viewModel != null)
        {
            await _viewModel.FilterPasswordItemsAsync(args.QueryText);
        }
    }
    
    private async void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel?.SelectedPasswordItem != null)
        {
            try
            {
                // Create service provider from main window
                var mainWindow = GetMainWindow();
                if (mainWindow?.Content is FrameworkElement element)
                {
                    var serviceProvider = (element.DataContext as IServiceProvider) ?? 
                        ((App.Current as App)?.Services);
                    
                    if (serviceProvider != null)
                    {
                        var dialog = new Dialogs.AddPasswordDialog(serviceProvider, _viewModel.SelectedPasswordItem);
                        dialog.XamlRoot = this.XamlRoot;
                        
                        var result = await dialog.ShowAsync();
                        
                        if (result == ContentDialogResult.Primary)
                        {
                            // Refresh the dashboard to show the updated item
                            await _viewModel.RefreshAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening edit password dialog: {ex.Message}");
                
                // Fallback to inline editing
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
        else if (_viewModel != null)
        {
            // Toggle inline editing mode if no item selected
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
        // Find the PasswordBox in the detail form
        var passwordField = FindName("PasswordField") as PasswordBox;
        if (passwordField != null && _viewModel?.SelectedPasswordItem != null)
        {
            // Create a TextBox to replace PasswordBox for visibility
            var parentGrid = passwordField.Parent as Grid;
            if (parentGrid != null)
            {
                var button = sender as Button;
                if (button != null)
                {
                    // Check current state by button content
                    bool isCurrentlyVisible = button.Content?.ToString() == "🙈";
                    
                    if (isCurrentlyVisible)
                    {
                        // Hide password - switch back to PasswordBox
                        button.Content = "👁";
                        button.ToolTipService.SetToolTip(button, "Show password");
                    }
                    else
                    {
                        // Show password
                        button.Content = "🙈";
                        button.ToolTipService.SetToolTip(button, "Hide password");
                    }
                }
            }
        }
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