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
            dialog.XamlRoot = this.XamlRoot;
            
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
                dialog.XamlRoot = this.XamlRoot;
                
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
        if (sender is Button button && button.DataContext is Category category)
        {
            var dialog = new ContentDialog
            {
                Title = "Delete Category",
                Content = $"Are you sure you want to delete '{category.Name}'? This action cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && _viewModel != null)
            {
                try
                {
                    await _viewModel.DeleteCategoryAsync(category);
                }
                catch (Exception ex)
                {
                    await ShowErrorDialog($"Error deleting category: {ex.Message}");
                }
            }
        }
    }

    private async Task ShowErrorDialog(string message)
    {
        var errorDialog = new ContentDialog
        {
            Title = "Error",
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        await errorDialog.ShowAsync();
    }
}