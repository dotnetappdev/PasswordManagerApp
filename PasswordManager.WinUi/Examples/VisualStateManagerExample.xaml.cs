using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace PasswordManager.WinUi.Examples;

/// <summary>
/// Example demonstrating VisualStateManager usage in WinUI 3
/// as an alternative to WPF-style triggers for complex UI state management.
/// </summary>
public sealed partial class VisualStateExample : Page
{
    public VisualStateExample()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Sets the authentication visual state
    /// </summary>
    /// <param name="stateName">The state name (Loading, Ready, Error)</param>
    private void SetAuthenticationState(string stateName)
    {
        VisualStateManager.GoToState(this, stateName, true);
    }

    /// <summary>
    /// Sets the UI mode for first-time setup vs login
    /// </summary>
    /// <param name="isFirstTimeSetup">True for first-time setup mode, false for login mode</param>
    private void SetUiMode(bool isFirstTimeSetup)
    {
        string stateName = isFirstTimeSetup ? "FirstTimeSetup" : "Login";
        VisualStateManager.GoToState(this, stateName, true);
    }

    /// <summary>
    /// Handles the login button click event
    /// </summary>
    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        // Show loading state
        SetAuthenticationState("Loading");
        
        try
        {
            // Simulate authentication process
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