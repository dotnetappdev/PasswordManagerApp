using VaultGuard.Components.Shared.Services;
using Windows.Security.Credentials;

namespace VaultGuard.App.Services;

/// <summary>
/// Windows Hello (face, fingerprint or PIN) for the MAUI app on Windows, the same way the WPF app does it: a
/// Hello-protected key credential signs a fresh challenge, which Windows only allows after the user verifies.
/// </summary>
public sealed class WindowsHelloUnlock : IDeviceUnlock
{
    private const string KeyName = "VaultGuard.Maui.Hello.Key";

    public string Name => "Windows Hello";

    public async Task<bool> IsAvailableAsync()
    {
        try { return await KeyCredentialManager.IsSupportedAsync(); }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Windows Hello availability check failed", ex); return false; }
    }

    public async Task<bool> VerifyAsync(string reason)
    {
        try
        {
            if (!await KeyCredentialManager.IsSupportedAsync()) return false;
            var open = await KeyCredentialManager.OpenAsync(KeyName);
            if (open.Status == KeyCredentialStatus.NotFound)
            {
                // First use: creating the key credential is itself a Hello verification.
                var created = await KeyCredentialManager.RequestCreateAsync(KeyName, KeyCredentialCreationOption.ReplaceExisting);
                return created.Status == KeyCredentialStatus.Success;
            }
            if (open.Status != KeyCredentialStatus.Success) return false;
            var challenge = System.Text.Encoding.UTF8.GetBytes($"VaultGuard:{DateTime.UtcNow:O}:{reason}");
            var signed = await open.Credential.RequestSignAsync(
                global::Windows.Security.Cryptography.CryptographicBuffer.CreateFromByteArray(challenge));
            return signed.Status == KeyCredentialStatus.Success;
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Warning("Windows Hello verification failed", ex);
            return false;
        }
    }
}
