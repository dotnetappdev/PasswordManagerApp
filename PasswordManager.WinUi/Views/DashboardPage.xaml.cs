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
    // Switch to inline create mode
    _viewModel?.BeginCreate();
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
        if (_viewModel == null) return;
        if (_viewModel.SelectedPasswordItem is not null)
        {
            _viewModel.BeginEdit();
        }
    }
    
    
    private void PasswordItemsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView listView)
        {
            var selectedItem = listView.SelectedItem as PasswordItem;
            if (selectedItem is not null)
            {
                _viewModel?.SelectPasswordItem(selectedItem);
            }
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
        if (passwordField is not null && _viewModel?.SelectedPasswordItem is not null)
        {
            // Create a TextBox to replace PasswordBox for visibility
            var parentGrid = passwordField.Parent as Grid;
            if (parentGrid is not null)
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
                     }
                    else
                    {
                        // Show password
                        button.Content = "🙈";
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

    private void InlinePasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_viewModel?.EditItem is PasswordItem item && sender is PasswordBox pb)
        {
            item.Password = pb.Password;
        }
    }

    private async void SaveInlineButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            await _viewModel.SaveAsync();
        }
    }

    private void CancelInlineButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel?.CancelEdit();
    }
}