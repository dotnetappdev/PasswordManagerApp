using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.WinUi.Views;

/// <summary>
/// Login page for the Password Manager application
/// </summary>
public sealed partial class LoginPage : Page
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAuthService _authService;
    private readonly IVaultSessionService _vaultSessionService;

    public LoginPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        if (e.Parameter is IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            // _authService = serviceProvider.GetRequiredService<IAuthService>();
            // _vaultSessionService = serviceProvider.GetRequiredService<IVaultSessionService>();
        }
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            LoginButton.IsEnabled = false;
            LoginProgressRing.IsActive = true;
            ErrorText.Visibility = Visibility.Collapsed;

            var email = EmailTextBox.Text;
            var password = PasswordBox.Password;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ErrorText.Text = "Please enter both email and password.";
                ErrorText.Visibility = Visibility.Visible;
                return;
            }

            // TODO: Implement authentication logic
            // For now, simulate login
            await Task.Delay(1000);

            // Navigate to main dashboard
            Frame.Navigate(typeof(DashboardPage), _serviceProvider);
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"Login failed: {ex.Message}";
            ErrorText.Visibility = Visibility.Visible;
        }
        finally
        {
            LoginButton.IsEnabled = true;
            LoginProgressRing.IsActive = false;
        }
    }
}