using VaultGuard.Models.DTOs;

namespace VaultGuard.Services.Interfaces;

/// <summary>
/// NAS / network-drive backup service interface.
/// Backs up to custom network locations, UNC paths and mapped drives — optionally connecting
/// with explicit credentials when the share requires authentication.
/// </summary>
public interface INetworkLocationBackupService : ICloudBackupService
{
    /// <summary>
    /// Set the network backup location path
    /// </summary>
    /// <param name="networkPath">UNC path, mapped drive, or network location</param>
    void SetNetworkPath(string networkPath);

    /// <summary>
    /// Get the current network backup location
    /// </summary>
    /// <returns>Network backup path</returns>
    string GetNetworkPath();

    /// <summary>
    /// Apply full connection settings (path + optional credentials). When
    /// <see cref="NetworkConnectionSettings.RequireAuthentication"/> is set, the share is connected
    /// with the supplied credentials before any read/write.
    /// </summary>
    void SetConnectionSettings(NetworkConnectionSettings settings);

    /// <summary>
    /// Validate if the network location is accessible
    /// </summary>
    /// <returns>True if accessible</returns>
    Task<bool> ValidateNetworkLocationAsync();

    /// <summary>Connects (if credentials are required) and confirms the path is reachable.</summary>
    Task<bool> TestConnectionAsync();

    /// <summary>Lists the network drives currently mapped on this machine, for the target picker.</summary>
    IReadOnlyList<MappedDriveInfo> GetMappedDrives();
}
