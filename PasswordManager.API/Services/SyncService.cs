using Microsoft.EntityFrameworkCore;
using PasswordManager.API.Interfaces;
using PasswordManager.Services.Interfaces;
using PasswordManager.DAL;
using PasswordManager.Models;
using PasswordManager.Models.DTOs.Sync;

namespace PasswordManager.API.Services;

public class SyncService : ISyncService
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
                        var passwordItemStats = await SyncPasswordItems(sourceContext, targetContext, request.LastSyncTime, request.ConflictResolution);
                        statistics = MergeStatistics(statistics, passwordItemStats.statistics);
                        conflicts.AddRange(passwordItemStats.conflicts);
                        break;
                    case "categories":
                        var categoryStats = await SyncCategories(sourceContext, targetContext, request.LastSyncTime, request.ConflictResolution);
                        statistics = MergeStatistics(statistics, categoryStats.statistics);
                        conflicts.AddRange(categoryStats.conflicts);
                        break;
                    case "collections":
                        var collectionStats = await SyncCollections(sourceContext, targetContext, request.LastSyncTime, request.ConflictResolution);
                        statistics = MergeStatistics(statistics, collectionStats.statistics);
                        conflicts.AddRange(collectionStats.conflicts);
                        break;
                    case "tags":
                        var tagStats = await SyncTags(sourceContext, targetContext, request.LastSyncTime, request.ConflictResolution);
                        statistics = MergeStatistics(statistics, tagStats.statistics);
                        conflicts.AddRange(tagStats.conflicts);
                        break;
                }
            }

            var syncTime = DateTime.UtcNow;
            await UpdateLastSyncTimeAsync(request.SourceDatabase, request.TargetDatabase, syncTime);

            statistics.Duration = syncTime - startTime;

            _logger.LogInformation("Sync completed successfully. Duration: {Duration}, Records processed: {Total}",
                statistics.Duration, statistics.TotalRecordsProcessed);

            return new SyncResponseDto
            {
                Success = true,
                Message = "Sync completed successfully",
                SyncTime = syncTime,
                Statistics = statistics,
                Conflicts = conflicts
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync failed");
            return new SyncResponseDto
            {
                Success = false,
                Message = $"Sync failed: {ex.Message}",
                SyncTime = DateTime.UtcNow,
                Statistics = statistics,
                Conflicts = conflicts
            };
        }
    }

    public async Task<SyncResponseDto> SyncFromSqliteToSqlServerAsync(DateTime? lastSyncTime = null)
    {
        var request = new SyncRequestDto
        {
            SourceDatabase = "Sqlite",
            TargetDatabase = "SqlServer",
            LastSyncTime = lastSyncTime,
            EntitiesToSync = new List<string> { "Collections", "Categories", "Tags", "PasswordItems" },
            ConflictResolution = SyncConflictResolution.LastWriteWins
        };

        return await SyncDatabasesAsync(request);
    }

    public async Task<SyncResponseDto> SyncFromSqlServerToSqliteAsync(DateTime? lastSyncTime = null)
    {
        var request = new SyncRequestDto
        {
            SourceDatabase = "SqlServer",
            TargetDatabase = "Sqlite",
            LastSyncTime = lastSyncTime,
            EntitiesToSync = new List<string> { "Collections", "Categories", "Tags", "PasswordItems" },
            ConflictResolution = SyncConflictResolution.LastWriteWins
        };

        return await SyncDatabasesAsync(request);
    }

    public async Task<SyncResponseDto> FullSyncAsync(SyncDirection direction)
    {
        return direction switch
        {
            SyncDirection.Push => await SyncFromSqliteToSqlServerAsync(),
            SyncDirection.Pull => await SyncFromSqlServerToSqliteAsync(),
            SyncDirection.Bidirectional => throw new NotImplementedException("Bidirectional sync not yet implemented"),
            _ => throw new ArgumentException("Invalid sync direction")
        };
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
            _logger.LogError(ex, "Cannot connect to database with provider {Provider}", provider);
            return false;
        }
    }

    public async Task<DateTime?> GetLastSyncTimeAsync(string sourceDb, string targetDb)
    {
        // This would typically be stored in a sync metadata table
        // For now, return null to force full sync
        await Task.CompletedTask;
        return null;
    }

    public async Task UpdateLastSyncTimeAsync(string sourceDb, string targetDb, DateTime syncTime)
    {
        // This would typically update a sync metadata table
        // For now, just log the sync time
        _logger.LogInformation("Sync completed at {SyncTime} from {Source} to {Target}", syncTime, sourceDb, targetDb);
        await Task.CompletedTask;
    }

    private async Task<(SyncStatisticsDto statistics, List<SyncConflictDto> conflicts)> SyncPasswordItems(
        PasswordManagerDbContext sourceContext, 
        PasswordManagerDbContext targetContext, 
        DateTime? lastSyncTime, 
        SyncConflictResolution conflictResolution)
    {
        var statistics = new SyncStatisticsDto();
        var conflicts = new List<SyncConflictDto>();

        var query = sourceContext.PasswordItems
            .Include(p => p.LoginItem)
            .Include(p => p.CreditCardItem)
            .Include(p => p.SecureNoteItem)
            .Include(p => p.WiFiItem)
            .Include(p => p.Tags)
            .AsQueryable();

        if (lastSyncTime.HasValue)
        {
            query = query.Where(p => p.LastModified > lastSyncTime.Value);
        }

        var sourceItems = await query.ToListAsync();
        statistics.TotalRecordsProcessed += sourceItems.Count;

        foreach (var sourceItem in sourceItems)
        {
            // Filter sensitive data before syncing
            var filteredItem = DataFilterService.FilterSensitiveData(sourceItem);
            if (filteredItem == null)
            {
                // Item was filtered out (e.g., master password)
                continue;
            }

            var targetItem = await targetContext.PasswordItems
                .Include(p => p.LoginItem)
                .Include(p => p.CreditCardItem)
                .Include(p => p.SecureNoteItem)
                .Include(p => p.WiFiItem)
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == filteredItem.Id);

            if (targetItem == null)
            {
                // Add new item
                var newItem = ClonePasswordItem(filteredItem);
                targetContext.PasswordItems.Add(newItem);
                statistics.RecordsAdded++;
            }
            else if (filteredItem.LastModified > targetItem.LastModified)
            {
                // Update existing item
                UpdatePasswordItem(targetItem, filteredItem);
                statistics.RecordsUpdated++;
            }
            else if (filteredItem.LastModified < targetItem.LastModified)
            {
                // Conflict detected
                var conflict = new SyncConflictDto
                {
                    EntityType = "PasswordItem",
                    EntityId = filteredItem.Id,
                    ConflictType = "ModificationConflict",
                    SourceModified = filteredItem.LastModified,
                    TargetModified = targetItem.LastModified,
                    Resolution = conflictResolution.ToString()
                };

                conflicts.Add(conflict);

                if (conflictResolution == SyncConflictResolution.SourceWins)
                {
                    UpdatePasswordItem(targetItem, filteredItem);
                    statistics.RecordsUpdated++;
                }
                // If TargetWins or LastWriteWins with target being newer, do nothing

                statistics.ConflictsResolved++;
            }
        }

        await targetContext.SaveChangesAsync();
        return (statistics, conflicts);
    }

    private async Task<(SyncStatisticsDto statistics, List<SyncConflictDto> conflicts)> SyncCategories(
        PasswordManagerDbContext sourceContext, 
        PasswordManagerDbContext targetContext, 
        DateTime? lastSyncTime, 
        SyncConflictResolution conflictResolution)
    {
        var statistics = new SyncStatisticsDto();
        var conflicts = new List<SyncConflictDto>();

        var query = sourceContext.Categories.AsQueryable();
        
        if (lastSyncTime.HasValue)
        {
            query = query.Where(c => c.CreatedAt > lastSyncTime.Value);
        }

        var sourceCategories = await query.ToListAsync();
        statistics.TotalRecordsProcessed += sourceCategories.Count;

        foreach (var sourceCategory in sourceCategories)
        {
            // Check if category should be synced
            if (!DataFilterService.ShouldSyncCategory(sourceCategory))
            {
                continue;
            }

            var targetCategory = await targetContext.Categories.FindAsync(sourceCategory.Id);

            if (targetCategory == null)
            {
                var newCategory = CloneCategory(sourceCategory);
                targetContext.Categories.Add(newCategory);
                statistics.RecordsAdded++;
            }
            else
            {
                UpdateCategory(targetCategory, sourceCategory);
                statistics.RecordsUpdated++;
            }
        }

        await targetContext.SaveChangesAsync();
        return (statistics, conflicts);
    }

    private async Task<(SyncStatisticsDto statistics, List<SyncConflictDto> conflicts)> SyncCollections(
        PasswordManagerDbContext sourceContext, 
        PasswordManagerDbContext targetContext, 
        DateTime? lastSyncTime, 
        SyncConflictResolution conflictResolution)
    {
        var statistics = new SyncStatisticsDto();
        var conflicts = new List<SyncConflictDto>();

        var query = sourceContext.Collections.AsQueryable();
        
        if (lastSyncTime.HasValue)
        {
            query = query.Where(c => c.CreatedAt > lastSyncTime.Value);
        }

        var sourceCollections = await query.ToListAsync();
        statistics.TotalRecordsProcessed += sourceCollections.Count;

        foreach (var sourceCollection in sourceCollections)
        {
            // Check if collection should be synced
            if (!DataFilterService.ShouldSyncCollection(sourceCollection))
            {
                continue;
            }

            var targetCollection = await targetContext.Collections.FindAsync(sourceCollection.Id);

            if (targetCollection == null)
            {
                var newCollection = CloneCollection(sourceCollection);
                targetContext.Collections.Add(newCollection);
                statistics.RecordsAdded++;
            }
            else
            {
                UpdateCollection(targetCollection, sourceCollection);
                statistics.RecordsUpdated++;
            }
        }

        await targetContext.SaveChangesAsync();
        return (statistics, conflicts);
    }

    private async Task<(SyncStatisticsDto statistics, List<SyncConflictDto> conflicts)> SyncTags(
        PasswordManagerDbContext sourceContext, 
        PasswordManagerDbContext targetContext, 
        DateTime? lastSyncTime, 
        SyncConflictResolution conflictResolution)
    {
        var statistics = new SyncStatisticsDto();
        var conflicts = new List<SyncConflictDto>();

        var query = sourceContext.Tags.AsQueryable();
        
        if (lastSyncTime.HasValue)
        {
            query = query.Where(t => t.CreatedAt > lastSyncTime.Value);
        }

        var sourceTags = await query.ToListAsync();
        statistics.TotalRecordsProcessed += sourceTags.Count;

        foreach (var sourceTag in sourceTags)
        {
            // Check if tag should be synced
            if (!DataFilterService.ShouldSyncTag(sourceTag))
            {
                continue;
            }

            var targetTag = await targetContext.Tags.FindAsync(sourceTag.Id);

            if (targetTag == null)
            {
                var newTag = CloneTag(sourceTag);
                targetContext.Tags.Add(newTag);
                statistics.RecordsAdded++;
            }
            else
            {
                UpdateTag(targetTag, sourceTag);
                statistics.RecordsUpdated++;
            }
        }

        await targetContext.SaveChangesAsync();
        return (statistics, conflicts);
    }

    private string GetConnectionString(string provider)
    {
        return provider.ToLower() switch
        {
            "sqlite" => _configuration.GetConnectionString("SqliteConnection") ?? throw new InvalidOperationException("Sqlite connection string not found"),
            "sqlserver" => _configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("SqlServer connection string not found"),
            "postgres" => _configuration.GetConnectionString("PostgresConnection") ?? throw new InvalidOperationException("Postgres connection string not found"),
            _ => throw new ArgumentException($"Unknown database provider: {provider}")
        };
    }

    private static SyncStatisticsDto MergeStatistics(SyncStatisticsDto existing, SyncStatisticsDto additional)
    {
        return new SyncStatisticsDto
        {
            TotalRecordsProcessed = existing.TotalRecordsProcessed + additional.TotalRecordsProcessed,
            RecordsAdded = existing.RecordsAdded + additional.RecordsAdded,
            RecordsUpdated = existing.RecordsUpdated + additional.RecordsUpdated,
            RecordsDeleted = existing.RecordsDeleted + additional.RecordsDeleted,
            ConflictsResolved = existing.ConflictsResolved + additional.ConflictsResolved,
            Duration = existing.Duration + additional.Duration
        };
    }

    // Helper methods for cloning entities
    private static PasswordItem ClonePasswordItem(PasswordItem source)
    {
        var clone = new PasswordItem
        {
            Id = source.Id,
            Title = source.Title,
            Description = source.Description,
            Type = source.Type,
            CreatedAt = source.CreatedAt,
            LastModified = source.LastModified,
            IsFavorite = source.IsFavorite,
            IsArchived = source.IsArchived,
            IsDeleted = source.IsDeleted,
            CategoryId = source.CategoryId,
            CollectionId = source.CollectionId
        };

        // Clone specific item types
        if (source.LoginItem != null)
        {
            clone.LoginItem = new LoginItem
            {
                Id = source.LoginItem.Id,
                Username = source.LoginItem.Username,
                Email = source.LoginItem.Email,
                Password = source.LoginItem.Password,
                Website = source.LoginItem.Website,
                TotpSecret = source.LoginItem.TotpSecret,
                Notes = source.LoginItem.Notes,
                PasswordItemId = source.LoginItem.PasswordItemId
            };
        }

        if (source.CreditCardItem != null)
        {
            clone.CreditCardItem = new CreditCardItem
            {
                Id = source.CreditCardItem.Id,
                CardholderName = source.CreditCardItem.CardholderName,
                CardNumber = source.CreditCardItem.CardNumber,
                CVV = source.CreditCardItem.CVV,
                ExpiryDate = source.CreditCardItem.ExpiryDate,
                PIN = source.CreditCardItem.PIN,
                CardType = source.CreditCardItem.CardType,
                Notes = source.CreditCardItem.Notes,
                PasswordItemId = source.CreditCardItem.PasswordItemId
            };
        }

        if (source.SecureNoteItem != null)
        {
            clone.SecureNoteItem = new SecureNoteItem
            {
                Id = source.SecureNoteItem.Id,
                Content = source.SecureNoteItem.Content,
                PasswordItemId = source.SecureNoteItem.PasswordItemId
            };
        }

        if (source.WiFiItem != null)
        {
            clone.WiFiItem = new WiFiItem
            {
                Id = source.WiFiItem.Id,
                SSID = source.WiFiItem.SSID,
                Password = source.WiFiItem.Password,
                SecurityType = source.WiFiItem.SecurityType,
                Frequency = source.WiFiItem.Frequency,
                RouterPassword = source.WiFiItem.RouterPassword,
                Notes = source.WiFiItem.Notes,
                PasswordItemId = source.WiFiItem.PasswordItemId
            };
        }

        return clone;
    }

    private static void UpdatePasswordItem(PasswordItem target, PasswordItem source)
    {
        target.Title = source.Title;
        target.Description = source.Description;
        target.Type = source.Type;
        target.LastModified = source.LastModified;
        target.IsFavorite = source.IsFavorite;
        target.IsArchived = source.IsArchived;
        target.IsDeleted = source.IsDeleted;
        target.CategoryId = source.CategoryId;
        target.CollectionId = source.CollectionId;

        // Update specific item types
        if (source.LoginItem != null && target.LoginItem != null)
        {
            target.LoginItem.Username = source.LoginItem.Username;
            target.LoginItem.Email = source.LoginItem.Email;
            target.LoginItem.Password = source.LoginItem.Password;
            target.LoginItem.Website = source.LoginItem.Website;
            target.LoginItem.TotpSecret = source.LoginItem.TotpSecret;
            target.LoginItem.Notes = source.LoginItem.Notes;
        }

        if (source.CreditCardItem != null && target.CreditCardItem != null)
        {
            target.CreditCardItem.CardholderName = source.CreditCardItem.CardholderName;
            target.CreditCardItem.CardNumber = source.CreditCardItem.CardNumber;
            target.CreditCardItem.CVV = source.CreditCardItem.CVV;
            target.CreditCardItem.ExpiryDate = source.CreditCardItem.ExpiryDate;
            target.CreditCardItem.PIN = source.CreditCardItem.PIN;
            target.CreditCardItem.CardType = source.CreditCardItem.CardType;
            target.CreditCardItem.Notes = source.CreditCardItem.Notes;
        }

        if (source.SecureNoteItem != null && target.SecureNoteItem != null)
        {
            target.SecureNoteItem.Content = source.SecureNoteItem.Content;
        }

        if (source.WiFiItem != null && target.WiFiItem != null)
        {
            target.WiFiItem.SSID = source.WiFiItem.SSID;
            target.WiFiItem.Password = source.WiFiItem.Password;
            target.WiFiItem.SecurityType = source.WiFiItem.SecurityType;
            target.WiFiItem.Frequency = source.WiFiItem.Frequency;
            target.WiFiItem.RouterPassword = source.WiFiItem.RouterPassword;
            target.WiFiItem.Notes = source.WiFiItem.Notes;
        }
    }

    private static Category CloneCategory(Category source)
    {
        return new Category
        {
            Id = source.Id,
            Name = source.Name,
            Description = source.Description,
            Icon = source.Icon,
            Color = source.Color,
            CreatedAt = source.CreatedAt,
            CollectionId = source.CollectionId
        };
    }

    private static void UpdateCategory(Category target, Category source)
    {
        target.Name = source.Name;
        target.Description = source.Description;
        target.Icon = source.Icon;
        target.Color = source.Color;
        target.CollectionId = source.CollectionId;
    }

    private static Collection CloneCollection(Collection source)
    {
        return new Collection
        {
            Id = source.Id,
            Name = source.Name,
            Description = source.Description,
            Icon = source.Icon,
            Color = source.Color,
            CreatedAt = source.CreatedAt,
            IsDefault = source.IsDefault,
            ParentCollectionId = source.ParentCollectionId
        };
    }

    private static void UpdateCollection(Collection target, Collection source)
    {
        target.Name = source.Name;
        target.Description = source.Description;
        target.Icon = source.Icon;
        target.Color = source.Color;
        target.IsDefault = source.IsDefault;
        target.ParentCollectionId = source.ParentCollectionId;
    }

    private static Tag CloneTag(Tag source)
    {
        return new Tag
        {
            Id = source.Id,
            Name = source.Name,
            Color = source.Color,
            IsSystemTag = source.IsSystemTag,
            CreatedAt = source.CreatedAt
        };
    }

    private static void UpdateTag(Tag target, Tag source)
    {
        target.Name = source.Name;
        target.Color = source.Color;
        target.IsSystemTag = source.IsSystemTag;
    }
}
