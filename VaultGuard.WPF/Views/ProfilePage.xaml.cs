using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.WPF.ViewModels;
using PasswordManager.Models.DTOs.Auth;
using System;

namespace PasswordManager.WPF.Views;

public sealed partial class ProfilePage : Page
{
    private ProfilePageViewModel? _viewModel;
    private IServiceProvider? _serviceProvider;

    public ProfilePage()
    {
        this.InitializeComponent();
    }

    public void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
    {
        // Note: WPF Page doesn't have base.OnNavigatedTo
        if (e.ExtraData is IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _viewModel = new ProfilePageViewModel(serviceProvider);
            this.DataContext = _viewModel;
        }
    }

    private void EditProfileButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel?.StartEditing();
    }

    private async void SaveProfileButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            await _viewModel.SaveProfileAsync();
        }
    }

    private void CancelEditButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel?.CancelEditing();
    }

    private void SwitchProfileButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel?.ShowManageProfiles();
    }

    private void CloseProfileManagementButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel?.HideManageProfiles();
    }

    private async void ProfileSwitchButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is UserDto user && _viewModel != null)
        {
            await _viewModel.SwitchToProfileAsync(user);
        }
    }

    private async void CreateNewProfileButton_Click(object sender, RoutedEventArgs e)
    {
        // For now, navigate back to login for profile creation
        // In a full implementation, this would open a profile creation dialog
        if (GetMainWindow() is MainWindow mainWindow)
        {
            mainWindow.HandleLogout();
        }
    }

    private MainWindow? GetMainWindow()
    {
        return (App.Current as App)?.MainWindow;
    }
}
