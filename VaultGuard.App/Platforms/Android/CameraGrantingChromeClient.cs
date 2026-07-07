using Android.Webkit;

namespace VaultGuard.App;

/// <summary>
/// WebChromeClient that grants the WebView's camera permission request so the shared QrScanner's
/// getUserMedia call can start the camera. The app-level runtime CAMERA permission is requested in
/// <see cref="MainActivity"/>; this client answers the per-page WebView request that follows.
/// </summary>
public class CameraGrantingChromeClient : WebChromeClient
{
	public override void OnPermissionRequest(PermissionRequest? request)
	{
		if (request is null)
		{
			return;
		}

		try
		{
			request.Grant(request.GetResources());
		}
		catch (System.Exception ex)
		{
			VaultGuard.Services.Logging.AppLogger.Warning("WebView camera permission grant failed", ex);
			base.OnPermissionRequest(request);
		}
	}
}
