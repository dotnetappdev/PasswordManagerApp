using VaultGuard.Models.DTOs;

namespace VaultGuard.Services.Interfaces;

public interface IPasswordItemApiService
{
    Task<IEnumerable<PasswordItemDto>> GetAllAsync();
    Task<PasswordItemDto?> GetByIdAsync(int id);
    Task<IEnumerable<PasswordItemDto>> GetByCollectionIdAsync(int collectionId);
    Task<IEnumerable<PasswordItemDto>> GetByCategoryIdAsync(int categoryId);
    Task<IEnumerable<PasswordItemDto>> GetByTagIdAsync(int tagId);
    Task<IEnumerable<PasswordItemDto>> SearchAsync(string searchTerm);
    Task<IEnumerable<PasswordItemDto>> SearchByUrlAsync(string url);
    Task<bool> UpdateTotpSecretAsync(int id, string otpauthUri);
    Task<PasswordItemDto> CreateAsync(CreatePasswordItemDto createDto, string userId);
    Task<PasswordItemDto?> UpdateAsync(int id, UpdatePasswordItemDto updateDto);
    Task<bool> DeleteAsync(int id);
    Task<bool> SoftDeleteAsync(int id);
    Task<bool> RestoreAsync(int id);
    Task<bool> ToggleFavoriteAsync(int id);
    Task<bool> ArchiveAsync(int id);
    Task<bool> UnarchiveAsync(int id);

    /// <summary>
    /// Decrypts the caller's own item with the vault key derived from <paramref name="masterPassword"/> and
    /// returns its secrets by field name (e.g. "Card.CVV"). Null if the item isn't the caller's. Throws
    /// VaultKeyRequiredException for a wrong master password.
    /// </summary>
    Task<IReadOnlyDictionary<string, string>?> RevealSecretsAsync(int id, string masterPassword);
}
