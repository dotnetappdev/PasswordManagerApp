using PasswordManager.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PasswordManager.Services.Interfaces
{
    public interface ICollectionService
    {
        Task<List<Collection>> GetAllAsync();
        Task<Collection?> GetByIdAsync(int id);
        Task<Collection> CreateAsync(Collection collection);
        Task<Collection> UpdateAsync(Collection collection);
        Task DeleteAsync(int id);
        Task<Collection?> GetDefaultCollectionAsync();
        Task SetAsDefaultAsync(int id);
    }
}
