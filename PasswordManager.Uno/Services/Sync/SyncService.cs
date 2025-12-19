using PasswordManager.Uno.Services.LocalDatabase;
using System.Net.Http.Json;
using System.Text.Json;

namespace PasswordManager.Uno.Services.Sync;

/// <summary>
/// Service for syncing data between local SQLite database and the API
/// </summary>
public class SyncService
{
    private readonly LocalDatabaseService _localDatabase;
    private readonly HttpClient _httpClient;
    private readonly ILogger<SyncService> _logger;
    private string? _authToken;

    public SyncService(
        LocalDatabaseService localDatabase, 
        HttpClient httpClient,
        ILogger<SyncService> logger)
    {
        _localDatabase = localDatabase;
        _httpClient = httpClient;
        _logger = logger;
    }

    public void SetAuthToken(string token)
    {
        _authToken = token;
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Sync all data from the server to local database
    /// </summary>
    public async Task<SyncResult> SyncFromServerAsync()
    {
        var result = new SyncResult();
        
        try
        {
            if (string.IsNullOrEmpty(_authToken))
            {
                result.Success = false;
                result.ErrorMessage = "Not authenticated. Please log in first.";
                return result;
            }

            // Sync categories first
            await SyncCategoriesFromServerAsync(result);

            // Then sync password items
            await SyncPasswordItemsFromServerAsync(result);

            result.Success = true;
            result.SyncedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing from server");
            result.Success = false;
            result.ErrorMessage = $"Sync failed: {ex.Message}";
        }

        return result;
    }

    private async Task SyncCategoriesFromServerAsync(SyncResult result)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/categories");
            response.EnsureSuccessStatusCode();

            var categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>();
            if (categories != null)
            {
                foreach (var category in categories)
                {
                    var localCategory = await _localDatabase.GetCategoryByServerIdAsync(category.Id);
                    
                    if (localCategory == null)
                    {
                        localCategory = new LocalCategory();
                        result.CategoriesAdded++;
                    }
                    else
                    {
                        result.CategoriesUpdated++;
                    }

                    localCategory.ServerId = category.Id;
                    localCategory.Name = category.Name;
                    localCategory.Description = category.Description;
                    localCategory.IconName = category.Icon;
                    localCategory.LastModified = category.LastModified;
                    localCategory.NeedsSyncToServer = false;
                    localCategory.LastSyncedAt = DateTime.UtcNow;

                    await _localDatabase.SaveCategoryAsync(localCategory);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing categories from server");
            throw;
        }
    }

    private async Task SyncPasswordItemsFromServerAsync(SyncResult result)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/passworditems");
            response.EnsureSuccessStatusCode();

            var items = await response.Content.ReadFromJsonAsync<List<PasswordItemDto>>();
            if (items != null)
            {
                foreach (var item in items)
                {
                    var localItem = await _localDatabase.GetPasswordItemByServerIdAsync(item.Id);
                    
                    if (localItem == null)
                    {
                        localItem = new LocalPasswordItem();
                        result.ItemsAdded++;
                    }
                    else
                    {
                        result.ItemsUpdated++;
                    }

                    localItem.ServerId = item.Id;
                    localItem.Title = item.Title;
                    localItem.Description = item.Description;
                    localItem.Type = item.Type?.ToString() ?? "Login";
                    localItem.Website = item.Website;
                    localItem.CategoryId = item.CategoryId;
                    localItem.IsFavorite = item.IsFavorite;
                    localItem.IsArchived = item.IsArchived;
                    localItem.IsDeleted = item.IsDeleted;
                    localItem.CreatedAt = item.CreatedAt;
                    localItem.LastModified = item.LastModified;
                    localItem.Username = item.LoginItem?.Username;
                    localItem.EncryptedPassword = item.LoginItem?.EncryptedPassword;
                    localItem.Notes = item.SecureNoteItem?.EncryptedNote;
                    localItem.NeedsSyncToServer = false;
                    localItem.LastSyncedAt = DateTime.UtcNow;

                    await _localDatabase.SavePasswordItemAsync(localItem);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing password items from server");
            throw;
        }
    }

    /// <summary>
    /// Sync local changes to the server
    /// </summary>
    public async Task<SyncResult> SyncToServerAsync()
    {
        var result = new SyncResult();
        
        try
        {
            if (string.IsNullOrEmpty(_authToken))
            {
                result.Success = false;
                result.ErrorMessage = "Not authenticated. Please log in first.";
                return result;
            }

            // Sync local categories to server
            var categoriesNeedingSync = await _localDatabase.GetCategoriesNeedingSyncAsync();
            foreach (var category in categoriesNeedingSync)
            {
                // Implementation would post to API
                // For now, just mark as synced
                await _localDatabase.MarkCategorySyncedAsync(category.LocalId, category.ServerId ?? 0);
            }

            // Sync local password items to server
            var itemsNeedingSync = await _localDatabase.GetItemsNeedingSyncAsync();
            foreach (var item in itemsNeedingSync)
            {
                // Implementation would post to API
                // For now, just mark as synced
                await _localDatabase.MarkItemSyncedAsync(item.LocalId, item.ServerId ?? 0);
            }

            result.Success = true;
            result.SyncedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing to server");
            result.Success = false;
            result.ErrorMessage = $"Sync failed: {ex.Message}";
        }

        return result;
    }
}

public class SyncResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? SyncedAt { get; set; }
    public int ItemsAdded { get; set; }
    public int ItemsUpdated { get; set; }
    public int CategoriesAdded { get; set; }
    public int CategoriesUpdated { get; set; }
}

// DTOs matching API responses
public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public DateTime LastModified { get; set; }
}

public class PasswordItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? Website { get; set; }
    public int? CategoryId { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsArchived { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModified { get; set; }
    public LoginItemDto? LoginItem { get; set; }
    public SecureNoteItemDto? SecureNoteItem { get; set; }
}

public class LoginItemDto
{
    public string? Username { get; set; }
    public string? EncryptedPassword { get; set; }
}

public class SecureNoteItemDto
{
    public string? EncryptedNote { get; set; }
}
