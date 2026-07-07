using Android.App;
using Android.Content.PM;
using Android.OS;

namespace VaultGuard.App;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);

		// Ensure the runtime CAMERA permission is granted so the WebView's QR scanner can access
		// the camera. Requested once; Android remembers the user's choice thereafter.
		if (CheckSelfPermission(Android.Manifest.Permission.Camera) != Permission.Granted)
		{
			RequestPermissions(new[] { Android.Manifest.Permission.Camera }, 1001);
		}
	}
}
