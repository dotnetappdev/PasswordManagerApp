using LocalAuthentication;
using VaultGuard.Components.Shared.Services;

namespace VaultGuard.App.Services;

/// <summary>
/// Face ID / Touch ID unlock on iPhone, iPad and Mac (LocalAuthentication). Like Windows Hello it only proves the
/// person at the device: the vault key itself stays in the app's SecureStorage (the Keychain) and is only read after
/// this succeeds. Biometrics only: the device passcode isn't accepted here (the master password is the fallback).
/// Needs NSFaceIDUsageDescription in Info.plist.
/// </summary>
public sealed class AppleBiometricUnlock : IDeviceUnlock
{
    public string Name
    {
        get
        {
            try
            {
                using var ctx = new LAContext();
                ctx.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out _);
                return ctx.BiometryType switch
                {
                    LABiometryType.FaceId => "Face ID",
                    LABiometryType.TouchId => "Touch ID",
                    _ => "Face ID / Touch ID"
                };
            }
            catch { return "Face ID / Touch ID"; }
        }
    }

    public Task<bool> IsAvailableAsync()
    {
        try
        {
            using var ctx = new LAContext();
            return Task.FromResult(ctx.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out _));
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Warning("Biometric availability check failed", ex);
            return Task.FromResult(false);
        }
    }

    public async Task<bool> VerifyAsync(string reason)
    {
        try
        {
            using var ctx = new LAContext { LocalizedFallbackTitle = "" }; // hide "Enter Password": the master password is the fallback
            if (!ctx.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out _)) return false;
            var result = await ctx.EvaluatePolicyAsync(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, reason);
            return result.Item1;
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Warning("Biometric verification failed", ex);
            return false;
        }
    }
}
