using PasswordManager.Models;
using PasswordManager.Models.Configuration;
using PasswordManager.Services.Interfaces;
using PasswordManager.Crypto.Interfaces;
using Xunit;

namespace PasswordManager.Tests.UI;

/// <summary>
/// Mock implementation of IDatabaseConfigurationService for testing
/// </summary>
public class MockDatabaseConfigurationService : IDatabaseConfigurationService
{
    private DatabaseConfiguration? _configuration;

    public Task<DatabaseConfiguration> GetConfigurationAsync()
    {
        return Task.FromResult(_configuration ?? new DatabaseConfiguration
        {
            Provider = DatabaseProvider.Sqlite,
            IsFirstRun = true,
            Sqlite = new SqliteConfig { DatabasePath = "test.db" }
        });
    }

    public Task SaveConfigurationAsync(DatabaseConfiguration configuration)
    {
        _configuration = configuration;
        return Task.CompletedTask;
    }

    public Task<bool> IsFirstRunAsync()
    {
        return Task.FromResult(_configuration?.IsFirstRun ?? true);
    }

    public string BuildConnectionString(DatabaseConfiguration configuration)
    {
        return configuration.Provider switch
        {
            DatabaseProvider.Sqlite => $"Data Source={configuration.Sqlite?.DatabasePath ?? "test.db"}",
            _ => throw new NotSupportedException($"Provider {configuration.Provider} not supported in mock")
        };
    }

    public Task<(bool Success, string ErrorMessage)> TestConnectionAsync(DatabaseConfiguration configuration)
    {
        // Always return success for testing
        return Task.FromResult((true, string.Empty));
    }

    public Task<string> EncryptPasswordAsync(string password)
    {
        // Simple base64 encoding for testing
        return Task.FromResult(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password)));
    }

    public Task<string> DecryptPasswordAsync(string encryptedPassword)
    {
        // Simple base64 decoding for testing
        return Task.FromResult(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encryptedPassword)));
    }

    public DatabaseConfiguration GetDefaultConfiguration()
    {
        return new DatabaseConfiguration
        {
            Provider = DatabaseProvider.Sqlite,
            IsFirstRun = true,
            Sqlite = new SqliteConfig { DatabasePath = "test.db" }
        };
    }

    public bool ShouldShowDatabaseSelection()
    {
        return true; // Always show for testing
    }
}

/// <summary>
/// Mock implementation of IAuthService for testing
/// </summary>
public class MockAuthService : IAuthService
{
    private string? _masterPasswordHash;
    private string? _hint;
    private ApplicationUser? _currentUser;
    private bool _isAuthenticated;

    public bool IsAuthenticated => _isAuthenticated;
    public ApplicationUser? CurrentUser => _currentUser;

    public Task<bool> SetupMasterPasswordAsync(string masterPassword, string hint = "")
    {
        _masterPasswordHash = BCrypt.Net.BCrypt.HashPassword(masterPassword);
        _hint = hint;
        _currentUser = new ApplicationUser 
        { 
            Id = "test-user-id", 
            UserName = "testuser", 
            Email = "test@example.com" 
        };
        _isAuthenticated = true;
        return Task.FromResult(true);
    }

    public Task<bool> AuthenticateAsync(string masterPassword)
    {
        if (_masterPasswordHash == null)
            return Task.FromResult(false);

        var isValid = BCrypt.Net.BCrypt.Verify(masterPassword, _masterPasswordHash);
        if (isValid)
        {
            _isAuthenticated = true;
            _currentUser ??= new ApplicationUser 
            { 
                Id = "test-user-id", 
                UserName = "testuser", 
                Email = "test@example.com" 
            };
        }
        return Task.FromResult(isValid);
    }

    public Task<bool> CheckAuthenticationStatusAsync()
    {
        return Task.FromResult(_isAuthenticated);
    }

    public Task<bool> IsAuthenticatedAsync()
    {
        return Task.FromResult(_isAuthenticated);
    }

    public void ClearAuthentication()
    {
        _isAuthenticated = false;
        _currentUser = null;
    }

    public void ClearMasterPassword()
    {
        _masterPasswordHash = null;
        _hint = null;
        ClearAuthentication();
    }

    // Additional interface methods (simplified implementations)
    public Task<bool> LoginAsync(string email, string password) => Task.FromResult(false);
    public Task LogoutAsync() { ClearAuthentication(); return Task.CompletedTask; }
    public Task<bool> RegisterAsync(string email, string password, string confirmPassword) => Task.FromResult(false);
    public Task<bool> ChangePasswordAsync(string currentPassword, string newPassword) => Task.FromResult(false);
    public Task<bool> ForgotPasswordAsync(string email) => Task.FromResult(false);
    public Task<bool> ResetPasswordAsync(string token, string newPassword) => Task.FromResult(false);
    public Task<string?> GetPasswordHintAsync() => Task.FromResult(_hint);
    public Task<bool> HasMasterPasswordAsync() => Task.FromResult(_masterPasswordHash != null);
    public Task<bool> ValidateMasterPasswordAsync(string masterPassword) => AuthenticateAsync(masterPassword);
    public Task<bool> IsFirstTimeSetupAsync() => Task.FromResult(_masterPasswordHash == null);
    public Task<string> GetMasterPasswordHintAsync() => Task.FromResult(_hint ?? string.Empty);
}

/// <summary>
/// Mock implementation of IPasswordItemService for testing
/// </summary>
public class MockPasswordItemService : IPasswordItemService
{
    private readonly List<PasswordItem> _items = new();
    private int _nextId = 1;

    public Task<IEnumerable<PasswordItem>> GetAllAsync()
    {
        return Task.FromResult(_items.Where(i => !i.IsDeleted).AsEnumerable());
    }

    public Task<IEnumerable<PasswordItem>> GetByTypeAsync(ItemType type)
    {
        return Task.FromResult(_items.Where(i => i.Type == type && !i.IsDeleted).AsEnumerable());
    }

    public Task<PasswordItem?> GetByIdAsync(int id)
    {
        return Task.FromResult(_items.FirstOrDefault(i => i.Id == id && !i.IsDeleted));
    }

    public Task<PasswordItem> CreateAsync(PasswordItem item)
    {
        item.Id = _nextId++;
        item.CreatedAt = DateTime.UtcNow;
        item.LastModified = DateTime.UtcNow;
        _items.Add(item);
        return Task.FromResult(item);
    }

    public Task<PasswordItem> UpdateAsync(PasswordItem item)
    {
        var existing = _items.FirstOrDefault(i => i.Id == item.Id);
        if (existing != null)
        {
            var index = _items.IndexOf(existing);
            item.LastModified = DateTime.UtcNow;
            _items[index] = item;
        }
        return Task.FromResult(item);
    }

    public Task DeleteAsync(int id)
    {
        var item = _items.FirstOrDefault(i => i.Id == id);
        if (item != null)
        {
            item.IsDeleted = true;
            item.LastModified = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    public Task<IEnumerable<PasswordItem>> SearchAsync(string searchTerm)
    {
        var results = _items.Where(i => 
            !i.IsDeleted && 
            (i.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
             (i.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false)));
        return Task.FromResult(results.AsEnumerable());
    }

    public Task<IEnumerable<PasswordItem>> GetFavoritesAsync()
    {
        return Task.FromResult(_items.Where(i => i.IsFavorite && !i.IsDeleted).AsEnumerable());
    }

    public Task<IEnumerable<PasswordItem>> GetByTagAsync(string tagName)
    {
        return Task.FromResult(_items.Where(i => 
            !i.IsDeleted && 
            i.Tags.Any(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase))).AsEnumerable());
    }

    public Task<IEnumerable<PasswordItem>> GetRecentlyUsedAsync(int count = 10)
    {
        return Task.FromResult(_items.Where(i => !i.IsDeleted)
            .OrderByDescending(i => i.LastModified)
            .Take(count)
            .AsEnumerable());
    }

    public Task<IEnumerable<PasswordItem>> GetArchivedAsync()
    {
        return Task.FromResult(_items.Where(i => i.IsArchived && !i.IsDeleted).AsEnumerable());
    }

    public Task<bool> ExistsAsync(int id)
    {
        return Task.FromResult(_items.Any(i => i.Id == id && !i.IsDeleted));
    }
}

/// <summary>
/// Mock implementation of ICollectionService for testing
/// </summary>
public class MockCollectionService : ICollectionService
{
    private readonly List<Collection> _collections = new();
    private int _nextId = 1;

    public Task<List<Collection>> GetAllAsync()
    {
        return Task.FromResult(_collections.ToList());
    }

    public Task<Collection?> GetByIdAsync(int id)
    {
        return Task.FromResult(_collections.FirstOrDefault(c => c.Id == id));
    }

    public Task<Collection> CreateAsync(Collection collection)
    {
        collection.Id = _nextId++;
        collection.CreatedAt = DateTime.UtcNow;
        collection.LastModified = DateTime.UtcNow;
        _collections.Add(collection);
        return Task.FromResult(collection);
    }

    public Task<Collection> UpdateAsync(Collection collection)
    {
        var existing = _collections.FirstOrDefault(c => c.Id == collection.Id);
        if (existing != null)
        {
            var index = _collections.IndexOf(existing);
            collection.LastModified = DateTime.UtcNow;
            _collections[index] = collection;
        }
        return Task.FromResult(collection);
    }

    public Task DeleteAsync(int id)
    {
        var collection = _collections.FirstOrDefault(c => c.Id == id);
        if (collection != null)
        {
            _collections.Remove(collection);
        }
        return Task.CompletedTask;
    }

    public Task<Collection?> GetDefaultCollectionAsync()
    {
        return Task.FromResult(_collections.FirstOrDefault(c => c.IsDefault));
    }

    public Task SetAsDefaultAsync(int id)
    {
        // Clear existing default
        foreach (var collection in _collections)
        {
            collection.IsDefault = false;
        }

        // Set new default
        var target = _collections.FirstOrDefault(c => c.Id == id);
        if (target != null)
        {
            target.IsDefault = true;
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Mock implementation of IPasswordCryptoService for testing
/// </summary>
public class MockPasswordCryptoService : IPasswordCryptoService
{
    public string HashMasterPassword(string masterPassword, byte[] userSalt, int iterations = 600000)
    {
        return BCrypt.Net.BCrypt.HashPassword(masterPassword);
    }

    public bool VerifyMasterPassword(string masterPassword, string storedHash, byte[] userSalt, int iterations = 600000)
    {
        return BCrypt.Net.BCrypt.Verify(masterPassword, storedHash);
    }

    public byte[] GenerateUserSalt()
    {
        var salt = new byte[16];
        Random.Shared.NextBytes(salt);
        return salt;
    }

    public EncryptedPasswordData EncryptPassword(string password, string masterPassword, byte[] userSalt)
    {
        // Simple encryption for testing (not secure, just for testing)
        return new EncryptedPasswordData
        {
            EncryptedPassword = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password)),
            Nonce = Convert.ToBase64String(new byte[12]),
            AuthenticationTag = Convert.ToBase64String(new byte[16])
        };
    }

    public string DecryptPassword(EncryptedPasswordData encryptedPasswordData, string masterPassword, byte[] userSalt)
    {
        // Simple decryption for testing
        return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encryptedPasswordData.EncryptedPassword));
    }

    public string CreateMasterPasswordHash(string masterPassword, byte[] userSalt, int iterations = 600000)
    {
        return BCrypt.Net.BCrypt.HashPassword(masterPassword);
    }

    public string CreateAuthHash(byte[] masterKey, string masterPassword)
    {
        return BCrypt.Net.BCrypt.HashPassword(masterPassword);
    }

    public byte[] DeriveMasterKey(string masterPassword, byte[] userSalt)
    {
        // Simple key derivation for testing
        var key = new byte[32];
        Random.Shared.NextBytes(key);
        return key;
    }

    public EncryptedPasswordData EncryptPasswordWithKey(string password, byte[] masterKey)
    {
        return new EncryptedPasswordData
        {
            EncryptedPassword = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password)),
            Nonce = Convert.ToBase64String(new byte[12]),
            AuthenticationTag = Convert.ToBase64String(new byte[16])
        };
    }

    public string DecryptPasswordWithKey(EncryptedPasswordData encryptedPasswordData, byte[] masterKey)
    {
        return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encryptedPasswordData.EncryptedPassword));
    }
}

/// <summary>
/// Mock implementation of IVaultSessionService for testing
/// </summary>
public class MockVaultSessionService : IVaultSessionService
{
    private readonly Dictionary<string, object> _sessionData = new();
    private readonly Dictionary<string, string> _sessions = new();
    private readonly Dictionary<string, byte[]> _masterKeys = new();

    public Task<T?> GetAsync<T>(string key) where T : class
    {
        _sessionData.TryGetValue(key, out var value);
        return Task.FromResult(value as T);
    }

    public Task SetAsync<T>(string key, T value) where T : class
    {
        _sessionData[key] = value;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _sessionData.Remove(key);
        return Task.CompletedTask;
    }

    public Task ClearSessionAsync()
    {
        _sessionData.Clear();
        _sessions.Clear();
        _masterKeys.Clear();
        return Task.CompletedTask;
    }

    public Task<bool> IsActiveAsync()
    {
        return Task.FromResult(_sessionData.Count > 0);
    }

    public string InitializeSession(string userId, byte[] masterKey)
    {
        var sessionId = Guid.NewGuid().ToString();
        _sessions[sessionId] = userId;
        _masterKeys[sessionId] = masterKey;
        return sessionId;
    }

    public string? GetSessionUserId(string sessionId)
    {
        _sessions.TryGetValue(sessionId, out var userId);
        return userId;
    }

    public void ClearSession(string sessionId)
    {
        _sessions.Remove(sessionId);
        _masterKeys.Remove(sessionId);
    }

    public string EncryptPassword(string password, string sessionId)
    {
        // Simple encryption for testing
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));
    }

    public string DecryptPassword(string encryptedPassword, string sessionId)
    {
        // Simple decryption for testing
        return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encryptedPassword));
    }

    public bool IsVaultUnlocked(string sessionId)
    {
        return _sessions.ContainsKey(sessionId);
    }

    public void LockVault(string sessionId)
    {
        _masterKeys.Remove(sessionId);
    }

    public bool UnlockVault(string sessionId, byte[] masterKey)
    {
        _masterKeys[sessionId] = masterKey;
        return true;
    }

    public byte[]? GetMasterKey(string sessionId)
    {
        _masterKeys.TryGetValue(sessionId, out var masterKey);
        return masterKey;
    }
}