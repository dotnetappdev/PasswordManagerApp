using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PasswordManager.Models;
using PasswordManager.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace PasswordManager.WinUi.Dialogs;

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
        
        LoadPasswordDetails();
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
                PasswordText.Text = "••••••••";
                
                if (!string.IsNullOrEmpty(_passwordItem.LoginItem.Url))
                {
                    UrlText.Text = _passwordItem.LoginItem.Url;
                    UrlText.Visibility = Visibility.Visible;
                    UrlLabel.Visibility = Visibility.Visible;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading password details: {ex.Message}");
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
                    // Reveal password
                    var revealResult = await _passwordRevealService.RevealPasswordAsync(_passwordItem.Id);
                    if (revealResult.IsSuccess && revealResult.RevealedPassword != null)
                    {
                        PasswordText.Text = revealResult.RevealedPassword;
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error toggling password visibility: {ex.Message}");
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error copying username: {ex.Message}");
            }
        }
    }

    private async void CopyPasswordButton_Click(object sender, RoutedEventArgs e)
    {
        if (_passwordItem.Type == ItemType.Login && _passwordItem.LoginItem != null)
        {
            try
            {
                var revealResult = await _passwordRevealService.RevealPasswordAsync(_passwordItem.Id);
                if (revealResult.IsSuccess && revealResult.RevealedPassword != null)
                {
                    var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                    dataPackage.SetText(revealResult.RevealedPassword);
                    Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
                    
                    CopyPasswordButton.Content = "✅ Copied";
                    await Task.Delay(2000);
                    CopyPasswordButton.Content = "📋 Copy";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error copying password: {ex.Message}");
            }
        }
    }

    private async void OpenUrlButton_Click(object sender, RoutedEventArgs e)
    {
        if (_passwordItem.Type == ItemType.Login && _passwordItem.LoginItem != null && 
            !string.IsNullOrEmpty(_passwordItem.LoginItem.Url))
        {
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _passwordItem.LoginItem.Url,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening URL: {ex.Message}");
            }
        }
    }
}