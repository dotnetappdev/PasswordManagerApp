using Microsoft.Extensions.Configuration;
using VaultGuard.Imports.Interfaces;
using VaultGuard.Models;
using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Utilities;
using System.Reflection;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace VaultGuard.Imports.Services;

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
        IVaultSessionService? vaultSessionService = null,
        IEnumerable<IPasswordImportProvider>? registeredProviders = null)
    {
        _passwordItemService = passwordItemService;
        _collectionService = collectionService;
        _categoryService = categoryService;
        _tagService = tagService;
        _pluginDiscovery = pluginDiscovery;
        _vaultSessionService = vaultSessionService;
        _importProviders = new Dictionary<string, IPasswordImportProvider>();
        // Providers the host registered in DI. Assembly scanning only sees assemblies that happen to be loaded
        // already, which isn't reliable (the MAUI app's import list came up empty), so hosts register them.
        foreach (var provider in registeredProviders ?? Enumerable.Empty<IPasswordImportProvider>())
            RegisterProvider(provider);
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
            VaultGuard.Services.Logging.AppLogger.Error($"Failed to load import plugins", ex);
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
                        catch (Exception ex)
                        {
                            VaultGuard.Services.Logging.AppLogger.Error($"Failed to instantiate import provider {providerType}", ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    VaultGuard.Services.Logging.AppLogger.Error($"Skipped assembly {assembly.FullName} that could not be reflected over", ex);
                }
            }
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Error($"Error loading built-in import providers", ex);
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
                            if (item.WiFiItem != null)
                                item.WiFiItem.UserId = userId;
                            if (item.PasskeyItem != null)
                                item.PasskeyItem.UserId = userId;
                        }

                        // Encrypt every plaintext secret with this user's vault key (refuses the item if that isn't possible)
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

                // The provider's Id is only a temporary key for linking its items; let the database assign the
                // real one (inserting the temporary value collided with existing rows on every later import).
                collection.Id = 0;
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
            catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", logEx); return null; }
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

                category.Id = 0; // temporary provider key — see EnsureCollectionsExistAsync
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

    /// <summary>
    /// Encrypts every imported secret (password, TOTP, notes, card number/CVV/PIN, Wi-Fi passwords, note
    /// content…) with the importing user's vault key. Uses the import's own session, or an unlocked session
    /// that belongs to the same user — never another user's (that would make the data unreadable to its
    /// owner). With no usable key the item is refused rather than stored without its secrets.
    /// </summary>
    private void EncryptItemCredentials(PasswordItem item, string? sessionId)
    {
        if (_vaultSessionService == null || !VaultGuard.Services.Security.VaultItemSecrets.HasPlaintext(item)) return;

        // The import's own session only if it belongs to the item's owner (a stale or other profile's session
        // would seal the secrets with a key the owner doesn't have); otherwise the owner's unlocked session.
        var sid = sessionId;
        if (string.IsNullOrEmpty(sid) || !_vaultSessionService.IsVaultUnlocked(sid) ||
            (!string.IsNullOrEmpty(item.UserId) && _vaultSessionService.GetSessionUserId(sid) != item.UserId))
        {
            sid = string.IsNullOrEmpty(item.UserId) ? null : _vaultSessionService.GetActiveSessionIdForUser(item.UserId);
        }

        var cipher = VaultGuard.Services.Security.VaultCipher.ForSession(_vaultSessionService, sid);
        if (cipher == null)
            throw new InvalidOperationException("the vault is locked, so its secrets can't be encrypted — unlock and import again");
        cipher.Seal(item);
        VaultGuard.Services.Security.VaultItemSecrets.Forget(item);
    }
}
