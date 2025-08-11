using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.WinUi.Views;

/// <summary>
/// Main dashboard page showing password items and navigation
/// </summary>
public sealed partial class DashboardPage : Page
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IPasswordItemService _passwordItemService;

    public DashboardPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        if (e.Parameter is IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            // _passwordItemService = serviceProvider.GetRequiredService<IPasswordItemService>();
            LoadDashboard();
        }
    }

    private async void LoadDashboard()
    {
        try
        {
            // TODO: Load password items and populate UI
            WelcomeText.Text = "Welcome to Password Manager!";
            StatusText.Text = "Ready";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
    }

    private void AddPasswordButton_Click(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(typeof(PasswordItemsPage), _serviceProvider);
    }

    private void ViewPasswordsButton_Click(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(typeof(PasswordItemsPage), _serviceProvider);
    }

    private void CategoriesButton_Click(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(typeof(CategoriesPage), _serviceProvider);
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(typeof(SettingsPage), _serviceProvider);
    }

    private void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(typeof(ImportPage), _serviceProvider);
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement logout logic
        Frame.Navigate(typeof(LoginPage), _serviceProvider);
    }
}