using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PasswordManager.Models;
using PasswordManager.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.WPF.Helpers;

namespace PasswordManager.WPF.Dialogs;

public sealed partial class AddPasswordDialog : ModernWpf.Controls.ContentDialog
{
    private readonly IPasswordItemService _passwordItemService;
    private readonly ICategoryInterface _categoryService;
    private readonly ICollectionService _collectionService;
    private readonly IPasskeyService _passkeyService;
    private readonly IAuthService _authService;
    private PasswordItem? _editingItem;
    private bool _isReadOnly = false;
    private List<CustomField> _customFields = new();

    public PasswordItem? Result { get; private set; }

    public AddPasswordDialog(IServiceProvider serviceProvider, PasswordItem? editingItem = null, bool isReadOnly = false)
    {
        this.InitializeComponent();

        _passwordItemService = serviceProvider.GetRequiredService<IPasswordItemService>();
        _categoryService = serviceProvider.GetRequiredService<ICategoryInterface>();
        _collectionService = serviceProvider.GetRequiredService<ICollectionService>();
        _passkeyService = serviceProvider.GetRequiredService<IPasskeyService>();
        _authService = serviceProvider.GetRequiredService<IAuthService>();
        _editingItem = editingItem;
        _isReadOnly = isReadOnly;

        Title = editingItem == null ? "Add Password Item" : "Edit Password Item";
        PrimaryButtonText = editingItem == null ? "Add" : "Save";
        CloseButtonText = "Cancel";

        LoadData();

        if (_editingItem != null)
        {
            PopulateFields();
            LoadCustomFields();
        }
        else
        {
            // Initialize with empty custom fields list
            _customFields = new List<CustomField>();
        }

        RefreshCustomFieldsUI();

        // Apply read-only mode UI changes
        if (_isReadOnly)
        {
            // Primary button becomes Close
            PrimaryButtonText = "Close";

            // Hide all editable textboxes and show text displays where added
            ToggleReadOnlyUI(true);
        }

        // Ensure the dialog is centered on the main window
        TryConfigureCentering();
    }

    private void TryConfigureCentering()
    {
        try
        {
            // Apply modern dialog style if none is set
            if (this.Style == null && Application.Current.Resources.Contains("Modern1PasswordDialogStyle"))
            {
                this.Style = Application.Current.Resources["Modern1PasswordDialogStyle"] as Style;
            }

            // WPF ContentDialog centers automatically
        }
        catch (Exception)
        {
            // Silently ignore; dialog will still show with default behavior
        }
    }

    private void ToggleReadOnlyUI(bool readOnly)
    {
        // Title
        TitleTextBox.Visibility = readOnly ? Visibility.Collapsed : Visibility.Visible;
        TitleTextDisplay.Visibility = readOnly ? Visibility.Visible : Visibility.Collapsed;
        if (readOnly) TitleTextDisplay.Text = TitleTextBox.Text;

        // Description
        DescriptionTextBox.Visibility = readOnly ? Visibility.Collapsed : Visibility.Visible;
        DescriptionTextDisplay.Visibility = readOnly ? Visibility.Visible : Visibility.Collapsed;
        if (readOnly) DescriptionTextDisplay.Text = DescriptionTextBox.Text;

        // Username
        UsernameTextBox.Visibility = readOnly ? Visibility.Collapsed : Visibility.Visible;
        UsernameTextDisplay.Visibility = readOnly ? Visibility.Visible : Visibility.Collapsed;
        if (readOnly) UsernameTextDisplay.Text = UsernameTextBox.Text;

        // Make the surrounding border look like a label when read-only
        if (this.FindName("UsernameFieldBorder") is Border usernameBorder)
        {
            // Remove the surrounding input-style background and border when read-only so the ReadOnlyField looks like a label
            usernameBorder.Background = readOnly ? null : (Brush)Application.Current.Resources["ModernSurfaceBrush"];
            usernameBorder.BorderThickness = readOnly ? new Thickness(0) : new Thickness(1);
            usernameBorder.BorderBrush = readOnly ? null : (Brush)Application.Current.Resources["ModernBorderBrush"];
        }

        // Password
        PasswordTextBox.Visibility = readOnly ? Visibility.Collapsed : Visibility.Visible;
        PasswordDisplayTextBox.Visibility = readOnly ? Visibility.Collapsed : Visibility.Visible; // keep hidden when in edit
        PasswordTextDisplay.Visibility = readOnly ? Visibility.Visible : Visibility.Collapsed;
        if (readOnly) PasswordTextDisplay.Text = PasswordTextBox.Password == null || PasswordTextBox.Password.Length == 0 ? "" : new string('•', Math.Max(8, PasswordTextBox.Password.Length));

        if (this.FindName("PasswordFieldBorder") is Border passwordBorder)
        {
            passwordBorder.Background = readOnly ? null : (Brush)Application.Current.Resources["ModernSurfaceBrush"];
            passwordBorder.BorderThickness = readOnly ? new Thickness(0) : new Thickness(1);
            passwordBorder.BorderBrush = readOnly ? null : (Brush)Application.Current.Resources["ModernBorderBrush"];
        }

        // URL
        UrlTextBox.Visibility = readOnly ? Visibility.Collapsed : Visibility.Visible;
        UrlTextDisplay.Visibility = readOnly ? Visibility.Visible : Visibility.Collapsed;
        if (readOnly) UrlTextDisplay.Text = UrlTextBox.Text;

        if (this.FindName("UrlFieldBorder") is Border urlBorder)
        {
            urlBorder.Background = readOnly ? null : (Brush)Application.Current.Resources["ModernSurfaceBrush"];
            urlBorder.BorderThickness = readOnly ? new Thickness(0) : new Thickness(1);
            urlBorder.BorderBrush = readOnly ? null : (Brush)Application.Current.Resources["ModernBorderBrush"];
        }

        // Disable or hide editing-related controls
        TypeComboBox.IsEnabled = !readOnly;
        CategoryComboBox.IsEnabled = !readOnly;
        CollectionComboBox.IsEnabled = !readOnly;
        IsFavoriteCheckBox.IsEnabled = !readOnly;

        // Hide buttons used for editing/generation
        TogglePasswordVisibilityButton.Visibility = readOnly ? Visibility.Collapsed : Visibility.Visible;
        // GeneratePasswordButton may not be present in older XAML templates; find safely
        var genObjLocal = this.FindName("GeneratePasswordButton");
        if (genObjLocal is Button genBtnLocal)
        {
            genBtnLocal.Visibility = readOnly ? Visibility.Collapsed : Visibility.Visible;
        }
        AddWebsiteButton.Visibility = readOnly ? Visibility.Collapsed : Visibility.Visible;
        AddMoreLoginButton.Visibility = readOnly ? Visibility.Collapsed : Visibility.Visible;
        AddCustomFieldButton.Visibility = readOnly ? Visibility.Collapsed : Visibility.Visible;
        AddMoreSecureNoteButton.Visibility = readOnly ? Visibility.Collapsed : Visibility.Visible;
        AddLocationButton.Visibility = readOnly ? Visibility.Collapsed : Visibility.Visible;

        // Disable custom fields editing
        // StackPanel doesn't expose IsEnabled in a way we can safely use everywhere in code-behind
        try
        {
            foreach (var child in CustomFieldsContainer.Children)
            {
                if (child is Control ctl)
                {
                    ctl.IsEnabled = !readOnly;
                }
            }
        }
        catch
        {
            // Defensive: if CustomFieldsContainer is missing or children are not controls, ignore
        }

        // Safely show/hide the generate button if it's present in the template/XAML
        var genBtnObj = this.FindName("GeneratePasswordButton");
        if (genBtnObj is Button genBtn)
        {
            genBtn.Visibility = readOnly ? Visibility.Collapsed : Visibility.Visible;
        }

    }


    // Method to set initial item type from the selection dialog
    public void SetInitialItemType(ItemType itemType, string? categoryName = null)
    {
        // Set the type combo box selection
        TypeComboBox.SelectedIndex = (int)itemType - 1;

        // Update the title based on category name
        if (!string.IsNullOrEmpty(categoryName))
        {
            Title = $"Add {categoryName}";
        }

        // Show the appropriate fields panel
        // Note: TypeComboBox_SelectionChanged doesn't use the SelectionChangedEventArgs parameter,
        // it only checks TypeComboBox.SelectedIndex. Passing null! is safe here as the parameter is unused.
        TypeComboBox_SelectionChanged(TypeComboBox, null!);

        // Handle special category-based forms
        if (!string.IsNullOrEmpty(categoryName))
        {
            // If the category is "Identity" we prefer using the Login fields layout
            if (categoryName.Contains("Identity", StringComparison.OrdinalIgnoreCase))
            {
                // Show login fields instead of identity-specific layout
                LoginFieldsPanel.Visibility = Visibility.Visible;
                SecureNoteFieldsPanel.Visibility = Visibility.Collapsed;
                IdentityFieldsPanel.Visibility = Visibility.Collapsed;
            }
            else if (categoryName.Contains("API", StringComparison.OrdinalIgnoreCase) && itemType == ItemType.SecureNote)
            {
                SecureNoteFieldsPanel.Visibility = Visibility.Collapsed;
                APICredentialsFieldsPanel.Visibility = Visibility.Visible;
            }
            else if (categoryName.Contains("Identity") && itemType == ItemType.SecureNote)
            {
                // Fallback to previous behavior
                SecureNoteFieldsPanel.Visibility = Visibility.Collapsed;
                IdentityFieldsPanel.Visibility = Visibility.Visible;
            }
        }
    }

    private async void LoadData()
    {
        try
        {
            // Load categories
            var categories = await _categoryService.GetAllAsync();
            CategoryComboBox.Items.Clear();
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
            CollectionComboBox.Items.Clear();
            foreach (var collection in collections)
            {
                CollectionComboBox.Items.Add(new ComboBoxItem
                {
                    Content = collection.Name,
                    Tag = collection
                });
            }

            // GeneratePasswordButton may not have an x:Name in XAML (older markup). Find safely and apply visibility based on current read-only state.
            var genBtnObj = this.FindName("GeneratePasswordButton");
            if (genBtnObj is Button genBtn)
            {
                genBtn.Visibility = _isReadOnly ? Visibility.Collapsed : Visibility.Visible;
            }

            // If LoadData completed after construction and we have an editing item,
            // ensure fields are populated and read-only UI is applied so display TextBlocks show values.
            try
            {
                if (_editingItem != null)
                {
                    // Populate fields again in case LoadData filled combo boxes used by selection logic
                    PopulateFields();
                    LoadCustomFields();
                    RefreshCustomFieldsUI();

                    if (_isReadOnly)
                    {
                        ToggleReadOnlyUI(true);
                    }
                }
            }
            catch
            {
                // Defensive: ignore any timing-related issues here
            }
        }
        catch (Exception ex)
        {
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

        // If this item belongs to an "Identity" category, prefer the login-style layout
        try
        {
            var catName = _editingItem.Category?.Name ?? string.Empty;
            if (!string.IsNullOrEmpty(catName) && catName.IndexOf("Identity", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Show login fields and hide other type-specific panels so Identity acts like a filtered Login form
                LoginFieldsPanel.Visibility = Visibility.Visible;
                CreditCardFieldsPanel.Visibility = Visibility.Collapsed;
                SecureNoteFieldsPanel.Visibility = Visibility.Collapsed;
                PasskeyFieldsPanel.Visibility = Visibility.Collapsed;
                IdentityFieldsPanel.Visibility = Visibility.Collapsed;
            }
        }
        catch
        {
            // Defensive: if panels are renamed in XAML, ignore and allow existing behavior
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

        // If in read-only mode, populate the display TextBlocks
        if (_isReadOnly)
        {
            TitleTextDisplay.Text = TitleTextBox.Text;
            TitleTextDisplay.CopyText = TitleTextBox.Text;

            DescriptionTextDisplay.Text = DescriptionTextBox.Text;
            DescriptionTextDisplay.CopyText = DescriptionTextBox.Text;

            UsernameTextDisplay.Text = UsernameTextBox.Text;
            UsernameTextDisplay.CopyText = UsernameTextBox.Text;

            PasswordTextDisplay.Text = PasswordTextBox.Password == null || PasswordTextBox.Password.Length == 0 ? "" : new string('•', Math.Max(8, PasswordTextBox.Password.Length));
            PasswordTextDisplay.CopyText = PasswordTextBox.Password;

            UrlTextDisplay.Text = UrlTextBox.Text;
            UrlTextDisplay.CopyText = UrlTextBox.Text;

            ToggleReadOnlyUI(true);
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

            // Handle special form types using SecureNote as base
            IdentityFieldsPanel.Visibility = Visibility.Collapsed;
            APICredentialsFieldsPanel.Visibility = Visibility.Collapsed;

            // Show Identity panel for Identity-related categories
            if (selectedType == ItemType.SecureNote)
            {
                // Check if this is an Identity or API Credentials item by looking at the category
                if (CategoryComboBox.SelectedItem is ComboBoxItem categoryItem && categoryItem.Tag is Category category)
                {
                    if (category.Name.Contains("Identity"))
                    {
                        SecureNoteFieldsPanel.Visibility = Visibility.Collapsed;
                        IdentityFieldsPanel.Visibility = Visibility.Visible;
                    }
                    else if (category.Name.Contains("API"))
                    {
                        SecureNoteFieldsPanel.Visibility = Visibility.Collapsed;
                        APICredentialsFieldsPanel.Visibility = Visibility.Visible;
                    }
                }
            }
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

    private async void ContentDialog_PrimaryButtonClick(ModernWpf.Controls.ContentDialog sender, ModernWpf.Controls.ContentDialogButtonClickEventArgs args)
    {
        // If read-only, just close
        if (_isReadOnly)
        {
            args.Cancel = false; // allow close
            return;
        }

        args.Cancel = true; // Prevent immediate close for save flow

        try
        {
            // Clear any previous error messages
            ErrorMessageBorder.Visibility = Visibility.Collapsed;

            // Validate required fields with inline feedback
            bool isValid = true;

            // Clear previous validation messages
            TitleValidationMessage.Visibility = Visibility.Collapsed;
            TypeValidationMessage.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                TitleValidationMessage.Text = "Title is required";
                TitleValidationMessage.Visibility = Visibility.Visible;
                isValid = false;
            }

            if (TypeComboBox.SelectedIndex < 0)
            {
                TypeValidationMessage.Text = "Please select a type";
                TypeValidationMessage.Visibility = Visibility.Visible;
                isValid = false;
            }

            // Show additional validation for login items
            if (TypeComboBox.SelectedIndex == 0 && LoginFieldsPanel.Visibility == Visibility.Visible)
            {
                if (string.IsNullOrWhiteSpace(UsernameTextBox.Text) && string.IsNullOrWhiteSpace(PasswordTextBox.Password))
                {
                    TitleValidationMessage.Text = "Username or password is required for login items";
                    TitleValidationMessage.Visibility = Visibility.Visible;
                    isValid = false;
                }
            }

            if (!isValid)
            {
                return;
            }

            // Validate services are available
            if (_passwordItemService == null)
            {
                await ShowErrorDialog("Password service is not initialized. Please restart the application.");
                return;
            }

            // Prefer asking the auth service for the current user id (works for configurable/auth modes)
            var currentUserId = await _authService.GetCurrentUserIdAsync();
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                await ShowErrorDialog("No authenticated user found. Please log in again.");
                return;
            }

            // Show loading indicator
            ShowLoadingIndicator(true, _editingItem == null ? "Creating item..." : "Updating item...");

            var selectedType = (ItemType)(TypeComboBox.SelectedIndex + 1);

            // Create or update password item
            var item = _editingItem ?? new PasswordItem();

            item.Title = TitleTextBox.Text.Trim();
            item.Description = DescriptionTextBox.Text?.Trim();
            item.Type = selectedType;
            item.IsFavorite = IsFavoriteCheckBox.IsChecked == true;
            item.LastModified = DateTime.UtcNow;

            // Set user ID from current authenticated user (use GetCurrentUserIdAsync result)
            item.UserId = currentUserId;

            // Set category
            if (CategoryComboBox.SelectedItem is ComboBoxItem categoryItem && categoryItem.Tag is Category category)
            {
                item.CategoryId = category.Id;
            }

            // Set collection with better fallback handling
            if (CollectionComboBox.SelectedItem is ComboBoxItem collectionItem && collectionItem.Tag is Collection collection)
            {
                item.CollectionId = collection.Id;
            }
            else if (_editingItem == null) // Only set default for new items
            {
                // If no collection selected, fallback to default collection to satisfy DB constraints
                try
                {
                    var defaultCollection = await _collectionService.GetDefaultCollectionAsync();
                    if (defaultCollection != null)
                    {
                        item.CollectionId = defaultCollection.Id;
                    }
                }
                catch (Exception ex)
                {
                    // Continue without collection ID - let the service handle it
                }
            }

            // Handle type-specific fields
            await PopulateTypeSpecificFields(item, selectedType);

            // Update custom fields
            UpdateCustomFieldsInPasswordItem(item);

            // Save item with better error handling
            if (_editingItem == null)
            {
                Result = await _passwordItemService.CreateAsync(item);
            }
            else
            {
                Result = await _passwordItemService.UpdateAsync(item);
            }

            // Verify the result
            if (Result == null)
            {
                throw new InvalidOperationException("Save operation completed but returned null result");
            }

            // Hide loading indicator and close dialog
            ShowLoadingIndicator(false);
            Hide();
        }
        catch (Exception ex)
        {
            // Hide loading indicator and show error
            ShowLoadingIndicator(false);

            var errorMessage = ex.InnerException?.Message ?? ex.Message;

            await ShowErrorDialog($"Error saving item: {errorMessage}");
        }
    }

    /// <summary>
    /// Populates type-specific fields for the password item
    /// </summary>
    private async Task PopulateTypeSpecificFields(PasswordItem item, ItemType selectedType)
    {
        // Handle login-specific fields
        if (selectedType == ItemType.Login)
        {
            if (item.LoginItem == null)
                item.LoginItem = new LoginItem();

            item.LoginItem.Username = UsernameTextBox.Text?.Trim() ?? string.Empty;
            item.LoginItem.Password = PasswordTextBox.Password;
            item.LoginItem.WebsiteUrl = UrlTextBox.Text?.Trim();

            // Set user ID for the login item
            if (_authService.CurrentUser != null)
            {
                item.LoginItem.UserId = _authService.CurrentUser.Id;
            }
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

            // Set user ID for the passkey item
            if (_authService.CurrentUser != null)
            {
                item.PasskeyItem.UserId = _authService.CurrentUser.Id;
            }

            // For now, use a placeholder credential ID (in real implementation, this would come from WebAuthn)
            item.PasskeyItem.CredentialId = "placeholder_credential_id_" + DateTime.Now.Ticks;
        }

        // Handle credit card-specific fields
        if (selectedType == ItemType.CreditCard)
        {
            if (item.CreditCardItem == null)
                item.CreditCardItem = new CreditCardItem();

            // Set user ID for the credit card item
            if (_authService.CurrentUser != null)
            {
                item.CreditCardItem.UserId = _authService.CurrentUser.Id;
            }

            // Additional credit card fields would be set here when UI is implemented
        }

        // Handle secure note-specific fields
        if (selectedType == ItemType.SecureNote)
        {
            if (item.SecureNoteItem == null)
                item.SecureNoteItem = new SecureNoteItem();

            // Set user ID for the secure note item
            if (_authService.CurrentUser != null)
            {
                item.SecureNoteItem.UserId = _authService.CurrentUser.Id;
            }

            // Additional secure note fields would be set here when UI is implemented
        }

        // Handle WiFi-specific fields
        if (selectedType == ItemType.WiFi)
        {
            if (item.WiFiItem == null)
                item.WiFiItem = new WiFiItem();

            // Set user ID for the WiFi item
            if (_authService.CurrentUser != null)
            {
                item.WiFiItem.UserId = _authService.CurrentUser.Id;
            }

            // Additional WiFi fields would be set here when UI is implemented
        }
    }

    private void ShowLoadingIndicator(bool show, string message = "Loading...")
    {
        try
        {
            if (LoadingIndicator != null)
            {
                LoadingIndicator.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            }

            if (LoadingText != null && show)
            {
                LoadingText.Text = message;
            }

            // Disable primary button during loading
            this.IsPrimaryButtonEnabled = !show;
        }
        catch
        {
            // Ignore if UI elements not found
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
            await ShowSuccessMessage("Passkey registration initiated. In a full implementation, this would use WebAuthn to register the passkey with the browser/OS.");
        }
        catch (Exception ex)
        {
            await ShowErrorDialog($"Error registering passkey: {ex.Message}");
        }
    }

    private async Task ShowErrorDialog(string message)
    {
        ErrorMessageText.Text = message;
        ErrorMessageBorder.Visibility = Visibility.Visible;

        // Auto-hide error after 5 seconds
        await Task.Delay(5000);
        ErrorMessageBorder.Visibility = Visibility.Collapsed;
    }

    private async Task ShowSuccessMessage(string message)
    {
        SuccessMessageText.Text = message;
        SuccessMessageBorder.Visibility = Visibility.Visible;

        // Auto-hide success message after 5 seconds
        await Task.Delay(5000);
        SuccessMessageBorder.Visibility = Visibility.Collapsed;
    }

    // New methods for the updated UI
    private void ItemTypeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string itemType)
        {
            // Hide the selection panel and show the detail panel
            if (ItemSelectionPanel != null)
                ItemSelectionPanel.Visibility = Visibility.Collapsed;
            if (LoginDetailPanel != null)
                LoginDetailPanel.Visibility = Visibility.Visible;

            // Update the dialog title - WPF handles this automatically
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

    private void PasswordTextBox_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Show password generator on right-click
        if (PasswordGeneratorPopup != null)
        {
            PasswordGeneratorPopup.Visibility = Visibility.Visible;
            GenerateNewPassword();
        }
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

    private void GenerateNewPassword()
    {
        // Get password settings from the UI
        int length = (int)(PasswordLengthSlider?.Value ?? 13);
        bool includeNumbers = NumbersToggle?.IsChecked ?? true;
        bool includeSymbols = SymbolsToggle?.IsChecked ?? false;

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

    private void PasswordLengthSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
    {
        if (PasswordLengthText != null)
        {
            PasswordLengthText.Text = ((int)e.NewValue).ToString();
        }
    }

    // Custom Fields Methods
    private void LoadCustomFields()
    {
        if (_editingItem?.CustomFields != null)
        {
            _customFields = new List<CustomField>(_editingItem.CustomFields);
        }
    }

    private void AddCustomField_Click(object sender, RoutedEventArgs e)
    {
        var newField = new CustomField
        {
            Name = $"Custom Field {_customFields.Count + 1}",
            Value = "",
            Type = CustomFieldType.Text,
            DisplayOrder = _customFields.Count,
            PasswordItemId = _editingItem?.Id ?? 0
        };

        _customFields.Add(newField);
        RefreshCustomFieldsUI();
    }

    private void RefreshCustomFieldsUI()
    {
        CustomFieldsContainer.Children.Clear();

        foreach (var field in _customFields.OrderBy(f => f.DisplayOrder))
        {
            var fieldControl = CustomFieldHelper.CreateCustomFieldControl(
                field,
                OnCustomFieldChanged,
                OnCustomFieldRemoved
            );
            CustomFieldsContainer.Children.Add(fieldControl);
        }
    }

    private void OnCustomFieldChanged(CustomField field)
    {
        // Update timestamp
        field.LastModified = DateTime.UtcNow;
    }

    private void OnCustomFieldRemoved(CustomField field)
    {
        _customFields.Remove(field);
        RefreshCustomFieldsUI();
    }

    private void UpdateCustomFieldsInPasswordItem(PasswordItem item)
    {
        // Clear existing custom fields
        item.CustomFields.Clear();

        // Add current custom fields
        foreach (var field in _customFields)
        {
            field.PasswordItemId = item.Id;
            item.CustomFields.Add(field);
        }
    }

    // Button Event Handlers for Form-Specific Actions
    private void AddWebsite_Click(object sender, RoutedEventArgs e)
    {
        // Add a new URL custom field for login items
        var websiteField = new CustomField
        {
            Name = "Website",
            Value = "",
            Type = CustomFieldType.Url,
            DisplayOrder = _customFields.Count,
            PasswordItemId = _editingItem?.Id ?? 0
        };

        _customFields.Add(websiteField);
        RefreshCustomFieldsUI();
    }

    private void AddMoreLogin_Click(object sender, RoutedEventArgs e)
    {
        // Add a generic text custom field for login items
        var moreField = new CustomField
        {
            Name = "Additional Information",
            Value = "",
            Type = CustomFieldType.Text,
            DisplayOrder = _customFields.Count,
            PasswordItemId = _editingItem?.Id ?? 0
        };

        _customFields.Add(moreField);
        RefreshCustomFieldsUI();
    }

    private void AddMoreSecureNote_Click(object sender, RoutedEventArgs e)
    {
        // Add a generic text custom field for secure notes
        var moreField = new CustomField
        {
            Name = "Additional Note",
            Value = "",
            Type = CustomFieldType.TextArea,
            DisplayOrder = _customFields.Count,
            PasswordItemId = _editingItem?.Id ?? 0
        };

        _customFields.Add(moreField);
        RefreshCustomFieldsUI();
    }

    private void AddLocation_Click(object sender, RoutedEventArgs e)
    {
        // Add a location custom field for secure notes
        var locationField = new CustomField
        {
            Name = "Location",
            Value = "",
            Type = CustomFieldType.Text,
            DisplayOrder = _customFields.Count,
            PasswordItemId = _editingItem?.Id ?? 0
        };

        _customFields.Add(locationField);
        RefreshCustomFieldsUI();
    }
}
