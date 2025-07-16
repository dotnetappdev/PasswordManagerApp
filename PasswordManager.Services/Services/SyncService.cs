using PasswordManager.DAL.Interfaces;
using PasswordManager.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PasswordManager.DAL;
using PasswordManager.Models;
using PasswordManager.Models.DTOs.Sync;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.Services.Services;

public class SyncService : PasswordManager.Services.Interfaces.ISyncService
{
    private readonly IDatabaseContextFactory _contextFactory;
    private readonly ILogger<SyncService> _logger;
    private readonly IConfiguration _configuration;

    public SyncService(
        IDatabaseContextFactory contextFactory,
        ILogger<SyncService> logger,
        IConfiguration configuration)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<SyncResponseDto> SyncDatabasesAsync(SyncRequestDto request)
    {
        var startTime = DateTime.UtcNow;
        var statistics = new SyncStatisticsDto();
        var conflicts = new List<SyncConflictDto>();

        try
        {
            _logger.LogInformation("Starting sync from {Source} to {Target}", request.SourceDatabase, request.TargetDatabase);

            using var sourceContext = await _contextFactory.CreateContextAsync(request.SourceDatabase, GetConnectionString(request.SourceDatabase));
            using var targetContext = await _contextFactory.CreateContextAsync(request.TargetDatabase, GetConnectionString(request.TargetDatabase));

            // Test connections
            if (!await CanConnectToDatabase(GetConnectionString(request.SourceDatabase), request.SourceDatabase))
                throw new InvalidOperationException($"Cannot connect to source database: {request.SourceDatabase}");

            if (!await CanConnectToDatabase(GetConnectionString(request.TargetDatabase), request.TargetDatabase))
                throw new InvalidOperationException($"Cannot connect to target database: {request.TargetDatabase}");

            // Sync each entity type
            foreach (var entityType in request.EntitiesToSync)
            {
                switch (entityType.ToLower())
                {
                    case "passworditems":
                        await SyncPasswordItems(sourceContext, targetContext, request.LastSyncTime, statistics, conflicts);
                        break;
                    case "categories":
                        await SyncCategories(sourceContext, targetContext, request.LastSyncTime, statistics, conflicts);
                        break;
                    case "collections":
                        await SyncCollections(sourceContext, targetContext, request.LastSyncTime, statistics, conflicts);
                        break;
                    case "tags":
                        await SyncTags(sourceContext, targetContext, request.LastSyncTime, statistics, conflicts);
                        break;
                    default:
                        _logger.LogWarning("Unknown entity type: {EntityType}", entityType);
                        break;
                }
            }

            // Update the last sync time
            await UpdateLastSyncTimeAsync(request.SourceDatabase, request.TargetDatabase, startTime);

            return new SyncResponseDto
            {
                Success = true,
                Message = "Sync completed successfully",
                SyncTime = startTime,
                ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                Statistics = statistics,
                Conflicts = conflicts
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database sync");
            return new SyncResponseDto
            {
                Success = false,
                Message = $"Sync failed: {ex.Message}",
                SyncTime = startTime,
                ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                Statistics = statistics,
                Conflicts = conflicts
            };
        }
    }

    public async Task<SyncResponseDto> SyncFromSqliteToSqlServerAsync(DateTime? lastSyncTime = null)
    {
        var request = new SyncRequestDto
        {
            SourceDatabase = "sqlite",
            TargetDatabase = "sqlserver",
            EntitiesToSync = new List<string> { "passworditems", "categories", "collections", "tags" },
            LastSyncTime = lastSyncTime ?? await GetLastSyncTimeAsync("sqlite", "sqlserver")
        };

        return await SyncDatabasesAsync(request);
    }

    public async Task<SyncResponseDto> SyncFromSqlServerToSqliteAsync(DateTime? lastSyncTime = null)
    {
        var request = new SyncRequestDto
        {
            SourceDatabase = "sqlserver",
            TargetDatabase = "sqlite",
            EntitiesToSync = new List<string> { "passworditems", "categories", "collections", "tags" },
            LastSyncTime = lastSyncTime ?? await GetLastSyncTimeAsync("sqlserver", "sqlite")
        };

        return await SyncDatabasesAsync(request);
    }

    public async Task<SyncResponseDto> FullSyncAsync(SyncDirection direction)
    {
        switch (direction)
        {
            case SyncDirection.SqliteToSqlServer:
                return await SyncFromSqliteToSqlServerAsync(null);
            case SyncDirection.SqlServerToSqlite:
                return await SyncFromSqlServerToSqliteAsync(null);
            case SyncDirection.Bidirectional:
                var sqliteToSqlServer = await SyncFromSqliteToSqlServerAsync(null);
                var sqlServerToSqlite = await SyncFromSqlServerToSqliteAsync(null);

                return new SyncResponseDto
                {
                    Success = sqliteToSqlServer.Success && sqlServerToSqlite.Success,
                    Message = $"Bidirectional sync completed. {sqliteToSqlServer.Message}. {sqlServerToSqlite.Message}",
                    SyncTime = sqliteToSqlServer.SyncTime,
                    ExecutionTimeMs = sqliteToSqlServer.ExecutionTimeMs + sqlServerToSqlite.ExecutionTimeMs,
                    Statistics = new SyncStatisticsDto
                    {
                        Created = sqliteToSqlServer.Statistics.Created + sqlServerToSqlite.Statistics.Created,
                        Updated = sqliteToSqlServer.Statistics.Updated + sqlServerToSqlite.Statistics.Updated,
                        Deleted = sqliteToSqlServer.Statistics.Deleted + sqlServerToSqlite.Statistics.Deleted,
                        Skipped = sqliteToSqlServer.Statistics.Skipped + sqlServerToSqlite.Statistics.Skipped
                    },
                    Conflicts = sqliteToSqlServer.Conflicts.Concat(sqlServerToSqlite.Conflicts).ToList()
                };
            default:
                throw new ArgumentException($"Unknown sync direction: {direction}");
        }
    }

    public async Task<bool> CanConnectToDatabase(string connectionString, string provider)
    {
        try
        {
            using var context = await _contextFactory.CreateContextAsync(provider, connectionString);
            return await context.Database.CanConnectAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to database with provider {Provider}", provider);
            return false;
        }
    }

    public async Task<DateTime?> GetLastSyncTimeAsync(string sourceDb, string targetDb)
    {
        try
        {
            using var context = await _contextFactory.CreateContextAsync(
                "sqlserver", 
                GetConnectionString("sqlserver"));

            var syncLog = await context.Database
                .SqlQuery<LastSyncDto>($"SELECT TOP 1 * FROM LastSync WHERE SourceDatabase = '{sourceDb}' AND TargetDatabase = '{targetDb}' ORDER BY SyncTime DESC")
                .FirstOrDefaultAsync();

            return syncLog?.SyncTime;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting last sync time for {SourceDb} to {TargetDb}", sourceDb, targetDb);
            return null;
        }
    }

    public async Task UpdateLastSyncTimeAsync(string sourceDb, string targetDb, DateTime syncTime)
    {
        try
        {
            using var context = await _contextFactory.CreateContextAsync(
                "sqlserver", 
                GetConnectionString("sqlserver"));

            await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO LastSync (SourceDatabase, TargetDatabase, SyncTime) VALUES ({0}, {1}, {2})",
                sourceDb, targetDb, syncTime);

            _logger.LogInformation("Updated last sync time for {SourceDb} to {TargetDb} at {SyncTime}", 
                sourceDb, targetDb, syncTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last sync time for {SourceDb} to {TargetDb}", sourceDb, targetDb);
        }
    }

    private string GetConnectionString(string provider)
    {
        return provider.ToLower() switch
        {
            "sqlite" => _configuration.GetConnectionString("SqliteConnection") 
                ?? throw new InvalidOperationException("SqliteConnection not found in configuration"),
            "sqlserver" => _configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("DefaultConnection not found in configuration"),
            "postgres" or "postgresql" => _configuration.GetConnectionString("PostgresConnection")
                ?? throw new InvalidOperationException("PostgresConnection not found in configuration"),
            _ => throw new ArgumentException($"Unsupported database provider: {provider}")
        };
    }

    private async Task SyncPasswordItems(IPasswordManagerDbContext source, IPasswordManagerDbContext target, DateTime? lastSyncTime, SyncStatisticsDto statistics, List<SyncConflictDto> conflicts)
    {
        _logger.LogInformation("Syncing password items");
        var sourceItems = await source.PasswordItems
            .Include(p => p.Category)
            .Include(p => p.Collection)
            .Include(p => p.LoginItem)
            .Include(p => p.CreditCardItem)
            .Include(p => p.SecureNoteItem)
            .Include(p => p.WiFiItem)
            .Include(p => p.Tags)
            .Where(p => !lastSyncTime.HasValue || p.LastModified > lastSyncTime)
            .ToListAsync();

        foreach (var sourceItem in sourceItems)
        {
            var targetItem = await target.PasswordItems
                .Include(p => p.Category)
                .Include(p => p.Collection)
                .Include(p => p.LoginItem)
                .Include(p => p.CreditCardItem)
                .Include(p => p.SecureNoteItem)
                .Include(p => p.WiFiItem)
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == sourceItem.Id);

            if (targetItem == null)
            {
                // Create new item in target
                var newItem = new PasswordItem
                {
                    Id = sourceItem.Id,
                    Title = sourceItem.Title,
                    Description = sourceItem.Description,
                    Type = sourceItem.Type,
                    CreatedAt = sourceItem.CreatedAt,
                    LastModified = sourceItem.LastModified,
                    IsFavorite = sourceItem.IsFavorite,
                    IsArchived = sourceItem.IsArchived,
                    IsDeleted = sourceItem.IsDeleted,
                    CategoryId = sourceItem.CategoryId,
                    CollectionId = sourceItem.CollectionId
                };

                target.PasswordItems.Add(newItem);
                await target.SaveChangesAsync();

                // Handle related entities
                if (sourceItem.LoginItem != null)
                {
                    var loginItem = new LoginItem
                    {
                        Id = sourceItem.LoginItem.Id,
                        Username = sourceItem.LoginItem.Username,
                        Password = sourceItem.LoginItem.Password,
                        Website = sourceItem.LoginItem.Website,
                        Notes = sourceItem.LoginItem.Notes,
                        LastAutoFill = sourceItem.LoginItem.LastAutoFill,
                        RequiresMasterPassword = sourceItem.LoginItem.RequiresMasterPassword,
                        PasswordId = newItem.Id
                    };
                    target.LoginItems.Add(loginItem);
                }
                else if (sourceItem.CreditCardItem != null)
                {
                    var creditCardItem = new CreditCardItem
                    {
                        Id = sourceItem.CreditCardItem.Id,
                        CardholderName = sourceItem.CreditCardItem.CardholderName,
                        CardNumber = sourceItem.CreditCardItem.CardNumber,
                        ExpirationMonth = sourceItem.CreditCardItem.ExpirationMonth,
                        ExpirationYear = sourceItem.CreditCardItem.ExpirationYear,
                        SecurityCode = sourceItem.CreditCardItem.SecurityCode,
                        CardType = sourceItem.CreditCardItem.CardType,
                        Notes = sourceItem.CreditCardItem.Notes,
                        RequiresMasterPassword = sourceItem.CreditCardItem.RequiresMasterPassword,
                        PasswordId = newItem.Id
                    };
                    target.CreditCardItems.Add(creditCardItem);
                }
                else if (sourceItem.SecureNoteItem != null)
                {
                    var secureNoteItem = new SecureNoteItem
                    {
                        Id = sourceItem.SecureNoteItem.Id,
                        Content = sourceItem.SecureNoteItem.Content,
                        RequiresMasterPassword = sourceItem.SecureNoteItem.RequiresMasterPassword,
                        PasswordId = newItem.Id
                    };
                    target.SecureNoteItems.Add(secureNoteItem);
                }
                else if (sourceItem.WiFiItem != null)
                {
                    var wifiItem = new WiFiItem
                    {
                        Id = sourceItem.WiFiItem.Id,
                        NetworkName = sourceItem.WiFiItem.NetworkName,
                        Password = sourceItem.WiFiItem.Password,
                        SecurityType = sourceItem.WiFiItem.SecurityType,
                        Notes = sourceItem.WiFiItem.Notes,
                        RequiresMasterPassword = sourceItem.WiFiItem.RequiresMasterPassword,
                        PasswordId = newItem.Id
                    };
                    target.WiFiItems.Add(wifiItem);
                }

                // Handle tags
                if (sourceItem.Tags.Any())
                {
                    var existingTags = await target.Tags
                        .Where(t => sourceItem.Tags.Select(st => st.Id).Contains(t.Id))
                        .ToListAsync();

                    var newItemInDb = await target.PasswordItems.FindAsync(newItem.Id);
                    newItemInDb.Tags = existingTags;
                }

                await target.SaveChangesAsync();
                statistics.Created++;
                _logger.LogDebug("Created password item {Id} in target database", sourceItem.Id);
            }
            else
            {
                // Check for conflicts
                if (targetItem.LastModified > sourceItem.LastModified)
                {
                    conflicts.Add(new SyncConflictDto
                    {
                        EntityType = "PasswordItem",
                        EntityId = sourceItem.Id,
                        SourceLastModified = sourceItem.LastModified,
                        TargetLastModified = targetItem.LastModified,
                        Resolution = SyncConflictResolution.TargetWins,
                        Message = $"Target item '{targetItem.Title}' is newer than source item '{sourceItem.Title}'"
                    });
                    statistics.Skipped++;
                    _logger.LogWarning("Conflict detected for password item {Id}, target is newer", sourceItem.Id);
                    continue;
                }

                // Update target item
                targetItem.Title = sourceItem.Title;
                targetItem.Description = sourceItem.Description;
                targetItem.Type = sourceItem.Type;
                targetItem.IsFavorite = sourceItem.IsFavorite;
                targetItem.IsArchived = sourceItem.IsArchived;
                targetItem.IsDeleted = sourceItem.IsDeleted;
                targetItem.CategoryId = sourceItem.CategoryId;
                targetItem.CollectionId = sourceItem.CollectionId;
                targetItem.LastModified = sourceItem.LastModified;

                // Handle related entities
                if (sourceItem.LoginItem != null && targetItem.LoginItem != null)
                {
                    targetItem.LoginItem.Username = sourceItem.LoginItem.Username;
                    targetItem.LoginItem.Password = sourceItem.LoginItem.Password;
                    targetItem.LoginItem.Website = sourceItem.LoginItem.Website;
                    targetItem.LoginItem.Notes = sourceItem.LoginItem.Notes;
                    targetItem.LoginItem.RequiresMasterPassword = sourceItem.LoginItem.RequiresMasterPassword;
                }
                else if (sourceItem.CreditCardItem != null && targetItem.CreditCardItem != null)
                {
                    targetItem.CreditCardItem.CardholderName = sourceItem.CreditCardItem.CardholderName;
                    targetItem.CreditCardItem.CardNumber = sourceItem.CreditCardItem.CardNumber;
                    targetItem.CreditCardItem.ExpirationMonth = sourceItem.CreditCardItem.ExpirationMonth;
                    targetItem.CreditCardItem.ExpirationYear = sourceItem.CreditCardItem.ExpirationYear;
                    targetItem.CreditCardItem.SecurityCode = sourceItem.CreditCardItem.SecurityCode;
                    targetItem.CreditCardItem.CardType = sourceItem.CreditCardItem.CardType;
                    targetItem.CreditCardItem.Notes = sourceItem.CreditCardItem.Notes;
                    targetItem.CreditCardItem.RequiresMasterPassword = sourceItem.CreditCardItem.RequiresMasterPassword;
                }
                else if (sourceItem.SecureNoteItem != null && targetItem.SecureNoteItem != null)
                {
                    targetItem.SecureNoteItem.Content = sourceItem.SecureNoteItem.Content;
                    targetItem.SecureNoteItem.RequiresMasterPassword = sourceItem.SecureNoteItem.RequiresMasterPassword;
                }
                else if (sourceItem.WiFiItem != null && targetItem.WiFiItem != null)
                {
                    targetItem.WiFiItem.NetworkName = sourceItem.WiFiItem.NetworkName;
                    targetItem.WiFiItem.Password = sourceItem.WiFiItem.Password;
                    targetItem.WiFiItem.SecurityType = sourceItem.WiFiItem.SecurityType;
                    targetItem.WiFiItem.Notes = sourceItem.WiFiItem.Notes;
                    targetItem.WiFiItem.RequiresMasterPassword = sourceItem.WiFiItem.RequiresMasterPassword;
                }

                // Handle tags
                targetItem.Tags.Clear();
                if (sourceItem.Tags.Any())
                {
                    var existingTags = await target.Tags
                        .Where(t => sourceItem.Tags.Select(st => st.Id).Contains(t.Id))
                        .ToListAsync();
                    targetItem.Tags = existingTags;
                }

                await target.SaveChangesAsync();
                statistics.Updated++;
                _logger.LogDebug("Updated password item {Id} in target database", sourceItem.Id);
            }
        }

        _logger.LogInformation("Synced {Count} password items", sourceItems.Count);
    }

    private async Task SyncCategories(IPasswordManagerDbContext source, IPasswordManagerDbContext target, DateTime? lastSyncTime, SyncStatisticsDto statistics, List<SyncConflictDto> conflicts)
    {
        _logger.LogInformation("Syncing categories");
        var sourceCategories = await source.Categories
            .Where(c => !lastSyncTime.HasValue || c.LastModified > lastSyncTime)
            .ToListAsync();

        foreach (var sourceCategory in sourceCategories)
        {
            var targetCategory = await target.Categories
                .FirstOrDefaultAsync(c => c.Id == sourceCategory.Id);

            if (targetCategory == null)
            {
                // Create new category in target
                var newCategory = new Category
                {
                    Id = sourceCategory.Id,
                    Name = sourceCategory.Name,
                    Description = sourceCategory.Description,
                    Icon = sourceCategory.Icon,
                    Color = sourceCategory.Color,
                    CreatedAt = sourceCategory.CreatedAt,
                    LastModified = sourceCategory.LastModified
                };

                target.Categories.Add(newCategory);
                await target.SaveChangesAsync();
                statistics.Created++;
                _logger.LogDebug("Created category {Id} in target database", sourceCategory.Id);
            }
            else
            {
                // Check for conflicts
                if (targetCategory.LastModified > sourceCategory.LastModified)
                {
                    conflicts.Add(new SyncConflictDto
                    {
                        EntityType = "Category",
                        EntityId = sourceCategory.Id,
                        SourceLastModified = sourceCategory.LastModified,
                        TargetLastModified = targetCategory.LastModified,
                        Resolution = SyncConflictResolution.TargetWins,
                        Message = $"Target category '{targetCategory.Name}' is newer than source category '{sourceCategory.Name}'"
                    });
                    statistics.Skipped++;
                    _logger.LogWarning("Conflict detected for category {Id}, target is newer", sourceCategory.Id);
                    continue;
                }

                // Update target category
                targetCategory.Name = sourceCategory.Name;
                targetCategory.Description = sourceCategory.Description;
                targetCategory.Icon = sourceCategory.Icon;
                targetCategory.Color = sourceCategory.Color;
                targetCategory.LastModified = sourceCategory.LastModified;

                await target.SaveChangesAsync();
                statistics.Updated++;
                _logger.LogDebug("Updated category {Id} in target database", sourceCategory.Id);
            }
        }

        _logger.LogInformation("Synced {Count} categories", sourceCategories.Count);
    }

    private async Task SyncCollections(IPasswordManagerDbContext source, IPasswordManagerDbContext target, DateTime? lastSyncTime, SyncStatisticsDto statistics, List<SyncConflictDto> conflicts)
    {
        _logger.LogInformation("Syncing collections");
        var sourceCollections = await source.Collections
            .Where(c => !lastSyncTime.HasValue || c.LastModified > lastSyncTime)
            .ToListAsync();

        foreach (var sourceCollection in sourceCollections)
        {
            var targetCollection = await target.Collections
                .FirstOrDefaultAsync(c => c.Id == sourceCollection.Id);

            if (targetCollection == null)
            {
                // Create new collection in target
                var newCollection = new Collection
                {
                    Id = sourceCollection.Id,
                    Name = sourceCollection.Name,
                    Description = sourceCollection.Description,
                    Icon = sourceCollection.Icon,
                    Color = sourceCollection.Color,
                    ParentId = sourceCollection.ParentId,
                    CreatedAt = sourceCollection.CreatedAt,
                    LastModified = sourceCollection.LastModified
                };

                target.Collections.Add(newCollection);
                await target.SaveChangesAsync();
                statistics.Created++;
                _logger.LogDebug("Created collection {Id} in target database", sourceCollection.Id);
            }
            else
            {
                // Check for conflicts
                if (targetCollection.LastModified > sourceCollection.LastModified)
                {
                    conflicts.Add(new SyncConflictDto
                    {
                        EntityType = "Collection",
                        EntityId = sourceCollection.Id,
                        SourceLastModified = sourceCollection.LastModified,
                        TargetLastModified = targetCollection.LastModified,
                        Resolution = SyncConflictResolution.TargetWins,
                        Message = $"Target collection '{targetCollection.Name}' is newer than source collection '{sourceCollection.Name}'"
                    });
                    statistics.Skipped++;
                    _logger.LogWarning("Conflict detected for collection {Id}, target is newer", sourceCollection.Id);
                    continue;
                }

                // Update target collection
                targetCollection.Name = sourceCollection.Name;
                targetCollection.Description = sourceCollection.Description;
                targetCollection.Icon = sourceCollection.Icon;
                targetCollection.Color = sourceCollection.Color;
                targetCollection.ParentId = sourceCollection.ParentId;
                targetCollection.LastModified = sourceCollection.LastModified;

                await target.SaveChangesAsync();
                statistics.Updated++;
                _logger.LogDebug("Updated collection {Id} in target database", sourceCollection.Id);
            }
        }

        _logger.LogInformation("Synced {Count} collections", sourceCollections.Count);
    }

    private async Task SyncTags(IPasswordManagerDbContext source, IPasswordManagerDbContext target, DateTime? lastSyncTime, SyncStatisticsDto statistics, List<SyncConflictDto> conflicts)
    {
        _logger.LogInformation("Syncing tags");
        var sourceTags = await source.Tags
            .Where(t => !lastSyncTime.HasValue || t.LastModified > lastSyncTime)
            .ToListAsync();

        foreach (var sourceTag in sourceTags)
        {
            var targetTag = await target.Tags
                .FirstOrDefaultAsync(t => t.Id == sourceTag.Id);

            if (targetTag == null)
            {
                // Create new tag in target
                var newTag = new Tag
                {
                    Id = sourceTag.Id,
                    Name = sourceTag.Name,
                    Color = sourceTag.Color,
                    CreatedAt = sourceTag.CreatedAt,
                    LastModified = sourceTag.LastModified
                };

                target.Tags.Add(newTag);
                await target.SaveChangesAsync();
                statistics.Created++;
                _logger.LogDebug("Created tag {Id} in target database", sourceTag.Id);
            }
            else
            {
                // Check for conflicts
                if (targetTag.LastModified > sourceTag.LastModified)
                {
                    conflicts.Add(new SyncConflictDto
                    {
                        EntityType = "Tag",
                        EntityId = sourceTag.Id,
                        SourceLastModified = sourceTag.LastModified,
                        TargetLastModified = targetTag.LastModified,
                        Resolution = SyncConflictResolution.TargetWins,
                        Message = $"Target tag '{targetTag.Name}' is newer than source tag '{sourceTag.Name}'"
                    });
                    statistics.Skipped++;
                    _logger.LogWarning("Conflict detected for tag {Id}, target is newer", sourceTag.Id);
                    continue;
                }

                // Update target tag
                targetTag.Name = sourceTag.Name;
                targetTag.Color = sourceTag.Color;
                targetTag.LastModified = sourceTag.LastModified;

                await target.SaveChangesAsync();
                statistics.Updated++;
                _logger.LogDebug("Updated tag {Id} in target database", sourceTag.Id);
            }
        }

        _logger.LogInformation("Synced {Count} tags", sourceTags.Count);
    }

    private class LastSyncDto
    {
        public int Id { get; set; }
        public string SourceDatabase { get; set; }
        public string TargetDatabase { get; set; }
        public DateTime SyncTime { get; set; }
    }
}
