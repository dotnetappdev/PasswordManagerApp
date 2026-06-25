using VaultGuard.Components.Shared.Services;

namespace VaultGuard.App.Services;

/// <summary>
/// "Remember this device" cache for MAUI, backed by the platform's SecureStorage
/// (Keychain on iOS/macOS, Keystore-protected on Android, DPAPI on Windows). Lets a
/// 2FA-enabled account sign in with just the authenticator code on a trusted device.
/// </summary>
public class MauiMasterKeyCacheService : IMasterKeyCacheService
{
    private static string Key(string userId) => $"vg_mk_{userId}";

    public async Task<bool> HasCachedAsync(string userId) => await GetCachedAsync(userId) is not null;

    public async Task<string?> GetCachedAsync(string userId)
    {
        try { return await SecureStorage.GetAsync(Key(userId)); }
        catch { return null; }
    }

    public async Task CacheAsync(string userId, string masterPassword)
    {
        try { await SecureStorage.SetAsync(Key(userId), masterPassword); }
        catch { }
    }

    public Task ForgetAsync(string userId)
    {
        try { SecureStorage.Remove(Key(userId)); }
        catch { }
        return Task.CompletedTask;
    }
}
