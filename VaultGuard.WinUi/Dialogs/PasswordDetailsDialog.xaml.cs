using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VaultGuard.Models;
using VaultGuard.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace VaultGuard.WinUi.Dialogs;

public sealed partial class PasswordDetailsDialog : ContentDialog
{
    private readonly IPasswordRevealService _passwordRevealService;
    private readonly PasswordItem _passwordItem;
    private bool _isPasswordVisible = false;

    public PasswordDetailsDialog(IServiceProvider serviceProvider, PasswordItem passwordItem)
    {
        this.InitializeComponent();

        _passwordRevealService = serviceProvider.GetRequiredService<IPasswordRevealService>();
        _passwordItem = passwordItem;

        Title = passwordItem.Title;
        CloseButtonText = "Close";

        // Update the title area and show delete button only if we have a valid item id
        DialogTitleText.Text = passwordItem.Title;
        if (_passwordItem != null && _passwordItem.Id > 0)
        {
            DeleteItemButton.Visibility = Visibility.Visible;
        }

        LoadPasswordDetails();
    }

    private async void DeleteItemButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_passwordItem == null || _passwordItem.Id <= 0) return;

            var confirm = new ContentDialog
            {
                Title = "Confirm Deletion",
                Content = $"Are you sure you want to delete '{_passwordItem.Title}'? This action cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            var res = await confirm.ShowAsync();
            if (res == ContentDialogResult.Primary)
            {
                // Resolve password item service from the app's host services
                var serviceProvider = (App.Current as App)?.Services;
                var passwordService = serviceProvider?.GetService<VaultGuard.Services.Interfaces.IPasswordItemService>();
                if (passwordService != null)
                {
                    await passwordService.DeleteAsync(_passwordItem.Id);

                    // Update and show the in-dialog ModerateInfoBar message
                    if (ModerateInfoBar != null)
                    {
                        ModerateInfoBar.IsOpen = true;
                        ModerateInfoBar.Visibility = Visibility.Visible;
                        // Update message content if possible
                        try
                        {
                            ModerateInfoBar.Message = $"'{_passwordItem.Title}' has been deleted.";
                        }
                        catch (Exception ex) {
                            VaultGuard.Services.Logging.AppLogger.Error($"Unhandled exception", ex);
                        }
                    }
                }

                // Close dialog after a short delay to let user see the message
                await Task.Delay(900);
                this.Hide();
            }
        }
        catch (Exception ex) {
            VaultGuard.Services.Logging.AppLogger.Error($"Unhandled exception", ex);
        }
    }

    private async void EditItemButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var serviceProvider = (App.Current as App)?.Services;
            if (serviceProvider == null) return;

            var dialog = new Dialogs.AddPasswordDialog(serviceProvider, _passwordItem);
            dialog.XamlRoot = this.XamlRoot;

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && dialog.Result is not null)
            {
                // Refresh details from service
                var passwordService = serviceProvider.GetService<VaultGuard.Services.Interfaces.IPasswordItemService>();
                if (passwordService != null)
                {
                    var updated = await passwordService.GetByIdAsync(_passwordItem.Id);
                    if (updated != null)
                    {
                        _passwordItem.Title = updated.Title;
                        _passwordItem.Description = updated.Description;
                        _passwordItem.LastModified = updated.LastModified;
                        LoadPasswordDetails();
                    }
                }
            }
        }
        catch (Exception ex) {
            VaultGuard.Services.Logging.AppLogger.Error($"Unhandled exception", ex);
        }
    }

    private async void LoadPasswordDetails()
    {
        try
        {
            // Populate basic details
            TitleText.Text = _passwordItem.Title;
            TypeText.Text = _passwordItem.Type.ToString();
            CreatedText.Text = _passwordItem.CreatedAt.ToString("g");
            ModifiedText.Text = _passwordItem.LastModified.ToString("g");

            if (!string.IsNullOrEmpty(_passwordItem.Description))
            {
                DescriptionText.Text = _passwordItem.Description;
                DescriptionText.Visibility = Visibility.Visible;
            }

            // Load type-specific details
            if (_passwordItem.Type == ItemType.Login && _passwordItem.LoginItem != null)
            {
                LoginDetailsPanel.Visibility = Visibility.Visible;
                UsernameText.Text = _passwordItem.LoginItem.Username ?? "Not set";
                UsernameText.CopyText = _passwordItem.LoginItem.Username ?? string.Empty;

                PasswordText.Text = "••••••••";
                // CopyText will be filled when revealing/copying via the reveal service; keep empty for now
                PasswordText.CopyText = string.Empty;

                if (!string.IsNullOrEmpty(_passwordItem.LoginItem.WebsiteUrl))
                {
                    UrlText.Text = _passwordItem.LoginItem.WebsiteUrl;
                    UrlText.CopyText = _passwordItem.LoginItem.WebsiteUrl;
                    UrlText.Visibility = Visibility.Visible;
                    UrlLabel.Visibility = Visibility.Visible;
                }
            }
        }
        catch (Exception ex) {
            VaultGuard.Services.Logging.AppLogger.Error($"Unhandled exception", ex);
        }
    }

    private async void TogglePasswordButton_Click(object sender, RoutedEventArgs e)
    {
        if (_passwordItem.Type == ItemType.Login && _passwordItem.LoginItem != null)
        {
            try
            {
                _isPasswordVisible = !_isPasswordVisible;

                if (_isPasswordVisible)
                {
                    // Reveal password - need to pass loginItem and sessionId
                    var revealedPassword = await _passwordRevealService.RevealPasswordAsync(_passwordItem.LoginItem, "current-session");
                    if (!string.IsNullOrEmpty(revealedPassword))
                    {
                        PasswordText.Text = revealedPassword;
                        TogglePasswordButton.Content = "🙈 Hide";
                    }
                }
                else
                {
                    // Hide password
                    PasswordText.Text = "••••••••";
                    TogglePasswordButton.Content = "👁️ Show";
                }
            }
            catch (Exception ex) {
                VaultGuard.Services.Logging.AppLogger.Error($"Unhandled exception", ex);
            }
        }
    }

    private async void CopyUsernameButton_Click(object sender, RoutedEventArgs e)
    {
        if (_passwordItem.Type == ItemType.Login && _passwordItem.LoginItem != null &&
            !string.IsNullOrEmpty(_passwordItem.LoginItem.Username))
        {
            try
            {
                var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                dataPackage.SetText(_passwordItem.LoginItem.Username);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);

                CopyUsernameButton.Content = "✅ Copied";
                await Task.Delay(2000);
                CopyUsernameButton.Content = "📋 Copy";
            }
            catch (Exception ex) {
                VaultGuard.Services.Logging.AppLogger.Error($"Unhandled exception", ex);
            }
        }
    }

    private async void CopyPasswordButton_Click(object sender, RoutedEventArgs e)
    {
        if (_passwordItem.Type == ItemType.Login && _passwordItem.LoginItem != null)
        {
            try
            {
                var revealedPassword = await _passwordRevealService.RevealPasswordAsync(_passwordItem.LoginItem, "current-session");
                if (!string.IsNullOrEmpty(revealedPassword))
                {
                    var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                    dataPackage.SetText(revealedPassword);
                    Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);

                    CopyPasswordButton.Content = "✅ Copied";
                    await Task.Delay(2000);
                    CopyPasswordButton.Content = "📋 Copy";
                }
            }
            catch (Exception ex) {
                VaultGuard.Services.Logging.AppLogger.Error($"Unhandled exception", ex);
            }
        }
    }

    private async void OpenUrlButton_Click(object sender, RoutedEventArgs e)
    {
        if (_passwordItem.Type == ItemType.Login && _passwordItem.LoginItem != null &&
            !string.IsNullOrEmpty(_passwordItem.LoginItem.WebsiteUrl))
        {
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _passwordItem.LoginItem.WebsiteUrl,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(startInfo);
            }
            catch (Exception ex) {
                VaultGuard.Services.Logging.AppLogger.Error($"Unhandled exception", ex);
            }
        }
    }
}