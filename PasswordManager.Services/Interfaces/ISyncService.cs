using Microsoft.EntityFrameworkCore;
using PasswordManager.Models;
using Microsoft.EntityFrameworkCore;
using PasswordManager.Models;
using PasswordManager.Models.DTOs.Sync;

namespace PasswordManager.Services.Interfaces;

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

public interface IDatabaseContextFactory
{
    Task<IPasswordManagerDbContext> CreateContextAsync(string provider, string connectionString);
}

public interface IPasswordManagerDbContext : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    DbSet<PasswordItem> PasswordItems { get; set; }
    DbSet<Category> Categories { get; set; }
    DbSet<Collection> Collections { get; set; }
    DbSet<Tag> Tags { get; set; }
    DbSet<ApplicationUser> Users { get; set; }
}
