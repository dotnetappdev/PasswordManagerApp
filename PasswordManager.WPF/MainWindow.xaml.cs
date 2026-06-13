using ModernWpf.Controls;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PasswordManager.Models;
using PasswordManager.Services.Interfaces;
using PasswordManager.WPF.Models;
using PasswordManager.WPF.Services;

namespace PasswordManager.WPF;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly IServiceProvider _serviceProvider;
    private bool _isAuthenticated = false;
    private string? _currentUserId = null;
    private Style? _navItemStyle; // cache for dynamic nav items
    
    // Protected default categories that cannot be deleted
    private static readonly string[] ProtectedCategoryTags = new[] 
    { 
        "LoginCategory", 
        "CreditCardCategory", 
        "SecureNotesCategory", 
        "WiFiCategory",
        "PasskeysCategory"
    };

    public MainWindow(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        this.InitializeComponent();
        this.Title = "Password Manager - WPF";

        // Set window size
        this.Width = 1200;
        this.Height = 800;

        // Cache style early (after resources loaded by InitializeComponent)
        _navItemStyle = TryGetNavItemStyle();

        // Initialize navigation - start with Login if not authenticated, otherwise Home
        InitializeNavigation();
        // Load dynamic categories for navigation
        _ = RefreshCategoriesAsync();
        // Load sidebar tags as chips
        _ = RefreshTagsAsync();
    }

    private void InitializeNavigation()
    {
        SetAuthenticationState(false);
        NavigateLoginFrame();
    }

    // Creates a fresh LoginPage instance and navigates LoginFrame to it.
    // Must use an instance — Frame.Navigate(Type, ...) renders the Type as text, not a page.
    private void NavigateLoginFrame()
    {
        var page = new Views.LoginPage();
        LoginFrame.Navigate(page, _serviceProvider);
    }

    private void SetAuthenticationState(bool isAuthenticated)
    {
        if (isAuthenticated)
        {
            // Hide login frame and show main navigation
            LoginFrame.Visibility = Visibility.Collapsed;
            MainNavigationView.Visibility = Visibility.Visible;
        }
        else
        {
            // Show login frame and hide main navigation
            LoginFrame.Visibility = Visibility.Visible;
            MainNavigationView.Visibility = Visibility.Collapsed;
        }
    }

    private void MainNavigationView_SelectionChanged(ModernWpf.Controls.NavigationView sender, ModernWpf.Controls.NavigationViewSelectionChangedEventArgs args)
    {
        // Only allow navigation if authenticated
        if (!_isAuthenticated)
        {
            // Prevent selection if not authenticated
            sender.SelectedItem = null;
            return;
        }

        if (args.SelectedItem is ModernWpf.Controls.NavigationViewItem selectedItem)
        {
            string tag = selectedItem.Tag?.ToString() ?? "";

            // Only navigate if the item has a tag (leaf items, not parent categories)
            if (!string.IsNullOrEmpty(tag))
            {
                try
                {
                    NavigateToPage(tag);
                }
                catch (Exception ex)
                {
                    // Reset selection on error
                    sender.SelectedItem = null;
                }
            }
        }
    }

    public void NavigateToPage(string pageTag)
    {
        if (!_isAuthenticated && pageTag != "Login") return;

        try
        {
            // Create a fresh page INSTANCE — Frame.Navigate(Type, ...) renders the Type
            // as text, not as a page, so we must instantiate explicitly.
            System.Windows.Controls.Page page = pageTag switch
            {
                "AllItems" or "Favorites" or "LoginCategory" or "CreditCardCategory" or
                "SecureNotesCategory" or "IdentityCategory" or "WiFiCategory" or
                "PasskeysCategory" or "Archive" or "RecentlyDeleted" or "Passwords"
                    => new Views.PasswordItemsPage(),
                "Profile"           => new Views.ProfilePage(),
                "Categories"        => new Views.CategoriesPage(),
                "Vaults"            => new Views.VaultsPage(),
                "Passkeys"          => new Views.PasskeysPage(),
                "ManageItems"       => new Views.ManageItemsPage(),
                "SecurityDashboard" => new Views.DashboardPage(),
                "Import"            => new Views.ImportPage(),
                "Settings"          => new Views.SettingsPage(),
                "Home"              => new Views.DashboardPage(),
                "Login"             => new Views.LoginPage(),
                _                   => pageTag.EndsWith("Category", StringComparison.OrdinalIgnoreCase)
                                           ? new Views.PasswordItemsPage()
                                           : (System.Windows.Controls.Page)new Views.DashboardPage()
            };

            object navigationParameter = CreateNavigationParameter(pageTag);

            if (ContentFrame != null)
                ContentFrame.Navigate(page, navigationParameter);
        }
        catch (Exception ex)
        { }
    }

    private object CreateNavigationParameter(string pageTag)
    {
        // For password item pages, create filter data
        if (IsPasswordItemsPage(pageTag))
        {
            var filterData = new NavigationFilterData(_serviceProvider);

            switch (pageTag)
            {
                case "Favorites":
                    filterData.ShowFavorites = true;
                    filterData.FilterName = "Favorites";
                    break;
                case "LoginCategory":
                    filterData.FilterType = ItemType.Login;
                    filterData.FilterName = "Logins";
                    break;
                case "CreditCardCategory":
                    filterData.FilterType = ItemType.CreditCard;
                    filterData.FilterName = "Credit Cards";
                    break;
                case "SecureNotesCategory":
                    filterData.FilterType = ItemType.SecureNote;
                    filterData.FilterName = "Secure Notes";
                    break;
                case "WiFiCategory":
                    filterData.FilterType = ItemType.WiFi;
                    filterData.FilterName = "WiFi";
                    break;
                case "PasskeysCategory":
                    filterData.FilterType = ItemType.Passkey;
                    filterData.FilterName = "Passkeys";
                    break;
                case "Archive":
                    filterData.ShowArchived = true;
                    filterData.FilterName = "Archive";
                    break;
                case "RecentlyDeleted":
                    filterData.ShowDeleted = true;
                    filterData.FilterName = "Recently Deleted";
                    break;
                default:
                    // Handle dynamic categories - filter by category name
                    if (pageTag.EndsWith("Category"))
                    {
                        var categoryName = pageTag.Substring(0, pageTag.Length - 8); // Remove "Category" suffix
                        filterData.FilterCategoryName = categoryName;
                        filterData.FilterName = categoryName;
                    }
                    else
                    {
                        filterData.FilterName = "All Items";
                    }
                    break;
            }

            return filterData;
        }

        // For other pages, just pass the service provider
        return _serviceProvider;
    }

    private bool IsPasswordItemsPage(string pageTag)
    {
        return pageTag switch
        {
            "AllItems" or "Favorites" or "LoginCategory" or "CreditCardCategory" or
            "SecureNotesCategory" or "IdentityCategory" or "WiFiCategory" or "PasskeysCategory" or
            "Archive" or "RecentlyDeleted" or "Passwords" => true,
            // Also treat any tag ending with "Category" as a password items page (for dynamic categories)
            _ => pageTag.EndsWith("Category", StringComparison.OrdinalIgnoreCase)
        };
    }



    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        // Handle search text changes and provide suggestions + auto-filter
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            var query = sender.Text?.ToLower() ?? "";

            if (!string.IsNullOrWhiteSpace(query) && query.Length >= 2)
            {
                // Provide basic search suggestions
                var suggestions = new List<string>();

                // Add common search categories as suggestions
                var commonSearches = new[] { "logins", "passwords", "credit cards", "secure notes", "wifi", "favorites" };
                foreach (var category in commonSearches)
                {
                    if (category.Contains(query))
                    {
                        suggestions.Add(category);
                    }
                }

                // Add "search for" prefix for better UX
                if (suggestions.Count == 0)
                {
                    suggestions.Add($"Search for '{query}'");
                }

                sender.ItemsSource = suggestions;

                // Auto-filter: Navigate to passwords page and apply search immediately
                try
                {
                    NavigateToPage("AllItems");

                    // Apply auto-filter with slight delay to ensure page is loaded
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        PassSearchQueryToPage(query);
                    }));
                }
                catch (Exception ex)
                { }
            }
            else
            {
                sender.ItemsSource = null;

                // Clear filters when search is empty
                if (string.IsNullOrWhiteSpace(query))
                {
                    try
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            PassSearchQueryToPage("");
                        }));
                    }
                    catch (Exception ex)
                    { }
                }
            }
        }
    }

    private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        // Handle search query submission with better feedback
        string searchQuery = args.QueryText?.Trim() ?? "";

        if (!string.IsNullOrEmpty(searchQuery))
        {
            try
            {
                // Navigate to passwords page with search query
                NavigateToPage("AllItems");

                // Use a more reliable way to pass search query
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    PassSearchQueryToPage(searchQuery);
                }));

                // Provide visual feedback
                sender.PlaceholderText = $"Searching for '{searchQuery}'...";

                // Reset placeholder after a delay
                Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
                {
                    Task.Delay(3000).ContinueWith(_ =>
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (sender.PlaceholderText.StartsWith("Searching"))
                            {
                                sender.PlaceholderText = "Search passwords...";
                            }
                        }));
                    });
                }));
            }
            catch (Exception ex)
            { }
        }
    }

    private void PassSearchQueryToPage(string searchQuery)
    {
        // Pass search query to the passwords page if it's currently loaded
        if (ContentFrame.Content is Views.PasswordItemsPage passwordsPage)
        {
            // Try to find the search textbox and set the search text
            var searchTextBox = FindChildControl<TextBox>(passwordsPage, "SearchTextBox");
            if (searchTextBox != null)
            {
                // Setting Text triggers TextChanged automatically; no need to manually raise the event
                searchTextBox.Text = searchQuery;
            }
        }
    }

    // Helper method to find child controls
    private T? FindChildControl<T>(DependencyObject parent, string controlName) where T : FrameworkElement
    {
        if (parent == null) return null;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is T element && element.Name == controlName)
                return element;

            var result = FindChildControl<T>(child, controlName);
            if (result != null)
                return result;
        }

        return null;
    }

    // Called by both LoginFrame and ContentFrame after every navigation.
    // WPF does not automatically call a page's OnNavigatedTo method — we must do it here.
    private void Frame_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
    {
        switch (e.Content)
        {
            case Views.LoginPage p:          p.OnNavigatedTo(e);      break;
            case Views.PasswordItemsPage p:  p.OnNavigatedTo(e);      break;
            case Views.DashboardPage p:      p.OnNavigatedTo(e);      break;
            case Views.ProfilePage p:        p.OnNavigatedTo(e);      break;
            case Views.CategoriesPage p:     p.OnNavigatedTo(e);      break;
            case Views.ImportPage p:         p.OnNavigatedTo(e);      break;
            case Views.ManageItemsPage p:    p.OnNavigatedTo(e);      break;
            case Views.SettingsPage p:       p.OnNavigatedTo(e);      break;
            case Views.VaultsPage p:         p.OnNavigatedTo(e);      break;
            case Views.PasskeysPage p:       p.OnNavigatedTo(e);      break;
        }
    }

    // Called by App after the host and startup service have fully initialised the database.
    // Re-navigates to LoginPage so profiles and auth checks run against a live DB.
    public void OnDatabaseInitialized()
    {
        NavigateLoginFrame();
        _ = RefreshCategoriesAsync();
        _ = RefreshTagsAsync();
    }

    // Public method to allow programmatic navigation (e.g., after login)
    public void NavigateToHome()
    {
        _isAuthenticated = true;

        // Capture current user from auth service and update sidebar
        var authService = _serviceProvider.GetService<IAuthService>();
        if (authService?.CurrentUser != null)
        {
            _currentUserId = authService.CurrentUser.Id;
            var user = authService.CurrentUser;
            var displayName = $"{user.FirstName} {user.LastName}".Trim();
            if (string.IsNullOrEmpty(displayName)) displayName = user.UserName ?? user.Email ?? "User";

            if (this.FindName("UserNameText") is System.Windows.Controls.TextBlock nameText)
                nameText.Text = displayName;
            if (this.FindName("UserAvatarText") is System.Windows.Controls.TextBlock avatarText)
                avatarText.Text = displayName.Length > 0 ? displayName[0].ToString().ToUpper() : "?";
        }

        SetAuthenticationState(true);
        MainNavigationView.SelectedItem = AllItemsNavItem;
        NavigateToPage("AllItems");
    }

    // Public method to handle logout
    public void HandleLogout()
    {
        _isAuthenticated = false;
        _currentUserId = null;

        // Show login frame and hide main navigation
        SetAuthenticationState(false);

        // Clear navigation selection
        MainNavigationView.SelectedItem = null;

        // Navigate back to login
        NavigateLoginFrame();
    }

    // Missing event handlers for XAML bindings

    private void ProfileButton_Click(object sender, RoutedEventArgs e)
    {
        // Navigate directly to Profile page
        NavigateToPage("Profile");
    }

    private async void ProfileSettings_Click(object sender, RoutedEventArgs e)
    {
        // This is now handled by the ProfilePage - keeping for compatibility
        NavigateToPage("Profile");
    }

    private async Task<string> GetPasswordItemCount()
    {
        try
        {
            var passwordService = _serviceProvider.GetService<IPasswordItemService>();
            if (passwordService != null)
            {
                var items = await passwordService.GetAllAsync();
                return items.Count().ToString();
            }
        }
        catch
        {
            // Ignore errors for now
        }
        return "N/A";
    }

    private async void Logout_Click(object sender, RoutedEventArgs e)
    {
        // Show confirmation dialog
        var dialog = new ModernWpf.Controls.ContentDialog
        {
            Title = "Sign Out",
            Content = "Are you sure you want to sign out?",
            PrimaryButtonText = "Sign Out",
            CloseButtonText = "Cancel",
            DefaultButton = ModernWpf.Controls.ContentDialogButton.Close
        };

        var result = await dialog.ShowAsync();
        if (result == ModernWpf.Controls.ContentDialogResult.Primary)
        {
            // Handle logout
            HandleLogout();
        }
    }

    private async void NavigationItem_RightTapped(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Handle right-click context menu functionality for navigation items
        if (sender is ModernWpf.Controls.NavigationViewItem navItem)
        {
            var tag = navItem.Tag?.ToString();

            // For now, just select the item if it's not already selected
            if (MainNavigationView.SelectedItem != navItem)
            {
                MainNavigationView.SelectedItem = navItem;
                if (!string.IsNullOrEmpty(tag))
                {
                    NavigateToPage(tag);
                }
            }
        }
    }

    private async void AddTagNavButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Open the TagDialog for creating a new tag
            var tagDialog = new Dialogs.TagDialog(_serviceProvider);

            var result = await tagDialog.ShowAsync();
            if (result == ModernWpf.Controls.ContentDialogResult.Primary && tagDialog.Result is not null)
            {
                await ShowInfoMessage("Tag Created", $"Tag '{tagDialog.Result.Name}' has been created successfully.");
                // Refresh categories panel as tags may influence UI
                await RefreshCategoriesAsync();
            }
        }
        catch (Exception ex)
        {
            await ShowErrorMessage("Error", $"Failed to create tag: {ex.Message}");
        }
    }

    // Delete tag navigation handler removed (UI buttons removed). Kept method removed per request.

    private async void AddCategoryNavButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Open the CategoryDialog for creating a new category
            var categoryDialog = new Dialogs.CategoryDialog(_serviceProvider);

            var result = await categoryDialog.ShowAsync();
            if (result == ModernWpf.Controls.ContentDialogResult.Primary && categoryDialog.Result is not null)
            {
                await ShowInfoMessage("Category Created", $"Category '{categoryDialog.Result.Name}' has been created successfully.");
            }
        }
        catch (Exception ex)
        {
            await ShowErrorMessage("Error", $"Failed to create category: {ex.Message}");
        }
    }

    private void NewItemButton_Click(object sender, RoutedEventArgs e)
    {
        // Navigate to AllItems page; the page's Add button handles the dialog
        NavigateToPage("AllItems");
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (ContentFrame.Content is Views.PasswordItemsPage page)
            {
                var addBtn = FindChildControl<Button>(page, "AddButton");
                addBtn?.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        }));
    }

    // Populate the DynamicCategoriesPanel with categories from the service
    public async Task RefreshCategoriesAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryInterface>();

            // Get all favorite categories
            var categories = await categoryService.GetAllAsync();
            var favoriteCategories = categories.Where(c => c.IsFavorite).ToList();

            // Update UI on dispatcher thread
            Dispatcher.Invoke(() =>
            {
                var categoriesPanel = this.FindName("FavoriteCategoriesPanel") as StackPanel;
                if (categoriesPanel == null) return;
                
                categoriesPanel.Children.Clear();
                
                foreach (var category in favoriteCategories)
                {
                    var navItem = new ModernWpf.Controls.NavigationViewItem
                    {
                        Content = category.Name,
                        Tag = $"{category.Name}Category",
                        Style = _navItemStyle
                    };
                    
                    // Set icon if available (using ModernWpf.Controls icon)
                    if (!string.IsNullOrWhiteSpace(category.Icon))
                    {
                        // ModernWPF NavigationViewItem uses IconElement property
                        // For WPF, we'll need to create a proper icon using ModernWpf types
                        // Skipping icon for now as it requires proper WPF icon implementation
                    }
                    
                    // Note: Context menus in WPF use System.Windows.Controls.ContextMenu
                    // not MenuFlyout from WinUI
                    // Context menu implementation would need to be rewritten for WPF
                    
                    categoriesPanel.Children.Add(navItem);
                }
            });
        }
        catch (Exception ex)
        { }
    }

    // Populate the SidebarTagsPanel with tag chips
    public async Task RefreshTagsAsync()
    {
        try
        {
            var tagService = _serviceProvider.GetService<ITagService>();
            if (tagService == null) return;

            var tags = (await tagService.GetAllAsync()).ToList();

            Dispatcher.Invoke(() =>
            {
                var tagsPanel = this.FindName("SidebarTagsPanel") as StackPanel;
                if (tagsPanel == null) return;

                tagsPanel.Children.Clear();

                foreach (var tag in tags)
                {
                    var border = new Border
                    {
                        CornerRadius = new CornerRadius(12),
                        Padding = new Thickness(8, 4, 8, 4),
                        Margin = new Thickness(8, 4, 8, 0),
                        Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 48, 50, 52)),
                    };

                    // Try to use tag color if available
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(tag.Color))
                        {
                            var color = System.Windows.Media.Colors.Transparent;
                            // Expecting hex like #rrggbb
                            if (tag.Color.StartsWith("#"))
                            {
                                var hex = tag.Color.TrimStart('#');
                                if (hex.Length == 6)
                                {
                                    var r = Convert.ToByte(hex.Substring(0, 2), 16);
                                    var g = Convert.ToByte(hex.Substring(2, 2), 16);
                                    var b = Convert.ToByte(hex.Substring(4, 2), 16);
                                    color = System.Windows.Media.Color.FromArgb(255, r, g, b);
                                }
                            }

                            if (color != System.Windows.Media.Colors.Transparent)
                                border.Background = new SolidColorBrush(color);
                        }
                    }
                    catch { }

                    var txt = new TextBlock
                    {
                        Text = tag.Name,
                        Foreground = new SolidColorBrush(System.Windows.Media.Colors.White),
                        FontSize = 13,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    border.Child = txt;

                    // Make chip clickable to filter by tag
                    var btn = new Button
                    {
                        Content = border,
                        Background = new SolidColorBrush(System.Windows.Media.Colors.Transparent),
                        BorderThickness = new Thickness(0),
                        Padding = new Thickness(0),
                        HorizontalAlignment = HorizontalAlignment.Left
                    };

                    btn.Click += (s, e) =>
                    {
                        try
                        {
                            // Navigate to PasswordItemsPage with a NavigationFilterData that includes the tag filter
                            var filterData = new Models.NavigationFilterData(_serviceProvider)
                            {
                                FilterName = tag.Name,
                                TagName = tag.Name
                            };
                            ContentFrame.Navigate(typeof(Views.PasswordItemsPage), filterData);
                        }
                        catch { }
                    };

                    tagsPanel.Children.Add(btn);
                }
            });
        }
        catch (Exception ex)
        { }
    }

    // Delete category navigation handler removed (UI buttons removed). Kept method removed per request.

    private async void AddVaultButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var collectionService = _serviceProvider.GetService<ICollectionService>();
            if (collectionService == null)
            {
                await ShowErrorMessage("Error", "Collection service is not available.");
                return;
            }

            // Create a simple collection creation dialog
            var dialog = new ModernWpf.Controls.ContentDialog
            {
                Title = "Create New Vault",
                PrimaryButtonText = "Create",
                CloseButtonText = "Cancel",
                DefaultButton = ModernWpf.Controls.ContentDialogButton.Primary};

            var nameTextBox = new TextBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var descriptionTextBox = new TextBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 12, 0, 0)
            };

            var content = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "Create a new vault to organize your password items." },
                    nameTextBox,
                    descriptionTextBox
                }
            };

            dialog.Content = content;

            var result = await dialog.ShowAsync();
            if (result == ModernWpf.Controls.ContentDialogResult.Primary)
            {
                var name = nameTextBox.Text?.Trim();
                if (string.IsNullOrEmpty(name))
                {
                    await ShowErrorMessage("Error", "Vault name is required.");
                    return;
                }

                var newCollection = new Collection
                {
                    Name = name,
                    Description = descriptionTextBox.Text?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };

                // Set user ID from current authenticated user
                var authService = _serviceProvider.GetService<IAuthService>();
                if (authService?.CurrentUser != null)
                {
                    newCollection.UserId = authService.CurrentUser.Id;
                }

                await collectionService.CreateAsync(newCollection);
                await ShowInfoMessage("Vault Created", $"Vault '{newCollection.Name}' has been created successfully.");

            }
        }
        catch (Exception ex)
        {
            await ShowErrorMessage("Error", $"Failed to create vault: {ex.Message}");
        }
    }

    // Delete vault navigation handler removed (UI buttons removed). Kept method removed per request.

    private async void EditItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Get the tag from the menu item to identify which navigation item to edit
            var menuItem = sender as System.Windows.Controls.MenuItem;
            var tag = menuItem?.Tag?.ToString();

            if (string.IsNullOrEmpty(tag))
            {
                await ShowErrorMessage("Error", "Unable to identify the item to edit.");
                return;
            }

            // Handle editing based on the item type
            switch (tag)
            {
                case "AllItems":
                case "Favorites":
                    await ShowInfoMessage("Edit Item", $"Editing '{tag}' - This would open item management interface.");
                    NavigateToPage("ManageItems");
                    break;

                case "LoginCategory":
                case "CreditCardCategory":
                case "SecureNotesCategory":
                case "PasskeysCategory":
                    await ShowInfoMessage("Edit Category", $"This would open the category editor for '{tag}'.");
                    // Could open category dialog in edit mode here
                    break;

                default:
                    await ShowInfoMessage("Edit Item", $"Editing functionality for '{tag}' would be implemented here.");
                    break;
            }

        }
        catch (Exception ex)
        {
            await ShowErrorMessage("Error", $"Failed to edit item: {ex.Message}");
        }
    }

    private async void DeleteItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Get the tag from the menu item to identify which navigation item to delete
            var menuItem = sender as System.Windows.Controls.MenuItem;
            var tag = menuItem?.Tag?.ToString();

            if (string.IsNullOrEmpty(tag))
            {
                await ShowErrorMessage("Error", "Unable to identify the item to delete.");
                return;
            }

            // Show confirmation dialog
            var confirmDialog = new ModernWpf.Controls.ContentDialog
            {
                Title = "Confirm Deletion",
                Content = $"Are you sure you want to delete '{tag}'? This action cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ModernWpf.Controls.ContentDialogButton.Close};

            var result = await confirmDialog.ShowAsync();
            if (result == ModernWpf.Controls.ContentDialogResult.Primary)
            {
                // Handle deletion based on the item type
                switch (tag)
                {
                    case "AllItems":
                    case "Favorites":
                        await ShowInfoMessage("Delete Item", $"'{tag}' is a system item and cannot be deleted.");
                        break;

                    case "LoginCategory":
                    case "CreditCardCategory":
                    case "SecureNotesCategory":
                    case "PasskeysCategory":
                        await ShowInfoMessage("Delete Category", $"Category '{tag}' deletion would be handled here.");
                        // Could implement actual category deletion here
                        break;

                    default:
                        await ShowInfoMessage("Delete Item", $"'{tag}' has been marked for deletion.");
                        break;
                }

            }
        }
        catch (Exception ex)
        {
            await ShowErrorMessage("Error", $"Failed to delete item: {ex.Message}");
        }
    }

    private async void DeleteCategoryItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var menuItem = sender as System.Windows.Controls.MenuItem;
            var tag = menuItem?.Tag?.ToString();

            if (string.IsNullOrEmpty(tag))
            {
                await ShowErrorMessage("Error", "Unable to identify the category to delete.");
                return;
            }

            // Check if this is a protected default category
            if (ProtectedCategoryTags.Contains(tag))
            {
                await ShowErrorMessage("Cannot Delete", "This is a default category and cannot be deleted.");
                return;
            }

            var confirmDialog = new ModernWpf.Controls.ContentDialog
            {
                Title = "Delete Category",
                Content = $"Are you sure you want to delete this category? Items in this category will not be deleted, but they will lose their category assignment.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ModernWpf.Controls.ContentDialogButton.Close};

            var result = await confirmDialog.ShowAsync();
            if (result == ModernWpf.Controls.ContentDialogResult.Primary)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryInterface>();
                    
                    // Try to parse as category ID first (for dynamic categories from navigation)
                    if (int.TryParse(tag, out int categoryId))
                    {
                        var category = await categoryService.GetByIdAsync(categoryId);
                        if (category != null)
                        {
                            await categoryService.DeleteAsync(category.Id);
                            await RefreshCategoriesAsync();
                            await ShowInfoMessage("Category Deleted", $"Category '{category.Name}' was successfully deleted.");
                            return;
                        }
                    }
                    
                    // Fallback: Try to match by category name patterns (for legacy hardcoded categories)
                    var categories = await categoryService.GetAllAsync();
                    const string categorySuffix = "Category";
                    var tagWithoutSuffix = tag.EndsWith(categorySuffix, StringComparison.OrdinalIgnoreCase) 
                        ? tag[..^categorySuffix.Length] 
                        : tag;
                    var categoryToDelete = categories.FirstOrDefault(c => 
                        string.Equals(c.Name, tag, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(c.Name, tagWithoutSuffix, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals($"{c.Name}{categorySuffix}", tag, StringComparison.OrdinalIgnoreCase));
                    
                    if (categoryToDelete != null)
                    {
                        await categoryService.DeleteAsync(categoryToDelete.Id);
                        await RefreshCategoriesAsync();
                        await ShowInfoMessage("Category Deleted", $"Category '{categoryToDelete.Name}' was successfully deleted.");
                    }
                    else
                    {
                        await ShowErrorMessage("Error", "Category not found in database.");
                    }
                }
                catch (Exception deleteEx)
                {
                    await ShowErrorMessage("Error", $"Failed to delete category: {deleteEx.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            await ShowErrorMessage("Error", $"Failed to delete category: {ex.Message}");
        }
    }

    // Helper methods for dialogs
    private async Task ShowErrorMessage(string title, string message)
    {
        var errorDialog = new ModernWpf.Controls.ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK"};
        await errorDialog.ShowAsync();
    }

    private async Task ShowInfoMessage(string title, string message)
    {
        var infoDialog = new ModernWpf.Controls.ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK"};
        await infoDialog.ShowAsync();
    }

    private Style? TryGetNavItemStyle()
    {
        try
        {
            // Check current content tree
            if (this.Content is FrameworkElement fe && fe.Resources.Contains("ModernWpfNavigationViewItemStyle"))
            {
                var styleObj2 = fe.Resources["ModernWpfNavigationViewItemStyle"];
                if (styleObj2 is Style s2)
                    return s2;
            }
            // Check application resources
            if (Application.Current.Resources.Contains("ModernWpfNavigationViewItemStyle"))
            {
                var appStyle = Application.Current.Resources["ModernWpfNavigationViewItemStyle"];
                if (appStyle is Style s3)
                    return s3;
            }
        }
        catch { }
        return null; // fallback - style optional
    }
    
    private async void ConfigureCategoriesButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Navigate to categories page for configuration
            NavigateToPage("Categories");
        }
        catch (Exception ex)
        {
            await ShowErrorMessage("Error", $"Failed to open categories page: {ex.Message}");
        }
    }
    
    private async void ToggleCategoryFavorite_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var menuItem = sender as System.Windows.Controls.MenuItem;
            var categoryIdStr = menuItem?.Tag?.ToString();
            
            if (string.IsNullOrEmpty(categoryIdStr) || !int.TryParse(categoryIdStr, out int categoryId))
            {
                await ShowErrorMessage("Error", "Unable to identify the category.");
                return;
            }
            
            using var scope = _serviceProvider.CreateScope();
            var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryInterface>();
            
            var category = await categoryService.GetByIdAsync(categoryId);
            if (category != null)
            {
                category.IsFavorite = !category.IsFavorite;
                await categoryService.UpdateAsync(category);
                await RefreshCategoriesAsync();
            }
        }
        catch (Exception ex)
        {
            await ShowErrorMessage("Error", $"Failed to update category: {ex.Message}");
        }
    }
}
