using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using VaultGuard.Services.Emergency;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.WPF.Services;

/// <summary>Emergency access plumbing for the desktop app (the page is Views/EmergencyAccessPage).</summary>
public static class WpfEmergency
{
    /// <summary>The unlocked user and a copy of their vault key (the caller wipes it), or null when locked.</summary>
    public static async Task<(string UserId, byte[] Key)?> SessionAsync(IServiceProvider? sp)
    {
        if (sp == null) return null;
        string? sid;
        try { sid = sp.GetService<ISecureStorageService>() is { } st ? await st.GetAsync("sessionId") : null; }
        catch { return null; }
        var sessions = sp.GetService<IVaultSessionService>();
        if (string.IsNullOrEmpty(sid) || sessions == null || !sessions.IsVaultUnlocked(sid)) return null;
        var userId = sessions.GetSessionUserId(sid);
        if (string.IsNullOrEmpty(userId) || sessions.GetMasterKey(sid)?.Clone() is not byte[] key) return null;
        return (userId, key);
    }

    /// <summary>
    /// After unlocking: keeps emergency access current (key pair, re-wrapped vault key for trusted contacts) and
    /// tells the user when a trusted contact has asked for access.
    /// </summary>
    public static async Task AfterUnlockAsync(IServiceProvider? sp)
    {
        try
        {
            var emergency = sp?.GetService<EmergencyAccessService>();
            if (emergency == null || await SessionAsync(sp) is not { } s) return;
            try { await Task.Run(() => emergency.OnUnlocked(s.UserId, s.Key)); }
            finally { CryptographicOperations.ZeroMemory(s.Key); }
            foreach (var r in emergency.PendingRequestsFor(s.UserId))
                ToastService.Instance.Show(
                    $"{r.GranteeEmail} asked for emergency access to your vault. It opens on {r.AvailableUtc?.ToLocalTime():f} unless you decline (Emergency Access).",
                    ToastType.Warning, durationMs: 15000);
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Emergency access refresh failed", ex); }
    }
}
