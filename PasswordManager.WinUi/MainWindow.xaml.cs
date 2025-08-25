using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Services.Interfaces;
using PasswordManager.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PasswordManager.WinUi;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly IServiceProvider _serviceProvider;
    private bool _isAuthenticated = false;

    public MainWindow(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        this.InitializeComponent();
        this.Title = "Password Manager - WinUI";

        // Set window size
        this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1200, 800));

        // Initialize navigation - start with Login if not authenticated, otherwise Home
        InitializeNavigation();
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
                "LoginCategory" => typeof(Views.PasswordItemsPage), // Filter for login items
                "CreditCardCategory" => typeof(Views.PasswordItemsPage), // Filter for credit cards
                "SecureNotesCategory" => typeof(Views.PasswordItemsPage), // Filter for secure notes
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

            // Ensure ContentFrame exists and navigate with service provider
            if (ContentFrame != null && _serviceProvider != null)
            {
                ContentFrame.Navigate(pageType, _serviceProvider);

                // Pass filter parameters for category-specific views
                PassFilterToPage(pageTag);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Navigation failed - ContentFrame: {ContentFrame != null}, ServiceProvider: {_serviceProvider != null}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation error for page '{pageTag}': {ex.Message}");
        }
    }

    private void PassFilterToPage(string pageTag)
    {
        // If we're navigating to PasswordItemsPage with a filter, apply the filter
        if (ContentFrame.Content is Views.PasswordItemsPage passwordsPage)
        {
            // This would need to be implemented in the PasswordItemsPage to accept filter parameters
            // For now, we'll handle this through a public method in the page or its ViewModel
            switch (pageTag)
            {
                case "Favorites":
                    // Apply favorites filter
                    break;
                case "LoginCategory":
                    // Apply login items filter
                    break;
                case "CreditCardCategory":
                    // Apply credit card filter
                    break;
                case "SecureNotesCategory":
                    // Apply secure notes filter
                    break;
                case "WiFiCategory":
                    // Apply WiFi items filter
                    break;
                case "PasskeysCategory":
                    // Apply passkeys filter
                    break;
                case "Archive":
                    // Apply archived items filter
                    break;
                case "RecentlyDeleted":
                    // Apply recently deleted filter
                    break;
            }
        }
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        // Handle search text changes
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            // You can implement search suggestions here
            // For now, we'll just handle the basic search functionality
        }
    }

    private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        // Handle search query submission
        string searchQuery = args.QueryText;

        if (!string.IsNullOrEmpty(searchQuery))
        {
            // Navigate to passwords page with search query
            NavigateToPage("AllItems");

            // Use a more reliable way to pass search query
            Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
            {
                PassSearchQueryToPage(searchQuery);
            });
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
        SetAuthenticationState(true);
        MainNavigationView.SelectedItem = AllItemsNavItem;
        NavigateToPage("AllItems");
    }

    // Public method to handle logout
    public void HandleLogout()
    {
        _isAuthenticated = false;

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
        // Create and show profile flyout menu
        var button = sender as Button;
        if (button == null) return;

        var flyout = new MenuFlyout();

        // Profile info section
        var profileItem = new MenuFlyoutItem
        {
            Text = "Profile Settings",
            Icon = new SymbolIcon(Symbol.Contact)
        };
        profileItem.Click += ProfileSettings_Click;
        flyout.Items.Add(profileItem);

        // Add separator
        flyout.Items.Add(new MenuFlyoutSeparator());

        // Settings
        var settingsItem = new MenuFlyoutItem
        {
            Text = "Settings",
            Icon = new SymbolIcon(Symbol.Setting)
        };
        settingsItem.Click += (s, args) => NavigateToPage("Settings");
        flyout.Items.Add(settingsItem);

        // Add separator
        flyout.Items.Add(new MenuFlyoutSeparator());

        // Logout
        var logoutItem = new MenuFlyoutItem
        {
            Text = "Sign Out",
            Icon = new SymbolIcon(Symbol.LeaveChat)
        };
        logoutItem.Click += Logout_Click;
        flyout.Items.Add(logoutItem);

        // Show the flyout
        flyout.ShowAt(button);
    }

    private async void ProfileSettings_Click(object sender, RoutedEventArgs e)
    {
        // Show a simple profile info dialog
        var dialog = new ContentDialog
        {
            Title = "Profile Information",
            Content = new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    new TextBlock { Text = "User: David Smith", FontWeight = FontWeights.SemiBold },
                    new TextBlock { Text = "Vault: Personal", FontSize = 14 },
                    new TextBlock { Text = "Items: " + await GetPasswordItemCount(), FontSize = 14 },
                    new TextBlock { Text = "Last Login: " + DateTime.Now.ToString("MM/dd/yyyy HH:mm"), FontSize = 14 }
                }
            },
            CloseButtonText = "Close",
            XamlRoot = this.Content.XamlRoot
        };
        
        await dialog.ShowAsync();
    }

    private async Task<string> GetPasswordItemCount()
    {
        try
        {
            var passwordService = _serviceProvider.GetService<IPasswordItemIterface>();
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

            var result = await tagDialog.ShowAsync();
            if (result == ContentDialogResult.Primary && tagDialog.Result != null)
            {
                System.Diagnostics.Debug.WriteLine($"Created new tag: {tagDialog.Result.Name}");
                
                // TODO: Refresh the navigation menu to show the new tag
                // This would require dynamically updating the navigation menu items
                
                // Show success message
                await ShowInfoMessage("Tag Created", $"Tag '{tagDialog.Result.Name}' has been created successfully.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating tag: {ex.Message}");
            await ShowErrorMessage("Error", $"Failed to create tag: {ex.Message}");
        }
    }

    private async void DeleteTagNavButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var tagService = _serviceProvider.GetService<ITagService>();
            if (tagService == null)
            {
                await ShowErrorMessage("Error", "Tag service is not available.");
                return;
            }

            // Get all tags to show selection
            var tags = await tagService.GetAllAsync();
            if (!tags.Any())
            {
                await ShowInfoMessage("No Tags", "There are no tags to delete.");
                return;
            }

            // Create a selection dialog
            var dialog = new ContentDialog
            {
                Title = "Delete Tag",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.Content.XamlRoot
            };

            var comboBox = new ComboBox
            {
                PlaceholderText = "Select a tag to delete",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 12, 0, 0)
            };

            foreach (var tag in tags.Where(t => !t.IsSystemTag)) // Don't allow deleting system tags
            {
                comboBox.Items.Add(new ComboBoxItem
                {
                    Content = tag.Name,
                    Tag = tag
                });
            }

            var content = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "Select a tag to delete. This action cannot be undone." },
                    comboBox
                }
            };

            dialog.Content = content;

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && comboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var selectedTag = (Tag)selectedItem.Tag;
                
                // Confirm deletion
                var confirmDialog = new ContentDialog
                {
                    Title = "Confirm Deletion",
                    Content = $"Are you sure you want to delete the tag '{selectedTag.Name}'? This action cannot be undone.",
                    PrimaryButtonText = "Delete",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.Content.XamlRoot
                };

                var confirmResult = await confirmDialog.ShowAsync();
                if (confirmResult == ContentDialogResult.Primary)
                {
                    await tagService.DeleteAsync(selectedTag.Id);
                    await ShowInfoMessage("Tag Deleted", $"Tag '{selectedTag.Name}' has been deleted successfully.");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting tag: {ex.Message}");
            await ShowErrorMessage("Error", $"Failed to delete tag: {ex.Message}");
        }
    }

    private async void AddCategoryNavButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Open the CategoryDialog for creating a new category
            var categoryDialog = new Dialogs.CategoryDialog(_serviceProvider);
            categoryDialog.XamlRoot = this.Content.XamlRoot;

            var result = await categoryDialog.ShowAsync();
            if (result == ContentDialogResult.Primary && categoryDialog.Result != null)
            {
                System.Diagnostics.Debug.WriteLine($"Created new category: {categoryDialog.Result.Name}");
                
                // TODO: Refresh the navigation menu to show the new category
                // This would require dynamically updating the navigation menu items
                
                // Show success message
                await ShowInfoMessage("Category Created", $"Category '{categoryDialog.Result.Name}' has been created successfully.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating category: {ex.Message}");
            await ShowErrorMessage("Error", $"Failed to create category: {ex.Message}");
        }
    }

    private async void DeleteCategoryNavButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var categoryService = _serviceProvider.GetService<ICategoryInterface>();
            if (categoryService == null)
            {
                await ShowErrorMessage("Error", "Category service is not available.");
                return;
            }

            // Get all categories to show selection
            var categories = await categoryService.GetAllAsync();
            if (!categories.Any())
            {
                await ShowInfoMessage("No Categories", "There are no categories to delete.");
                return;
            }

            // Create a selection dialog
            var dialog = new ContentDialog
            {
                Title = "Delete Category",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.Content.XamlRoot
            };

            var comboBox = new ComboBox
            {
                PlaceholderText = "Select a category to delete",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 12, 0, 0)
            };

            foreach (var category in categories)
            {
                comboBox.Items.Add(new ComboBoxItem
                {
                    Content = category.Name,
                    Tag = category
                });
            }

            var content = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "Select a category to delete. This action cannot be undone." },
                    comboBox
                }
            };

            dialog.Content = content;

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && comboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var selectedCategory = (Category)selectedItem.Tag;
                
                // Confirm deletion
                var confirmDialog = new ContentDialog
                {
                    Title = "Confirm Deletion",
                    Content = $"Are you sure you want to delete the category '{selectedCategory.Name}'? This action cannot be undone.",
                    PrimaryButtonText = "Delete",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.Content.XamlRoot
                };

                var confirmResult = await confirmDialog.ShowAsync();
                if (confirmResult == ContentDialogResult.Primary)
                {
                    await categoryService.DeleteAsync(selectedCategory.Id);
                    await ShowInfoMessage("Category Deleted", $"Category '{selectedCategory.Name}' has been deleted successfully.");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting category: {ex.Message}");
            await ShowErrorMessage("Error", $"Failed to delete category: {ex.Message}");
        }
    }

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

    private async void DeleteVaultButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var collectionService = _serviceProvider.GetService<ICollectionService>();
            if (collectionService == null)
            {
                await ShowErrorMessage("Error", "Collection service is not available.");
                return;
            }

            // Get all collections to show selection
            var collections = await collectionService.GetAllAsync();
            if (!collections.Any())
            {
                await ShowInfoMessage("No Vaults", "There are no vaults to delete.");
                return;
            }

            // Create a selection dialog
            var dialog = new ContentDialog
            {
                Title = "Delete Vault",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.Content.XamlRoot
            };

            var comboBox = new ComboBox
            {
                PlaceholderText = "Select a vault to delete",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 12, 0, 0)
            };

            foreach (var collection in collections.Where(c => !c.IsDefault)) // Don't allow deleting default vault
            {
                comboBox.Items.Add(new ComboBoxItem
                {
                    Content = collection.Name,
                    Tag = collection
                });
            }

            var content = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "Select a vault to delete. This action cannot be undone and will affect all items in the vault." },
                    comboBox
                }
            };

            dialog.Content = content;

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && comboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var selectedCollection = (Collection)selectedItem.Tag;
                
                // Confirm deletion
                var confirmDialog = new ContentDialog
                {
                    Title = "Confirm Deletion",
                    Content = $"Are you sure you want to delete the vault '{selectedCollection.Name}'? This will also delete all items in this vault. This action cannot be undone.",
                    PrimaryButtonText = "Delete",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.Content.XamlRoot
                };

                var confirmResult = await confirmDialog.ShowAsync();
                if (confirmResult == ContentDialogResult.Primary)
                {
                    await collectionService.DeleteAsync(selectedCollection.Id);
                    await ShowInfoMessage("Vault Deleted", $"Vault '{selectedCollection.Name}' has been deleted successfully.");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting vault: {ex.Message}");
            await ShowErrorMessage("Error", $"Failed to delete vault: {ex.Message}");
        }
    }

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
}