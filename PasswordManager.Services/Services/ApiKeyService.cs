using Microsoft.EntityFrameworkCore;
using PasswordManager.DAL;
using PasswordManager.Models;
using PasswordManager.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace PasswordManager.Services.Services
{
    public class ApiKeyService : IApiKeyService
    {
        private readonly PasswordManagerDbContext _context;
        
        public ApiKeyService(PasswordManagerDbContext context)
        {
            _context = context;
        }

        public async Task<List<ApiKey>> GetUserApiKeysAsync(string userId)
        {
            return await _context.ApiKeys
                .Where(k => k.UserId == userId && k.IsActive)
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
    }
}
