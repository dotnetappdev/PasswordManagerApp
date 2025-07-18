using PasswordManager.Models;

namespace PasswordManager.Services.Interfaces;

public interface IPasswordItemService
{
    Task<IEnumerable<PasswordItem>> GetAllAsync();
    Task<IEnumerable<PasswordItem>> GetByTypeAsync(ItemType type);
    Task<PasswordItem?> GetByIdAsync(int id);
    Task<PasswordItem> CreateAsync(PasswordItem item);
    Task<PasswordItem> UpdateAsync(PasswordItem item);
    Task DeleteAsync(int id);
    Task<IEnumerable<PasswordItem>> SearchAsync(string searchTerm);
    Task<IEnumerable<PasswordItem>> GetFavoritesAsync();
    Task<IEnumerable<PasswordItem>> GetByTagAsync(string tagName);
    Task<IEnumerable<PasswordItem>> GetRecentlyUsedAsync(int count = 10);
    Task<IEnumerable<PasswordItem>> GetArchivedAsync();
    Task<bool> ExistsAsync(int id);
}
