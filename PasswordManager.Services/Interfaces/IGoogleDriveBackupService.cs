using PasswordManager.Models.DTOs;

namespace PasswordManager.Services.Interfaces;

public interface IGoogleDriveBackupService : ICloudBackupService
{
    /// <summary>Start OAuth2 loopback flow; opens browser and awaits redirect.</summary>
    Task<bool> ConnectWithOAuthAsync(string clientId, string clientSecret, CancellationToken ct = default);

    /// <summary>True if valid OAuth tokens are stored locally.</summary>
    Task<bool> HasStoredTokenAsync();

    /// <summary>Remove locally stored OAuth tokens (disconnect).</summary>
    Task DisconnectAsync();

    GoogleDriveAccountInfo? AccountInfo { get; }
}

public class GoogleDriveAccountInfo
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public long StorageUsed { get; set; }
    public long StorageTotal { get; set; }
}
