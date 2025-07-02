using Microsoft.EntityFrameworkCore;
using PasswordManager.API.Helpers;
using PasswordManager.API.Interfaces;
using PasswordManager.DAL;
using PasswordManager.Models;
using PasswordManager.Models.DTOs;

namespace PasswordManager.API.Services;

public class CategoryApiService : ICategoryApiService
{
    private readonly PasswordManagerDbContext _context;
    private readonly ILogger<CategoryApiService> _logger;

    public CategoryApiService(
        PasswordManagerDbContext context,
        ILogger<CategoryApiService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<CategoryDto>> GetAllAsync()
    {
        try
        {
            var categories = await _context.Categories
                .Include(c => c.Collection)
                .ToListAsync();

            return categories.Select(c => c.ToDto()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all categories");
            throw;
        }
    }

    public async Task<CategoryDto?> GetByIdAsync(int id)
    {
        try
        {
            var category = await _context.Categories
                .Include(c => c.Collection)
                .FirstOrDefaultAsync(c => c.Id == id);

            return category?.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category with ID {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<CategoryDto>> GetByCollectionIdAsync(int collectionId)
    {
        try
        {
            var categories = await _context.Categories
                .Include(c => c.Collection)
                .Where(c => c.CollectionId == collectionId)
                .ToListAsync();

            return categories.Select(c => c.ToDto()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories for collection {CollectionId}", collectionId);
            throw;
        }
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto createDto)
    {
        try
        {
            var category = createDto.ToEntity();
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(category.Id) ?? throw new InvalidOperationException("Failed to retrieve created category");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            throw;
        }
    }

    public async Task<CategoryDto?> UpdateAsync(int id, UpdateCategoryDto updateDto)
    {
        try
        {
            var existingCategory = await _context.Categories.FindAsync(id);
            if (existingCategory == null)
                return null;

            existingCategory.UpdateFromDto(updateDto);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category with ID {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return false;

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category with ID {Id}", id);
            throw;
        }
    }
}

public class CollectionApiService : ICollectionApiService
{
    private readonly PasswordManagerDbContext _context;
    private readonly ILogger<CollectionApiService> _logger;

    public CollectionApiService(
        PasswordManagerDbContext context,
        ILogger<CollectionApiService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<CollectionDto>> GetAllAsync()
    {
        try
        {
            var collections = await _context.Collections
                .Include(c => c.ParentCollection)
                .Include(c => c.Children)
                .Include(c => c.Categories)
                .ToListAsync();

            return collections.Select(c => c.ToDto()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all collections");
            throw;
        }
    }

    public async Task<CollectionDto?> GetByIdAsync(int id)
    {
        try
        {
            var collection = await _context.Collections
                .Include(c => c.ParentCollection)
                .Include(c => c.Children)
                .Include(c => c.Categories)
                .FirstOrDefaultAsync(c => c.Id == id);

            return collection?.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collection with ID {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<CollectionDto>> GetHierarchyAsync()
    {
        try
        {
            var rootCollections = await _context.Collections
                .Include(c => c.Children)
                    .ThenInclude(child => child.Children)
                .Include(c => c.Categories)
                .Where(c => c.ParentCollectionId == null)
                .ToListAsync();

            return rootCollections.Select(c => c.ToDto()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collection hierarchy");
            throw;
        }
    }

    public async Task<CollectionDto> CreateAsync(CreateCollectionDto createDto)
    {
        try
        {
            var collection = createDto.ToEntity();
            _context.Collections.Add(collection);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(collection.Id) ?? throw new InvalidOperationException("Failed to retrieve created collection");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating collection");
            throw;
        }
    }

    public async Task<CollectionDto?> UpdateAsync(int id, UpdateCollectionDto updateDto)
    {
        try
        {
            var existingCollection = await _context.Collections.FindAsync(id);
            if (existingCollection == null)
                return null;

            existingCollection.UpdateFromDto(updateDto);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating collection with ID {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var collection = await _context.Collections.FindAsync(id);
            if (collection == null)
                return false;

            _context.Collections.Remove(collection);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting collection with ID {Id}", id);
            throw;
        }
    }
}

public class TagApiService : ITagApiService
{
    private readonly PasswordManagerDbContext _context;
    private readonly ILogger<TagApiService> _logger;

    public TagApiService(
        PasswordManagerDbContext context,
        ILogger<TagApiService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<TagDto>> GetAllAsync()
    {
        try
        {
            var tags = await _context.Tags.ToListAsync();
            return tags.Select(t => t.ToDto()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all tags");
            throw;
        }
    }

    public async Task<TagDto?> GetByIdAsync(int id)
    {
        try
        {
            var tag = await _context.Tags.FindAsync(id);
            return tag?.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tag with ID {Id}", id);
            throw;
        }
    }

    public async Task<TagDto> CreateAsync(CreateTagDto createDto)
    {
        try
        {
            var tag = createDto.ToEntity();
            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(tag.Id) ?? throw new InvalidOperationException("Failed to retrieve created tag");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tag");
            throw;
        }
    }

    public async Task<TagDto?> UpdateAsync(int id, UpdateTagDto updateDto)
    {
        try
        {
            var existingTag = await _context.Tags.FindAsync(id);
            if (existingTag == null)
                return null;

            existingTag.UpdateFromDto(updateDto);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tag with ID {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var tag = await _context.Tags.FindAsync(id);
            if (tag == null)
                return false;

            _context.Tags.Remove(tag);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tag with ID {Id}", id);
            throw;
        }
    }
}
