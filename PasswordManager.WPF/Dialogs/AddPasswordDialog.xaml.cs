using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PasswordManager.Models;
using PasswordManager.Services.Interfaces;
using PasswordManager.Services.Utilities;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.WPF.Helpers;

namespace PasswordManager.WPF.Dialogs;

public sealed partial class AddPasswordDialog : ModernWpf.Controls.ContentDialog
{
    private readonly IPasswordItemService _passwordItemService;
    private readonly ICategoryInterface _categoryService;
    private readonly IPasskeyService _passkeyService;
    private readonly IAuthService _authService;
    private readonly IPasswordStrengthService _strengthService;
    private readonly Services.IWindowsHelloService _windowsHello = new Services.WindowsHelloService();
    private PasswordItem? _editingItem;
    private bool _isReadOnly = false;
    private List<CustomField> _customFields = new();
    private string? _brandIconDataUrl;

    public PasswordItem? Result { get; private set; }

    public AddPasswordDialog(IServiceProvider serviceProvider, PasswordItem? editingItem = null, bool isReadOnly = false)
    {
        this.InitializeComponent();

        _passwordItemService = serviceProvider.GetRequiredService<IPasswordItemService>();
        _categoryService = serviceProvider.GetRequiredService<ICategoryInterface>();
        _passkeyService = serviceProvider.GetRequiredService<IPasskeyService>();
        _authService = serviceProvider.GetRequiredService<IAuthService>();
        _strengthService = serviceProvider.GetRequiredService<IPasswordStrengthService>();
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
        UpdateBrandIconStatus();

        // Apply read-only mode UI changes
        if (_isReadOnly)
        {
            // Primary button becomes Close
            PrimaryButtonText = "Close";

            // Hide all editable textboxes and show text displays where added
            ToggleReadOnlyUI(true);
        }

        // Live password strength meter
        PasswordTextBox.PasswordChanged += (_, _) => UpdateStrengthMeter();
        PasswordDisplayTextBox.TextChanged += (_, _) => UpdateStrengthMeter();
        UpdateStrengthMeter();

        // Ensure the dialog is centered on the main window
        TryConfigureCentering();
    }

    /// <summary>
    /// Recomputes and renders the password strength meter from the shared strength service.
    /// </summary>
    private void UpdateStrengthMeter()
    {
        try
        {
            if (PasswordStrengthPanel == null) return;

            var pwd = PasswordDisplayTextBox.Visibility == Visibility.Visible
                ? PasswordDisplayTextBox.Text
                : PasswordTextBox.Password;

            if (string.IsNullOrEmpty(pwd) || _isReadOnly)
            {
                PasswordStrengthPanel.Visibility = Visibility.Collapsed;
                return;
            }

            var result = _strengthService.Evaluate(pwd);
            PasswordStrengthPanel.Visibility = Visibility.Visible;

            var onBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(result.ColorHex));
            var offBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));

            int filled = Math.Max(1, result.Score); // 0..4 → always light at least one segment
            StrengthSeg1.Background = filled >= 1 ? onBrush : offBrush;
            StrengthSeg2.Background = filled >= 2 ? onBrush : offBrush;
            StrengthSeg3.Background = filled >= 3 ? onBrush : offBrush;
            StrengthSeg4.Background = filled >= 4 ? onBrush : offBrush;

            StrengthLabel.Text = result.Label;
            StrengthLabel.Foreground = onBrush;
            StrengthCrackTime.Text = $"Crack time: {result.CrackTimeDisplay}";
        }
        catch
        {
            // Non-fatal cosmetic feature.
        }
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
        UploadBrandIconButton.Visibility = readOnly ? Visibility.Collapsed : Visibility.Visible;
        RemoveBrandIconButton.Visibility = readOnly || string.IsNullOrWhiteSpace(_brandIconDataUrl) ? Visibility.Collapsed : Visibility.Visible;
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
            var textBrush = Application.Current.Resources.Contains("ModernTextPrimaryBrush")
                ? (System.Windows.Media.Brush)Application.Current.Resources["ModernTextPrimaryBrush"]
                : System.Windows.Media.Brushes.White;

            // The Category dropdown is a TYPE selector, not a DB-category browser.
            // GetAllAsync() returns every row the seeder ever inserted (24+) and can
            // also contain duplicates if the seeder ran more than once.
            // We always use the fixed 6-item list that mirrors the navigation sidebar.
            CategoryComboBox.Items.Clear();
            foreach (var category in GetDefaultCategories())
            {
                CategoryComboBox.Items.Add(new ComboBoxItem
                {
                    Content = $"{category.Icon} {category.Name}".Trim(),
                    Tag = category,
                    Foreground = textBrush
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

        // Authenticator (TOTP) secret stored on the item
        TotpSecretTextBox.Text = TotpHelper.GetSecret(_editingItem) ?? string.Empty;

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

        // Set credit card-specific fields if applicable
        if (_editingItem.CreditCardItem != null)
        {
            CardholderNameTextBox.Text = _editingItem.CreditCardItem.CardholderName ?? string.Empty;
            CardNumberTextBox.Text = _editingItem.CreditCardItem.CardNumber ?? string.Empty;
            CVVTextBox.Text = _editingItem.CreditCardItem.CVV ?? string.Empty;
            ExpiryDateTextBox.Text = _editingItem.CreditCardItem.ExpiryDate ?? string.Empty;
            ValidFromTextBox.Text = _editingItem.CreditCardItem.ValidFrom ?? string.Empty;
            CreditCardNotesTextBox.Text = _editingItem.CreditCardItem.Notes ?? string.Empty;

            var cardTypeName = _editingItem.CreditCardItem.CardType.ToString();
            foreach (var obj in CardTypeComboBox.Items)
            {
                if (obj is ComboBoxItem cbi && string.Equals(cbi.Content?.ToString(), cardTypeName, StringComparison.OrdinalIgnoreCase))
                {
                    CardTypeComboBox.SelectedItem = cbi;
                    break;
                }
            }
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
            LoginFieldsPanel.Visibility         = selectedType is ItemType.Login or ItemType.Password ? Visibility.Visible : Visibility.Collapsed;
            CreditCardFieldsPanel.Visibility    = selectedType == ItemType.CreditCard  ? Visibility.Visible : Visibility.Collapsed;
            SecureNoteFieldsPanel.Visibility    = selectedType == ItemType.SecureNote  ? Visibility.Visible : Visibility.Collapsed;
            WiFiFieldsPanel.Visibility          = selectedType == ItemType.WiFi        ? Visibility.Visible : Visibility.Collapsed;
            PasskeyFieldsPanel.Visibility       = selectedType == ItemType.Passkey     ? Visibility.Visible : Visibility.Collapsed;
            IdentityFieldsPanel.Visibility      = selectedType == ItemType.Identity    ? Visibility.Visible : Visibility.Collapsed;
            APICredentialsFieldsPanel.Visibility = Visibility.Collapsed;

            // API Credentials panel replaces SecureNote when category contains "API"
            if (selectedType == ItemType.SecureNote &&
                CategoryComboBox.SelectedItem is ComboBoxItem catItem && catItem.Tag is Category cat2 &&
                cat2.Name.Contains("API", StringComparison.OrdinalIgnoreCase))
            {
                SecureNoteFieldsPanel.Visibility     = Visibility.Collapsed;
                APICredentialsFieldsPanel.Visibility = Visibility.Visible;
            }
        }
    }

    private void GeneratePasswordButton_Click(object sender, RoutedEventArgs e)
    {
        if (InlinePasswordGeneratorPanel.Visibility == Visibility.Visible)
        {
            InlinePasswordGeneratorPanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            InlinePasswordGeneratorPanel.Visibility = Visibility.Visible;
            GenerateNewPassword();
        }
    }

    private void PasswordOption_Changed(object sender, RoutedEventArgs e)
    {
        if (InlinePasswordGeneratorPanel?.Visibility == Visibility.Visible)
            GenerateNewPassword();
    }

    private void TogglePasswordVisibility_Click(object sender, RoutedEventArgs e)
    {
        if (PasswordTextBox.Visibility == Visibility.Visible)
        {
            PasswordTextBox.Visibility = Visibility.Collapsed;
            PasswordDisplayTextBox.Visibility = Visibility.Visible;
            PasswordDisplayTextBox.Text = PasswordTextBox.Password;
            if (TogglePasswordVisibilityButton.Content is TextBlock tb1)
                tb1.Text = ""; // EyeHide
        }
        else
        {
            PasswordTextBox.Visibility = Visibility.Visible;
            PasswordDisplayTextBox.Visibility = Visibility.Collapsed;
            if (TogglePasswordVisibilityButton.Content is TextBlock tb2)
                tb2.Text = ""; // Eye
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

            // TypeComboBox is auto-driven by category selection — if still unset, default to Login
            if (TypeComboBox.SelectedIndex < 0)
                TypeComboBox.SelectedIndex = 0;

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

            // Persist the passkey into the native Windows 10/11 credential store (Windows Hello /
            // KeyCredentialManager — the same secure subsystem Windows uses for platform passkeys).
            // The resulting device-bound key name is stored as the CredentialId so the passkey can
            // be re-opened/verified later.
            var keyName = BuildPasskeyKeyName(item.PasskeyItem);
            item.PasskeyItem.CredentialId = keyName;

            try
            {
                if (await _windowsHello.IsAvailableAsync())
                {
                    var registration = await _windowsHello.RegisterKeyAsync(keyName);
                    item.PasskeyItem.DeviceType = string.IsNullOrWhiteSpace(item.PasskeyItem.DeviceType)
                        ? "Windows Hello"
                        : item.PasskeyItem.DeviceType;
                    item.PasskeyItem.IsBackedUp = registration == Services.HelloResult.Success
                        || (PasskeyIsBackedUpCheckBox.IsChecked ?? false);
                }
            }
            catch
            {
                // Non-fatal: if the Windows credential store is unavailable we still save the item.
            }
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

            item.CreditCardItem.CardholderName = CardholderNameTextBox.Text?.Trim();
            item.CreditCardItem.CardNumber = CardNumberTextBox.Text?.Trim();
            item.CreditCardItem.CVV = CVVTextBox.Text?.Trim();
            item.CreditCardItem.ExpiryDate = ExpiryDateTextBox.Text?.Trim();
            item.CreditCardItem.ValidFrom = ValidFromTextBox.Text?.Trim();
            item.CreditCardItem.Notes = CreditCardNotesTextBox.Text?.Trim();
            item.CreditCardItem.LastModified = DateTime.UtcNow;

            if (CardTypeComboBox.SelectedItem is ComboBoxItem cardTypeItem &&
                Enum.TryParse<CardType>((cardTypeItem.Content?.ToString() ?? string.Empty).Replace(" ", string.Empty), true, out var parsedCardType))
            {
                item.CreditCardItem.CardType = parsedCardType;
            }
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
            PasskeyDeviceTypeTextBox.Text = "Windows Hello";

            if (!await _windowsHello.IsAvailableAsync())
            {
                await ShowErrorDialog("Windows Hello isn't set up on this PC. Add a PIN or fingerprint in Windows Settings → Accounts → Sign-in options, then try again.");
                return;
            }

            // Register the passkey credential with the native Windows credential store.
            var keyName = BuildPasskeyKeyName(new PasskeyItem
            {
                Username = PasskeyUsernameTextBox.Text?.Trim(),
                Website = PasskeyWebsiteTextBox.Text?.Trim(),
                WebsiteUrl = PasskeyUrlTextBox.Text?.Trim()
            });

            var result = await _windowsHello.RegisterKeyAsync(keyName);
            switch (result)
            {
                case Services.HelloResult.Success:
                    PasskeyIsBackedUpCheckBox.IsChecked = true;
                    await ShowSuccessMessage("Passkey saved to the Windows credential store via Windows Hello.");
                    break;
                case Services.HelloResult.Cancelled:
                    await ShowErrorDialog("Windows Hello prompt was cancelled.");
                    break;
                default:
                    await ShowErrorDialog("Could not save the passkey to Windows. Please try again.");
                    break;
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialog($"Error registering passkey: {ex.Message}");
        }
    }

    /// <summary>
    /// Builds a stable, unique Windows credential key name for a passkey so it can be stored in and
    /// later opened from the native Windows 10/11 credential (Windows Hello) subsystem.
    /// </summary>
    private static string BuildPasskeyKeyName(PasskeyItem passkey)
    {
        var site = passkey.WebsiteUrl ?? passkey.Website ?? "site";
        var user = passkey.Username ?? "user";

        static string Clean(string value)
        {
            var cleaned = new string(value.Where(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_').ToArray());
            return string.IsNullOrWhiteSpace(cleaned) ? "x" : cleaned;
        }

        return $"VaultGuard.Passkey.{Clean(site)}.{Clean(user)}";
    }

    /// <summary>
    /// Routes mouse-wheel input straight to the dialog's ScrollViewer. Without this the wheel is
    /// swallowed by inner controls (ComboBox, TextBox, the password generator panel, etc.) and the
    /// long form barely scrolls.
    /// </summary>
    private void DialogScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        if (e.Handled) return;
        if (sender is not ScrollViewer scrollViewer) return;

        scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - (e.Delta / 2.0));
        e.Handled = true;
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

    private void RefreshPassword_Click(object sender, RoutedEventArgs e)
    {
        GenerateNewPassword();
    }

    private void ClosePasswordGenerator_Click(object sender, RoutedEventArgs e)
    {
        InlinePasswordGeneratorPanel.Visibility = Visibility.Collapsed;
    }

    private void CancelPasswordGenerator_Click(object sender, RoutedEventArgs e)
    {
        InlinePasswordGeneratorPanel.Visibility = Visibility.Collapsed;
    }

    private void UseGeneratedPassword_Click(object sender, RoutedEventArgs e)
    {
        if (GeneratedPasswordText?.Text is string password &&
            password != "Click Regenerate to generate a password")
        {
            PasswordTextBox.Password = password;
            if (PasswordDisplayTextBox.Visibility == Visibility.Visible)
                PasswordDisplayTextBox.Text = password;
        }
        InlinePasswordGeneratorPanel.Visibility = Visibility.Collapsed;
    }

    private void GenerateNewPassword()
    {
        int length = (int)(PasswordLengthSlider?.Value ?? 20);
        bool upper   = UppercaseToggle?.IsChecked ?? true;
        bool lower   = LowercaseToggle?.IsChecked ?? true;
        bool numbers = NumbersToggle?.IsChecked  ?? true;
        bool symbols = SymbolsToggle?.IsChecked  ?? true;

        string chars = "";
        if (upper)   chars += "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        if (lower)   chars += "abcdefghijklmnopqrstuvwxyz";
        if (numbers) chars += "0123456789";
        if (symbols) chars += "!@#$%^&*()-_=+[]{}|;:,.<>?";
        if (string.IsNullOrEmpty(chars)) chars = "abcdefghijklmnopqrstuvwxyz";

        var random = new Random();
        var password = new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());

        if (GeneratedPasswordText != null)
            GeneratedPasswordText.Text = password;

        if (PasswordLengthText != null)
            PasswordLengthText.Text = length.ToString();
    }

    private void PasswordLengthSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
    {
        if (PasswordLengthText != null)
            PasswordLengthText.Text = ((int)e.NewValue).ToString();
        if (InlinePasswordGeneratorPanel?.Visibility == Visibility.Visible)
            GenerateNewPassword();
    }

    // Matches the 5 type-based nav items plus Identity — used only when DB has no categories
    private static List<Category> GetDefaultCategories() =>
    [
        new() { Id = 1, Name = "Login",       Icon = "🔐", Color = "#3b82f6" },
        new() { Id = 2, Name = "Credit Card", Icon = "💳", Color = "#10b981" },
        new() { Id = 3, Name = "Secure Note", Icon = "📝", Color = "#f59e0b" },
        new() { Id = 4, Name = "Wi-Fi",       Icon = "📶", Color = "#06b6d4" },
        new() { Id = 5, Name = "Passkey",     Icon = "🔑", Color = "#ec4899" },
        new() { Id = 6, Name = "Identity",    Icon = "👤", Color = "#10b981" },
    ];

    private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryComboBox.SelectedItem is not ComboBoxItem ci || ci.Tag is not Category cat)
            return;

        // Map category name → item type so the correct field panels show
        var name = cat.Name ?? string.Empty;
        int typeIndex = name.ToLower() switch
        {
            var n when n.Contains("card") || n.Contains("credit") || n.Contains("payment") || n.Contains("debit") => 1,   // CreditCard
            var n when n.Contains("note") || n.Contains("memo") || n.Contains("document") || n.Contains("text") => 2,     // SecureNote
            var n when n.Contains("wifi") || n.Contains("network") || n.Contains("wireless") || n.Contains("router") => 3,// WiFi
            var n when n.Contains("password") || n.Contains("pwd") || n.Contains("credential") => 4,                      // Password
            var n when n.Contains("passkey") || n.Contains("biometric") || n.Contains("fido") => 5,                       // Passkey
            var n when n.Contains("identity") || n.Contains("person") || n.Contains("profile") => 6,                      // Identity
            _ => 0 // Login (default — covers "login", "social", "email", "banking", "work", etc.)
        };

        if (TypeComboBox.SelectedIndex != typeIndex)
            TypeComboBox.SelectedIndex = typeIndex;
        else
            TypeComboBox_SelectionChanged(TypeComboBox, null!); // force refresh if same index
    }

    // Custom Fields Methods
    private void LoadCustomFields()
    {
        if (_editingItem?.CustomFields != null)
        {
            _brandIconDataUrl = BrandIconHelper.GetCustomBrandIconDataUrl(_editingItem);
            _customFields = _editingItem.CustomFields
                .Where(field => !string.Equals(field.Name, BrandIconHelper.BrandIconCustomFieldName, StringComparison.OrdinalIgnoreCase)
                             && !string.Equals(field.Name, TotpHelper.TotpCustomFieldName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        UpdateBrandIconStatus();
    }

    private void AddCustomField_Click(object sender, RoutedEventArgs e)
    {
        // Toggle the type picker panel
        FieldTypePickerPanel.Visibility = FieldTypePickerPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void FieldTypeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string typeStr && Enum.TryParse<CustomFieldType>(typeStr, out var fieldType))
        {
            var newField = new CustomField
            {
                Name = GetDefaultFieldName(fieldType),
                Value = "",
                Type = fieldType,
                DisplayOrder = _customFields.Count,
                PasswordItemId = _editingItem?.Id ?? 0
            };
            _customFields.Add(newField);
            RefreshCustomFieldsUI();
            FieldTypePickerPanel.Visibility = Visibility.Collapsed;
        }
    }

    private static string GetDefaultFieldName(CustomFieldType type) => type switch
    {
        CustomFieldType.Password => "Password",
        CustomFieldType.Email    => "Email",
        CustomFieldType.Url      => "URL",
        CustomFieldType.Phone    => "Phone",
        CustomFieldType.Number   => "Number",
        CustomFieldType.Date     => "Date",
        CustomFieldType.Toggle   => "Yes / No",
        CustomFieldType.TextArea => "Notes",
        _                        => "Text Field",
    };

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
        field.LastModified = DateTime.UtcNow;
    }

    private void OnCustomFieldRemoved(CustomField field)
    {
        _customFields.Remove(field);
        RefreshCustomFieldsUI();
    }

    private void UpdateCustomFieldsInPasswordItem(PasswordItem item)
    {
        int order = 0;
        foreach (var f in _customFields)
            f.DisplayOrder = order++;

        if (item.Id == 0)
        {
            // New item: populate the empty list; EF Core assigns PasswordItemId after the parent INSERT
            foreach (var field in _customFields)
                item.CustomFields.Add(field);
        }
        else
        {
            // Edit item: sync the tracked collection
            // Remove fields the user deleted
            var toRemove = item.CustomFields
                .Where(existing => !_customFields.Any(cf => cf.Id > 0 && cf.Id == existing.Id))
                .ToList();
            foreach (var f in toRemove)
                item.CustomFields.Remove(f);

            // Add brand-new fields (Id == 0)
            foreach (var field in _customFields.Where(cf => cf.Id == 0))
            {
                field.PasswordItemId = item.Id;
                item.CustomFields.Add(field);
            }
        }

        if (!string.IsNullOrWhiteSpace(_brandIconDataUrl))
            BrandIconHelper.SetCustomBrandIcon(item, _brandIconDataUrl);

        // Persist the authenticator (TOTP) secret (empty clears it).
        TotpHelper.SetSecret(item, TotpSecretTextBox?.Text);
    }

    private void UpdateBrandIconStatus()
    {
        if (BrandIconStatusText == null || RemoveBrandIconButton == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_brandIconDataUrl))
        {
            BrandIconStatusText.Text = "We'll use the website icon automatically, or you can upload your own.";
            RemoveBrandIconButton.Visibility = Visibility.Collapsed;
            return;
        }

        BrandIconStatusText.Text = "A custom brand icon will be saved with this item.";
        RemoveBrandIconButton.Visibility = _isReadOnly ? Visibility.Collapsed : Visibility.Visible;
    }

    private async void UploadBrandIcon_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp;*.svg",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var fileInfo = new FileInfo(dialog.FileName);
            if (fileInfo.Length > BrandIconHelper.MaxBrandIconBytes)
            {
                await ShowErrorDialog("Brand icons must be 1 MB or smaller.");
                return;
            }

            var bytes = await File.ReadAllBytesAsync(dialog.FileName);
            var mimeType = GetImageMimeType(dialog.FileName);
            if (mimeType == null)
            {
                await ShowErrorDialog("Please choose a supported image file.");
                return;
            }

            _brandIconDataUrl = $"data:{mimeType};base64,{Convert.ToBase64String(bytes)}";
            UpdateBrandIconStatus();
        }
        catch (Exception ex)
        {
            await ShowErrorDialog($"Unable to load brand icon: {ex.Message}");
        }
    }

    private void RemoveBrandIcon_Click(object sender, RoutedEventArgs e)
    {
        _brandIconDataUrl = null;
        UpdateBrandIconStatus();
    }

    private static string? GetImageMimeType(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            _ => null
        };
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
