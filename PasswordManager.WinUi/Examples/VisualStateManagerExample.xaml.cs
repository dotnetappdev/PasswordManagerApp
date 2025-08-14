using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace PasswordManager.WinUi.Examples;

public sealed partial class VisualStateExample : Page
{
    public VisualStateExample()
    {
        this.InitializeComponent();
    }

    private void SetAuthenticationState(string stateName)
    {
        VisualStateManager.GoToState(this, stateName, true);
    }

    private void SetUiMode(bool isFirstTimeSetup)
    {
        string stateName = isFirstTimeSetup ? "FirstTimeSetup" : "Login";
        VisualStateManager.GoToState(this, stateName, true);
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        // Show loading state
        SetAuthenticationState("Loading");
        
        try
        {
            // Simulate authentication
            await Task.Delay(2000);
            
            // Show ready state on success
            SetAuthenticationState("Ready");
        }
        catch
        {
            // Show error state on failure
            SetAuthenticationState("Error");
        }
    }
}