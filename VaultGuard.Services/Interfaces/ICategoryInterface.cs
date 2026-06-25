using VaultGuard.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VaultGuard.Services.Interfaces
{
    public interface ICategoryInterface
    {
        Task<List<Category>> GetAllAsync();
        Task<Category?> GetByIdAsync(int id);
        Task<Category> CreateAsync(Category category);
        Task<Category> UpdateAsync(Category category);
        Task DeleteAsync(int id);
        Task<bool> HasPasswordItemsAsync(int categoryId);
        Task<int> GetPasswordItemCountAsync(int categoryId);
    }
}
