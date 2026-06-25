using PasswordManager.Models;
using PasswordManager.Models.DTOs;
using PasswordManager.Models.Configuration;

namespace PasswordManager.Services.Interfaces
{
    public interface IApiKeyService
    {
        Task<List<ApiKey>> GetUserApiKeysAsync(string userId);
        Task<ApiKey> CreateApiKeyAsync(string name, string userId);
        Task<ApiKeyResponseDto> CreateProviderApiKeyAsync(ApiKeyRequestDto request, string userId);
        Task<bool> DeleteApiKeyAsync(Guid keyId, string userId);
        Task<ApiKey?> ValidateApiKeyAsync(string keyValue);
        Task UpdateLastUsedAsync(Guid keyId);
        Task<List<ApiKey>> GetUserApiKeysByProviderAsync(string userId, DatabaseProvider? provider);
    }
}
