using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.WinUi.ViewModels;
using PasswordManager.Models;
using PasswordManager.WinUi.Dialogs;
using System;
using System.Threading.Tasks;

namespace PasswordManager.WinUi.Views;

public sealed partial class CategoriesPage : Page
{
    private CategoriesViewModel? _viewModel;
    private IServiceProvider? _serviceProvider;

    public CategoriesPage()
    {
        this.InitializeComponent();
    }

    // Helper to prefer the main window XamlRoot so dialogs center on the app window
    private Microsoft.UI.Xaml.XamlRoot? GetMainXamlRoot()
    {
        return (App.Current as App)?.MainWindow?.Content?.XamlRoot;
    }

    /// <summary>
    /// Helper method to properly configure dialog for centering
    /// </summary>
    private void ConfigureDialogForCentering(ContentDialog dialog)
    {
        try
        {
            // Set XamlRoot to the main window's content for proper centering
            var mainXamlRoot = GetMainXamlRoot();
            if (mainXamlRoot != null)
            {
                dialog.XamlRoot = mainXamlRoot;
            }
            else if (this.XamlRoot != null)
            {
                dialog.XamlRoot = this.XamlRoot;
            }
            
            // Ensure the dialog uses the proper style for centering if it doesn't have one already
            if (dialog.Style == null)
            {
                // Apply the Modern1PasswordDialogStyle from resources
                if (Application.Current.Resources.ContainsKey("Modern1PasswordDialogStyle"))
                {
                    dialog.Style = Application.Current.Resources["Modern1PasswordDialogStyle"] as Style;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error configuring dialog centering: {ex.Message}");
            // Fallback to page XamlRoot
            if (this.XamlRoot != null)
            {
                dialog.XamlRoot = this.XamlRoot;
            }
        }
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        if (e.Parameter is IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _viewModel = new CategoriesViewModel(serviceProvider);
            this.DataContext = _viewModel;
        }
    }

    private async void AddCategoryButton_Click(object sender, RoutedEventArgs e)
    {
        if (_serviceProvider == null) return;

        try
        {
            var dialog = new CategoryDialog(_serviceProvider);
            ConfigureDialogForCentering(dialog);
            
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && dialog.Result != null && _viewModel != null)
            {
                await _viewModel.RefreshAsync();
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialog($"Error adding category: {ex.Message}");
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            await _viewModel.RefreshAsync();
        }
    }

    private void CategoriesGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel != null && CategoriesGridView.SelectedItem is Category category)
        {
            _viewModel.SelectedCategory = category;
        }
    }

    private async void EditCategoryButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is Category category && _serviceProvider != null)
        {
            try
            {
                var dialog = new CategoryDialog(_serviceProvider, category);
                ConfigureDialogForCentering(dialog);
                
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary && dialog.Result != null && _viewModel != null)
                {
                    await _viewModel.RefreshAsync();
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"Error editing category: {ex.Message}");
            }
        }
    }

    private async void DeleteCategoryButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is Category category && _serviceProvider != null)
        {
            try
            {
                // Check if category has password items
                var categoryService = _serviceProvider.GetRequiredService<PasswordManager.Services.Interfaces.ICategoryInterface>();
                var hasPasswordItems = await categoryService.HasPasswordItemsAsync(category.Id);
                
                if (hasPasswordItems)
                {
                    var count = await categoryService.GetPasswordItemCountAsync(category.Id);
                    var warningDialog = new ContentDialog
                    {
                        Title = "Cannot Delete Category",
                        Content = $"The category '{category.Name}' cannot be deleted because it contains {count} password item{(count == 1 ? "" : "s")}. Please move or delete the password items first.",
                        CloseButtonText = "OK"
                    };
                    ConfigureDialogForCentering(warningDialog);
                    await warningDialog.ShowAsync();
                    return;
                }

                var dialog = new ContentDialog
                {
                    Title = "Delete Category",
                    Content = $"Are you sure you want to delete '{category.Name}'? This action cannot be undone.",
                    PrimaryButtonText = "Delete",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Close
                };
                ConfigureDialogForCentering(dialog);

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary && _viewModel != null)
                {
                    await _viewModel.DeleteCategoryAsync(category);
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"Error deleting category: {ex.Message}");
            }
        }
    }
    
    private async void ToggleFavoriteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is Category category && _serviceProvider != null)
        {
            try
            {
                var categoryService = _serviceProvider.GetRequiredService<PasswordManager.Services.Interfaces.ICategoryInterface>();
                category.IsFavorite = !category.IsFavorite;
                await categoryService.UpdateAsync(category);
                
                if (_viewModel != null)
                {
                    await _viewModel.RefreshAsync();
                }
                
                // Show toast notification
                var message = category.IsFavorite 
                    ? $"'{category.Name}' added to favorites" 
                    : $"'{category.Name}' removed from favorites";
                    
                var infoDialog = new ContentDialog
                {
                    Title = "Success",
                    Content = message,
                    CloseButtonText = "OK"
                };
                ConfigureDialogForCentering(infoDialog);
                await infoDialog.ShowAsync();
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"Error updating category: {ex.Message}");
            }
        }
    }

    private async Task ShowErrorDialog(string message)
    {
        var errorDialog = new ContentDialog
        {
            Title = "Error",
            Content = message,
            CloseButtonText = "OK"
        };
        ConfigureDialogForCentering(errorDialog);
        await errorDialog.ShowAsync();
    }
}