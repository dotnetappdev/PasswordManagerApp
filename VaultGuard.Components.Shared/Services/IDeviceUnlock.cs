namespace VaultGuard.Components.Shared.Services;

/// <summary>
/// Quick unlock with the device's own sign-in (Windows Hello; Face ID / Touch ID on iOS; fingerprint / face on Android).
/// It only proves the person at the device: the vault key still comes from the master key kept in the
/// platform's secure storage (IMasterKeyCacheService), released only after this check succeeds. Hosts without it
/// simply don't register an implementation.
/// </summary>
public interface IDeviceUnlock
{
    /// <summary>What to call it on buttons, e.g. "Windows Hello".</summary>
    string Name { get; }

    Task<bool> IsAvailableAsync();

    /// <summary>Shows the platform's prompt; true only when the user verified.</summary>
    Task<bool> VerifyAsync(string reason);
}
