using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.Services.Security;

/// <summary>Who the vault data being read or written belongs to.</summary>
public interface ICurrentVaultUser
{
    /// <summary>The signed-in user's id, or null when nobody is signed in (callers then see no data).</summary>
    Task<string?> GetUserIdAsync();
}

/// <summary>
/// For the apps (WPF, MAUI, Blazor Web): the owner of the unlocked vault session. That is the strongest
/// binding available — you can only ever see the vault you unlocked — and it doesn't depend on which DI
/// scope a page happens to resolve its auth service from. Falls back to the auth service's current user
/// (e.g. API-token sign-ins that have no local vault session). Never guesses: no session and no user
/// means no data.
/// </summary>
public sealed class SessionVaultUser : ICurrentVaultUser
{
    private readonly IServiceProvider _sp;
    public SessionVaultUser(IServiceProvider sp) => _sp = sp;

    public async Task<string?> GetUserIdAsync()
    {
        var sessions = _sp.GetService<IVaultSessionService>();
        var sessionId = await GetSessionIdAsync();
        if (sessions != null && !string.IsNullOrEmpty(sessionId) && sessions.IsVaultUnlocked(sessionId))
        {
            var owner = sessions.GetSessionUserId(sessionId);
            if (!string.IsNullOrEmpty(owner)) return owner;
        }
        return _sp.GetService<IAuthService>()?.CurrentUser?.Id;
    }

    private async Task<string?> GetSessionIdAsync()
    {
        // Desktop: DPAPI-backed secure storage. Web/MAUI: the browser tab's sessionStorage.
        try
        {
            var storage = _sp.GetService<ISecureStorageService>();
            var sid = storage != null ? await storage.GetAsync("sessionId") : null;
            if (!string.IsNullOrEmpty(sid)) return sid;
        }
        catch (Exception) { /* fall through */ }

        try
        {
            var js = _sp.GetService<IJSRuntime>();
            return js != null ? await js.InvokeAsync<string?>("sessionStorage.getItem", "sessionId") : null;
        }
        catch (Exception) { return null; } // e.g. prerendering, where there is no JS yet
    }
}

/// <summary>
/// Thrown when a request would store a secret but no (valid) master password was supplied to encrypt it.
/// The API turns this into a 400 — secrets are never stored unencrypted.
/// </summary>
public sealed class VaultKeyRequiredException : Exception
{
    public VaultKeyRequiredException(string message) : base(message) { }
}
