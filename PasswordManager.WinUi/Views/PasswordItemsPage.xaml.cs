using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.WinUi.ViewModels;
using PasswordManager.Models;
using System.Linq;

namespace PasswordManager.WinUi.Views;

public sealed partial class PasswordItemsPage : Page
{
    private PasswordItemsViewModel? _viewModel;
    private IServiceProvider? _serviceProvider;
    private PasswordItem? _selectedItem;

    public PasswordItemsPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        if (e.Parameter is IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _viewModel = new PasswordItemsViewModel(serviceProvider);
            this.DataContext = _viewModel;
        }
    }

    public void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_viewModel != null && sender is TextBox textBox)
        {
            _viewModel.SearchText = textBox.Text;
        }
    }

    private void FilterButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string filterType)
        {
            // Update UI to show selected filter
            UpdateFilterButtonStyles(button);
            
            // Apply filter to view model
            if (_viewModel != null)
            {
                _viewModel.FilterType = filterType;
            }
            
            // Update content titles
            UpdateContentTitles(filterType);
        }
    }
    
    private void UpdateFilterButtonStyles(Button selectedButton)
    {
        // Reset all filter buttons to normal style
        AllItemsButton.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
        
        // Find all filter buttons and reset them
        var buttons = new[] { AllItemsButton };
        foreach (var button in buttons)
        {
            button.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
        }
        
        // Set selected button style
        selectedButton.Background = Application.Current.Resources["ModernPrimaryBrush"] as Microsoft.UI.Xaml.Media.Brush;
    }
    
    private void UpdateContentTitles(string filterType)
    {
        switch (filterType)
        {
            case "All":
                ContentTitle.Text = "All Items";
                ContentSubtitle.Text = "Showing all password items";
                break;
            case "Favorites":
                ContentTitle.Text = "Favorites";
                ContentSubtitle.Text = "Your favorite password items";
                break;
            case "Recent":
                ContentTitle.Text = "Recently Used";
                ContentSubtitle.Text = "Recently accessed items";
                break;
            case "Login":
                ContentTitle.Text = "Logins";
                ContentSubtitle.Text = "Login credentials";
                break;
            case "CreditCard":
                ContentTitle.Text = "Credit Cards";
                ContentSubtitle.Text = "Payment card information";
                break;
            case "SecureNote":
                ContentTitle.Text = "Secure Notes";
                ContentSubtitle.Text = "Private notes and documents";
                break;
        }
    }

    private void CategoryDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            var category = selectedItem.Content.ToString();
            if (_viewModel != null)
            {
                _viewModel.SelectedCategory = category;
            }
        }
    }

    private void ItemsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView listView && listView.SelectedItem is PasswordItem selectedItem)
        {
            _selectedItem = selectedItem;
            ShowItemDetails(selectedItem);
            
            // Sync with cards view
            PasswordCardsView.SelectedItem = selectedItem;
        }
    }

    private void PasswordCardsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is GridView gridView && gridView.SelectedItem is PasswordItem selectedItem)
        {
            _selectedItem = selectedItem;
            ShowItemDetails(selectedItem);
            
            // Sync with list view
            ItemsList.SelectedItem = selectedItem;
        }
    }

    private void ShowItemDetails(PasswordItem item)
    {
        if (item == null) return;

        // Show detail panel
        DetailPanel.Visibility = Visibility.Visible;
        
        // Update detail content
        DetailTitle.Text = "Item Details";
        DetailSubtitle.Text = $"Details for {item.Title}";
        DetailItemTitle.Text = item.Title;
        DetailItemSubtitle.Text = item.Description ?? item.Username ?? "No additional information";
        DetailUsername.Text = item.Username ?? "";
        DetailWebsite.Text = item.Website ?? "";
        
        // Update icon based on type
        DetailIcon.Text = GetTypeIcon(item.Type.ToString());
    }

    private string GetTypeIcon(string type)
    {
        return type?.ToLower() switch
        {
            "login" => "🔑",
            "creditcard" => "💳", 
            "securenote" => "📝",
            "wifi" => "📶",
            _ => "🔒"
        };
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            await _viewModel.RefreshAsync();
        }
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        // Navigate to add password page
        // Frame.Navigate(typeof(AddPasswordItemPage), serviceProvider);
        ShowAddPasswordDialog();
    }

    private async void ShowAddPasswordDialog()
    {
        try
        {
            // Check if service provider is available
            if (_serviceProvider == null)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = "Service provider not initialized. Please navigate to this page properly.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
                return;
            }

            // First show the item type selection dialog (1Password style)
            var typeSelectionDialog = new Dialogs.ItemTypeSelectionDialog();
            typeSelectionDialog.XamlRoot = this.XamlRoot;

            var typeResult = await typeSelectionDialog.ShowAsync();
            if (typeResult == ContentDialogResult.Primary || typeSelectionDialog.SelectedItemType != null)
            {
                // Then show the main add dialog with the selected type pre-filled
                var dialog = new Dialogs.AddPasswordDialog(_serviceProvider);
                dialog.XamlRoot = this.XamlRoot;

                // Pre-select the item type if one was chosen
                if (typeSelectionDialog.SelectedItemType.HasValue)
                {
                    // Pass the selected type to the dialog
                    dialog.SetInitialItemType(typeSelectionDialog.SelectedItemType.Value, typeSelectionDialog.SelectedCategoryName);
                }

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary && dialog.Result != null)
                {
                    // Refresh the list to show the new item
                    if (_viewModel != null)
                    {
                        await _viewModel.RefreshAsync();
                    }
                }
            }
        }  
        catch (Exception ex)
        {
            var errorDialog = new ContentDialog
            {
                Title = "Error",
                Content = $"Error adding password: {ex.Message}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await errorDialog.ShowAsync();
        }
    }

    private async void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuItem && 
            menuItem.DataContext is PasswordItem item &&
            _viewModel != null)
        {
            var dialog = new ContentDialog
            {
                Title = "Delete Password Item",
                Content = $"Are you sure you want to delete '{item.Title}'?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await _viewModel.DeleteItemAsync(item);
            }
        }
    }

    private async void EditMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuItem && 
            menuItem.DataContext is PasswordItem item)
        {
            try
            {
                // Check if service provider is available
                if (_serviceProvider == null)
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "Error",
                        Content = "Service provider not initialized. Please navigate to this page properly.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                    return;
                }

                var dialog = new Dialogs.AddPasswordDialog(_serviceProvider, item);
                dialog.XamlRoot = this.XamlRoot;
                
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary && dialog.Result != null)
                {
                    // Refresh the list to show the updated item
                    if (_viewModel != null)
                    {
                        await _viewModel.RefreshAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Error editing password: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }
    }

    private void AddCategoryButton_Click(object sender, RoutedEventArgs e)
    {
        // Navigate to Categories page via main window
        try
        {
            var mainWindow = GetMainWindow();
            if (mainWindow != null)
            {
                mainWindow.NavigateToPage("Categories");
            }
            else
            {
                // Fallback navigation
                Frame?.Navigate(typeof(CategoriesPage), _serviceProvider);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error navigating to Categories page: {ex.Message}");
        }
    }

    private MainWindow? GetMainWindow()
    {
        // Use the MainWindow property exposed in App
        return (App.Current as App)?.MainWindow;
    }

    private void ItemsList_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if (ItemsList.SelectedItem is PasswordItem item)
        {
            // Open password details view
            ShowPasswordDetails(item);
        }
    }

    private async void ShowPasswordDetails(PasswordItem item)
    {
        try
        {
            var dialog = new Dialogs.PasswordDetailsDialog(_serviceProvider, item);
            dialog.XamlRoot = this.XamlRoot;
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            var errorDialog = new ContentDialog
            {
                Title = "Error",
                Content = $"Error showing password details: {ex.Message}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await errorDialog.ShowAsync();
        }
    }
}