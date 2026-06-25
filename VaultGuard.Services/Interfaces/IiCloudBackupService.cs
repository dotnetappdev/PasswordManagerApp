namespace VaultGuard.Services.Interfaces;

/// <summary>
/// iCloud-specific cloud backup service interface
/// Note: iCloud integration is limited on non-Apple platforms
/// This provides basic functionality where possible
/// </summary>
public interface IiCloudBackupService : ICloudBackupService
{
    /// <summary>
    /// Get current iCloud account information
    /// </summary>
    /// <returns>Account information or null if not authenticated</returns>
    Task<iCloudAccountInfo?> GetAccountInfoAsync();

    /// <summary>
    /// Check if iCloud is available on this platform
    /// </summary>
    /// <returns>True if iCloud integration is available</returns>
    bool IsAvailable { get; }
}

/// <summary>
/// iCloud account information
/// </summary>
public class iCloudAccountInfo
{
    public string? AppleId { get; set; }
    public string? DisplayName { get; set; }
    public long AvailableSpace { get; set; }
}