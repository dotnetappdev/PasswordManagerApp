using Microsoft.EntityFrameworkCore;
using PasswordManager.DAL;
using PasswordManager.Models;
using PasswordManager.Models.DTOs;
using PasswordManager.Models.Configuration;
using PasswordManager.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PasswordManager.Services.Services
{
    public class ApiKeyService : IApiKeyService
    {
        private readonly PasswordManagerDbContext _context;
        private readonly IDatabaseConfigurationService _databaseConfigService;
        
        public ApiKeyService(PasswordManagerDbContext context, IDatabaseConfigurationService databaseConfigService)
        {
            _context = context;
            _databaseConfigService = databaseConfigService;
        }

        public async Task<List<ApiKey>> GetUserApiKeysAsync(string userId)
        {
            return await _context.ApiKeys
                .Where(k => k.UserId == userId && k.IsActive)
                .OrderByDescending(k => k.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ApiKey>> GetUserApiKeysByProviderAsync(string userId, DatabaseProvider? provider)
        {
            return await _context.ApiKeys
                .Where(k => k.UserId == userId && k.IsActive && k.Provider == provider)
                .OrderByDescending(k => k.CreatedAt)
                .ToListAsync();
        }

        public async Task<ApiKey> CreateApiKeyAsync(string name, string userId)
        {
            // Generate a secure API key
            var keyValue = GenerateSecureApiKey();
            var keyHash = HashApiKey(keyValue);
            
            var apiKey = new ApiKey
            {
                Id = Guid.NewGuid(),
                Name = name,
                KeyHash = keyHash,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.ApiKeys.Add(apiKey);
            await _context.SaveChangesAsync();

            // Return the API key with the unhashed value for display
            apiKey.KeyHash = keyValue; // Temporarily store for return
            return apiKey;
        }

        public async Task<ApiKeyResponseDto> CreateProviderApiKeyAsync(ApiKeyRequestDto request, string userId)
        {
            // Generate a secure API key
            var keyValue = GenerateSecureApiKey();
            var keyHash = HashApiKey(keyValue);
            
            // Get existing database configuration if requested
            string? providerConfig = null;
            string? apiUrl = null;
            
            if (request.UseExistingConfig && request.Provider.HasValue)
            {
                var dbConfig = await _databaseConfigService.GetConfigurationAsync();
                var providerConfigData = GetProviderConfig(dbConfig, request.Provider.Value);
                if (providerConfigData != null)
                {
                    providerConfig = JsonSerializer.Serialize(providerConfigData);
                    apiUrl = GetProviderApiUrl(dbConfig, request.Provider.Value);
                }
            }
            
            var apiKey = new ApiKey
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                KeyHash = keyHash,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                Provider = request.Provider,
                ProviderConfig = providerConfig
            };

            _context.ApiKeys.Add(apiKey);
            await _context.SaveChangesAsync();

            return new ApiKeyResponseDto
            {
                Id = apiKey.Id,
                Name = apiKey.Name,
                ApiKey = keyValue,
                Provider = apiKey.Provider,
                CreatedAt = apiKey.CreatedAt,
                ProviderDisplayName = GetProviderDisplayName(apiKey.Provider),
                ApiUrl = apiUrl
            };
        }

        public async Task<bool> DeleteApiKeyAsync(Guid keyId, string userId)
        {
            var apiKey = await _context.ApiKeys
                .FirstOrDefaultAsync(k => k.Id == keyId && k.UserId == userId);
            
            if (apiKey == null)
                return false;

            apiKey.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ApiKey?> ValidateApiKeyAsync(string keyValue)
        {
            var keyHash = HashApiKey(keyValue);
            
            var apiKey = await _context.ApiKeys
                .Include(k => k.User)
                .FirstOrDefaultAsync(k => k.KeyHash == keyHash && k.IsActive);

            return apiKey;
        }

        public async Task UpdateLastUsedAsync(Guid keyId)
        {
            var apiKey = await _context.ApiKeys
                .FirstOrDefaultAsync(k => k.Id == keyId);
            
            if (apiKey != null)
            {
                apiKey.LastUsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        private string GenerateSecureApiKey()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[32];
                rng.GetBytes(bytes);
                return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
            }
        }

        private string HashApiKey(string keyValue)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyValue));
                return Convert.ToBase64String(hashedBytes);
            }
        }
        
        private object? GetProviderConfig(DatabaseConfiguration dbConfig, DatabaseProvider provider)
        {
            return provider switch
            {
                DatabaseProvider.SqlServer => dbConfig.SqlServer,
                DatabaseProvider.MySql => dbConfig.MySql,
                DatabaseProvider.PostgreSql => dbConfig.PostgreSql,
                DatabaseProvider.Supabase => dbConfig.Supabase,
                DatabaseProvider.Sqlite => dbConfig.Sqlite,
                _ => null
            };
        }
        
        private string? GetProviderApiUrl(DatabaseConfiguration dbConfig, DatabaseProvider provider)
        {
            var providerApiUrl = provider switch
            {
                DatabaseProvider.SqlServer => dbConfig.SqlServer?.ApiUrl,
                DatabaseProvider.MySql => dbConfig.MySql?.ApiUrl,
                DatabaseProvider.PostgreSql => dbConfig.PostgreSql?.ApiUrl,
                DatabaseProvider.Supabase => dbConfig.Supabase?.ApiUrl,
                DatabaseProvider.Sqlite => dbConfig.Sqlite?.ApiUrl,
                _ => null
            };
            
            return providerApiUrl ?? dbConfig.ApiUrl;
        }
        
        private string? GetProviderDisplayName(DatabaseProvider? provider)
        {
            return provider switch
            {
                DatabaseProvider.SqlServer => "SQL Server",
                DatabaseProvider.MySql => "MySQL",
                DatabaseProvider.PostgreSql => "PostgreSQL",
                DatabaseProvider.Supabase => "Supabase",
                DatabaseProvider.Sqlite => "SQLite",
                _ => "General"
            };
        }
    }
}
