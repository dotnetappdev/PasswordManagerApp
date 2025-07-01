using PasswordManager.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PasswordManager.Services.Interfaces
{
    public interface ICategoryInterface
    {
        Task<List<Category>> GetAllAsync();
        Task<Category?> GetByIdAsync(int id);
        Task<Category> CreateAsync(Category category);
        Task<Category> UpdateAsync(Category category);
        Task DeleteAsync(int id);
    }
}
