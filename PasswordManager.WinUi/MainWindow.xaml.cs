using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
        // For now, we'll start with Login page. In a real app, you'd check authentication state
        // and navigate to Home if already authenticated
        ContentFrame.Navigate(typeof(Views.LoginPage), _serviceProvider);
    }

    private void MainNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem selectedItem)
        {
            string tag = selectedItem.Tag?.ToString() ?? "";
            NavigateToPage(tag);
        }
    }

    private void NavigateToPage(string pageTag)
    {
        Type pageType = pageTag switch
        {
            "Home" => typeof(Views.DashboardPage),
            "Passwords" => typeof(Views.PasswordItemsPage),
            "Categories" => typeof(Views.CategoriesPage),
            "Import" => typeof(Views.ImportPage),
            "Settings" => typeof(Views.SettingsPage),
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
            // You would typically pass the search query as a parameter
            NavigateToPage("Passwords");
            
            // TODO: Implement search functionality in the target page
            // This could involve passing the search query to the page or using a search service
        }
    }

    // Public method to allow programmatic navigation (e.g., after login)
    public void NavigateToHome()
    {
        MainNavigationView.SelectedItem = HomeNavItem;
        NavigateToPage("Home");
    }

    // Public method to handle logout
    public void HandleLogout()
    {
        // Clear navigation selection
        MainNavigationView.SelectedItem = null;
        
        // Navigate back to login
        ContentFrame.Navigate(typeof(Views.LoginPage), _serviceProvider);
    }
}