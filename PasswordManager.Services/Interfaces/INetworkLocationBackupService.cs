using PasswordManager.Models.DTOs;

namespace PasswordManager.Services.Interfaces;

/// <summary>
/// Network location backup service interface
/// Allows backing up to custom network locations, UNC paths, mapped drives, etc.
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
    /// Validate if the network location is accessible
    /// </summary>
    /// <returns>True if accessible</returns>
    Task<bool> ValidateNetworkLocationAsync();
}