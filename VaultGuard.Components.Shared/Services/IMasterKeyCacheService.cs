namespace VaultGuard.Components.Shared.Services;

/// <summary>
/// Per-device "remember this device" cache for the master password, used so a 2FA-enabled
/// account can sign in with just the authenticator code (the vault key is derived from the
/// cached password). Implementations must protect the value at rest:
///   • MAUI  → platform SecureStorage (Keychain / Keystore / DPAPI).
///   • Web   → ProtectedLocalStorage (encrypted with the server's data-protection keys).
/// Mirrors the WPF DPAPI MasterPasswordCacheService.
/// </summary>
public interface IMasterKeyCacheService
{
    Task<bool> HasCachedAsync(string userId);
    Task<string?> GetCachedAsync(string userId);
    Task CacheAsync(string userId, string masterPassword);
    Task ForgetAsync(string userId);
}
