namespace VaultGuard.Services.Security;

/// <summary>
/// Lets platform code ask the UI to lock: e.g. when Windows locks (Win+L / Ctrl+Alt+Del → Lock), the PC sleeps,
/// or a phone app goes to the background. Register as a singleton; the Blazor <c>VaultAutoLock</c> component
/// listens and performs the lock (sign out of the vault, wipe the clipboard, show the sign-in page). Callers wipe
/// the vault key themselves first, so it's gone even while the UI is suspended.
/// </summary>
public sealed class VaultLockSignal
{
    /// <summary>Raised with a reason: <see cref="DeviceLocked"/> or <see cref="Idle"/>.</summary>
    public event Action<string>? LockRequested;

    public const string DeviceLocked = "device";
    public const string Idle = "idle";

    public void RequestLock(string reason) => LockRequested?.Invoke(reason);
}
