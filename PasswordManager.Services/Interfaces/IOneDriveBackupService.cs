namespace PasswordManager.Services.Interfaces;

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