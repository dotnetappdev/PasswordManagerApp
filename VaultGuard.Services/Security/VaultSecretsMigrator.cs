using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VaultGuard.DAL;
using VaultGuard.DAL.Security;
using VaultGuard.Models;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.Services.Security;

/// <summary>
/// Upgrades one user's existing vault data to the encrypted-at-rest format the first time it is unlocked
/// after updating (and is a cheap no-op afterwards):
/// <list type="bullet">
/// <item>secrets still sitting in the retired plaintext columns (card number, CVV, PIN, online-banking
///   password, Wi-Fi/router/guest passwords, note content) are encrypted and the old column is wiped;</item>
/// <item>demo-seeded login passwords stored in clear in EncryptedPassword are encrypted;</item>
/// <item>values written in the older single-column "cipher|nonce|tag" layout are rewritten in the split
///   layout other clients (browser extension host, API) expect.</item>
/// </list>
/// On SQLite it turns on secure_delete and checkpoints the WAL, so the overwritten plaintext is zeroed on
/// disk rather than left behind in free pages.
/// It needs the user's vault key, which is why it runs at unlock and only ever touches that user's rows.
/// </summary>
public static class VaultSecretsMigrator
{
    private static readonly string[] CardLegacy = { "CardNumber", "CVV", "PIN", "OnlineBankingPassword" };
    private static readonly string[] WiFiLegacy = { "Password", "RouterPassword", "GuestNetworkPassword" };
    private static readonly string[] NoteLegacy = { "Content" };
    private static readonly HashSet<string> RequiredLegacy = new() { "CardNumber", "CVV", "Password", "Content" };

    /// <summary>Best-effort wrapper for login flows: never throws, never blocks sign-in.</summary>
    public static async Task RunAfterUnlockAsync(VaultGuardDbContext db, IVaultSessionService sessions,
        string userId, string sessionId, ILogger? logger = null)
    {
        try
        {
            var cipher = VaultCipher.ForSession(sessions, sessionId);
            if (cipher == null) return;
            var changed = await MigrateAsync(db, userId, cipher);
            if (changed > 0) logger?.LogInformation("Encrypted/normalised {Count} stored secret(s) for user {UserId}", changed, userId);
        }
        catch (Exception ex)
        {
            // Old data stays readable through the legacy columns until the next successful run.
            logger?.LogError(ex, "Vault secrets migration failed for user {UserId}", userId);
        }
    }

    /// <summary>Returns the number of secrets encrypted or rewritten.</summary>
    public static async Task<int> MigrateAsync(VaultGuardDbContext db, string userId, VaultCipher cipher, CancellationToken ct = default)
    {
        int changed = 0;

        // Only rows that need work are loaded: legacy plaintext present, or a pipe-layout / no-nonce value.
        var items = await db.PasswordItems
            .Where(p => p.UserId == userId)
            .Include(p => p.LoginItem).Include(p => p.CreditCardItem).Include(p => p.WiFiItem)
            .Include(p => p.SecureNoteItem).Include(p => p.PasskeyItem).Include(p => p.CustomFields)
            .Where(p =>
                // secret custom fields still in plaintext (password / OTP / protected / TOTP secret)
                p.CustomFields.Any(f => f.StoredValue != "" && !f.StoredValue.StartsWith(CustomField.EncryptedPrefix) &&
                    f.Name != "VaultGuard.Protected" && f.Name != "vaultguard.password-history" && f.Name != "Brand Icon" &&
                    (f.Type == CustomFieldType.Password || f.Type == CustomFieldType.OneTimePassword || f.IsProtected || f.Name == "TOTP Secret")) ||
                (p.CreditCardItem != null && (
                    (EF.Property<string>(p.CreditCardItem, "LegacyCardNumber") ?? "") != "" ||
                    (EF.Property<string>(p.CreditCardItem, "LegacyCVV") ?? "") != "" ||
                    (EF.Property<string>(p.CreditCardItem, "LegacyPIN") ?? "") != "" ||
                    (EF.Property<string>(p.CreditCardItem, "LegacyOnlineBankingPassword") ?? "") != "")) ||
                (p.WiFiItem != null && (
                    (EF.Property<string>(p.WiFiItem, "LegacyPassword") ?? "") != "" ||
                    (EF.Property<string>(p.WiFiItem, "LegacyRouterPassword") ?? "") != "" ||
                    (EF.Property<string>(p.WiFiItem, "LegacyGuestNetworkPassword") ?? "") != "")) ||
                (p.SecureNoteItem != null && (EF.Property<string>(p.SecureNoteItem, "LegacyContent") ?? "") != "") ||
                (p.LoginItem != null && p.LoginItem.EncryptedPassword != null && p.LoginItem.EncryptedPassword != "" &&
                    (p.LoginItem.PasswordNonce == null || p.LoginItem.PasswordNonce == "")) ||
                (p.LoginItem != null && (p.LoginItem.EncryptedNotes ?? "").Contains("|")) ||
                (p.LoginItem != null && (p.LoginItem.EncryptedTotpSecret ?? "").Contains("|")) ||
                (p.PasskeyItem != null && ((p.PasskeyItem.EncryptedCredentialId ?? "").Contains("|") || (p.PasskeyItem.EncryptedNotes ?? "").Contains("|"))))
            .ToListAsync(ct);

        if (items.Count == 0) return 0;

        foreach (var item in items)
        {
            // 1. Pull legacy plaintext into the in-memory properties.
            if (item.CreditCardItem is { } cc)
                changed += Adopt(db, cc, CardLegacy, (n, v) =>
                {
                    switch (n)
                    {
                        case "CardNumber": if (string.IsNullOrEmpty(cc.EncryptedCardNumber)) cc.CardNumber = v; break;
                        case "CVV": if (string.IsNullOrEmpty(cc.EncryptedCvv)) cc.CVV = v; break;
                        case "PIN": if (string.IsNullOrEmpty(cc.EncryptedPin)) cc.PIN = v; break;
                        case "OnlineBankingPassword": if (string.IsNullOrEmpty(cc.EncryptedOnlineBankingPassword)) cc.OnlineBankingPassword = v; break;
                    }
                });
            if (item.WiFiItem is { } w)
                changed += Adopt(db, w, WiFiLegacy, (n, v) =>
                {
                    switch (n)
                    {
                        case "Password": if (string.IsNullOrEmpty(w.EncryptedPassword)) w.Password = v; break;
                        case "RouterPassword": if (string.IsNullOrEmpty(w.EncryptedRouterPassword)) w.RouterPassword = v; break;
                        case "GuestNetworkPassword": if (string.IsNullOrEmpty(w.EncryptedGuestNetworkPassword)) w.GuestNetworkPassword = v; break;
                    }
                });
            if (item.SecureNoteItem is { } note)
                changed += Adopt(db, note, NoteLegacy, (_, v) => { if (string.IsNullOrEmpty(note.EncryptedContent)) note.Content = v; });

            // 2. Login password stored in clear (seeded demo data) or in the pipe layout; other pipe-layout fields.
            if (item.LoginItem is { } li)
            {
                changed += TakeForReseal(li.EncryptedPassword, li.PasswordNonce, li.PasswordAuthTag, cipher, legacyPlaintextOk: true,
                    v => { li.Password = v; li.EncryptedPassword = li.PasswordNonce = li.PasswordAuthTag = null; });
                changed += TakeForReseal(li.EncryptedNotes, li.NotesNonce, li.NotesAuthTag, cipher, false,
                    v => { li.Notes = v; li.EncryptedNotes = li.NotesNonce = li.NotesAuthTag = null; });
                changed += TakeForReseal(li.EncryptedTotpSecret, li.TotpNonce, li.TotpAuthTag, cipher, false,
                    v => { li.TotpSecret = v; li.EncryptedTotpSecret = li.TotpNonce = li.TotpAuthTag = null; });
            }
            if (item.PasskeyItem is { } pk)
            {
                changed += TakeForReseal(pk.EncryptedCredentialId, pk.CredentialIdNonce, pk.CredentialIdAuthTag, cipher, false,
                    v => { pk.CredentialId = v; pk.EncryptedCredentialId = pk.CredentialIdNonce = pk.CredentialIdAuthTag = null; });
                changed += TakeForReseal(pk.EncryptedNotes, pk.NotesNonce, pk.NotesAuthTag, cipher, false,
                    v => { pk.Notes = v; pk.EncryptedNotes = pk.NotesNonce = pk.NotesAuthTag = null; });
            }

            // 3. Encrypt everything now in memory (split layout) — plus plaintext secret custom fields — then
            //    drop the plaintext.
            changed += (item.CustomFields ?? new List<CustomField>()).Count(cf => cf.IsSecret && !cf.IsEncrypted && cf.StoredValue != "");
            cipher.Seal(item);
            VaultItemSecrets.Forget(item);
        }

        var sqlite = db.Database.IsSqlite();
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            if (sqlite) await db.Database.ExecuteSqlRawAsync("PRAGMA secure_delete = ON", ct);
            await db.SaveChangesAsync(ct);
            if (sqlite) await db.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE)", ct);
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
        return changed;
    }

    /// <summary>Moves non-empty legacy column values into plaintext properties and wipes the columns.</summary>
    private static int Adopt(VaultGuardDbContext db, object entity, string[] columns, Action<string, string> adopt)
    {
        int n = 0;
        var entry = db.Entry(entity);
        foreach (var col in columns)
        {
            var prop = entry.Property(LegacySecretColumns.ShadowName(col));
            var value = prop.CurrentValue as string;
            if (string.IsNullOrEmpty(value)) continue;
            adopt(col, value);
            prop.CurrentValue = RequiredLegacy.Contains(col) ? string.Empty : null;
            n++;
        }
        return n;
    }

    /// <summary>
    /// For a value in the pipe layout (or legacy plaintext), decrypts it into the plaintext property so the
    /// subsequent Seal rewrites it in the split layout. Values already in the split layout are left alone.
    /// </summary>
    private static int TakeForReseal(string? stored, string? nonce, string? tag, VaultCipher cipher, bool legacyPlaintextOk, Action<string> take)
    {
        if (string.IsNullOrEmpty(stored)) return 0;
        if (stored.Split('|').Length == 3)
        {
            try { take(cipher.Decrypt(stored)); return 1; }
            catch (Exception) { return 0; } // not this key's data — leave it untouched
        }
        if (legacyPlaintextOk && string.IsNullOrEmpty(nonce) && string.IsNullOrEmpty(tag))
        {
            take(stored);
            return 1;
        }
        return 0;
    }
}
