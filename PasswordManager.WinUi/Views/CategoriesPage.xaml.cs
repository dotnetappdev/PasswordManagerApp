using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.WinUi.ViewModels;
using PasswordManager.Models;

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
        await ShowAddCategoryDialog();
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
        if (sender is Button button && button.DataContext is Category category)
        {
            await ShowEditCategoryDialog(category);
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
                await _viewModel.DeleteCategoryAsync(category);
            }
        }
    }

    private async Task ShowAddCategoryDialog()
    {
        var nameTextBox = new TextBox
        {
            Header = "Category Name",
            PlaceholderText = "Enter category name"
        };

        var descriptionTextBox = new TextBox
        {
            Header = "Description (Optional)",
            PlaceholderText = "Enter description",
            AcceptsReturn = true,
            MaxHeight = 80
        };

        var stackPanel = new StackPanel { Spacing = 16 };
        stackPanel.Children.Add(nameTextBox);
        stackPanel.Children.Add(descriptionTextBox);

        var dialog = new ContentDialog
        {
            Title = "Add Category",
            Content = stackPanel,
            PrimaryButtonText = "Add",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && _viewModel != null)
        {
            if (!string.IsNullOrWhiteSpace(nameTextBox.Text))
            {
                await _viewModel.CreateCategoryAsync(nameTextBox.Text, descriptionTextBox.Text);
            }
        }
    }

    private async Task ShowEditCategoryDialog(Category category)
    {
        var nameTextBox = new TextBox
        {
            Header = "Category Name",
            PlaceholderText = "Enter category name",
            Text = category.Name
        };

        var descriptionTextBox = new TextBox
        {
            Header = "Description (Optional)",
            PlaceholderText = "Enter description",
            Text = category.Description ?? string.Empty,
            AcceptsReturn = true,
            MaxHeight = 80
        };

        var stackPanel = new StackPanel { Spacing = 16 };
        stackPanel.Children.Add(nameTextBox);
        stackPanel.Children.Add(descriptionTextBox);

        var dialog = new ContentDialog
        {
            Title = "Edit Category",
            Content = stackPanel,
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && _viewModel != null)
        {
            if (!string.IsNullOrWhiteSpace(nameTextBox.Text))
            {
                category.Name = nameTextBox.Text;
                category.Description = descriptionTextBox.Text;
                await _viewModel.UpdateCategoryAsync(category);
            }
        }
    }
}