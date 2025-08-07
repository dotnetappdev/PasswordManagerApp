using Microsoft.Extensions.Configuration;
using PasswordManager.Imports.Interfaces;
using PasswordManager.Models;
using PasswordManager.Services.Interfaces;
using System.Reflection;

namespace PasswordManager.Imports.Services;

public class ImportService : IImportService
{
    private readonly IPasswordItemService _passwordItemService;
    private readonly ICollectionService _collectionService;
    private readonly ICategoryInterface _categoryService;
    private readonly ITagService _tagService;
    private readonly PluginDiscoveryService _pluginDiscovery;
    private readonly Dictionary<string, IPasswordImportProvider> _importProviders;
    private bool _pluginsLoaded = false;

    public ImportService(
        IPasswordItemService passwordItemService,
        ICollectionService collectionService,
        ICategoryInterface categoryService,
        ITagService tagService,
        PluginDiscoveryService pluginDiscovery)
    {
        _passwordItemService = passwordItemService;
        _collectionService = collectionService;
        _categoryService = categoryService;
        _tagService = tagService;
        _pluginDiscovery = pluginDiscovery;
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

    public async Task<ImportResult> ImportPasswordsAsync(string providerName, Stream fileStream, string fileName)
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
                // Create required collections, categories, and tags
                await EnsureCollectionsExistAsync(result.RequiredCollections);
                await EnsureCategoriesExistAsync(result.RequiredCategories);
                await EnsureTagsExistAsync(result.RequiredTags);

                // Import the password items
                foreach (var item in result.ImportedItems)
                {
                    try
                    {
                        await _passwordItemService.CreateAsync(item);
                        result.SuccessfulImports++;
                    }
                    catch (Exception ex)
                    {
                        result.FailedImports++;
                        result.Warnings.Add($"Failed to import '{item.Title}': {ex.Message}");
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            return new ImportResult
            {
                Success = false,
                ErrorMessage = $"Import failed: {ex.Message}"
            };
        }
    }

    private async Task EnsureCollectionsExistAsync(List<Collection> requiredCollections)
    {
        var existingCollections = await _collectionService.GetAllAsync();
        
        foreach (var collection in requiredCollections)
        {
            if (!existingCollections.Any(c => c.Name.Equals(collection.Name, StringComparison.OrdinalIgnoreCase)))
            {
                await _collectionService.CreateAsync(collection);
            }
        }
    }

    private async Task EnsureCategoriesExistAsync(List<Category> requiredCategories)
    {
        var existingCategories = await _categoryService.GetAllAsync();
        
        foreach (var category in requiredCategories)
        {
            if (!existingCategories.Any(c => c.Name.Equals(category.Name, StringComparison.OrdinalIgnoreCase)))
            {
                await _categoryService.CreateAsync(category);
            }
        }
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
}
