using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Services.Interfaces;
using System;

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
        // Hide navigation menu initially until authenticated
        SetNavigationVisibility(false);
        
        // Start with Login page
        ContentFrame.Navigate(typeof(Views.LoginPage), _serviceProvider);
    }
    
    private void SetNavigationVisibility(bool isVisible)
    {
        if (isVisible)
        {
            MainNavigationView.IsEnabled = true;
            MainNavigationView.IsPaneVisible = true;
            MainNavigationView.Opacity = 1.0;
        }
        else
        {
            MainNavigationView.IsEnabled = false;
            MainNavigationView.IsPaneVisible = false;
            MainNavigationView.Opacity = 0.5;
        }
    }

    private void MainNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        // Only allow navigation if authenticated
        if (!_isAuthenticated) return;
        
        if (args.SelectedItem is NavigationViewItem selectedItem)
        {
            string tag = selectedItem.Tag?.ToString() ?? "";
            
            // Only navigate if the item has a tag (leaf items, not parent categories)
            if (!string.IsNullOrEmpty(tag))
            {
                NavigateToPage(tag);
            }
        }
    }

    public void NavigateToPage(string pageTag)
    {
        // Only allow navigation if authenticated, except for login
        if (!_isAuthenticated && pageTag != "Login") return;
        
        Type pageType = pageTag switch
        {
            "Home" => typeof(Views.DashboardPage),
            "Passwords" => typeof(Views.PasswordItemsPage),
            "Categories" => typeof(Views.CategoriesPage),
            "Import" => typeof(Views.ImportPage),
            "Settings" => typeof(Views.SettingsPage),
            "About" => typeof(Views.DashboardPage), // Could create an About page later
            "Login" => typeof(Views.LoginPage),
            _ => typeof(Views.DashboardPage)
        };

        ContentFrame.Navigate(pageType, _serviceProvider);
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
            NavigateToPage("Passwords");
            
            // Pass search query to the passwords page if it's currently loaded
            if (ContentFrame.Content is Views.PasswordItemsPage passwordsPage)
            {
                // Find the search textbox in the passwords page and set the search text
                var searchTextBox = FindChildControl<TextBox>(passwordsPage, "SearchTextBox");
                if (searchTextBox != null)
                {
                    searchTextBox.Text = searchQuery;
                }
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
        SetNavigationVisibility(true);
        MainNavigationView.SelectedItem = HomeNavItem;
        NavigateToPage("Home");
    }

    // Public method to handle logout
    public void HandleLogout()
    {
        _isAuthenticated = false;
        
        // Hide navigation menu
        SetNavigationVisibility(false);
        
        // Clear navigation selection
        MainNavigationView.SelectedItem = null;
        
        // Navigate back to login
        ContentFrame.Navigate(typeof(Views.LoginPage), _serviceProvider);
    }
}