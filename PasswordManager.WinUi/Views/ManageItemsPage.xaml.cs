using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Models;
using PasswordManager.WinUi.Dialogs;
using PasswordManager.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace PasswordManager.WinUi.Views;

public sealed partial class ManageItemsPage : Page
{
    private IServiceProvider? _serviceProvider;
    private ICategoryInterface? _categoryService;
    private ITagService? _tagService;
    
    private ObservableCollection<Category> _collections = new();
    private ObservableCollection<string> _types = new();
    private ObservableCollection<Tag> _tags = new();

    public ManageItemsPage()
    {
        this.InitializeComponent();
        
        // Initialize collections
        CollectionsListView.ItemsSource = _collections;
        TypesListView.ItemsSource = _types;
        TagsListView.ItemsSource = _tags;
        
        // Load initial data with predefined types
        LoadPredefinedTypes();
    }

    protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        if (e.Parameter is IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            
            try
            {
                _categoryService = serviceProvider.GetService<ICategoryInterface>();
                _tagService = serviceProvider.GetService<ITagService>();
                
                await LoadAllData();
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"Error initializing page: {ex.Message}");
            }
        }
    }

    private void LoadPredefinedTypes()
    {
        _types.Clear();
        _types.Add("Login");
        _types.Add("Credit Card");
        _types.Add("Secure Note");
        _types.Add("WiFi Password");
        _types.Add("Passkey");
        _types.Add("Identity");
        _types.Add("Document");
    }

    private async Task LoadAllData()
    {
        await LoadCollections();
        await LoadTags();
    }

    private async Task LoadCollections()
    {
    if (_categoryService is null) return;

        try
        {
            var categories = await _categoryService.GetAllAsync();
            _collections.Clear();
            foreach (var category in categories)
            {
                _collections.Add(category);
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialog($"Error loading collections: {ex.Message}");
        }
    }

    private async Task LoadTags()
    {
        if (_tagService == null) return;

        try
        {
            var tags = await _tagService.GetAllAsync();
            _tags.Clear();
            foreach (var tag in tags)
            {
                _tags.Add(tag);
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialog($"Error loading tags: {ex.Message}");
        }
    }

    private async void RefreshAllButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadAllData();
    }

    // Collection Management
    private async void AddCollectionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_serviceProvider == null) return;

        try
        {
            var dialog = new CategoryDialog(_serviceProvider);
            dialog.XamlRoot = this.XamlRoot;
            
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && dialog.Result != null)
            {
                await LoadCollections();
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialog($"Error adding collection: {ex.Message}");
        }
    }

    private async void EditCollectionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is Category category && _serviceProvider != null)
        {
            try
            {
                var dialog = new CategoryDialog(_serviceProvider, category);
                dialog.XamlRoot = this.XamlRoot;
                
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary && dialog.Result != null)
                {
                    await LoadCollections();
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"Error editing collection: {ex.Message}");
            }
        }
    }

    private async void DeleteCollectionButton_Click(object sender, RoutedEventArgs e)
    {
    if (sender is Button button && button.DataContext is Category category && _categoryService is not null)
        {
            var confirmDialog = new ContentDialog
            {
                Title = "Delete Collection",
                Content = $"Are you sure you want to delete '{category.Name}'? This action cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    await _categoryService.DeleteAsync(category.Id);
                    await LoadCollections();
                }
                catch (Exception ex)
                {
                    await ShowErrorDialog($"Error deleting collection: {ex.Message}");
                }
            }
        }
    }

    // Type Management
    private async void AddTypeButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new TypeDialog();
            dialog.XamlRoot = this.XamlRoot;
            
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && !string.IsNullOrEmpty(dialog.Result))
            {
                if (!_types.Contains(dialog.Result))
                {
                    _types.Add(dialog.Result);
                }
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialog($"Error adding type: {ex.Message}");
        }
    }

    private async void EditTypeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is string typeName)
        {
            try
            {
                var dialog = new TypeDialog(typeName);
                dialog.XamlRoot = this.XamlRoot;
                
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary && !string.IsNullOrEmpty(dialog.Result))
                {
                    var index = _types.IndexOf(typeName);
                    if (index >= 0)
                    {
                        _types[index] = dialog.Result;
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"Error editing type: {ex.Message}");
            }
        }
    }

    private async void DeleteTypeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is string typeName)
        {
            var confirmDialog = new ContentDialog
            {
                Title = "Delete Type",
                Content = $"Are you sure you want to delete '{typeName}'? This action cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                _types.Remove(typeName);
            }
        }
    }

    // Tag Management
    private async void AddTagButton_Click(object sender, RoutedEventArgs e)
    {
        if (_serviceProvider == null) return;

        try
        {
            var dialog = new TagDialog(_serviceProvider);
            dialog.XamlRoot = this.XamlRoot;
            
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && dialog.Result != null)
            {
                await LoadTags();
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialog($"Error adding tag: {ex.Message}");
        }
    }

    private async void EditTagButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is Tag tag && _serviceProvider != null)
        {
            try
            {
                var dialog = new TagDialog(_serviceProvider, tag);
                dialog.XamlRoot = this.XamlRoot;
                
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary && dialog.Result != null)
                {
                    await LoadTags();
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"Error editing tag: {ex.Message}");
            }
        }
    }

    private async void DeleteTagButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is Tag tag && _tagService != null)
        {
            var confirmDialog = new ContentDialog
            {
                Title = "Delete Tag",
                Content = $"Are you sure you want to delete '{tag.Name}'? This action cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    await _tagService.DeleteAsync(tag.Id);
                    await LoadTags();
                }
                catch (Exception ex)
                {
                    await ShowErrorDialog($"Error deleting tag: {ex.Message}");
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