using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using VaultGuard.WPF.ViewModels;
using VaultGuard.Models.DTOs.Auth;
using VaultGuard.Services.Interfaces;
using System;

namespace VaultGuard.WPF.Views;

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
            _ = RefreshTwoFactorStatusAsync();
        }
    }

    private async System.Threading.Tasks.Task RefreshTwoFactorStatusAsync()
    {
        if (_serviceProvider == null || TwoFactorStatusText == null) return;
        try
        {
            // CurrentUser loads asynchronously in the view model's constructor — give it a moment.
            for (var i = 0; i < 20 && _viewModel?.CurrentUser == null; i++)
                await System.Threading.Tasks.Task.Delay(50);

            var enabled = await Helpers.TwoFactorDialogHelper.GetTwoFactorEnabledAsync(_serviceProvider, _viewModel?.CurrentUser?.Id);
            TwoFactorStatusText.Text = enabled ? "Enabled" : "Not enabled";
        }
        catch { TwoFactorStatusText.Text = "Not enabled"; }
    }

    private async void TwoFactorManageButton_Click(object sender, RoutedEventArgs e)
    {
        if (_serviceProvider == null || _viewModel == null) return;
        await Helpers.TwoFactorDialogHelper.OpenManageDialogAsync(_serviceProvider, _viewModel.CurrentUser?.Id, _viewModel.CurrentUser?.Email);
        await RefreshTwoFactorStatusAsync();
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
            // Re-authenticate before switching into the target profile.
            //  • If the target account has 2FA enabled, we only ask for the authenticator
            //    (or recovery) code — no master password.
            //  • Otherwise we fall back to the normal master-password workflow.
            if (_serviceProvider != null)
            {
                var profileService = _serviceProvider.GetService<IUserProfileService>();
                var twoFaEnabled = profileService != null && await profileService.IsTwoFactorEnabledAsync(user.Id);

                bool verified;
                if (twoFaEnabled)
                {
                    verified = await Helpers.SecurityGateHelper.PromptAndVerifyAsync(
                        _serviceProvider, user.Id,
                        "Verify it's you",
                        $"Enter the authenticator code for {user.Email} to switch to this profile.");
                }
                else
                {
                    verified = await Helpers.SecurityGateHelper.PromptMasterPasswordAsync(
                        _serviceProvider, user.Id,
                        "Switch profile",
                        $"Enter the master password for {user.Email} to switch to this profile.");
                }

                if (!verified) return;
            }

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
