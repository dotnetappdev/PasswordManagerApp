using System;
using System.Threading.Tasks;
#if WINDOWS10_0_17763_0_OR_GREATER
using Windows.Security.Credentials;
#endif

namespace VaultGuard.WPF.Services;

public enum HelloResult { Success, Cancelled, NotConfigured, NotAvailable, Failed }

/// <summary>
/// Integrates with the native Windows 10/11 credential (Windows Hello) subsystem via
/// KeyCredentialManager — the same secure enclave Windows uses for platform passkeys.
/// Used to gate vault unlock and to create a device-bound key for sensitive operations.
///
/// The real WinRT implementation compiles only when the project targets a Windows-version
/// TFM (net10.0-windows10.0.19041.0) built with the Windows SDK workload. On the default
/// CLI TFM it degrades gracefully to "not available" so the whole solution always builds.
/// </summary>
public interface IWindowsHelloService
{
    Task<bool> IsAvailableAsync();
    Task<HelloResult> VerifyAsync(string reason = "Verify it's you");
    Task<HelloResult> RegisterKeyAsync(string keyName);
    Task<bool> KeyExistsAsync(string keyName);
    Task<bool> DeleteKeyAsync(string keyName);
}

public sealed class WindowsHelloService : IWindowsHelloService
{
    public const string DefaultKeyName = "VaultGuard.Hello.Key";

#if WINDOWS10_0_17763_0_OR_GREATER
    public async Task<bool> IsAvailableAsync()
    {
        try { return await KeyCredentialManager.IsSupportedAsync(); }
        catch { return false; }
    }

    public async Task<HelloResult> VerifyAsync(string reason = "Verify it's you")
    {
        try
        {
            if (!await KeyCredentialManager.IsSupportedAsync())
                return HelloResult.NotAvailable;

            var open = await KeyCredentialManager.OpenAsync(DefaultKeyName);
            if (open.Status == KeyCredentialStatus.NotFound)
            {
                var created = await KeyCredentialManager.RequestCreateAsync(
                    DefaultKeyName, KeyCredentialCreationOption.ReplaceExisting);
                return MapStatus(created.Status);
            }
            if (open.Status != KeyCredentialStatus.Success)
                return MapStatus(open.Status);

            var challenge = System.Text.Encoding.UTF8.GetBytes($"VaultGuard:{DateTime.UtcNow:O}:{reason}");
            var buffer = Windows.Security.Cryptography.CryptographicBuffer.CreateFromByteArray(challenge);
            var signResult = await open.Credential.RequestSignAsync(buffer);
            return MapStatus(signResult.Status);
        }
        catch { return HelloResult.Failed; }
    }

    public async Task<HelloResult> RegisterKeyAsync(string keyName)
    {
        try
        {
            if (!await KeyCredentialManager.IsSupportedAsync())
                return HelloResult.NotAvailable;
            var result = await KeyCredentialManager.RequestCreateAsync(
                keyName, KeyCredentialCreationOption.ReplaceExisting);
            return MapStatus(result.Status);
        }
        catch { return HelloResult.Failed; }
    }

    public async Task<bool> KeyExistsAsync(string keyName)
    {
        try
        {
            var open = await KeyCredentialManager.OpenAsync(keyName);
            return open.Status == KeyCredentialStatus.Success;
        }
        catch { return false; }
    }

    public async Task<bool> DeleteKeyAsync(string keyName)
    {
        try { await KeyCredentialManager.DeleteAsync(keyName); return true; }
        catch { return false; }
    }

    private static HelloResult MapStatus(KeyCredentialStatus status) => status switch
    {
        KeyCredentialStatus.Success             => HelloResult.Success,
        KeyCredentialStatus.UserCanceled        => HelloResult.Cancelled,
        KeyCredentialStatus.NotFound            => HelloResult.NotConfigured,
        KeyCredentialStatus.UserPrefersPassword => HelloResult.Cancelled,
        _                                       => HelloResult.Failed
    };
#else
    // Graceful fallback for the default TFM (CLI builds). The UI shows "set up in Windows"
    // and native binding activates once built with the version-specific TFM.
    public Task<bool> IsAvailableAsync() => Task.FromResult(false);
    public Task<HelloResult> VerifyAsync(string reason = "Verify it's you") => Task.FromResult(HelloResult.NotAvailable);
    public Task<HelloResult> RegisterKeyAsync(string keyName) => Task.FromResult(HelloResult.NotAvailable);
    public Task<bool> KeyExistsAsync(string keyName) => Task.FromResult(false);
    public Task<bool> DeleteKeyAsync(string keyName) => Task.FromResult(false);
#endif
}
