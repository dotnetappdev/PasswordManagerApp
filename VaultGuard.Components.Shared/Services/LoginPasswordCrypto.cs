using Microsoft.JSInterop;
using VaultGuard.Models;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.Components.Shared.Services;

/// <summary>
/// Bridges <see cref="LoginItem.Password"/> (plaintext, NOT a database column) and
/// <see cref="LoginItem.EncryptedPassword"/> (what is actually stored) for the Blazor pages.
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
    /// Decrypts the stored password into <see cref="LoginItem.Password"/> once (no-op if it's
    /// already populated). Returns false if there was a stored password that couldn't be decrypted.
    /// </summary>
    public static async Task<bool> DecryptIntoAsync(PasswordItem? item, IPasswordRevealService reveal, IJSRuntime js)
    {
        var login = item?.LoginItem;
        if (login == null || !string.IsNullOrEmpty(login.Password) || string.IsNullOrEmpty(login.EncryptedPassword))
            return true;

        var pwd = await reveal.RevealPasswordAsync(login, await GetSessionIdAsync(js));
        if (string.IsNullOrEmpty(pwd)) return false;
        login.Password = pwd;
        return true;
    }

    /// <summary>
    /// Encrypts <see cref="LoginItem.Password"/> into the stored columns before a save. Returns false
    /// when there is a password to save but the vault session can't encrypt it (locked / expired).
    /// An empty password leaves the stored one unchanged.
    /// </summary>
    public static async Task<bool> EncryptForSaveAsync(PasswordItem item, IPasswordRevealService reveal, IJSRuntime js)
    {
        var login = item.LoginItem;
        if (login == null || string.IsNullOrEmpty(login.Password)) return true;
        return reveal.TryEncryptLoginPassword(login, await GetSessionIdAsync(js));
    }
}
