namespace VaultGuard.App;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new MainPage()) { Title = "Vault Guard" };
		// Phones: lock when the app goes to the background or the screen turns off (see MauiDeviceLock).
		window.Stopped += (_, _) => VaultGuard.App.Services.MauiDeviceLock.OnAppBackgrounded();
		return window;
	}
}
