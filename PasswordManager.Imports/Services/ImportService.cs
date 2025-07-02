using PasswordManager.Imports.Interfaces;
using PasswordManager.Models;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.Imports.Services;

public class ImportService : IImportService
{
    private readonly IPasswordItemService _passwordItemService;
    private readonly ICollectionService _collectionService;
    private readonly ICategoryInterface _categoryService;
    private readonly ITagInterface _tagService;
    private readonly Dictionary<string, IPasswordImportProvider> _importProviders;

    public ImportService(
        IPasswordItemService passwordItemService,
        ICollectionService collectionService,
        ICategoryInterface categoryService,
        ITagInterface tagService)
    {
        _passwordItemService = passwordItemService;
        _collectionService = collectionService;
        _categoryService = categoryService;
        _tagService = tagService;
        _importProviders = new Dictionary<string, IPasswordImportProvider>();
    }

    public void RegisterProvider(IPasswordImportProvider provider)
    {
        _importProviders[provider.ProviderName] = provider;
    }

    public IEnumerable<IPasswordImportProvider> GetAvailableProviders()
    {
        return _importProviders.Values;
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
