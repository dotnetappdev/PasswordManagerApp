using Microsoft.AspNetCore.Components.WebView.Maui;

namespace PasswordManager.App;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
		
		// TODO: Fix Razor component compilation issue
		// The Routes component cannot be resolved due to RazorSourceGenerator errors
		// This needs to be addressed separately
		
		// Configure the root component programmatically using the full type name
		// blazorWebView.RootComponents.Add(new RootComponent
		// {
		//     Selector = "#app",
		//     ComponentType = typeof(PasswordManager.App.Components.Routes)
		// });
	}
}
