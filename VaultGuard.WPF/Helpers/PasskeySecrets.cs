using Microsoft.Extensions.DependencyInjection;
using VaultGuard.Models;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.WPF.Helpers;

/// <summary>
/// PasskeyItem.CredentialId and PasskeyItem.Notes are [NotMapped] plaintext; only the Encrypted*
/// columns are stored. Without this, a passkey's notes and its Windows Hello key name were silently
/// dropped on save, and the Passkeys page could never tell which keys exist in Windows.
/// </summary>
public static class PasskeySecrets
{
    private static async Task<string> SessionIdAsync(IServiceProvider sp)
    {
        try { return await (sp.GetService<ISecureStorageService>()?.GetAsync("sessionId") ?? Task.FromResult<string?>(null)) ?? string.Empty; }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Couldn't read sessionId", ex); return string.Empty; }
    }

    /// <summary>Fills CredentialId/Notes from the encrypted columns (no-op for values already present).</summary>
    public static async Task HydrateAsync(PasskeyItem? pk, IServiceProvider sp)
    {
        if (pk == null) return;
        var reveal = sp.GetService<IPasswordRevealService>();
        if (reveal == null) return;
        reveal.OpenItem(new PasswordItem { PasskeyItem = pk }, await SessionIdAsync(sp));
    }

    /// <summary>
    /// Encrypts CredentialId/Notes into the stored columns (split layout, same as every other secret).
    /// Throws if there is something to store but the vault is locked, so the caller reports it instead of
    /// losing the data. Empty Notes keeps what's stored unless <paramref name="notesWereLoaded"/> says the
    /// form really showed the stored notes, in which case the user cleared them.
    /// </summary>
    public static async Task ProtectAsync(PasskeyItem pk, IServiceProvider sp, bool notesWereLoaded = true)
    {
        var reveal = sp.GetService<IPasswordRevealService>()
                     ?? throw new InvalidOperationException("Encryption service unavailable.");
        var holder = new PasswordItem { PasskeyItem = pk };
        if (VaultGuard.Services.Security.VaultItemSecrets.HasPlaintext(holder)
            && !reveal.TrySealItem(holder, await SessionIdAsync(sp)))
            throw new InvalidOperationException("Couldn't encrypt the passkey details — the vault may be locked. Unlock it and try again.");

        if (string.IsNullOrEmpty(pk.Notes) && notesWereLoaded)
            pk.EncryptedNotes = pk.NotesNonce = pk.NotesAuthTag = null;
    }
}
