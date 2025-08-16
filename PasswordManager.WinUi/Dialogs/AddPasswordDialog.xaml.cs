using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PasswordManager.Models;
using PasswordManager.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace PasswordManager.WinUi.Dialogs;

public sealed partial class AddPasswordDialog : ContentDialog
{
    private readonly IPasswordItemService _passwordItemService;
    private readonly ICategoryInterface _categoryService;
    private readonly ICollectionService _collectionService;
    private readonly IPasskeyService _passkeyService;
    private PasswordItem? _editingItem;

    public PasswordItem? Result { get; private set; }

    public AddPasswordDialog(IServiceProvider serviceProvider, PasswordItem? editingItem = null)
    {
        this.InitializeComponent();
        
        _passwordItemService = serviceProvider.GetRequiredService<IPasswordItemService>();
        _categoryService = serviceProvider.GetRequiredService<ICategoryInterface>();
        _collectionService = serviceProvider.GetRequiredService<ICollectionService>();
        _passkeyService = serviceProvider.GetRequiredService<IPasskeyService>();
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

        // Set passkey-specific fields if applicable
        if (_editingItem.PasskeyItem != null)
        {
            PasskeyWebsiteTextBox.Text = _editingItem.PasskeyItem.Website ?? string.Empty;
            PasskeyUrlTextBox.Text = _editingItem.PasskeyItem.WebsiteUrl ?? string.Empty;
            PasskeyUsernameTextBox.Text = _editingItem.PasskeyItem.Username ?? string.Empty;
            PasskeyDisplayNameTextBox.Text = _editingItem.PasskeyItem.DisplayName ?? string.Empty;
            PasskeyDeviceTypeTextBox.Text = _editingItem.PasskeyItem.DeviceType ?? string.Empty;
            PasskeyRequiresVerificationCheckBox.IsChecked = _editingItem.PasskeyItem.RequiresUserVerification;
            PasskeyIsBackedUpCheckBox.IsChecked = _editingItem.PasskeyItem.IsBackedUp;
            PasskeyNotesTextBox.Text = _editingItem.PasskeyItem.Notes ?? string.Empty;
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
            PasskeyFieldsPanel.Visibility = selectedType == ItemType.Passkey ? Visibility.Visible : Visibility.Collapsed;
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
            
            // Update button icon to hide password
            if (TogglePasswordVisibilityButton.Content is FontIcon icon)
            {
                icon.Glyph = "&#xE7B2;"; // Eye with slash icon
            }
        }
        else
        {
            PasswordTextBox.Visibility = Visibility.Visible;
            PasswordDisplayTextBox.Visibility = Visibility.Collapsed;
            
            // Update button icon to show password
            if (TogglePasswordVisibilityButton.Content is FontIcon icon)
            {
                icon.Glyph = "&#xE7B3;"; // Eye icon
            }
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

            // Handle passkey-specific fields
            if (selectedType == ItemType.Passkey)
            {
                if (item.PasskeyItem == null)
                    item.PasskeyItem = new PasskeyItem();
                
                item.PasskeyItem.Website = PasskeyWebsiteTextBox.Text?.Trim();
                item.PasskeyItem.WebsiteUrl = PasskeyUrlTextBox.Text?.Trim();
                item.PasskeyItem.Username = PasskeyUsernameTextBox.Text?.Trim();
                item.PasskeyItem.DisplayName = PasskeyDisplayNameTextBox.Text?.Trim();
                item.PasskeyItem.DeviceType = PasskeyDeviceTypeTextBox.Text?.Trim();
                item.PasskeyItem.RequiresUserVerification = PasskeyRequiresVerificationCheckBox.IsChecked ?? true;
                item.PasskeyItem.IsBackedUp = PasskeyIsBackedUpCheckBox.IsChecked ?? false;
                item.PasskeyItem.Notes = PasskeyNotesTextBox.Text?.Trim();
                
                // For now, use a placeholder credential ID (in real implementation, this would come from WebAuthn)
                item.PasskeyItem.CredentialId = "placeholder_credential_id_" + DateTime.Now.Ticks;
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

    private async void RegisterPasskeyButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(PasskeyWebsiteTextBox.Text))
            {
                await ShowErrorDialog("Website is required for passkey registration");
                return;
            }

            if (string.IsNullOrWhiteSpace(PasskeyUsernameTextBox.Text))
            {
                await ShowErrorDialog("Username is required for passkey registration");
                return;
            }

            // Set device type automatically
            PasskeyDeviceTypeTextBox.Text = "Windows PC";

            // Show success message for now (actual WebAuthn integration would happen here)
            var successDialog = new ContentDialog
            {
                Title = "Passkey Registration",
                Content = "Passkey registration initiated. In a full implementation, this would use WebAuthn to register the passkey with the browser/OS.",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await successDialog.ShowAsync();
        }
        catch (Exception ex)
        {
            await ShowErrorDialog($"Error registering passkey: {ex.Message}");
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

    // New methods for the updated UI
    private void ItemTypeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string itemType)
        {
            // Hide the selection panel and show the detail panel
            ItemSelectionPanel.Visibility = Visibility.Collapsed;
            LoginDetailPanel.Visibility = Visibility.Visible;
            
            // Set the type combobox selection based on the item type
            if (TypeComboBox != null)
            {
                switch (itemType)
                {
                    case "Login":
                        TypeComboBox.SelectedIndex = 0;
                        break;
                    case "SecureNote":
                        TypeComboBox.SelectedIndex = 1;
                        break;
                    case "CreditCard":
                        TypeComboBox.SelectedIndex = 2;
                        break;
                    case "Identity":
                        TypeComboBox.SelectedIndex = 3;
                        break;
                    case "Password":
                        TypeComboBox.SelectedIndex = 4;
                        break;
                    case "Document":
                        TypeComboBox.SelectedIndex = 5;
                        break;
                }
            }
            
            // Update the dialog title
            if (DialogTitle != null)
            {
                DialogTitle.Text = $"Add {itemType}";
            }
            
            // Show the back button
            if (BackButton != null)
            {
                BackButton.Visibility = Visibility.Visible;
            }
        }
    }

    private void ShowMoreButton_Click(object sender, RoutedEventArgs e)
    {
        // In a full implementation, this would show additional item types
        // For now, just a placeholder
    }

    private void ImportDataButton_Click(object sender, RoutedEventArgs e)
    {
        // Navigate to import functionality
        // For now, just a placeholder
    }

    private void ShowUsernameHistory_Click(object sender, RoutedEventArgs e)
    {
        // Show dropdown with previous usernames
        // For now, just a placeholder
    }

    private void PasswordTextBox_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
    {
        // Show password generator on right-click
        PasswordGeneratorPopup.Visibility = Visibility.Visible;
        GenerateNewPassword();
    }

    private void RefreshPassword_Click(object sender, RoutedEventArgs e)
    {
        GenerateNewPassword();
    }

    private void ClosePasswordGenerator_Click(object sender, RoutedEventArgs e)
    {
        PasswordGeneratorPopup.Visibility = Visibility.Collapsed;
    }

    private void CancelPasswordGenerator_Click(object sender, RoutedEventArgs e)
    {
        PasswordGeneratorPopup.Visibility = Visibility.Collapsed;
    }

    private void UseGeneratedPassword_Click(object sender, RoutedEventArgs e)
    {
        if (GeneratedPasswordText?.Text is string password)
        {
            PasswordTextBox.Password = password;
        }
        PasswordGeneratorPopup.Visibility = Visibility.Collapsed;
    }

    private void SaveItem_Click(object sender, RoutedEventArgs e)
    {
        // Trigger the same logic as the primary button
        var args = new ContentDialogButtonClickEventArgs();
        ContentDialog_PrimaryButtonClick(this, args);
    }

    private void GenerateNewPassword()
    {
        // Get password settings from the UI
        int length = (int)(PasswordLengthSlider?.Value ?? 13);
        bool includeNumbers = NumbersToggle?.IsOn ?? true;
        bool includeSymbols = SymbolsToggle?.IsOn ?? false;

        string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        
        if (includeNumbers)
            chars += "0123456789";
            
        if (includeSymbols)
            chars += "!@#$%^&*()-_=+[]{}|;:,.<>?";

        var random = new Random();
        var password = new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());

        if (GeneratedPasswordText != null)
        {
            GeneratedPasswordText.Text = password;
        }

        // Update the length display
        if (PasswordLengthText != null)
        {
            PasswordLengthText.Text = length.ToString();
        }
    }
    
    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        // Go back to item type selection
        LoginDetailPanel.Visibility = Visibility.Collapsed;
        ItemSelectionPanel.Visibility = Visibility.Visible;
        
        // Update the dialog title
        if (DialogTitle != null)
        {
            DialogTitle.Text = "What would you like to add?";
        }
        
        // Hide the back button
        if (BackButton != null)
        {
            BackButton.Visibility = Visibility.Collapsed;
        }
    }
}