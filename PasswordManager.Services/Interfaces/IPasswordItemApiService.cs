using PasswordManager.Models.DTOs;

namespace PasswordManager.Services.Interfaces;

public interface IPasswordItemApiService
{
    Task<IEnumerable<PasswordItemDto>> GetAllAsync();
    Task<PasswordItemDto?> GetByIdAsync(int id);
    Task<IEnumerable<PasswordItemDto>> GetByCollectionIdAsync(int collectionId);
    Task<IEnumerable<PasswordItemDto>> GetByCategoryIdAsync(int categoryId);
    Task<IEnumerable<PasswordItemDto>> GetByTagIdAsync(int tagId);
    Task<IEnumerable<PasswordItemDto>> SearchAsync(string searchTerm);
    Task<PasswordItemDto> CreateAsync(CreatePasswordItemDto createDto);
    Task<PasswordItemDto?> UpdateAsync(int id, UpdatePasswordItemDto updateDto);
    Task<bool> DeleteAsync(int id);
    Task<bool> SoftDeleteAsync(int id);
    Task<bool> RestoreAsync(int id);
    Task<bool> ToggleFavoriteAsync(int id);
    Task<bool> ArchiveAsync(int id);
    Task<bool> UnarchiveAsync(int id);
}
