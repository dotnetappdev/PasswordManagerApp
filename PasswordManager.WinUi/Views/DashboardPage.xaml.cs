using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Services.Interfaces;
using PasswordManager.WinUi.ViewModels;

namespace PasswordManager.WinUi.Views;

/// <summary>
/// Main dashboard page showing password items and navigation
/// </summary>
public sealed partial class DashboardPage : Page
{
    private readonly IServiceProvider _serviceProvider;
    private DashboardViewModel? _viewModel;

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
            _viewModel = new DashboardViewModel(serviceProvider);
            this.DataContext = _viewModel;
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

    private async void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            await _viewModel.LogoutAsync();
        }
        
        // Navigate back to login
        Frame.Navigate(typeof(LoginPage), _serviceProvider);
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            await _viewModel.RefreshAsync();
        }
    }
}