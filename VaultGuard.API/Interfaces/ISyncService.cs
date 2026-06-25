using Microsoft.EntityFrameworkCore;
using VaultGuard.Models;
using VaultGuard.Models.DTOs.Sync;

namespace VaultGuard.API.Interfaces;

public interface ISyncService
{
    Task<SyncResponseDto> SyncDatabasesAsync(SyncRequestDto request);
    Task<SyncResponseDto> SyncFromSqliteToSqlServerAsync(DateTime? lastSyncTime = null);
    Task<SyncResponseDto> SyncFromSqlServerToSqliteAsync(DateTime? lastSyncTime = null);
    Task<SyncResponseDto> FullSyncAsync(SyncDirection direction);
    Task<bool> CanConnectToDatabase(string connectionString, string provider);
    Task<DateTime?> GetLastSyncTimeAsync(string sourceDb, string targetDb);
    Task UpdateLastSyncTimeAsync(string sourceDb, string targetDb, DateTime syncTime);
}
