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
        var sid = await SessionIdAsync(sp);
        if (string.IsNullOrEmpty(pk.CredentialId) && !string.IsNullOrEmpty(pk.EncryptedCredentialId))
            pk.CredentialId = reveal.RevealField(pk.EncryptedCredentialId, pk.CredentialIdNonce, pk.CredentialIdAuthTag, sid);
        if (string.IsNullOrEmpty(pk.Notes) && !string.IsNullOrEmpty(pk.EncryptedNotes))
            pk.Notes = reveal.RevealField(pk.EncryptedNotes, pk.NotesNonce, pk.NotesAuthTag, sid);
    }

    /// <summary>
    /// Encrypts CredentialId/Notes into the stored columns. Throws if there is something to store but
    /// the vault is locked, so the caller reports it instead of losing the data. When
    /// <paramref name="notesWereLoaded"/> is false (the existing notes couldn't be decrypted for the
    /// form), empty Notes means "unknown", not "cleared", so the stored notes are kept.
    /// </summary>
    public static async Task ProtectAsync(PasskeyItem pk, IServiceProvider sp, bool notesWereLoaded = true)
    {
        var reveal = sp.GetService<IPasswordRevealService>()
                     ?? throw new InvalidOperationException("Encryption service unavailable.");
        var sid = await SessionIdAsync(sp);

        string? Protect(string? value) => string.IsNullOrEmpty(value) ? null
            : reveal.ProtectField(value, sid)
              ?? throw new InvalidOperationException("Couldn't encrypt the passkey details — the vault may be locked. Unlock it and try again.");

        pk.EncryptedCredentialId = Protect(pk.CredentialId);
        pk.CredentialIdNonce = pk.CredentialIdAuthTag = null;
        if (!string.IsNullOrEmpty(pk.Notes) || notesWereLoaded)
        {
            pk.EncryptedNotes = Protect(pk.Notes);
            pk.NotesNonce = pk.NotesAuthTag = null;
        }
    }
}
