using PasswordManager.Models.DTOs;

namespace PasswordManager.Services.Interfaces;

public interface ICategoryApiService
{
    Task<IEnumerable<CategoryDto>> GetAllAsync();
    Task<CategoryDto?> GetByIdAsync(int id);
    Task<IEnumerable<CategoryDto>> GetByCollectionIdAsync(int collectionId);
    Task<CategoryDto> CreateAsync(CreateCategoryDto createDto);
    Task<CategoryDto?> UpdateAsync(int id, UpdateCategoryDto updateDto);
    Task<bool> DeleteAsync(int id);
}

public interface ICollectionApiService
{
    Task<IEnumerable<CollectionDto>> GetAllAsync();
    Task<CollectionDto?> GetByIdAsync(int id);
    Task<IEnumerable<CollectionDto>> GetHierarchyAsync();
    Task<CollectionDto> CreateAsync(CreateCollectionDto createDto);
    Task<CollectionDto?> UpdateAsync(int id, UpdateCollectionDto updateDto);
    Task<bool> DeleteAsync(int id);
}

public interface ITagApiService
{
    Task<IEnumerable<TagDto>> GetAllAsync();
    Task<TagDto?> GetByIdAsync(int id);
    Task<TagDto> CreateAsync(CreateTagDto createDto);
    Task<TagDto?> UpdateAsync(int id, UpdateTagDto updateDto);
    Task<bool> DeleteAsync(int id);
}
