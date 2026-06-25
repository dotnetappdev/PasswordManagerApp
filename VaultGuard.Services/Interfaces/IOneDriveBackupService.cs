namespace VaultGuard.Services.Interfaces;

/// <summary>
/// OneDrive-specific cloud backup service interface
/// </summary>
public interface IOneDriveBackupService : ICloudBackupService
{
    /// <summary>
    /// Get current OneDrive account information
    /// </summary>
    /// <returns>Account information or null if not authenticated</returns>
    Task<OneDriveAccountInfo?> GetAccountInfoAsync();

    /// <summary>
    /// Get available storage space on OneDrive
    /// </summary>
    /// <returns>Available space in bytes</returns>
    Task<long> GetAvailableStorageAsync();

    /// <summary>
    /// Sign in a personal Microsoft account using VaultGuard's own built-in app registration —
    /// no setup required from the user.
    /// </summary>
    Task<bool> ConnectPersonalAsync(CancellationToken ct = default);

    /// <summary>True if valid OAuth tokens are stored locally.</summary>
    Task<bool> HasStoredTokenAsync();

    /// <summary>
    /// Lightweight reachability check — refreshes the access token if needed and makes a small
    /// authenticated call to Microsoft Graph. Used to drive an online/offline status indicator;
    /// returns false (rather than throwing) on any network or auth failure.
    /// </summary>
    Task<bool> PingAsync(CancellationToken ct = default);

    /// <summary>Remove locally stored OAuth tokens (disconnect).</summary>
    Task DisconnectAsync();

    OneDriveAccountInfo? AccountInfo { get; }

    /// <summary>
    /// The reason the most recent <see cref="ConnectPersonalAsync"/> call returned false — null if
    /// it succeeded or hasn't been called yet. Lets the UI show the real failure (e.g. the exact
    /// Microsoft Graph error body) instead of a generic message.
    /// </summary>
    string? LastError { get; }
}

/// <summary>
/// OneDrive account information
/// </summary>
public class OneDriveAccountInfo
{
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public long TotalSpace { get; set; }
    public long UsedSpace { get; set; }
    public long AvailableSpace { get; set; }
}