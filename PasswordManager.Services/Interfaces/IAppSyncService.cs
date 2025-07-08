using PasswordManager.Models.DTOs.Sync;

namespace PasswordManager.Services.Interfaces;

/// <summary>
/// Service for synchronizing data with the web API
/// </summary>
public interface IAppSyncService
{
    /// <summary>
    /// Sync data to the web API on app startup
    /// </summary>
    Task<SyncResponseDto> SyncOnStartupAsync();

    /// <summary>
    /// Sync data to the web API
    /// </summary>
    Task<SyncResponseDto> SyncToApiAsync();

    /// <summary>
    /// Sync data from the web API
    /// </summary>
    Task<SyncResponseDto> SyncFromApiAsync();

    /// <summary>
    /// Check if sync is available (API is reachable)
    /// </summary>
    Task<bool> IsSyncAvailableAsync();

    /// <summary>
    /// Get the last sync time
    /// </summary>
    Task<DateTime?> GetLastSyncTimeAsync();

    /// <summary>
    /// Update the last sync time
    /// </summary>
    Task UpdateLastSyncTimeAsync(DateTime syncTime);
}
