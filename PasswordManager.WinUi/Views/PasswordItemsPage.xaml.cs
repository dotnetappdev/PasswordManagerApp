using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.WinUi.ViewModels;
using PasswordManager.WinUi.Helpers;
using PasswordManager.WinUi.Models;
using PasswordManager.Models;
using PasswordManager.Services.Interfaces;
using System.Linq;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace PasswordManager.WinUi.Views;

public sealed partial class PasswordItemsPage : Page
{
    private PasswordItemsViewModel? _viewModel;
    private IServiceProvider? _serviceProvider;
    private PasswordItem? _selectedItem;
    private ICategoryInterface? _categoryService;
    private List<Category> _categories = new();
    private List<CustomField> _customFields = new();
    private ICustomFieldService? _customFieldService;

    public PasswordItemsPage()
    {
        this.InitializeComponent();
    }

    private T? GetElement<T>(string name) where T : class
    {
        try
        {
            return this.FindName(name) as T;
        }
        catch
        {
            return null;
        }
    }

    protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // Handle both old service provider parameter and new filter data parameter
        if (e.Parameter is NavigationFilterData filterData)
        {
            _serviceProvider = filterData.ServiceProvider;
            _categoryService = _serviceProvider.GetRequiredService<ICategoryInterface>();
            _customFieldService = _serviceProvider.GetService<ICustomFieldService>();
            _viewModel = new PasswordItemsViewModel(_serviceProvider);
            this.DataContext = _viewModel;

            // Apply the filter from navigation
            ApplyNavigationFilter(filterData);
        }
        else if (e.Parameter is IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _categoryService = serviceProvider.GetRequiredService<ICategoryInterface>();
            _customFieldService = serviceProvider.GetService<ICustomFieldService>();
            _viewModel = new PasswordItemsViewModel(serviceProvider);
            this.DataContext = _viewModel;
        }

        if (_serviceProvider != null)
        {
            // Seed sample data if needed (only runs once)
            await SampleDataSeeder.SeedSampleDataAsync(_serviceProvider);

            // Load categories from database
            await LoadCategoriesAsync();
        }
    }

    private void ApplyNavigationFilter(NavigationFilterData filterData)
    {
        if (_viewModel == null) return;

        // Update page title
        if (!string.IsNullOrEmpty(filterData.FilterName))
        {
            var contentTitle = GetElement<TextBlock>("ContentTitle");
            var contentSubtitle = GetElement<TextBlock>("ContentSubtitle");
            if (contentTitle != null) contentTitle.Text = filterData.FilterName;
            if (contentSubtitle != null) contentSubtitle.Text = $"Showing {filterData.FilterName.ToLower()}";
        }

        // Apply type filter
        if (filterData.FilterType.HasValue)
        {
            _viewModel.FilterType = filterData.FilterType.Value.ToString();
        }

        // Apply favorites filter
        if (filterData.ShowFavorites == true)
        {
            _viewModel.FilterType = "Favorites";
        }

        // Apply archived filter
        if (filterData.ShowArchived == true)
        {
            _viewModel.FilterType = "Archive";
        }

        // Apply deleted filter
        if (filterData.ShowDeleted == true)
        {
            _viewModel.FilterType = "RecentlyDeleted";
        }
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            if (_categoryService != null)
            {
                _categories = await _categoryService.GetAllAsync();
                await PopulateCategoryDropdownAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading categories: {ex.Message}");
        }
    }

    private async Task PopulateCategoryDropdownAsync()
    {

        var categoryDropdown = GetElement<ComboBox>("CategoryDropdown");
        if (categoryDropdown == null)
        {
            // If the control is missing, skip population to avoid exceptions
            return;
        }
        categoryDropdown.Items.Clear();

        // Add "All Categories" option
        var allCategoriesItem = new ComboBoxItem();
        var allStackPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        allStackPanel.Children.Add(new Border
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray),
            CornerRadius = new CornerRadius(4),
            Width = 16,
            Height = 16
        });
        allStackPanel.Children.Add(new TextBlock { Text = "All Categories", FontWeight = Microsoft.UI.Text.FontWeights.Medium });
        allCategoriesItem.Content = allStackPanel;
        allCategoriesItem.Tag = "all";
        categoryDropdown.Items.Add(allCategoriesItem);

        // Add categories from database
        foreach (var category in _categories)
        {
            var item = new ComboBoxItem();
            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };

            // Add color indicator
            var colorBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush();
            if (!string.IsNullOrEmpty(category.Color) && Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Microsoft.UI.Xaml.Media.SolidColorBrush), category.Color) is Microsoft.UI.Xaml.Media.SolidColorBrush brush)
            {
                colorBrush = brush;
            }
            else
            {
                colorBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray);
            }

            stackPanel.Children.Add(new Border
            {
                Background = colorBrush,
                CornerRadius = new CornerRadius(4),
                Width = 16,
                Height = 16
            });

            // Add category name and count
            var categoryText = new TextBlock
            {
                Text = category.Name,
                FontWeight = Microsoft.UI.Text.FontWeights.Medium
            };
            stackPanel.Children.Add(categoryText);

            // Add password count if available
            if (_categoryService != null)
            {
                try
                {
                    var count = await _categoryService.GetPasswordItemCountAsync(category.Id);
                    if (count > 0)
                    {
                        stackPanel.Children.Add(new TextBlock
                        {
                            Text = $"({count})",
                            FontSize = 12,
                            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
                            Margin = new Thickness(4, 0, 0, 0)
                        });
                    }
                }
                catch
                {
                    // Ignore count errors
                }
            }

            item.Content = stackPanel;
            item.Tag = category;
            categoryDropdown.Items.Add(item);
        }

        // Select first item (All Categories)
        categoryDropdown.SelectedIndex = 0;
    }

    public void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_viewModel != null && sender is TextBox textBox)
        {
            _viewModel.SearchText = textBox.Text;
        }
    }

    private void FilterButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string filterType)
        {
            // Update UI to show selected filter
            UpdateFilterButtonStyles(button);

            // Apply filter to view model
            if (_viewModel != null)
            {
                _viewModel.FilterType = filterType;
            }

            // Update content titles
            UpdateContentTitles(filterType);
        }
    }

    private void UpdateFilterButtonStyles(Button selectedButton)
    {
        // Safely reset filter buttons if they exist (some layouts removed buttons)
        try
        {
            var allItemsBtn = this.FindName("AllItemsButton") as Button;
            if (allItemsBtn != null)
            {
                allItemsBtn.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
            }

            // Example: if there are more named filter buttons, try to reset them too
            var namedButtons = new[] { "AllItemsButton", "FavoritesButton", "RecentButton", "LoginButton" };
            foreach (var name in namedButtons)
            {
                var btn = this.FindName(name) as Button;
                if (btn != null)
                {
                    btn.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
                }
            }

            // Set selected button style if provided (defensive resource lookup)
            if (selectedButton != null)
            {
                selectedButton.Background = Helpers.ResourceHelper.GetBrush("ModernPrimaryBrush", new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent));
            }
        }
        catch
        {
            // Defensive: if FindName or resource lookup fails, ignore and continue
        }
    }

    private void UpdateContentTitles(string filterType)
    {
        var contentTitle = GetElement<TextBlock>("ContentTitle");
        var contentSubtitle = GetElement<TextBlock>("ContentSubtitle");
        if (contentTitle == null || contentSubtitle == null) return;

        switch (filterType)
        {
            case "All":
                contentTitle.Text = "All Items";
                contentSubtitle.Text = "Showing all password items";
                break;
            case "Favorites":
                contentTitle.Text = "Favorites";
                contentSubtitle.Text = "Your favorite password items";
                break;
            case "Recent":
                contentTitle.Text = "Recently Used";
                contentSubtitle.Text = "Recently accessed items";
                break;
            case "Login":
                contentTitle.Text = "Logins";
                contentSubtitle.Text = "Login credentials";
                break;
            case "CreditCard":
                contentTitle.Text = "Credit Cards";
                contentSubtitle.Text = "Payment card information";
                break;
            case "SecureNote":
                contentTitle.Text = "Secure Notes";
                contentSubtitle.Text = "Private notes and documents";
                break;
        }
    }

    private void CategoryDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            if (_viewModel != null)
            {
                if (selectedItem.Tag is string tag && tag == "all")
                {
                    // Show all items
                    _viewModel.SelectedCategoryId = null;
                }
                else if (selectedItem.Tag is Category category)
                {
                    // Filter by specific category using CategoryId only
                    _viewModel.SelectedCategoryId = category.Id;
                }
            }
        }
    }

    private void ItemsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var listView = sender as ListView ?? GetElement<ListView>("ItemsList");
        if (listView != null && listView.SelectedItem is PasswordItem selectedItem)
        {
            _selectedItem = selectedItem;
            ShowItemDetails(selectedItem);
        }
    }

    private void PasswordCardsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Card view removed; keep method stub for compatibility if referenced elsewhere
    }

    private void RevealPasswordButton_Click(object sender, RoutedEventArgs e)
    {
        var list = GetElement<ListView>("ItemsList");
        var selected = _selectedItem ?? (list?.SelectedItem as PasswordItem);
        if (selected == null) return;

        var detailPassword = GetElement<TextBlock>("DetailPassword");
        if (detailPassword == null) return;

        // Toggle between masked and plain text
        if (!string.IsNullOrEmpty(detailPassword.Text) && detailPassword.Text.StartsWith("•"))
        {
            // Show actual password if available
            detailPassword.Text = selected.Password ?? selected.LoginItem?.Password ?? "";
        }
        else
        {
            // Mask
            var pwd = selected.Password ?? selected.LoginItem?.Password ?? "";
            detailPassword.Text = string.IsNullOrEmpty(pwd) ? "" : new string('•', Math.Max(8, pwd.Length));
        }
    }

    private async void CopyPasswordButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var list = GetElement<ListView>("ItemsList");
            var selected = _selectedItem ?? (list?.SelectedItem as PasswordItem);
            var pwd = selected?.Password ?? selected?.LoginItem?.Password;
            if (string.IsNullOrEmpty(pwd))
            {
                await ShowTemporaryMessageAsync("No password available to copy");
                return;
            }

            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(pwd);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
            await ShowTemporaryMessageAsync("Password copied to clipboard");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error copying password: {ex.Message}");
        }
    }

    private async void OpenWebsiteButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var list = GetElement<ListView>("ItemsList");
            var selected = _selectedItem ?? (list?.SelectedItem as PasswordItem);
            var url = selected?.Website ?? selected?.LoginItem?.WebsiteUrl;
            if (string.IsNullOrEmpty(url))
            {
                await ShowTemporaryMessageAsync("No website URL available");
                return;
            }

            var uri = new Uri(url);
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error opening website: {ex.Message}");
        }
    }

    private async Task ShowTemporaryMessageAsync(string message)
    {
        try
        {
            // Try to show a subtle notification instead of a dialog
            var contentSubtitle = GetElement<TextBlock>("ContentSubtitle");
            if (contentSubtitle != null)
            {
                var originalText = contentSubtitle.Text;
                contentSubtitle.Text = $"✓ {message}";
                contentSubtitle.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
                
                // Reset after a delay
                await Task.Delay(2000);
                contentSubtitle.Text = originalText;
                contentSubtitle.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray);
            }
            else
            {
                // Fallback to dialog if we can't find the subtitle element
                var dialog = new ContentDialog
                {
                    Title = "",
                    Content = message,
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }
        catch
        {
            // Fallback to debug output if UI updates fail
            System.Diagnostics.Debug.WriteLine($"Notification: {message}");
        }
    }

    private void ShowItemDetails(PasswordItem item)
    {
        if (item == null) return;

        // Safe lookups for all named XAML elements to avoid compile-time errors when XAML g.i.cs is missing
        var detailPanel = GetElement<Grid>("DetailPanel");
        var detailTitle = GetElement<TextBlock>("DetailTitle");
        var detailSubtitle = GetElement<TextBlock>("DetailSubtitle");
        var detailItemTitle = GetElement<TextBlock>("DetailItemTitle");
        var detailItemSubtitle = GetElement<TextBlock>("DetailItemSubtitle");
        var detailUsername = GetElement<TextBlock>("DetailUsername");
        var detailWebsite = GetElement<TextBlock>("DetailWebsite");
        var detailPassword = GetElement<TextBlock>("DetailPassword");
        var detailIcon = GetElement<TextBlock>("DetailIcon");
        var detailCategory = GetElement<TextBlock>("DetailCategory");

        if (detailPanel != null) detailPanel.Visibility = Visibility.Visible;
        if (detailTitle != null) detailTitle.Text = "Item Details";
        if (detailSubtitle != null) detailSubtitle.Text = $"Details for {item.Title}";
        if (detailItemTitle != null) detailItemTitle.Text = item.Title;

        // Prefer login-style display. If item has a LoginItem use that. If the item category contains "Identity",
        // treat it like a login and map passkey/secure-note data where possible into username/website fields.
        string username = string.Empty;
        string website = string.Empty;
        string pwd = string.Empty;

        if (item.LoginItem != null)
        {
            username = item.LoginItem.Username ?? item.Username ?? string.Empty;
            website = item.LoginItem.WebsiteUrl ?? item.Website ?? string.Empty;
            pwd = item.LoginItem.Password ?? item.Password ?? string.Empty;
        }
        else if (item.Category != null && item.Category.Name != null && item.Category.Name.IndexOf("Identity", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            // Treat identity category as login-style: prefer passkey username/website where available
            username = item.PasskeyItem?.Username ?? item.Username ?? string.Empty;
            website = item.PasskeyItem?.WebsiteUrl ?? item.Website ?? string.Empty;
            // Identity items typically don't have a password; prefer stored Password field if present
            pwd = item.Password ?? string.Empty;
        }
        else
        {
            username = item.Username ?? string.Empty;
            website = item.Website ?? string.Empty;
            pwd = item.Password ?? string.Empty;
        }

        if (detailItemSubtitle != null) detailItemSubtitle.Text = item.Description ?? (!string.IsNullOrEmpty(username) ? username : "No additional information");
        if (detailUsername != null) detailUsername.Text = username;
        if (detailWebsite != null) detailWebsite.Text = website;

        // Show masked password if there is one
        if (detailPassword != null) detailPassword.Text = string.IsNullOrEmpty(pwd) ? string.Empty : new string('•', Math.Max(8, pwd.Length));

        // Update icon based on type
        if (detailIcon != null) detailIcon.Text = GetTypeIcon(item.Type.ToString());

        // Category
        var cat = item.Category;
        if (detailCategory != null) detailCategory.Text = cat != null ? cat.Name : "Uncategorized";
    }

    private string GetTypeIcon(string type)
    {
        // Use the new IconHelper to get emoji for type
        if (Enum.TryParse<ItemType>(type, true, out var itemType))
        {
            return IconHelper.GetEmojiForType(itemType);
        }
        return IconHelper.GetEmojiForIcon("folder");
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
        try
        {
            // Check if service provider is available
            if (_serviceProvider == null)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = "Service provider not initialized. Please navigate to this page properly.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
                return;
            }

            // First show the item type selection dialog (1Password style)
            var typeSelectionDialog = new Dialogs.ItemTypeSelectionDialog();
            typeSelectionDialog.XamlRoot = this.XamlRoot;

            var typeResult = await typeSelectionDialog.ShowAsync();
            if (typeResult == ContentDialogResult.Primary || typeSelectionDialog.SelectedItemType != null)
            {
                // Then show the main add dialog with the selected type pre-filled
                var dialog = new Dialogs.AddPasswordDialog(_serviceProvider);
                dialog.XamlRoot = this.XamlRoot;

                // Pre-select the item type if one was chosen
                if (typeSelectionDialog.SelectedItemType.HasValue)
                {
                    // Pass the selected type to the dialog
                    dialog.SetInitialItemType(typeSelectionDialog.SelectedItemType.Value, typeSelectionDialog.SelectedCategoryName);
                }

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary && dialog.Result != null)
                {
                    // Refresh the list to show the new item
                    if (_viewModel != null)
                    {
                        await _viewModel.RefreshAsync();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            var errorDialog = new ContentDialog
            {
                Title = "Error",
                Content = $"Error adding password: {ex.Message}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await errorDialog.ShowAsync();
        }
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

    private bool _isInEditMode = false;

    private async void EditDetailButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedItem == null) return;

        if (!_isInEditMode)
        {
            // Switch to edit mode
            await EnterEditMode(_selectedItem);
        }
        else
        {
            // Save changes and exit edit mode
            await SaveAndExitEditMode();
        }
    }

    private async Task EnterEditMode(PasswordItem item)
    {
        try
        {
            _isInEditMode = true;

            // Update button text and icon
            var editButton = GetElement<TextBlock>("EditDetailText");
            var editIcon = GetElement<FontIcon>("EditDetailIcon");
            if (editButton != null) editButton.Text = "Save";
            if (editIcon != null) editIcon.Glyph = "\uE74E"; // Save icon

            // Show edit fields and hide view fields
            ToggleEditMode(true);

            // Load current values into edit fields
            LoadValuesIntoEditFields(item);

            // Load categories for the dropdown
            await LoadCategoriesForEdit();

            // Load custom fields
            await LoadCustomFields();
        }
        catch (Exception ex)
        {
            await ShowErrorDialog($"Error entering edit mode: {ex.Message}");
        }
    }

    private async Task SaveAndExitEditMode()
    {
        try
        {
            if (_selectedItem == null || _serviceProvider == null) return;

            // Get the password service
            var passwordService = _serviceProvider.GetRequiredService<IPasswordItemService>();

            // Update the item with values from edit fields
            UpdateItemFromEditFields(_selectedItem);

            // Save custom fields first
            await SaveCustomFields();

            // Save to database
            await passwordService.UpdateAsync(_selectedItem);

            // Refresh the view model
            if (_viewModel != null)
            {
                await _viewModel.RefreshAsync();
            }

            // Exit edit mode
            await ExitEditMode();

            // Refresh the item details
            ShowItemDetails(_selectedItem);

            await ShowTemporaryMessageAsync("Item updated successfully");
        }
        catch (Exception ex)
        {
            await ShowErrorDialog($"Error saving changes: {ex.Message}");
        }
    }

    private async Task ExitEditMode()
    {
        _isInEditMode = false;

        // Update button text and icon back to Edit
        var editButton = GetElement<TextBlock>("EditDetailText");
        var editIcon = GetElement<FontIcon>("EditDetailIcon");
        if (editButton != null) editButton.Text = "Edit";
        if (editIcon != null) editIcon.Glyph = "\uE70F"; // Edit icon

        // Hide edit fields and show view fields
        ToggleEditMode(false);
    }

    private void ToggleEditMode(bool isEdit)
    {
        // Toggle Title field
        var editTitleField = GetElement<StackPanel>("EditTitleField");
        if (editTitleField != null) editTitleField.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;

        // Toggle Username fields
        var detailUsername = GetElement<Controls.ReadOnlyField>("DetailUsername");
        var editUsernameTextBox = GetElement<TextBox>("EditUsernameTextBox");
        if (detailUsername != null) detailUsername.Visibility = isEdit ? Visibility.Collapsed : Visibility.Visible;
        if (editUsernameTextBox != null) editUsernameTextBox.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;

        // Toggle Password fields
        var detailPassword = GetElement<Controls.ReadOnlyField>("DetailPassword");
        var editPasswordBox = GetElement<PasswordBox>("EditPasswordBox");
        var generatePasswordButton = GetElement<Button>("GeneratePasswordButton");
        if (detailPassword != null) detailPassword.Visibility = isEdit ? Visibility.Collapsed : Visibility.Visible;
        if (editPasswordBox != null) editPasswordBox.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;
        if (generatePasswordButton != null) generatePasswordButton.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;

        // Toggle Website fields
        var detailWebsite = GetElement<Controls.ReadOnlyField>("DetailWebsite");
        var editWebsiteTextBox = GetElement<TextBox>("EditWebsiteTextBox");
        if (detailWebsite != null) detailWebsite.Visibility = isEdit ? Visibility.Collapsed : Visibility.Visible;
        if (editWebsiteTextBox != null) editWebsiteTextBox.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;

        // Toggle edit-only fields
        var editDescriptionField = GetElement<StackPanel>("EditDescriptionField");
        var editCategoryField = GetElement<StackPanel>("EditCategoryField");
        var editCustomFieldsSection = GetElement<StackPanel>("EditCustomFieldsSection");
        if (editDescriptionField != null) editDescriptionField.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;
        if (editCategoryField != null) editCategoryField.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;
        if (editCustomFieldsSection != null) editCustomFieldsSection.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;
    }

    private void LoadValuesIntoEditFields(PasswordItem item)
    {
        var editTitleTextBox = GetElement<TextBox>("EditTitleTextBox");
        var editUsernameTextBox = GetElement<TextBox>("EditUsernameTextBox");
        var editPasswordBox = GetElement<PasswordBox>("EditPasswordBox");
        var editWebsiteTextBox = GetElement<TextBox>("EditWebsiteTextBox");
        var editDescriptionTextBox = GetElement<TextBox>("EditDescriptionTextBox");

        if (editTitleTextBox != null) editTitleTextBox.Text = item.Title ?? "";
        if (editUsernameTextBox != null) editUsernameTextBox.Text = item.Username ?? "";
        if (editPasswordBox != null) editPasswordBox.Password = item.Password ?? "";
        if (editWebsiteTextBox != null) editWebsiteTextBox.Text = item.Website ?? "";
        if (editDescriptionTextBox != null) editDescriptionTextBox.Text = item.Description ?? "";
    }

    private void UpdateItemFromEditFields(PasswordItem item)
    {
        var editTitleTextBox = GetElement<TextBox>("EditTitleTextBox");
        var editUsernameTextBox = GetElement<TextBox>("EditUsernameTextBox");
        var editPasswordBox = GetElement<PasswordBox>("EditPasswordBox");
        var editWebsiteTextBox = GetElement<TextBox>("EditWebsiteTextBox");
        var editDescriptionTextBox = GetElement<TextBox>("EditDescriptionTextBox");
        var editCategoryComboBox = GetElement<ComboBox>("EditCategoryComboBox");

        if (editTitleTextBox != null) item.Title = editTitleTextBox.Text;
        if (editUsernameTextBox != null) item.Username = editUsernameTextBox.Text;
        if (editPasswordBox != null) item.Password = editPasswordBox.Password;
        if (editWebsiteTextBox != null) item.Website = editWebsiteTextBox.Text;
        if (editDescriptionTextBox != null) item.Description = editDescriptionTextBox.Text;

        if (editCategoryComboBox?.SelectedItem is Category selectedCategory)
        {
            item.CategoryId = selectedCategory.Id;
            item.Category = selectedCategory;
        }
    }

    private async Task LoadCategoriesForEdit()
    {
        if (_categoryService == null) return;

        try
        {
            var categories = await _categoryService.GetAllAsync();
            var editCategoryComboBox = GetElement<ComboBox>("EditCategoryComboBox");
            
            if (editCategoryComboBox != null)
            {
                editCategoryComboBox.Items.Clear();
                foreach (var category in categories)
                {
                    editCategoryComboBox.Items.Add(category);
                }

                // Select current category if available
                if (_selectedItem?.Category != null)
                {
                    var currentCategory = categories.FirstOrDefault(c => c.Id == _selectedItem.CategoryId);
                    if (currentCategory != null)
                    {
                        editCategoryComboBox.SelectedItem = currentCategory;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading categories: {ex.Message}");
        }
    }

    private async void GeneratePasswordButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Simple password generation - in a real app, you'd use a proper password generator
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            var password = new string(Enumerable.Repeat(chars, 16)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            var editPasswordBox = GetElement<PasswordBox>("EditPasswordBox");
            if (editPasswordBox != null)
            {
                editPasswordBox.Password = password;
            }

            await ShowTemporaryMessageAsync("New password generated");
        }
        catch (Exception ex)
        {
            await ShowErrorDialog($"Error generating password: {ex.Message}");
        }
    }

    private async void CopyUsernameButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var username = _selectedItem?.Username;
            if (string.IsNullOrEmpty(username))
            {
                await ShowTemporaryMessageAsync("No username available to copy");
                return;
            }

            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(username);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
            await ShowTemporaryMessageAsync("Username copied to clipboard");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error copying username: {ex.Message}");
        }
    }

    private void CategoryFilterBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            var query = sender.Text.ToLower();
            var suggestions = _categories
                .Where(c => c.Name.ToLower().Contains(query))
                .Select(c => c.Name)
                .ToList();
            
            sender.ItemsSource = suggestions;
        }
    }

    private void CategoryFilterBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is string selectedCategory && _viewModel != null)
        {
            var category = _categories.FirstOrDefault(c => c.Name == selectedCategory);
            if (category != null)
            {
                _viewModel.SelectedCategoryId = category.Id;
            }
        }
    }

    private void AddCategoryButton_Click(object sender, RoutedEventArgs e)
    {
        // Navigate to Categories page via main window
        try
        {
            var mainWindow = GetMainWindow();
            if (mainWindow != null)
            {
                mainWindow.NavigateToPage("Categories");
            }
            else
            {
                // Fallback navigation
                Frame?.Navigate(typeof(CategoriesPage), _serviceProvider);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error navigating to Categories page: {ex.Message}");
        }
    }

    private MainWindow? GetMainWindow()
    {
        // Use the MainWindow property exposed in App
        return (App.Current as App)?.MainWindow;
    }

    private void ItemsList_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        var list = GetElement<ListView>("ItemsList");
        if (list != null && list.SelectedItem is PasswordItem item)
        {
            // Open password details view
            ShowPasswordDetails(item);
        }
    }

    private async void ShowPasswordDetails(PasswordItem item)
    {
        try
        {
            if (_serviceProvider is null)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = "Service provider not initialized. Please navigate to this page properly.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
                return;
            }

            // Use the AddPasswordDialog in read-only mode for a richer view
            var dialog = new Dialogs.AddPasswordDialog(_serviceProvider, item, true);
            dialog.XamlRoot = this.XamlRoot;
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            var errorDialog = new ContentDialog
            {
                Title = "Error",
                Content = $"Error showing password details: {ex.Message}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await errorDialog.ShowAsync();
        }
    }

    private async void ViewDetailButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedItem is null)
        {
            await ShowTemporaryMessageAsync("No item selected to view");
            return;
        }

        try
        {
            if (_serviceProvider is null)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = "Service provider not initialized. Please navigate to this page properly.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
                return;
            }

            var dialog = new Dialogs.AddPasswordDialog(_serviceProvider, _selectedItem, true);
            dialog.XamlRoot = this.XamlRoot;
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            var errorDialog = new ContentDialog
            {
                Title = "Error",
                Content = $"Error opening view dialog: {ex.Message}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await errorDialog.ShowAsync();
        }
    }

    private void ClearFilters_Click(object sender, RoutedEventArgs e)
    {
        // Clear all filter checkboxes and reset to default state
        if (_viewModel != null)
        {
            _viewModel.FilterType = "All";
        }

        // Close the filter flyout (safe)
        var filterFlyout = GetElement<Flyout>("FilterFlyout");
        filterFlyout?.Hide();

        // Update content titles
        UpdateContentTitles("All");
    }

    private async void ApplyFilters_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Get the filter flyout once and reuse to avoid variable shadowing
            var filterFlyout = GetElement<Flyout>("FilterFlyout");

            // Apply the selected filters based on checkbox states in the filter flyout
            if (_viewModel != null)
            {
                // Get the filter flyout content and read checkbox states
                if (filterFlyout?.Content is StackPanel filterPanel)
                {
                    // Apply filters based on checkbox selections
                    // This would be implemented based on the specific filter criteria
                    // For now, we'll apply a general filter update
                    await _viewModel.ApplyFilters();
                }
            }

            // Close the flyout
            filterFlyout?.Hide();
            
            // Show feedback to user
            ShowFilterAppliedFeedback();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error applying filters: {ex.Message}");
        }
    }

    private void ShowFilterAppliedFeedback()
    {
        // Provide visual feedback that filters have been applied
        // This could be a subtle animation or status update
        try
        {
            var contentTitle = GetElement<TextBlock>("ContentTitle");
            if (contentTitle != null)
            {
                // Temporarily update the subtitle to show filter feedback
                var contentSubtitle = GetElement<TextBlock>("ContentSubtitle");
                if (contentSubtitle != null)
                {
                    var originalText = contentSubtitle.Text;
                    contentSubtitle.Text = "Filters applied";
                    
                    // Reset after a brief delay
                    Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(
                        Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                    {
                        Task.Delay(2000).ContinueWith(_ =>
                        {
                            Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
                            {
                                contentSubtitle.Text = originalText;
                            });
                        });
                    });
                }
            }
        }
        catch
        {
            // Ignore feedback errors
        }
    }

    private void CategorySearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox searchBox)
        {
            string searchText = searchBox.Text?.ToLower() ?? "";
            FilterCategoryDropdown(searchText);
        }
    }

    private void FilterCategoryDropdown(string searchText)
    {
        var categoryDropdown = GetElement<ComboBox>("CategoryDropdown");
        if (categoryDropdown == null) return;

        categoryDropdown.Items.Clear();

        // Always add "All Categories" option
        var allCategoriesItem = new ComboBoxItem();
        var allStackPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        allStackPanel.Children.Add(new Border
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray),
            CornerRadius = new CornerRadius(4),
            Width = 16,
            Height = 16
        });
        allStackPanel.Children.Add(new TextBlock { Text = "All Categories", FontWeight = Microsoft.UI.Text.FontWeights.Medium });
        allCategoriesItem.Content = allStackPanel;
        allCategoriesItem.Tag = "all";
        categoryDropdown.Items.Add(allCategoriesItem);

        // Filter categories based on search text
        var filteredCategories = string.IsNullOrEmpty(searchText) 
            ? _categories 
            : _categories.Where(c => c.Name.ToLower().Contains(searchText)).ToList();

        // Add filtered categories to dropdown
        foreach (var category in filteredCategories)
        {
            var item = new ComboBoxItem();
            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };

            // Add color indicator
            var colorBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush();
            if (!string.IsNullOrEmpty(category.Color) && Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Microsoft.UI.Xaml.Media.SolidColorBrush), category.Color) is Microsoft.UI.Xaml.Media.SolidColorBrush brush)
            {
                colorBrush = brush;
            }
            else
            {
                colorBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray);
            }

            stackPanel.Children.Add(new Border
            {
                Background = colorBrush,
                CornerRadius = new CornerRadius(4),
                Width = 16,
                Height = 16
            });

            // Add category name
            stackPanel.Children.Add(new TextBlock 
            { 
                Text = category.Name, 
                FontWeight = Microsoft.UI.Text.FontWeights.Medium 
            });

            item.Content = stackPanel;
            item.Tag = category;
            categoryDropdown.Items.Add(item);
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

    private async Task ShowTemporaryMessageAsync(string message)
    {
        // In a real application, you might use a more sophisticated notification system
        // For now, we'll just show a simple content dialog that auto-closes
        var messageDialog = new ContentDialog
        {
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        
        // Auto-close after 2 seconds
        var timer = new System.Threading.Timer(_ =>
        {
            Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
            {
                try
                {
                    messageDialog.Hide();
                }
                catch { /* Ignore if already closed */ }
            });
        }, null, 2000, System.Threading.Timeout.Infinite);

        await messageDialog.ShowAsync();
        timer.Dispose();
    }

    #region Custom Fields Management

    private async void AddCustomFieldButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Show field type selection dialog
            var typeDialog = new ContentDialog
            {
                Title = "Add Custom Field",
                Content = await CreateCustomFieldTypeSelector(),
                PrimaryButtonText = "Add",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            var result = await typeDialog.ShowAsync();
            if (result == ContentDialogResult.Primary && typeDialog.Content is Grid grid)
            {
                var nameBox = grid.Children.OfType<TextBox>().FirstOrDefault(tb => tb.Name == "FieldNameBox");
                var typeCombo = grid.Children.OfType<ComboBox>().FirstOrDefault(cb => cb.Name == "FieldTypeCombo");

                if (nameBox != null && typeCombo != null && !string.IsNullOrWhiteSpace(nameBox.Text))
                {
                    var fieldType = (CustomFieldType)(typeCombo.SelectedIndex + 1);
                    await AddCustomField(nameBox.Text.Trim(), fieldType);
                }
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialog($"Error adding custom field: {ex.Message}");
        }
    }

    private async Task<Grid> CreateCustomFieldTypeSelector()
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Field name input
        var nameLabel = new TextBlock { Text = "Field Name", Margin = new Thickness(0, 0, 0, 8) };
        var nameBox = new TextBox 
        { 
            Name = "FieldNameBox",
            PlaceholderText = "Enter field name",
            Margin = new Thickness(0, 0, 0, 16)
        };

        // Field type selector
        var typeLabel = new TextBlock { Text = "Field Type", Margin = new Thickness(0, 0, 0, 8) };
        var typeCombo = new ComboBox 
        { 
            Name = "FieldTypeCombo",
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        
        typeCombo.Items.Add("Text");
        typeCombo.Items.Add("Password");
        typeCombo.Items.Add("Date");
        typeCombo.Items.Add("Number");
        typeCombo.Items.Add("Email");
        typeCombo.Items.Add("URL");
        typeCombo.Items.Add("Text Area");
        typeCombo.Items.Add("Phone");
        typeCombo.Items.Add("File");
        typeCombo.SelectedIndex = 0;

        var nameStack = new StackPanel();
        nameStack.Children.Add(nameLabel);
        nameStack.Children.Add(nameBox);

        var typeStack = new StackPanel();
        typeStack.Children.Add(typeLabel);
        typeStack.Children.Add(typeCombo);

        Grid.SetRow(nameStack, 0);
        Grid.SetRow(typeStack, 1);

        grid.Children.Add(nameStack);
        grid.Children.Add(typeStack);

        return grid;
    }

    private async Task AddCustomField(string name, CustomFieldType type)
    {
        var newField = new CustomField
        {
            Name = name,
            Value = "",
            Type = type,
            DisplayOrder = _customFields.Count,
            PasswordItemId = _selectedItem?.Id ?? 0,
            IsRequired = false,
            IsProtected = type == CustomFieldType.Password
        };

        _customFields.Add(newField);
        await RefreshCustomFieldsUI();
    }

    private async Task RefreshCustomFieldsUI()
    {
        var container = GetElement<StackPanel>("CustomFieldsContainer");
        if (container == null) return;

        container.Children.Clear();

        foreach (var field in _customFields.OrderBy(f => f.DisplayOrder))
        {
            var fieldUI = await CreateCustomFieldUI(field);
            container.Children.Add(fieldUI);
        }
    }

    private async Task<Border> CreateCustomFieldUI(CustomField field)
    {
        var border = new Border
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
            CornerRadius = new CornerRadius(8),
            BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 4)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var content = new StackPanel { Spacing = 8 };

        // Field label
        var label = new TextBlock 
        { 
            Text = field.Name,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            FontSize = 14
        };
        content.Children.Add(label);

        // Field input based on type
        FrameworkElement input = field.Type switch
        {
            CustomFieldType.Password => new PasswordBox 
            { 
                Password = field.Value,
                PlaceholderText = "Enter password"
            },
            CustomFieldType.TextArea => new TextBox 
            { 
                Text = field.Value,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 80,
                PlaceholderText = "Enter text"
            },
            CustomFieldType.Date => new DatePicker 
            { 
                Date = DateTime.TryParse(field.Value, out var date) ? date : DateTime.Now
            },
            CustomFieldType.File => await CreateFileInputUI(field),
            _ => new TextBox 
            { 
                Text = field.Value,
                PlaceholderText = GetPlaceholderForType(field.Type)
            }
        };

        input.Tag = field;
        content.Children.Add(input);

        // Delete button
        var deleteButton = new Button
        {
            Content = "🗑️",
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
            BorderThickness = new Thickness(0),
            VerticalAlignment = VerticalAlignment.Top,
            Padding = new Thickness(8),
            Tag = field
        };
        deleteButton.Click += DeleteCustomField_Click;

        Grid.SetColumn(content, 0);
        Grid.SetColumn(deleteButton, 1);

        grid.Children.Add(content);
        grid.Children.Add(deleteButton);
        border.Child = grid;

        return border;
    }

    private async Task<Grid> CreateFileInputUI(CustomField field)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var textBox = new TextBox
        {
            Text = field.Value,
            IsReadOnly = true,
            PlaceholderText = "No file selected"
        };

        var browseButton = new Button
        {
            Content = "Browse...",
            Margin = new Thickness(8, 0, 0, 0),
            Tag = field
        };
        browseButton.Click += BrowseFileButton_Click;

        Grid.SetColumn(textBox, 0);
        Grid.SetColumn(browseButton, 1);

        grid.Children.Add(textBox);
        grid.Children.Add(browseButton);

        return grid;
    }

    private async void BrowseFileButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button button && button.Tag is CustomField field)
            {
                var filePicker = new Windows.Storage.Pickers.FileOpenPicker();
                
                // Get the current window's HWND
                var app = App.Current as App;
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(app?.MainWindow);
                
                // Initialize the file picker with the window handle
                WinRT.Interop.InitializeWithWindow.Initialize(filePicker, hWnd);
                
                filePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                filePicker.FileTypeFilter.Add("*");
                
                var file = await filePicker.PickSingleFileAsync();
                if (file != null)
                {
                    // Update the field value with the file path
                    field.Value = file.Path;
                    
                    // Update the UI
                    if (button.Parent is Grid grid && grid.Children[0] is TextBox textBox)
                    {
                        textBox.Text = file.Name; // Show just the filename in the UI
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialog($"Error selecting file: {ex.Message}");
        }
    }

    private void DeleteCustomField_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is CustomField field)
        {
            _customFields.Remove(field);
            _ = RefreshCustomFieldsUI();
        }
    }

    private string GetPlaceholderForType(CustomFieldType type)
    {
        return type switch
        {
            CustomFieldType.Email => "Enter email address",
            CustomFieldType.Url => "Enter URL",
            CustomFieldType.Phone => "Enter phone number",
            CustomFieldType.Number => "Enter number",
            _ => "Enter value"
        };
    }

    private async Task LoadCustomFields()
    {
        if (_selectedItem == null || _customFieldService == null) return;

        try
        {
            _customFields = await _customFieldService.GetByPasswordItemIdAsync(_selectedItem.Id);
            await RefreshCustomFieldsUI();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading custom fields: {ex.Message}");
        }
    }

    private async Task SaveCustomFields()
    {
        if (_selectedItem == null || _customFieldService == null) return;

        try
        {
            // Get values from UI and update custom fields
            var container = GetElement<StackPanel>("CustomFieldsContainer");
            if (container != null)
            {
                foreach (var child in container.Children)
                {
                    if (child is Border border && border.Child is Grid grid)
                    {
                        var content = grid.Children[0] as StackPanel;
                        if (content?.Children.Count > 1)
                        {
                            var input = content.Children[1];
                            if (input.Tag is CustomField field)
                            {
                                field.Value = input switch
                                {
                                    TextBox tb => tb.Text,
                                    PasswordBox pb => pb.Password,
                                    DatePicker dp => dp.Date?.ToString("yyyy-MM-dd") ?? "",
                                    Grid fileGrid => field.Value, // File path already updated
                                    _ => field.Value
                                };
                            }
                        }
                    }
                }
            }

            // Save to database
            foreach (var field in _customFields)
            {
                if (field.Id == 0)
                {
                    field.PasswordItemId = _selectedItem.Id;
                    await _customFieldService.CreateAsync(field);
                }
                else
                {
                    await _customFieldService.UpdateAsync(field);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving custom fields: {ex.Message}");
        }
    }

    #endregion
}