using PasswordManager.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PasswordManager.Services.Interfaces
{
    public interface IVaultService
    {
        Task<List<Vault>> GetAllAsync();
        Task<Vault?> GetByIdAsync(int id);
        Task<Vault> CreateAsync(Vault vault);
        Task<Vault> UpdateAsync(Vault vault);
        Task DeleteAsync(int id);
        Task<Vault?> GetDefaultVaultAsync();
        Task SetAsDefaultAsync(int id);
        Task<Vault> GetOrCreateDefaultVaultAsync(string userId);
        Task<List<PasswordItem>> GetItemsAsync(int vaultId);
        Task MoveItemToVaultAsync(int passwordItemId, int targetVaultId);
        Task SeedDefaultVaultsAsync(string userId);

        // Resolves the collection that new items should be attached to so they "belong" to this vault.
        Task<int?> GetDefaultCollectionIdAsync(int vaultId);
    }
}
