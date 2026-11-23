using Android.Content;
using Android.Hardware.Biometrics;
using Android.OS;
using VaultGuard.Components.Shared.Services;

namespace VaultGuard.App.Services;

/// <summary>
/// Fingerprint / face unlock on Android with the system BiometricPrompt (Android 10+). Like Windows Hello it only
/// proves the person holding the phone: the vault key itself stays in the app's SecureStorage (Android Keystore)
/// and is only read after this succeeds. Only "strong" (class 3) biometrics count where Android can tell.
/// </summary>
public sealed class AndroidBiometricUnlock : IDeviceUnlock
{
    /// <summary>The activity on screen; set by MainActivity (the prompt needs it).</summary>
    public static WeakReference<Android.App.Activity>? CurrentActivity { get; set; }

    public string Name => "fingerprint or face";

    public Task<bool> IsAvailableAsync()
    {
        try
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(29)) return Task.FromResult(false);
            if (Android.App.Application.Context.GetSystemService(Context.BiometricService) is not BiometricManager bm) return Task.FromResult(false);
            var code = OperatingSystem.IsAndroidVersionAtLeast(30)
                ? bm.CanAuthenticate((int)BiometricManagerAuthenticators.BiometricStrong)
#pragma warning disable CA1422 // the pre-Android 11 overload
                : bm.CanAuthenticate();
#pragma warning restore CA1422
            return Task.FromResult(code == BiometricCode.Success);
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Warning("Biometric availability check failed", ex);
            return Task.FromResult(false);
        }
    }

    public Task<bool> VerifyAsync(string reason)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(29)) return Task.FromResult(false);
        Android.App.Activity? activity = null;
        if (CurrentActivity?.TryGetTarget(out activity) != true || activity == null) return Task.FromResult(false);

        var done = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        activity.RunOnUiThread(() =>
        {
            try
            {
                var executor = activity.MainExecutor!;
                var builder = new BiometricPrompt.Builder(activity)
                    .SetTitle("Unlock Vault Guard")!
                    .SetSubtitle(reason)!;
                if (OperatingSystem.IsAndroidVersionAtLeast(30))
                    builder.SetAllowedAuthenticators((int)BiometricManagerAuthenticators.BiometricStrong);
                builder.SetNegativeButton("Use master password", executor, new CancelListener(done));
                var prompt = builder.Build();
                prompt.Authenticate(new CancellationSignal(), executor, new Callback(done));
            }
            catch (Exception ex)
            {
                VaultGuard.Services.Logging.AppLogger.Warning("Biometric prompt failed", ex);
                done.TrySetResult(false);
            }
        });
        return done.Task;
    }

    private sealed class Callback : BiometricPrompt.AuthenticationCallback
    {
        private readonly TaskCompletionSource<bool> _done;
        public Callback(TaskCompletionSource<bool> done) => _done = done;

        public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult? result) => _done.TrySetResult(true);

        // Cancelled, too many attempts, no hardware...: the user falls back to the master password.
        public override void OnAuthenticationError(BiometricErrorCode errorCode, Java.Lang.ICharSequence? errString) => _done.TrySetResult(false);

        // A finger / face that didn't match: Android keeps the prompt open for another try, so don't finish here.
        public override void OnAuthenticationFailed() { }
    }

    private sealed class CancelListener : Java.Lang.Object, IDialogInterfaceOnClickListener
    {
        private readonly TaskCompletionSource<bool> _done;
        public CancelListener(TaskCompletionSource<bool> done) => _done = done;
        public void OnClick(IDialogInterface? dialog, int which) => _done.TrySetResult(false);
    }
}
