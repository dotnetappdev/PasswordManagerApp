using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using VaultGuard.Components.Shared.Services;

namespace VaultGuard.Web.Services;

/// <summary>
/// "Remember this device" cache for Blazor Server. The master password is stored in the browser's
/// localStorage but encrypted with the server's data-protection keys via ProtectedLocalStorage, so
/// the ciphertext is useless without the server. Lets a 2FA-enabled account sign in code-only on a
/// remembered browser. Calls only work during interactive rendering (ProfileLogin is interactive).
/// </summary>
public class WebMasterKeyCacheService : IMasterKeyCacheService
{
    private readonly ProtectedLocalStorage _storage;

    public WebMasterKeyCacheService(ProtectedLocalStorage storage)
    {
        _storage = storage;
    }

    private static string Key(string userId) => $"vg_mk_{userId}";

    public async Task<bool> HasCachedAsync(string userId) => await GetCachedAsync(userId) is not null;

    public async Task<string?> GetCachedAsync(string userId)
    {
        try
        {
            var result = await _storage.GetAsync<string>(Key(userId));
            return result.Success ? result.Value : null;
        }
        catch
        {
            return null;
        }
    }

    public async Task CacheAsync(string userId, string masterPassword)
    {
        try { await _storage.SetAsync(Key(userId), masterPassword); }
        catch { }
    }

    public async Task ForgetAsync(string userId)
    {
        try { await _storage.DeleteAsync(Key(userId)); }
        catch { }
    }
}
