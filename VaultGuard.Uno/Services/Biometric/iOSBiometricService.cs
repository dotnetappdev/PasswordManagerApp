#if __IOS__
using Foundation;
using LocalAuthentication;
using UIKit;

namespace VaultGuard.Uno.Services.Biometric;

/// <summary>
/// iOS-specific biometric authentication using LocalAuthentication framework
/// Supports both Touch ID and Face ID
/// </summary>
public class iOSBiometricService
{
    private readonly ILogger _logger;

    public iOSBiometricService(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<bool> IsBiometricAvailableAsync()
    {
        try
        {
            var context = new LAContext();
            NSError? error;
            var canEvaluate = context.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out error);

            if (error != null)
            {
                _logger.LogWarning($"Biometric not available: {error.LocalizedDescription}");
            }

            return canEvaluate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking biometric availability on iOS");
            return false;
        }
    }

    public async Task<string> GetBiometricTypeAsync()
    {
        try
        {
            var context = new LAContext();
            NSError? error;
            var canEvaluate = context.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out error);

            if (!canEvaluate)
            {
                return "Not Available";
            }

            // Check biometry type
            if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
            {
                return context.BiometryType switch
                {
                    LABiometryType.FaceId => "Face ID",
                    LABiometryType.TouchId => "Touch ID",
                    LABiometryType.Optic => "Optic ID",
                    _ => "Biometric"
                };
            }

            return "Touch ID";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting biometric type on iOS");
            return "Unknown";
        }
    }

    public async Task<bool> AuthenticateAsync(string reason)
    {
        try
        {
            var context = new LAContext();
            NSError? error;

            var canEvaluate = context.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out error);

            if (!canEvaluate)
            {
                _logger.LogWarning($"Cannot evaluate biometric policy: {error?.LocalizedDescription}");
                return false;
            }

            var tcs = new TaskCompletionSource<bool>();

            context.EvaluatePolicy(
                LAPolicy.DeviceOwnerAuthenticationWithBiometrics,
                reason,
                (success, authError) =>
                {
                    if (success)
                    {
                        _logger.LogInformation("Biometric authentication succeeded");
                        tcs.TrySetResult(true);
                    }
                    else
                    {
                        _logger.LogWarning($"Biometric authentication failed: {authError?.LocalizedDescription}");
                        tcs.TrySetResult(false);
                    }
                });

            return await tcs.Task;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during iOS biometric authentication");
            return false;
        }
    }
}
#endif
