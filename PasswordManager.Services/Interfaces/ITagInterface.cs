using PasswordManager.Models;

namespace PasswordManager.Interfaces;

public interface ITagService
{
    Task<IEnumerable<Tag>> GetAllAsync();
    Task<Tag?> GetByIdAsync(int id);
    Task<Tag?> GetByNameAsync(string name);
    Task<Tag> CreateAsync(Tag tag);
    Task<Tag> UpdateAsync(Tag tag);
    Task DeleteAsync(int id);
    Task<IEnumerable<Tag>> GetSystemTagsAsync();
    Task<IEnumerable<Tag>> GetUserTagsAsync();
    Task<bool> ExistsAsync(string name);
}
