using Microsoft.Extensions.Configuration;
using PasswordManager.Imports.Interfaces;
using PasswordManager.Models;
using PasswordManager.Services.Interfaces;
using PasswordManager.Services.Utilities;
using System.Reflection;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace PasswordManager.Imports.Services;

public class ImportService : IImportService
{
    private readonly IPasswordItemService _passwordItemService;
    private readonly ICollectionService _collectionService;
    private readonly ICategoryInterface _categoryService;
    private readonly ITagService _tagService;
    private readonly PluginDiscoveryService _pluginDiscovery;
    private readonly IVaultSessionService? _vaultSessionService;
    private readonly Dictionary<string, IPasswordImportProvider> _importProviders;
    private readonly FileLogger _logger;
    private bool _pluginsLoaded = false;

    public ImportService(
        IPasswordItemService passwordItemService,
        ICollectionService collectionService,
        ICategoryInterface categoryService,
        ITagService tagService,
        PluginDiscoveryService pluginDiscovery,
        IVaultSessionService? vaultSessionService = null)
    {
        _passwordItemService = passwordItemService;
        _collectionService = collectionService;
        _categoryService = categoryService;
        _tagService = tagService;
        _pluginDiscovery = pluginDiscovery;
        _vaultSessionService = vaultSessionService;
        _importProviders = new Dictionary<string, IPasswordImportProvider>();
    }

    public void RegisterProvider(IPasswordImportProvider provider)
    {
        _importProviders[provider.ProviderName] = provider;
    }

    public async Task<IEnumerable<IPasswordImportProvider>> GetAvailableProvidersAsync()
    {
        if (!_pluginsLoaded)
        {
            await LoadPluginsAsync();
            _pluginsLoaded = true;
        }

        return _importProviders.Values;
    }

    public IEnumerable<IPasswordImportProvider> GetAvailableProviders()
    {
        // For backward compatibility, load plugins synchronously if not already loaded
        if (!_pluginsLoaded)
        {
            Task.Run(async () => await LoadPluginsAsync()).Wait();
            _pluginsLoaded = true;
        }

        return _importProviders.Values;
    }

    private async Task LoadPluginsAsync()
    {
        try
        {
            // First, discover and load built-in providers from loaded assemblies
            LoadBuiltInProviders();

            // Then, discover and load external plugins from plugin directories
            var plugins = await _pluginDiscovery.DiscoverPluginsAsync();
            foreach (var plugin in plugins)
            {
                RegisterProvider(plugin);
            }
        }
        catch (Exception ex)
        {
            // Log error but don't fail the service
            // In production, use proper logging instead of Console.WriteLine
        }
    }

    private void LoadBuiltInProviders()
    {
        try
        {
            // Get all loaded assemblies
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => !assembly.IsDynamic)
                .ToList();

            foreach (var assembly in assemblies)
            {
                try
                {
                    var providerTypes = assembly.GetTypes()
                        .Where(type => typeof(IPasswordImportProvider).IsAssignableFrom(type) &&
                                       !type.IsInterface && !type.IsAbstract)
                        .ToList();

                    foreach (var providerType in providerTypes)
                    {
                        try
                        {
                            var provider = Activator.CreateInstance(providerType) as IPasswordImportProvider;
                            if (provider != null && !_importProviders.ContainsKey(provider.ProviderName))
                            {
                                RegisterProvider(provider);
                            }
                        }
                        catch
                        {
                            // Failed to instantiate provider, continue with others
                        }
                    }
                }
                catch
                {
                    // Skip assemblies that can't be reflected over (system assemblies, etc.)
                }
            }
        }
        catch
        {
            // Error loading built-in providers, continue without them
        }
    }

    public async Task<ImportResult> ImportPasswordsAsync(string providerName, Stream fileStream, string fileName, string? userId = null, IProgress<int>? progress = null, string? sessionId = null)
    {
        if (!_importProviders.TryGetValue(providerName, out var provider))
        {
            return new ImportResult
            {
                Success = false,
                ErrorMessage = $"Import provider '{providerName}' not found."
            };
        }

            try
            {
                var result = await provider.ImportFromFileAsync(fileStream, fileName);

            if (result.Success)
            {
                // Create required collections first (attach to user if provided)
                var collectionMapping = await EnsureCollectionsExistAsync(result.RequiredCollections, userId);

                // Create required categories and link them to collections (attach to user if provided)
                var categoryMapping = await EnsureCategoriesExistAsync(result.RequiredCategories, collectionMapping, userId);

                // Create required tags (tags currently global - create as-is)
                await EnsureTagsExistAsync(result.RequiredTags);

                // Build a map of persisted Tag entities so items link to existing rows rather than
                // new Tag() objects. EF Core tracks new objects as Added → INSERT, causing duplicate
                // rows or unique-constraint failures on the second save within the same DbContext.
                var persistedTagMap = (await _tagService.GetAllAsync())
                    .ToDictionary(t => t.Name, t => t, StringComparer.OrdinalIgnoreCase);

                // Reset counts — provider may have pre-populated them during parsing
                result.SuccessfulImports = 0;
                result.FailedImports = 0;

                // Import the password items and map them to the actual created collections/categories
                var totalToImport = result.ImportedItems.Count;
                var processed = 0;
                progress?.Report(0);
                foreach (var item in result.ImportedItems)
                {
                    try
                    {
                        // Map the item to the actual created collection and category IDs
                        if (item.CollectionId.HasValue && item.CollectionId.Value > 0 && collectionMapping.TryGetValue(item.CollectionId.Value, out var actualCollectionId))
                        {
                            item.CollectionId = actualCollectionId;
                        }
                        else if (item.CollectionId is null or 0 && collectionMapping.Count > 0)
                        {
                            // Fallback: assign to first available collection
                            item.CollectionId = collectionMapping.Values.First();
                        }

                        if (item.CategoryId.HasValue && item.CategoryId.Value > 0 && categoryMapping.TryGetValue(item.CategoryId.Value, out var actualCategoryId))
                        {
                            item.CategoryId = actualCategoryId;
                        }
                        else if (item.CategoryId is null or 0 && categoryMapping.Count > 0)
                        {
                            // Fallback: assign to first available category
                            item.CategoryId = categoryMapping.Values.First();
                        }

                        // Ensure item is assigned to the requesting user/tenant if provided
                        if (!string.IsNullOrEmpty(userId))
                        {
                            item.UserId = userId;
                            if (item.LoginItem != null)
                                item.LoginItem.UserId = userId;
                            if (item.CreditCardItem != null)
                                item.CreditCardItem.UserId = userId;
                            if (item.SecureNoteItem != null)
                                item.SecureNoteItem.UserId = userId;
                        }

                        // Encrypt plaintext credentials — always attempt, GetActiveSessionId fallback handles missing sessionId
                        if (_vaultSessionService != null)
                        {
                            EncryptItemCredentials(item, sessionId);
                        }

                        // Replace new Tag() instances with the persisted entity so EF Core
                        // creates a junction-table row instead of trying to INSERT a duplicate tag.
                        if (item.Tags.Count > 0)
                        {
                            var resolvedTags = item.Tags
                                .Select(t => persistedTagMap.TryGetValue(t.Name, out var pt) ? pt : t)
                                .GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                                .Select(g => g.First())
                                .ToList();
                            item.Tags.Clear();
                            resolvedTags.ForEach(t => item.Tags.Add(t));
                        }

                        await _passwordItemService.CreateAsync(item);
                        result.SuccessfulImports++;
                    }
                    catch (Exception ex)
                    {
                        result.FailedImports++;
                        var detail = ex.InnerException != null ? $"{ex.Message} → {ex.InnerException.Message}" : ex.Message;
                        result.Warnings.Add($"Failed to import '{item.Title}': {detail}");
                    }

                    processed++;
                    progress?.Report((int)(processed * 100.0 / Math.Max(1, totalToImport)));
                }

                progress?.Report(100);
            }

            return result;
        }
        catch (Exception ex)
        {
            var inner = ex.InnerException;
            var chain = new System.Text.StringBuilder(ex.Message);
            while (inner != null) { chain.Append(" → ").Append(inner.Message); inner = inner.InnerException; }
            return new ImportResult
            {
                Success = false,
                ErrorMessage = $"Import failed: {chain}"
            };
        }
    }

    private async Task<Dictionary<int, int>> EnsureCollectionsExistAsync(List<Collection> requiredCollections, string? userId = null)
    {
        var mapping = new Dictionary<int, int>();
        var existingCollections = await _collectionService.GetAllAsync();

        foreach (var collection in requiredCollections)
        {
            var tempId = collection.Id;

            // Check if collection already exists
            var existing = existingCollections.FirstOrDefault(c => c.Name.Equals(collection.Name, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                mapping[tempId] = existing.Id;
            }
            else
            {
                // Attach to user if provided
                if (!string.IsNullOrEmpty(userId))
                    collection.UserId = userId;

                var created = await _collection_service_CreateAsync_with_fallback(collection);
                if (created != null)
                {
                    mapping[tempId] = created.Id;
                }
            }
        }

        return mapping;
    }

    // Helper to call collection create and handle any differences in implementations
    private async Task<Collection?> _collection_service_CreateAsync_with_fallback(Collection collection)
    {
        try
        {
            return await _collectionService.CreateAsync(collection);
        }
        catch
        {
            // Fallback: try to set CreatedAt and call again
            try
            {
                collection.CreatedAt = DateTime.UtcNow;
                return await _collectionService.CreateAsync(collection);
            }
            catch
            {
                return null;
            }
        }
    }

    private async Task<Dictionary<int, int>> EnsureCategoriesExistAsync(List<Category> requiredCategories, Dictionary<int, int> collectionMapping, string? userId = null)
    {
        var mapping = new Dictionary<int, int>();
        var existingCategories = await _categoryService.GetAllAsync();

        foreach (var category in requiredCategories)
        {
            var tempId = category.Id;

            // Map the category's collection ID to the actual created collection
            if (category.CollectionId.HasValue && collectionMapping.TryGetValue(category.CollectionId.Value, out var actualCollectionId))
            {
                category.CollectionId = actualCollectionId;
            }

            // Check if category already exists (by name and collection)
            var existing = existingCategories.FirstOrDefault(c =>
                c.Name.Equals(category.Name, StringComparison.OrdinalIgnoreCase) &&
                c.CollectionId == category.CollectionId);

            if (existing != null)
            {
                mapping[tempId] = existing.Id;
            }
            else
            {
                // Attach to user if provided
                if (!string.IsNullOrEmpty(userId))
                    category.UserId = userId;

                var created = await _categoryService.CreateAsync(category);
                if (created != null)
                {
                    mapping[tempId] = created.Id;
                }
            }
        }

        return mapping;
    }

    private async Task EnsureTagsExistAsync(List<Tag> requiredTags)
    {
        var existingTags = await _tagService.GetAllAsync();

        foreach (var tag in requiredTags)
        {
            if (!existingTags.Any(t => t.Name.Equals(tag.Name, StringComparison.OrdinalIgnoreCase)))
            {
                await _tagService.CreateAsync(tag);
            }
        }
    }

    private void EncryptItemCredentials(PasswordItem item, string? sessionId)
    {
        if (_vaultSessionService == null) return;

        // Fall back to any active session if the provided sessionId is missing or stale
        var sid = sessionId;
        if (string.IsNullOrEmpty(sid) || !_vaultSessionService.IsVaultUnlocked(sid))
            sid = _vaultSessionService.GetActiveSessionId();

        if (string.IsNullOrEmpty(sid)) return;

        if (item.LoginItem != null && !string.IsNullOrEmpty(item.LoginItem.Password))
        {
            try
            {
                item.LoginItem.EncryptedPassword = _vaultSessionService.EncryptPassword(item.LoginItem.Password, sid);
            }
            catch { /* vault may be locked; skip encryption, password will be missing */ }
        }

        if (item.LoginItem != null && !string.IsNullOrEmpty(item.LoginItem.Notes))
        {
            try
            {
                item.LoginItem.EncryptedNotes = _vaultSessionService.EncryptPassword(item.LoginItem.Notes, sid);
            }
            catch { }
        }

        if (item.LoginItem != null && !string.IsNullOrEmpty(item.LoginItem.TotpSecret))
        {
            try
            {
                item.LoginItem.EncryptedTotpSecret = _vaultSessionService.EncryptPassword(item.LoginItem.TotpSecret, sid);
            }
            catch { }
        }
    }
}
