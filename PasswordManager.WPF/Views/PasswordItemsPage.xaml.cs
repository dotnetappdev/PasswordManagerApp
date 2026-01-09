using System.Windows;
using System.Windows.Controls;
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

namespace PasswordManager.WPF.Views;

public sealed partial class PasswordItemsPage : Page
{
    private PasswordItemsViewModel? _viewModel;
    private IServiceProvider? _serviceProvider;
    private PasswordItem? _selectedItem;
    private ICategoryInterface? _categoryService;
    private List<Category> _categories = new();
    private ITagService? _tagService;
    private IPasswordItemService? _passwordItemService;
    private List<Tag> _allTags = new();

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
            _viewModel = new PasswordItemsViewModel(_serviceProvider);
            this.DataContext = _viewModel;

            // Apply the filter from navigation
            ApplyNavigationFilter(filterData);
        }
        else if (e.Parameter is IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _categoryService = serviceProvider.GetRequiredService<ICategoryInterface>();
            _viewModel = new PasswordItemsViewModel(serviceProvider);
            this.DataContext = _viewModel;
        }

        if (_serviceProvider != null)
        {
            // Seed sample data if needed (only runs once)
            await SampleDataSeeder.SeedSampleDataAsync(_serviceProvider);

            // Load categories from database
            await LoadCategoriesAsync();

            // Load tags and services
            try
            {
                _tagService = _serviceProvider.GetService<ITagService>();
                _passwordItemService = _serviceProvider.GetService<IPasswordItemService>();
                if (_tagService != null)
                {
                    _allTags = (await _tagService.GetAllAsync()).ToList();
                }
            }
            catch (Exception ex)
            {
            }

            // Reload view model items after seeding to ensure UI shows newly created items
            try
            {
                if (_viewModel != null)
                {
                    await _viewModel.RefreshAsync();
                }
            }
            catch (Exception ex)
            {
            }
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
        
        // Apply category name filter
        if (!string.IsNullOrEmpty(filterData.FilterCategoryName))
        {
            _viewModel.FilterCategoryName = filterData.FilterCategoryName;
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
                var dialog = new ModernWpf.Controls.ContentDialog
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

        // Populate tags UI
        try
        {
            var detailTags = GetElement<ItemsControl>("DetailTagsPanel");
            if (detailTags != null)
            {
                detailTags.ItemsSource = item.Tags ?? new List<Tag>();
            }

            var editTagsPanel = GetElement<ItemsControl>("EditTagsPanel");
            var editTagsSection = GetElement<StackPanel>("EditTagsSection");
            if (editTagsPanel != null && editTagsSection != null)
            {
                // Populate edit tags panel with current tags
                editTagsPanel.ItemsSource = item.Tags ?? new List<Tag>();
            }
        }
        catch (Exception ex)
        {
        }
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
                var errorDialog = new ModernWpf.Controls.ContentDialog
                {
                    Title = "Error",
                    Content = "Service provider not initialized. Please navigate to this page properly.",
                    CloseButtonText = "OK"
                };
                ConfigureDialogForCentering(errorDialog);
                await errorDialog.ShowAsync();
                return;
            }

            // First show the item type selection dialog (1Password style)
            var typeSelectionDialog = new Dialogs.ItemTypeSelectionDialog();
            ConfigureDialogForCentering(typeSelectionDialog);

            var typeResult = await typeSelectionDialog.ShowAsync();
            if (typeResult == ModernWpf.Controls.ContentDialogResult.Primary || typeSelectionDialog.SelectedItemType != null)
            {
                // Then show the main add dialog with the selected type pre-filled
                var dialog = new Dialogs.AddPasswordDialog(_serviceProvider);
                ConfigureDialogForCentering(dialog);

                // Pre-select the item type if one was chosen
                if (typeSelectionDialog.SelectedItemType.HasValue)
                {
                    // Pass the selected type to the dialog
                    dialog.SetInitialItemType(typeSelectionDialog.SelectedItemType.Value, typeSelectionDialog.SelectedCategoryName);
                }

                var result = await dialog.ShowAsync();
                if (result == ModernWpf.Controls.ContentDialogResult.Primary && dialog.Result != null)
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
            var errorDialog = new ModernWpf.Controls.ContentDialog
            {
                Title = "Error",
                Content = $"Error adding password: {ex.Message}",
                CloseButtonText = "OK"
            };
            ConfigureDialogForCentering(errorDialog);
            await errorDialog.ShowAsync();
        }
    }

    private async void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuItem &&
            menuItem.DataContext is PasswordItem item &&
            _viewModel != null)
        {
            var dialog = new ModernWpf.Controls.ContentDialog
            {
                Title = "Delete Password Item",
                Content = $"Are you sure you want to delete '{item.Title}'?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ModernWpf.Controls.ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ModernWpf.Controls.ContentDialogResult.Primary)
            {
                await _viewModel.DeleteItemAsync(item);
            }
        }
    }

    private async void EditMenuItem_Click(object sender, RoutedEventArgs e)
    {
        PasswordItem? item = null;

        // Support both MenuFlyoutItem (context menu) and Button (detail header Edit button)
        if (sender is MenuFlyoutItem menuItem && menuItem.DataContext is PasswordItem mi)
        {
            item = mi;
        }
        else if (sender is Button btn)
        {
            // Use the currently selected item in the list/detail view
            item = _selectedItem;
        }

        if (item == null)
        {
            // Nothing to edit
            return;
        }

        try
        {
            if (_serviceProvider == null)
            {
                var errorDialog = new ModernWpf.Controls.ContentDialog
                {
                    Title = "Error",
                    Content = "Service provider not initialized. Please navigate to this page properly.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
                return;
            }

            // Open the edit dialog (reuse AddPasswordDialog in edit mode)
            var dialog = new Dialogs.AddPasswordDialog(_serviceProvider, item);
            ConfigureDialogForCentering(dialog);

            var result = await dialog.ShowAsync();
            if (result == ModernWpf.Controls.ContentDialogResult.Primary && dialog.Result != null)
            {
                // Refresh the list to show the updated item and re-display details
                if (_viewModel != null)
                {
                    await _viewModel.RefreshAsync();
                }

                // Re-load item details from fresh data source if possible
                if (_viewModel != null)
                {
                    var fresh = _viewModel.PasswordItems?.FirstOrDefault(pi => pi.Id == item.Id);
                    if (fresh != null)
                    {
                        _selectedItem = fresh;
                        ShowItemDetails(fresh);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            var errorDialog = new ModernWpf.Controls.ContentDialog
            {
                Title = "Error",
                Content = $"Error editing password: {ex.Message}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await errorDialog.ShowAsync();
        }
    }

    // Tag suggest box handlers
    private void TagSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        try
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var query = sender.Text ?? string.Empty;
                var suggestions = _allTags?
                    .Where(t => t.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .Select(t => t.Name)
                    .Distinct()
                    .ToList() ?? new List<string>();

                sender.ItemsSource = suggestions;
            }
        }
        catch { }
    }

    private async void TagSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        try
        {
            var chosen = args.SelectedItem as string;
            if (string.IsNullOrEmpty(chosen)) return;
            await AddTagByNameToSelectedItemAsync(chosen);
            sender.Text = string.Empty;
            sender.ItemsSource = null;
        }
        catch { }
    }

    private async void TagSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        try
        {
            var text = args.QueryText?.Trim();
            if (string.IsNullOrEmpty(text)) return;
            await AddTagByNameToSelectedItemAsync(text!);
            sender.Text = string.Empty;
            sender.ItemsSource = null;
        }
        catch { }
    }

    private async Task AddTagByNameToSelectedItemAsync(string tagName)
    {
        if (_selectedItem == null || _serviceProvider == null) return;

        try
        {
            // Find existing tag or create
            Tag? tag = null;
            if (_tagService != null)
            {
                tag = await _tagService.GetByNameAsync(tagName);
            }

            if (tag == null)
            {
                // create a new tag
                if (_tagService != null)
                {
                    tag = await _tagService.CreateAsync(new Tag { Name = tagName, UserId = _selectedItem.UserId });
                    // update local cache
                    _allTags.Add(tag);
                }
                else
                {
                    // fallback: create in-memory tag object
                    tag = new Tag { Id = 0, Name = tagName };
                }
            }

            // Add to item if not present
            if (!_selectedItem.Tags.Any(t => string.Equals(t.Name, tag.Name, StringComparison.OrdinalIgnoreCase)))
            {
                _selectedItem.Tags.Add(tag);
                if (_passwordItemService != null)
                {
                    await _passwordItemService.UpdateAsync(_selectedItem);
                }

                // Refresh UI panels
                var editTagsPanel = GetElement<ItemsControl>("EditTagsPanel");
                var detailTags = GetElement<ItemsControl>("DetailTagsPanel");
                if (editTagsPanel != null) editTagsPanel.ItemsSource = null; // force refresh
                if (detailTags != null) detailTags.ItemsSource = null;
                if (editTagsPanel != null) editTagsPanel.ItemsSource = _selectedItem.Tags;
                if (detailTags != null) detailTags.ItemsSource = _selectedItem.Tags;
            }
        }
        catch (Exception ex)
        {
        }
    }

    private async void RemoveTagButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button btn && _selectedItem != null)
            {
                var tagIdObj = btn.Tag;
                if (tagIdObj == null) return;

                int tagId = 0;
                if (tagIdObj is int i) tagId = i;
                else if (!int.TryParse(tagIdObj.ToString(), out tagId)) return;

                var existing = _selectedItem.Tags.FirstOrDefault(t => t.Id == tagId || string.Equals(t.Id.ToString(), tagIdObj.ToString(), StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    _selectedItem.Tags.Remove(existing);
                    if (_passwordItemService != null)
                    {
                        await _passwordItemService.UpdateAsync(_selectedItem);
                    }

                    var editTagsPanel = GetElement<ItemsControl>("EditTagsPanel");
                    var detailTags = GetElement<ItemsControl>("DetailTagsPanel");
                    if (editTagsPanel != null) editTagsPanel.ItemsSource = _selectedItem.Tags;
                    if (detailTags != null) detailTags.ItemsSource = _selectedItem.Tags;
                }
            }
        }
        catch (Exception ex)
        {
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
        }
    }

    private MainWindow? GetMainWindow()
    {
        // Use the MainWindow property exposed in App
        return (App.Current as App)?.MainWindow;
    }

    /// <summary>
    /// Helper method to properly configure dialog for centering
    /// </summary>
    private void ConfigureDialogForCentering(ModernWpf.Controls.ContentDialog dialog)
    {
        try
        {
            // Try to get the main window for proper centering
            var mainWindow = GetMainWindow();

            // Set XamlRoot to the main window's content for proper centering
            if (mainWindow?.Content?.XamlRoot != null)
            {
                dialog.XamlRoot = mainWindow.Content.XamlRoot;
            }
            else if (this.XamlRoot != null)
            {
                dialog.XamlRoot = this.XamlRoot;
            }

            // Ensure the dialog uses the proper style for centering if it doesn't have one already
            if (dialog.Style == null)
            {
                // Apply the Modern1PasswordDialogStyle from resources
                if (Application.Current.Resources.ContainsKey("Modern1PasswordDialogStyle"))
                {
                    dialog.Style = Application.Current.Resources["Modern1PasswordDialogStyle"] as Style;
                }
            }
        }
        catch (Exception ex)
        {
            // Fallback to page XamlRoot
            if (this.XamlRoot != null)
            {
                dialog.XamlRoot = this.XamlRoot;
            }
        }
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
                var errorDialog = new ModernWpf.Controls.ContentDialog
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
            ConfigureDialogForCentering(dialog);
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            var errorDialog = new ModernWpf.Controls.ContentDialog
            {
                Title = "Error",
                Content = $"Error showing password details: {ex.Message}",
                CloseButtonText = "OK"
            };
            ConfigureDialogForCentering(errorDialog);
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
                var errorDialog = new ModernWpf.Controls.ContentDialog
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
            ConfigureDialogForCentering(dialog);
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            var errorDialog = new ModernWpf.Controls.ContentDialog
            {
                Title = "Error",
                Content = $"Error opening view dialog: {ex.Message}",
                CloseButtonText = "OK"
            };
            ConfigureDialogForCentering(errorDialog);
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
                    System.Windows.Threading.Dispatcher.GetForCurrentThread().TryEnqueue(
                        System.Windows.Threading.DispatcherPriority.Low, () =>
                    {
                        Task.Delay(2000).ContinueWith(_ =>
                        {
                            System.Windows.Threading.Dispatcher.GetForCurrentThread().TryEnqueue(() =>
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

    // Missing event handlers implementation
    private async void CopyUsernameButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var list = GetElement<ListView>("ItemsList");
            var selected = _selectedItem ?? (list?.SelectedItem as PasswordItem);
            var username = selected?.Username ?? selected?.LoginItem?.Username;
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
        }
    }

    private void GeneratePasswordButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Generate a secure random password
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            var password = new string(Enumerable.Repeat(chars, 16)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            // Find the password field and set the generated password
            var passwordBox = GetElement<PasswordBox>("EditPasswordBox");
            if (passwordBox != null)
            {
                passwordBox.Password = password;
            }
        }
        catch (Exception ex)
        {
        }
    }

    private async void EditDetailButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedItem is null)
        {
            await ShowTemporaryMessageAsync("No item selected to edit");
            return;
        }

        try
        {
            if (_serviceProvider is null)
            {
                var errorDialog = new ModernWpf.Controls.ContentDialog
                {
                    Title = "Error",
                    Content = "Service provider not initialized. Please navigate to this page properly.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
                return;
            }

            var dialog = new Dialogs.AddPasswordDialog(_serviceProvider, _selectedItem, false);
            ConfigureDialogForCentering(dialog);
            var result = await dialog.ShowAsync();

            if (result == ModernWpf.Controls.ContentDialogResult.Primary)
            {
                // Refresh the view after editing
                await ShowTemporaryMessageAsync("Item updated successfully");
            }
        }
        catch (Exception ex)
        {
            var errorDialog = new ModernWpf.Controls.ContentDialog
            {
                Title = "Error",
                Content = $"Error opening edit dialog: {ex.Message}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await errorDialog.ShowAsync();
        }
    }

    private void CategoryFilterBox_TextChanged(object sender, AutoSuggestBoxTextChangedEventArgs e)
    {
        if (sender is AutoSuggestBox autoSuggestBox && e.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            var suggestions = new List<string>();
            string queryText = autoSuggestBox.Text?.ToLower() ?? "";

            if (!string.IsNullOrEmpty(queryText))
            {
                suggestions = _categories
                    .Where(c => c.Name.ToLower().Contains(queryText))
                    .Select(c => c.Name)
                    .ToList();
            }

            autoSuggestBox.ItemsSource = suggestions;
        }
    }

    private void CategoryFilterBox_SuggestionChosen(object sender, AutoSuggestBoxSuggestionChosenEventArgs e)
    {
        if (sender is AutoSuggestBox autoSuggestBox && e.SelectedItem is string selectedCategory)
        {
            autoSuggestBox.Text = selectedCategory;
            // Apply category filter logic here if needed
        }
    }

    private void AddCustomFieldButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var customFieldsContainer = GetElement<StackPanel>("CustomFieldsContainer");
            if (customFieldsContainer != null)
            {
                // Create a new custom field UI
                var fieldGrid = new Grid();
                fieldGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                fieldGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                fieldGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var nameTextBox = new TextBox
                {
                    PlaceholderText = "Field name",
                    Margin = new Thickness(0, 0, 4, 0)
                };
                Grid.SetColumn(nameTextBox, 0);

                var valueTextBox = new TextBox
                {
                    PlaceholderText = "Field value",
                    Margin = new Thickness(4, 0, 4, 0)
                };
                Grid.SetColumn(valueTextBox, 1);

                var removeButton = new Button
                {
                    Content = "✕",
                    Width = 32,
                    Height = 32,
                    Margin = new Thickness(4, 0, 0, 0)
                };
                Grid.SetColumn(removeButton, 2);

                removeButton.Click += (s, args) => customFieldsContainer.Children.Remove(fieldGrid);

                fieldGrid.Children.Add(nameTextBox);
                fieldGrid.Children.Add(valueTextBox);
                fieldGrid.Children.Add(removeButton);

                customFieldsContainer.Children.Add(fieldGrid);
            }
        }
        catch (Exception ex)
        {
        }
    }
}
