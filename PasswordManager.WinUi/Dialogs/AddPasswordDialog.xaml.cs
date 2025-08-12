using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PasswordManager.Models;
using PasswordManager.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace PasswordManager.WinUi.Dialogs;

public sealed partial class AddPasswordDialog : ContentDialog
{
    private readonly IPasswordItemService _passwordItemService;
    private readonly ICategoryInterface _categoryService;
    private readonly ICollectionService _collectionService;
    private PasswordItem? _editingItem;

    public PasswordItem? Result { get; private set; }

    public AddPasswordDialog(IServiceProvider serviceProvider, PasswordItem? editingItem = null)
    {
        this.InitializeComponent();
        
        _passwordItemService = serviceProvider.GetRequiredService<IPasswordItemService>();
        _categoryService = serviceProvider.GetRequiredService<ICategoryInterface>();
        _collectionService = serviceProvider.GetRequiredService<ICollectionService>();
        _editingItem = editingItem;

        Title = editingItem == null ? "Add Password Item" : "Edit Password Item";
        PrimaryButtonText = editingItem == null ? "Add" : "Save";
        CloseButtonText = "Cancel";

        LoadData();
        
        if (_editingItem != null)
        {
            PopulateFields();
        }
    }

    private async void LoadData()
    {
        try
        {
            // Load categories
            var categories = await _categoryService.GetAllAsync();
            foreach (var category in categories)
            {
                CategoryComboBox.Items.Add(new ComboBoxItem 
                { 
                    Content = category.Name, 
                    Tag = category 
                });
            }

            // Load collections
            var collections = await _collectionService.GetAllAsync();
            foreach (var collection in collections)
            {
                CollectionComboBox.Items.Add(new ComboBoxItem 
                { 
                    Content = collection.Name, 
                    Tag = collection 
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
        }
    }

    private void PopulateFields()
    {
        if (_editingItem == null) return;

        TitleTextBox.Text = _editingItem.Title;
        DescriptionTextBox.Text = _editingItem.Description ?? string.Empty;
        TypeComboBox.SelectedIndex = (int)_editingItem.Type - 1;
        IsFavoriteCheckBox.IsChecked = _editingItem.IsFavorite;

        // Set login-specific fields if applicable
        if (_editingItem.LoginItem != null)
        {
            UsernameTextBox.Text = _editingItem.LoginItem.Username;
            PasswordTextBox.Password = _editingItem.LoginItem.Password;
            UrlTextBox.Text = _editingItem.LoginItem.WebsiteUrl ?? string.Empty;
        }

        // Select category
        if (_editingItem.CategoryId.HasValue)
        {
            for (int i = 0; i < CategoryComboBox.Items.Count; i++)
            {
                if (CategoryComboBox.Items[i] is ComboBoxItem item && 
                    item.Tag is Category cat && 
                    cat.Id == _editingItem.CategoryId)
                {
                    CategoryComboBox.SelectedIndex = i;
                    break;
                }
            }
        }

        // Select collection
        if (_editingItem.CollectionId.HasValue)
        {
            for (int i = 0; i < CollectionComboBox.Items.Count; i++)
            {
                if (CollectionComboBox.Items[i] is ComboBoxItem item && 
                    item.Tag is Collection col && 
                    col.Id == _editingItem.CollectionId)
                {
                    CollectionComboBox.SelectedIndex = i;
                    break;
                }
            }
        }
    }

    private void TypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TypeComboBox.SelectedIndex >= 0)
        {
            var selectedType = (ItemType)(TypeComboBox.SelectedIndex + 1);
            
            // Show/hide fields based on type
            LoginFieldsPanel.Visibility = selectedType == ItemType.Login ? Visibility.Visible : Visibility.Collapsed;
            CreditCardFieldsPanel.Visibility = selectedType == ItemType.CreditCard ? Visibility.Visible : Visibility.Collapsed;
            SecureNoteFieldsPanel.Visibility = selectedType == ItemType.SecureNote ? Visibility.Visible : Visibility.Collapsed;
            WiFiFieldsPanel.Visibility = selectedType == ItemType.WiFi ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void GeneratePasswordButton_Click(object sender, RoutedEventArgs e)
    {
        // Generate a random password
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new Random();
        var password = new string(Enumerable.Repeat(chars, 16)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        
        PasswordTextBox.Password = password;
    }

    private void TogglePasswordVisibility_Click(object sender, RoutedEventArgs e)
    {
        if (PasswordTextBox.Visibility == Visibility.Visible)
        {
            PasswordTextBox.Visibility = Visibility.Collapsed;
            PasswordDisplayTextBox.Visibility = Visibility.Visible;
            PasswordDisplayTextBox.Text = PasswordTextBox.Password;
            TogglePasswordVisibilityButton.Content = "🙈";
        }
        else
        {
            PasswordTextBox.Visibility = Visibility.Visible;
            PasswordDisplayTextBox.Visibility = Visibility.Collapsed;
            TogglePasswordVisibilityButton.Content = "👁️";
        }
    }

    private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        args.Cancel = true; // Prevent immediate close
        
        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                await ShowErrorDialog("Title is required");
                return;
            }

            if (TypeComboBox.SelectedIndex < 0)
            {
                await ShowErrorDialog("Please select a type");
                return;
            }

            var selectedType = (ItemType)(TypeComboBox.SelectedIndex + 1);

            // Create or update password item
            var item = _editingItem ?? new PasswordItem();
            
            item.Title = TitleTextBox.Text.Trim();
            item.Description = DescriptionTextBox.Text?.Trim();
            item.Type = selectedType;
            item.IsFavorite = IsFavoriteCheckBox.IsChecked ?? false;
            item.LastModified = DateTime.UtcNow;

            // Set category
            if (CategoryComboBox.SelectedItem is ComboBoxItem categoryItem && categoryItem.Tag is Category category)
            {
                item.CategoryId = category.Id;
            }

            // Set collection
            if (CollectionComboBox.SelectedItem is ComboBoxItem collectionItem && collectionItem.Tag is Collection collection)
            {
                item.CollectionId = collection.Id;
            }

            // Handle login-specific fields
            if (selectedType == ItemType.Login)
            {
                if (item.LoginItem == null)
                    item.LoginItem = new LoginItem();
                
                item.LoginItem.Username = UsernameTextBox.Text?.Trim() ?? string.Empty;
                item.LoginItem.Password = PasswordTextBox.Password;
                item.LoginItem.WebsiteUrl = UrlTextBox.Text?.Trim();
            }

            // Save item
            if (_editingItem == null)
            {
                Result = await _passwordItemService.CreateAsync(item);
            }
            else
            {
                Result = await _passwordItemService.UpdateAsync(item);
            }

            Hide();
        }
        catch (Exception ex)
        {
            await ShowErrorDialog($"Error saving password: {ex.Message}");
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