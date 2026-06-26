using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using VaultGuard.WPF.ViewModels;
using VaultGuard.Models;
using VaultGuard.WPF.Dialogs;
using System;
using System.Threading.Tasks;

namespace VaultGuard.WPF.Views;

public sealed partial class CategoriesPage : Page
{
    private CategoriesViewModel? _viewModel;
    private IServiceProvider? _serviceProvider;

    public CategoriesPage()
    {
        this.InitializeComponent();
    }

    // Helper to prefer the main window XamlRoot so dialogs center on the app window
    // WPF: null removed - not needed

    /// <summary>
    /// Helper method to properly configure dialog for centering
    /// </summary>
    private void ConfigureDialogForCentering(ModernWpf.Controls.ContentDialog dialog)
    {
        try
        {
            // WPF: XamlRoot not needed in WPF
            
            // Ensure the dialog uses the proper style for centering if it doesn't have one already
            if (dialog.Style == null)
            {
                // Apply the Modern1PasswordDialogStyle from resources
                if (Application.Current.Resources.Contains("Modern1PasswordDialogStyle"))
                {
                    dialog.Style = Application.Current.Resources["Modern1PasswordDialogStyle"] as Style;
                }
            }
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Error($"Failed to apply dialog centering style", ex);
        }
    }

    public void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
    {
        // Note: WPF Page doesn't have base.OnNavigatedTo
        if (e.ExtraData is IServiceProvider serviceProvider)
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
            if (result == ModernWpf.Controls.ContentDialogResult.Primary && dialog.Result != null && _viewModel != null)
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
                if (result == ModernWpf.Controls.ContentDialogResult.Primary && dialog.Result != null && _viewModel != null)
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
                var categoryService = _serviceProvider.GetRequiredService<VaultGuard.Services.Interfaces.ICategoryInterface>();
                var hasPasswordItems = await categoryService.HasPasswordItemsAsync(category.Id);
                
                if (hasPasswordItems)
                {
                    var count = await categoryService.GetPasswordItemCountAsync(category.Id);
                    var warningDialog = new ModernWpf.Controls.ContentDialog
                    {
                        Title = "Cannot Delete Category",
                        Content = $"The category '{category.Name}' cannot be deleted because it contains {count} password item{(count == 1 ? "" : "s")}. Please move or delete the password items first.",
                        CloseButtonText = "OK"
                    };
                    ConfigureDialogForCentering(warningDialog);
                    await warningDialog.ShowAsync();
                    return;
                }

                var dialog = new ModernWpf.Controls.ContentDialog
                {
                    Title = "Delete Category",
                    Content = $"Are you sure you want to delete '{category.Name}'? This action cannot be undone.",
                    PrimaryButtonText = "Delete",
                    CloseButtonText = "Cancel",
                    DefaultButton = ModernWpf.Controls.ContentDialogButton.Close
                };
                ConfigureDialogForCentering(dialog);

                var result = await dialog.ShowAsync();
                if (result == ModernWpf.Controls.ContentDialogResult.Primary && _viewModel != null)
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
                var categoryService = _serviceProvider.GetRequiredService<VaultGuard.Services.Interfaces.ICategoryInterface>();
                category.IsFavorite = !category.IsFavorite;
                await categoryService.UpdateAsync(category);
                
                if (_viewModel != null)
                {
                    await _viewModel.RefreshAsync();
                }
                
                // Log success to debug output
                var message = category.IsFavorite 
                    ? $"'{category.Name}' added to favorites" 
                    : $"'{category.Name}' removed from favorites";
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"Error updating category: {ex.Message}");
            }
        }
    }

    private async Task ShowErrorDialog(string message)
    {
        var errorDialog = new ModernWpf.Controls.ContentDialog
        {
            Title = "Error",
            Content = message,
            CloseButtonText = "OK"
        };
        ConfigureDialogForCentering(errorDialog);
        await errorDialog.ShowAsync();
    }
}
