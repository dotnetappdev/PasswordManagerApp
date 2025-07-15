using PasswordManager.Models;

namespace PasswordManager.Services.Interfaces
{
    public interface IApiKeyService
    {
        Task<List<ApiKey>> GetUserApiKeysAsync(string userId);
        Task<ApiKey> CreateApiKeyAsync(string name, string userId);
        Task<bool> DeleteApiKeyAsync(Guid keyId, string userId);
        Task<ApiKey?> ValidateApiKeyAsync(string keyValue);
        Task UpdateLastUsedAsync(Guid keyId);
    }
}
