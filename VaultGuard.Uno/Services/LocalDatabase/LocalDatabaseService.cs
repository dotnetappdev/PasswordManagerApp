using SQLite;

namespace VaultGuard.Uno.Services.LocalDatabase;

/// <summary>
/// Service for managing local SQLite database operations
/// </summary>
public class LocalDatabaseService
{
    private readonly SQLiteAsyncConnection _database;
    private readonly Task _initializationTask;

    public LocalDatabaseService(string databasePath)
    {
        _database = new SQLiteAsyncConnection(databasePath);
        _initializationTask = InitializeDatabaseAsync();
    }

    private async Task InitializeDatabaseAsync()
    {
        await _database.CreateTableAsync<LocalPasswordItem>();
        await _database.CreateTableAsync<LocalCategory>();
    }

    private async Task EnsureInitializedAsync()
    {
        await _initializationTask;
    }

    #region Password Items

    public async Task<List<LocalPasswordItem>> GetAllPasswordItemsAsync()
    {
        await EnsureInitializedAsync();
        return await _database.Table<LocalPasswordItem>()
            .Where(item => !item.IsDeleted)
            .OrderByDescending(item => item.LastModified)
            .ToListAsync();
    }

    public async Task<LocalPasswordItem?> GetPasswordItemAsync(int localId)
    {
        await EnsureInitializedAsync();
        return await _database.Table<LocalPasswordItem>()
            .Where(item => item.LocalId == localId)
            .FirstOrDefaultAsync();
    }

    public async Task<LocalPasswordItem?> GetPasswordItemByServerIdAsync(int serverId)
    {
        return await _database.Table<LocalPasswordItem>()
            .Where(item => item.ServerId == serverId)
            .FirstOrDefaultAsync();
    }

    public async Task<List<LocalPasswordItem>> GetPasswordItemsByCategoryAsync(int categoryId)
    {
        return await _database.Table<LocalPasswordItem>()
            .Where(item => item.CategoryId == categoryId && !item.IsDeleted)
            .OrderByDescending(item => item.LastModified)
            .ToListAsync();
    }

    public async Task<List<LocalPasswordItem>> GetFavoritePasswordItemsAsync()
    {
        return await _database.Table<LocalPasswordItem>()
            .Where(item => item.IsFavorite && !item.IsDeleted)
            .OrderByDescending(item => item.LastModified)
            .ToListAsync();
    }

    public async Task<List<LocalPasswordItem>> SearchPasswordItemsAsync(string searchTerm)
    {
        return await _database.Table<LocalPasswordItem>()
            .Where(item => 
                !item.IsDeleted && 
                (item.Title.Contains(searchTerm) || 
                 (item.Description != null && item.Description.Contains(searchTerm)) ||
                 (item.Website != null && item.Website.Contains(searchTerm))))
            .OrderByDescending(item => item.LastModified)
            .ToListAsync();
    }

    public async Task<int> SavePasswordItemAsync(LocalPasswordItem item)
    {
        await EnsureInitializedAsync();
        item.LastModified = DateTime.UtcNow;
        item.NeedsSyncToServer = true;

        if (item.LocalId != 0)
        {
            await _database.UpdateAsync(item);
            return item.LocalId;
        }
        else
        {
            item.CreatedAt = DateTime.UtcNow;
            return await _database.InsertAsync(item);
        }
    }

    public async Task<int> DeletePasswordItemAsync(LocalPasswordItem item)
    {
        item.IsDeleted = true;
        item.LastModified = DateTime.UtcNow;
        item.NeedsSyncToServer = true;
        return await _database.UpdateAsync(item);
    }

    public async Task<List<LocalPasswordItem>> GetItemsNeedingSyncAsync()
    {
        return await _database.Table<LocalPasswordItem>()
            .Where(item => item.NeedsSyncToServer)
            .ToListAsync();
    }

    #endregion

    #region Categories

    public async Task<List<LocalCategory>> GetAllCategoriesAsync()
    {
        await EnsureInitializedAsync();
        return await _database.Table<LocalCategory>()
            .OrderBy(category => category.Name)
            .ToListAsync();
    }

    public async Task<LocalCategory?> GetCategoryAsync(int localId)
    {
        return await _database.Table<LocalCategory>()
            .Where(category => category.LocalId == localId)
            .FirstOrDefaultAsync();
    }

    public async Task<LocalCategory?> GetCategoryByServerIdAsync(int serverId)
    {
        return await _database.Table<LocalCategory>()
            .Where(category => category.ServerId == serverId)
            .FirstOrDefaultAsync();
    }

    public async Task<int> SaveCategoryAsync(LocalCategory category)
    {
        category.LastModified = DateTime.UtcNow;
        category.NeedsSyncToServer = true;

        if (category.LocalId != 0)
        {
            await _database.UpdateAsync(category);
            return category.LocalId;
        }
        else
        {
            category.CreatedAt = DateTime.UtcNow;
            return await _database.InsertAsync(category);
        }
    }

    public async Task<int> DeleteCategoryAsync(LocalCategory category)
    {
        return await _database.DeleteAsync(category);
    }

    public async Task<List<LocalCategory>> GetCategoriesNeedingSyncAsync()
    {
        return await _database.Table<LocalCategory>()
            .Where(category => category.NeedsSyncToServer)
            .ToListAsync();
    }

    #endregion

    #region Sync Operations

    public async Task ClearAllDataAsync()
    {
        await _database.DeleteAllAsync<LocalPasswordItem>();
        await _database.DeleteAllAsync<LocalCategory>();
    }

    public async Task MarkItemSyncedAsync(int localId, int serverId)
    {
        var item = await GetPasswordItemAsync(localId);
        if (item != null)
        {
            item.ServerId = serverId;
            item.NeedsSyncToServer = false;
            item.LastSyncedAt = DateTime.UtcNow;
            await _database.UpdateAsync(item);
        }
    }

    public async Task MarkCategorySyncedAsync(int localId, int serverId)
    {
        var category = await GetCategoryAsync(localId);
        if (category != null)
        {
            category.ServerId = serverId;
            category.NeedsSyncToServer = false;
            category.LastSyncedAt = DateTime.UtcNow;
            await _database.UpdateAsync(category);
        }
    }

    #endregion
}
