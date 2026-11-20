using Microsoft.JSInterop;
using VaultGuard.Models;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.Components.Shared.Services;

/// <summary>
/// Bridges the plaintext secret properties (NOT database columns) and their encrypted stored columns
/// for the Blazor pages — see VaultGuard.Services.Security.VaultItemSecrets.
/// Pages bind their password inputs/displays to <c>Password</c>, so it must be decrypted after
/// loading an item and encrypted before saving one — otherwise a loaded item shows no password
/// and a typed password is silently dropped on save.
/// </summary>
public static class LoginPasswordCrypto
{
    private static async Task<string> GetSessionIdAsync(IJSRuntime js)
    {
        try { return await js.InvokeAsync<string?>("sessionStorage.getItem", "sessionId") ?? string.Empty; }
        catch (Exception ex)
        {
            // Prerendering has no JS interop; the caller simply gets no plaintext.
            VaultGuard.Services.Logging.AppLogger.Debug($"sessionId unavailable: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Decrypts every stored secret on the item (login password, card number/CVV/PIN, Wi-Fi passwords,
    /// note content, passkey…) into its in-memory plaintext property. Already-populated values are kept.
    /// Returns false if something couldn't be decrypted (e.g. the vault session expired).
    /// </summary>
    public static async Task<bool> DecryptIntoAsync(PasswordItem? item, IPasswordRevealService reveal, IJSRuntime js)
    {
        if (item == null) return true;
        return reveal.OpenItem(item, await GetSessionIdAsync(js));
    }

    /// <summary>
    /// Encrypts every plaintext secret on the item into its stored columns before a save. Returns false
    /// when there is something to save but the session can't encrypt it (locked / expired) — the caller
    /// must not save then. Empty plaintext leaves the stored value unchanged.
    /// </summary>
    public static async Task<bool> EncryptForSaveAsync(PasswordItem item, IPasswordRevealService reveal, IJSRuntime js)
    {
        if (!VaultGuard.Services.Security.VaultItemSecrets.HasPlaintext(item)) return true;
        return reveal.TrySealItem(item, await GetSessionIdAsync(js));
    }
}
