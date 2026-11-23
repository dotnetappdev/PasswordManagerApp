using Microsoft.JSInterop;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.Web.Services;

/// <summary>The signed-in user's vault key for emergency access (a copy; the caller wipes it).</summary>
public sealed class EmergencySession
{
    private readonly IJSRuntime _js;
    private readonly IVaultSessionService _sessions;
    private readonly IAuthService _auth;

    public EmergencySession(IJSRuntime js, IVaultSessionService sessions, IAuthService auth)
    {
        _js = js;
        _sessions = sessions;
        _auth = auth;
    }

    /// <summary>The signed-in user, from the vault session itself (set once <see cref="LoadAsync"/> has run).</summary>
    public string? UserId { get; private set; }

    private string? _sid;

    /// <summary>Reads the browser's vault session (JS interop, so after the first render). False when not unlocked.</summary>
    public async Task<bool> LoadAsync()
    {
        try { _sid = await _js.InvokeAsync<string?>("sessionStorage.getItem", "sessionId"); }
        catch { return false; } // prerendering: no JS yet
        UserId = string.IsNullOrEmpty(_sid) || !_sessions.IsVaultUnlocked(_sid) ? null : _sessions.GetSessionUserId(_sid);
        return UserId != null;
    }

    public async Task<byte[]?> VaultKeyCopyAsync()
    {
        if (UserId == null && !await LoadAsync()) return null;
        return _sessions.GetMasterKey(_sid!)?.Clone() as byte[];
    }
}
