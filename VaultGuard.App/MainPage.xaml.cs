using Microsoft.AspNetCore.Components.WebView;

namespace VaultGuard.App;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
	}

	// Grants the WebView camera access so the shared QrScanner component's getUserMedia call can
	// start the camera for "Scan to sign in". Each platform's WebView gates camera differently.
	private void OnBlazorWebViewInitialized(object? sender, BlazorWebViewInitializedEventArgs e)
	{
#if WINDOWS
		var webView2 = e.WebView;
		if (webView2 is null) return;

		void Hook()
		{
			if (webView2.CoreWebView2 is null) return;
			webView2.CoreWebView2.PermissionRequested += (_, args) =>
			{
				if (args.PermissionKind == Microsoft.Web.WebView2.Core.CoreWebView2PermissionKind.Camera)
				{
					args.State = Microsoft.Web.WebView2.Core.CoreWebView2PermissionState.Allow;
				}
			};
		}

		if (webView2.CoreWebView2 is not null)
		{
			Hook();
		}
		else
		{
			webView2.CoreWebView2Initialized += (_, _) => Hook();
		}
#elif ANDROID
		// Let getUserMedia autoplay the camera feed without a user gesture, and grant the
		// WebView's runtime permission request (the app-level CAMERA permission is requested
		// in MainActivity).
		e.WebView.Settings.MediaPlaybackRequiresUserGesture = false;
		e.WebView.SetWebChromeClient(new CameraGrantingChromeClient());
#endif
	}
}
