#if __ANDROID__
using Android.Content;
using AndroidX.Biometric;
using AndroidX.Fragment.App;

namespace VaultGuard.Uno.Services.Biometric;

/// <summary>
/// Android-specific biometric authentication using AndroidX.Biometric
/// </summary>
public class AndroidBiometricService
{
    private readonly ILogger _logger;

    public AndroidBiometricService(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<bool> IsBiometricAvailableAsync()
    {
        try
        {
            var context = Android.App.Application.Context;
            var biometricManager = BiometricManager.From(context);
            var canAuthenticate = biometricManager.CanAuthenticate(BiometricManager.Authenticators.BiometricStrong);

            return canAuthenticate == BiometricManager.BiometricSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking biometric availability on Android");
            return false;
        }
    }

    public async Task<string> GetBiometricTypeAsync()
    {
        var available = await IsBiometricAvailableAsync();
        return available ? "Fingerprint/Face" : "Not Available";
    }

    public async Task<bool> AuthenticateAsync(string reason)
    {
        var tcs = new TaskCompletionSource<bool>();

        try
        {
            var context = Android.App.Application.Context;
            var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity 
                           ?? throw new InvalidOperationException("Current activity is null");

            var executor = Microsoft.Maui.ApplicationModel.Platform.AppContext.MainExecutor;

            var promptInfo = new BiometricPrompt.PromptInfo.Builder()
                .SetTitle("Biometric Authentication")
                .SetSubtitle(reason)
                .SetNegativeButtonText("Cancel")
                .Build();

            var authCallback = new BiometricAuthCallback(
                onSuccess: () => tcs.TrySetResult(true),
                onError: (errorCode, errString) =>
                {
                    _logger.LogWarning($"Biometric auth error: {errorCode} - {errString}");
                    tcs.TrySetResult(false);
                },
                onFailed: () =>
                {
                    _logger.LogWarning("Biometric authentication failed");
                    tcs.TrySetResult(false);
                });

            var biometricPrompt = new BiometricPrompt(
                (FragmentActivity)activity,
                executor!,
                authCallback);

            biometricPrompt.Authenticate(promptInfo);

            return await tcs.Task;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Android biometric authentication");
            return false;
        }
    }

    private class BiometricAuthCallback : BiometricPrompt.AuthenticationCallback
    {
        private readonly Action _onSuccess;
        private readonly Action<int, string> _onError;
        private readonly Action _onFailed;

        public BiometricAuthCallback(Action onSuccess, Action<int, string> onError, Action onFailed)
        {
            _onSuccess = onSuccess;
            _onError = onError;
            _onFailed = onFailed;
        }

        public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult result)
        {
            base.OnAuthenticationSucceeded(result);
            _onSuccess?.Invoke();
        }

        public override void OnAuthenticationError(int errorCode, Java.Lang.ICharSequence? errString)
        {
            base.OnAuthenticationError(errorCode, errString);
            _onError?.Invoke(errorCode, errString?.ToString() ?? "Unknown error");
        }

        public override void OnAuthenticationFailed()
        {
            base.OnAuthenticationFailed();
            _onFailed?.Invoke();
        }
    }
}
#endif
