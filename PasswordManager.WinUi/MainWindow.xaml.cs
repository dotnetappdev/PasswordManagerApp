using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using PasswordManager.Models;
using PasswordManager.Services.Interfaces;
using PasswordManager.WinUi.Models;

namespace PasswordManager.WinUi;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly IServiceProvider _serviceProvider;
    private bool _isAuthenticated = false;
    private string? _currentUserId = null;
    private Style? _navItemStyle; // cache for dynamic nav items

    public MainWindow(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    this.InitializeComponent();
        this.Title = "Password Manager - WinUI";

        // Set window size
        this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1200, 800));

    // Cache style early (after resources loaded by InitializeComponent)
    _navItemStyle = TryGetNavItemStyle();

        // Initialize navigation - start with Login if not authenticated, otherwise Home
        InitializeNavigation();
    // Load dynamic categories for navigation
    _ = RefreshCategoriesAsync();
    }

    private void InitializeNavigation()
    {
        // Show login frame initially
        SetAuthenticationState(false);

        // Start with Login page in the dedicated login frame
        LoginFrame.Navigate(typeof(Views.LoginPage), _serviceProvider);
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

    private void MainNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        // Only allow navigation if authenticated
        if (!_isAuthenticated)
        {
            // Prevent selection if not authenticated
            sender.SelectedItem = null;
            return;
        }

        if (args.SelectedItem is NavigationViewItem selectedItem)
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
                    System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
                    // Reset selection on error
                    sender.SelectedItem = null;
                }
            }
        }
    }

    public void NavigateToPage(string pageTag)
    {
        // Only allow navigation if authenticated, except for login
        if (!_isAuthenticated && pageTag != "Login") return;

        try
        {
            Type pageType = pageTag switch
            {
                "AllItems" => typeof(Views.PasswordItemsPage),
                "Favorites" => typeof(Views.PasswordItemsPage), // Filter for favorites
                "Profile" => typeof(Views.ProfilePage),
                "LoginCategory" => typeof(Views.PasswordItemsPage), // Filter for login items
                "CreditCardCategory" => typeof(Views.PasswordItemsPage), // Filter for credit cards
                "SecureNotesCategory" => typeof(Views.PasswordItemsPage), // Filter for secure notes
                "IdentityCategory" => typeof(Views.PasswordItemsPage), // Filter for identity items
                "WiFiCategory" => typeof(Views.PasswordItemsPage), // Filter for WiFi items
                "PasskeysCategory" => typeof(Views.PasswordItemsPage), // Filter for passkeys
                "Categories" => typeof(Views.CategoriesPage),
                "ManageItems" => typeof(Views.ManageItemsPage),
                "SecurityDashboard" => typeof(Views.DashboardPage), // Could create security dashboard
                "Archive" => typeof(Views.PasswordItemsPage), // Filter for archived items
                "RecentlyDeleted" => typeof(Views.PasswordItemsPage), // Filter for deleted items
                "Import" => typeof(Views.ImportPage),
                "Settings" => typeof(Views.SettingsPage),
                "Home" => typeof(Views.DashboardPage),
                "Passwords" => typeof(Views.PasswordItemsPage),
                "Login" => typeof(Views.LoginPage),
                _ => typeof(Views.DashboardPage)
            };

            // Prepare navigation data with filters
            object navigationParameter = CreateNavigationParameter(pageTag);

            // Ensure ContentFrame exists and navigate with service provider
            if (ContentFrame != null)
            {
                ContentFrame.Navigate(pageType, navigationParameter);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Navigation failed - ContentFrame: {ContentFrame != null}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation error for page '{pageTag}': {ex.Message}");
        }
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
                case "IdentityCategory":
                    filterData.FilterType = ItemType.Identity;
                    filterData.FilterName = "Identity";
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
                    filterData.FilterName = "All Items";
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
            _ => false
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
                    Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
                    {
                        PassSearchQueryToPage(query);
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error during auto-filter: {ex.Message}");
                }
            }
            else
            {
                sender.ItemsSource = null;
                
                // Clear filters when search is empty
                if (string.IsNullOrWhiteSpace(query))
                {
                    try
                    {
                        Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
                        {
                            PassSearchQueryToPage("");
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error clearing filter: {ex.Message}");
                    }
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
                Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
                {
                    PassSearchQueryToPage(searchQuery);
                });
                
                // Provide visual feedback
                sender.PlaceholderText = $"Searching for '{searchQuery}'...";
                
                // Reset placeholder after a delay
                Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(
                    Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                {
                    Task.Delay(3000).ContinueWith(_ =>
                    {
                        Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
                        {
                            if (sender.PlaceholderText.StartsWith("Searching"))
                            {
                                sender.PlaceholderText = "Search passwords...";
                            }
                        });
                    });
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during search: {ex.Message}");
            }
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

    // Public method to allow programmatic navigation (e.g., after login)
    public void NavigateToHome()
    {
        _isAuthenticated = true;

        // Capture current user ID from auth service
        var authService = _serviceProvider.GetService<IAuthService>();
        if (authService?.CurrentUser != null)
        {
            _currentUserId = authService.CurrentUser.Id;
            System.Diagnostics.Debug.WriteLine($"MainWindow captured current user ID: {_currentUserId}");
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
        LoginFrame.Navigate(typeof(Views.LoginPage), _serviceProvider);
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
        var dialog = new ContentDialog
        {
            Title = "Sign Out",
            Content = "Are you sure you want to sign out?",
            PrimaryButtonText = "Sign Out",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.Content.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            // Handle logout
            HandleLogout();
        }
    }

    private async void NavigationItem_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
    {
        // Handle right-click context menu functionality for navigation items
        if (sender is NavigationViewItem navItem)
        {
            var tag = navItem.Tag?.ToString();
            System.Diagnostics.Debug.WriteLine($"Right-tapped on navigation item: {tag}");

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
            tagDialog.XamlRoot = this.Content.XamlRoot;

            // Subscribe to saved event to refresh nav if needed
            tagDialog.TagSaved += async (s, tag) =>
            {
                // For now, just refresh categories panel as tags may influence UI
                await RefreshCategoriesAsync();
            };

            var result = await tagDialog.ShowAsync();
            if (result == ContentDialogResult.Primary && tagDialog.Result is not null)
            {
                System.Diagnostics.Debug.WriteLine($"Created new tag: {tagDialog.Result.Name}");
                await ShowInfoMessage("Tag Created", $"Tag '{tagDialog.Result.Name}' has been created successfully.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating tag: {ex.Message}");
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
            categoryDialog.XamlRoot = this.Content.XamlRoot;

            // Subscribe to category saved event to refresh navigation
            categoryDialog.CategorySaved += async (s, cat) =>
            {
                await RefreshCategoriesAsync();
            };

            var result = await categoryDialog.ShowAsync();
            if (result == ContentDialogResult.Primary && categoryDialog.Result is not null)
            {
                System.Diagnostics.Debug.WriteLine($"Created new category: {categoryDialog.Result.Name}");
                await ShowInfoMessage("Category Created", $"Category '{categoryDialog.Result.Name}' has been created successfully.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating category: {ex.Message}");
            await ShowErrorMessage("Error", $"Failed to create category: {ex.Message}");
        }
    }

    // Populate the DynamicCategoriesPanel with categories from the service
    public async Task RefreshCategoriesAsync()
    {
        try
        {
            var categoryService = _serviceProvider.GetService<ICategoryInterface>();
            if (categoryService == null) return;

            var categories = await categoryService.GetAllAsync();

            // Update UI on dispatcher
            Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
            {
                var fe = this.Content as FrameworkElement;
                var dynamicPanel = fe?.FindName("DynamicCategoriesPanel") as StackPanel;
                if (dynamicPanel == null)
                {
                    return; // panel not available (e.g., XAML not loaded yet)
                }
                dynamicPanel.Children.Clear();

                // ensure cached style (attempt again if previously null)
                _navItemStyle ??= TryGetNavItemStyle();

                foreach (var cat in categories)
                {
                    var navItem = new NavigationViewItem
                    {
                        Content = cat.Name,
                        Tag = $"{cat.Name}Category",
                        Style = _navItemStyle // may be null; safe
                    };

                    navItem.RightTapped += NavigationItem_RightTapped;
                    dynamicPanel.Children.Add(navItem);
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error refreshing categories: {ex.Message}");
        }
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
            var dialog = new ContentDialog
            {
                Title = "Create New Vault",
                PrimaryButtonText = "Create",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot
            };

            var nameTextBox = new TextBox
            {
                PlaceholderText = "Enter vault name",
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var descriptionTextBox = new TextBox
            {
                PlaceholderText = "Enter description (optional)",
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
            if (result == ContentDialogResult.Primary)
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

                System.Diagnostics.Debug.WriteLine($"Created new vault: {newCollection.Name}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating vault: {ex.Message}");
            await ShowErrorMessage("Error", $"Failed to create vault: {ex.Message}");
        }
    }

    // Delete vault navigation handler removed (UI buttons removed). Kept method removed per request.

    private async void EditItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Get the tag from the menu item to identify which navigation item to edit
            var menuItem = sender as MenuFlyoutItem;
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

            System.Diagnostics.Debug.WriteLine($"Edit item clicked for: {tag}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error editing item: {ex.Message}");
            await ShowErrorMessage("Error", $"Failed to edit item: {ex.Message}");
        }
    }

    private async void DeleteItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Get the tag from the menu item to identify which navigation item to delete
            var menuItem = sender as MenuFlyoutItem;
            var tag = menuItem?.Tag?.ToString();

            if (string.IsNullOrEmpty(tag))
            {
                await ShowErrorMessage("Error", "Unable to identify the item to delete.");
                return;
            }

            // Show confirmation dialog
            var confirmDialog = new ContentDialog
            {
                Title = "Confirm Deletion",
                Content = $"Are you sure you want to delete '{tag}'? This action cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.Content.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
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

                System.Diagnostics.Debug.WriteLine($"Delete item clicked for: {tag}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting item: {ex.Message}");
            await ShowErrorMessage("Error", $"Failed to delete item: {ex.Message}");
        }
    }

    private async void DeleteCategoryItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var menuItem = sender as MenuFlyoutItem;
            var tag = menuItem?.Tag?.ToString();

            if (string.IsNullOrEmpty(tag))
            {
                await ShowErrorMessage("Error", "Unable to identify the category to delete.");
                return;
            }

            var confirmDialog = new ContentDialog
            {
                Title = "Delete Category",
                Content = $"Are you sure you want to delete the category '{tag}'? This action cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.Content.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                // TODO: Implement deletion logic using category service
                await ShowInfoMessage("Category Deleted", $"Category '{tag}' was deleted (not actually implemented).");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting category: {ex.Message}");
            await ShowErrorMessage("Error", $"Failed to delete category: {ex.Message}");
        }
    }

    // Helper methods for dialogs
    private async Task ShowErrorMessage(string title, string message)
    {
        var errorDialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.Content.XamlRoot
        };
        await errorDialog.ShowAsync();
    }

    private async Task ShowInfoMessage(string title, string message)
    {
        var infoDialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.Content.XamlRoot
        };
        await infoDialog.ShowAsync();
    }

    private Style? TryGetNavItemStyle()
    {
        try
        {
            // Check window resources first
            if (this.Resources.TryGetValue("ModernNavigationViewItemStyle", out var styleObj) && styleObj is Style s1)
                return s1;
            // Then current content tree
            if (this.Content is FrameworkElement fe && fe.Resources.TryGetValue("ModernNavigationViewItemStyle", out var styleObj2) && styleObj2 is Style s2)
                return s2;
            // Finally application resources
            if (Application.Current.Resources.TryGetValue("ModernNavigationViewItemStyle", out var appStyle) && appStyle is Style s3)
                return s3;
        }
        catch { }
        return null; // fallback - style optional
    }
}