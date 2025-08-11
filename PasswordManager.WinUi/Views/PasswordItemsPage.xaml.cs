using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.WinUi.ViewModels;
using PasswordManager.Models;

namespace PasswordManager.WinUi.Views;

public sealed partial class PasswordItemsPage : Page
{
    private readonly PasswordItemsViewModel _viewModel;

    public PasswordItemsPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        if (e.Parameter is IServiceProvider serviceProvider)
        {
            _viewModel = new PasswordItemsViewModel(serviceProvider);
            this.DataContext = _viewModel;
        }
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_viewModel != null && sender is TextBox textBox)
        {
            _viewModel.SearchText = textBox.Text;
        }
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
        var dialog = new ContentDialog
        {
            Title = "Add Password Item",
            Content = "Add password functionality would be implemented here",
            CloseButtonText = "Cancel",
            PrimaryButtonText = "Add",
            XamlRoot = this.XamlRoot
        };

        await dialog.ShowAsync();
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
            var dialog = new ContentDialog
            {
                Title = "Edit Password Item",
                Content = $"Edit functionality for '{item.Title}' would be implemented here",
                CloseButtonText = "Cancel",
                PrimaryButtonText = "Save",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }

    private void PasswordItemsList_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if (PasswordItemsList.SelectedItem is PasswordItem item)
        {
            // Open password details view
            ShowPasswordDetails(item);
        }
    }

    private async void ShowPasswordDetails(PasswordItem item)
    {
        var dialog = new ContentDialog
        {
            Title = item.Title,
            Content = $"Details for '{item.Title}' would be shown here.\n\nType: {item.Type}\nCreated: {item.CreatedAt:g}\nModified: {item.LastModified:g}",
            CloseButtonText = "Close",
            XamlRoot = this.XamlRoot
        };

        await dialog.ShowAsync();
    }
}