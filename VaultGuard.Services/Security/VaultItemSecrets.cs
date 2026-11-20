using VaultGuard.Models;

namespace VaultGuard.Services.Security;

/// <summary>
/// The one place that knows which fields of a vault item are secrets and how they are stored.
///
/// Security model (shared by the standalone apps and the API):
/// <list type="bullet">
/// <item>Every secret is encrypted with AES-256-GCM under the user's vault key — PBKDF2-SHA256
///   (600,000 iterations) over the master password and the user's salt. The standalone apps hold that key
///   in the unlocked vault session; the API derives the same key per request. Ciphertext written by one is
///   readable by the other.</item>
/// <item>Each encryption uses a fresh random 96-bit nonce and a 128-bit tag, so tampering is detected.</item>
/// <item>Stored layout: ciphertext, nonce and tag in separate Base64 columns ("split"), the format the API,
///   browser-extension host and earlier data already use. Readers also accept the older single-column
///   "cipher|nonce|tag" layout.</item>
/// <item>Plaintext properties are [NotMapped], so a save can never write a secret in clear. Plaintext only
///   exists in memory after <see cref="Open"/>.</item>
/// </list>
///
/// The encrypt/decrypt delegates speak the "cipher|nonce|tag" string that
/// <c>IVaultSessionService.EncryptPassword/DecryptPassword</c> use, so callers can pass those directly,
/// or a key-based pair for the API (see <c>VaultCipher</c>).
/// </summary>
public static class VaultItemSecrets
{
    private sealed record Field(
        string Name,
        Func<PasswordItem, object?> Owner,
        Func<object, string?> GetPlain, Action<object, string?> SetPlain,
        Func<object, (string? C, string? N, string? T)> GetStored,
        Action<object, string?, string?, string?> SetStored,
        bool LegacyPlaintextInCipherColumn = false);

    private static Field F<TOwner>(string name, Func<PasswordItem, TOwner?> owner,
        Func<TOwner, string?> getPlain, Action<TOwner, string?> setPlain,
        Func<TOwner, (string?, string?, string?)> getStored, Action<TOwner, string?, string?, string?> setStored,
        bool legacyPlaintext = false) where TOwner : class
        => new(name, i => owner(i), o => getPlain((TOwner)o), (o, v) => setPlain((TOwner)o, v),
               o => getStored((TOwner)o), (o, c, n, t) => setStored((TOwner)o, c, n, t), legacyPlaintext);

    private static readonly Field[] Fields =
    {
        // Login. Demo-seeded rows put the password itself in EncryptedPassword with no nonce/tag.
        F("Login.Password", i => i.LoginItem, l => l.Password, (l, v) => l.Password = v,
          l => (l.EncryptedPassword, l.PasswordNonce, l.PasswordAuthTag),
          (l, c, n, t) => { l.EncryptedPassword = c; l.PasswordNonce = n; l.PasswordAuthTag = t; }, legacyPlaintext: true),
        F("Login.TotpSecret", i => i.LoginItem, l => l.TotpSecret, (l, v) => l.TotpSecret = v,
          l => (l.EncryptedTotpSecret, l.TotpNonce, l.TotpAuthTag),
          (l, c, n, t) => { l.EncryptedTotpSecret = c; l.TotpNonce = n; l.TotpAuthTag = t; }),
        F("Login.SecurityAnswer1", i => i.LoginItem, l => l.SecurityAnswer1, (l, v) => l.SecurityAnswer1 = v,
          l => (l.EncryptedSecurityAnswer1, l.SecurityAnswer1Nonce, l.SecurityAnswer1AuthTag),
          (l, c, n, t) => { l.EncryptedSecurityAnswer1 = c; l.SecurityAnswer1Nonce = n; l.SecurityAnswer1AuthTag = t; }),
        F("Login.SecurityAnswer2", i => i.LoginItem, l => l.SecurityAnswer2, (l, v) => l.SecurityAnswer2 = v,
          l => (l.EncryptedSecurityAnswer2, l.SecurityAnswer2Nonce, l.SecurityAnswer2AuthTag),
          (l, c, n, t) => { l.EncryptedSecurityAnswer2 = c; l.SecurityAnswer2Nonce = n; l.SecurityAnswer2AuthTag = t; }),
        F("Login.SecurityAnswer3", i => i.LoginItem, l => l.SecurityAnswer3, (l, v) => l.SecurityAnswer3 = v,
          l => (l.EncryptedSecurityAnswer3, l.SecurityAnswer3Nonce, l.SecurityAnswer3AuthTag),
          (l, c, n, t) => { l.EncryptedSecurityAnswer3 = c; l.SecurityAnswer3Nonce = n; l.SecurityAnswer3AuthTag = t; }),
        F("Login.Notes", i => i.LoginItem, l => l.Notes, (l, v) => l.Notes = v,
          l => (l.EncryptedNotes, l.NotesNonce, l.NotesAuthTag),
          (l, c, n, t) => { l.EncryptedNotes = c; l.NotesNonce = n; l.NotesAuthTag = t; }),

        // Credit card
        F("Card.CardNumber", i => i.CreditCardItem, c => c.CardNumber, (c, v) => c.CardNumber = v,
          c => (c.EncryptedCardNumber, c.CardNumberNonce, c.CardNumberAuthTag),
          (c, x, n, t) => { c.EncryptedCardNumber = x; c.CardNumberNonce = n; c.CardNumberAuthTag = t; }),
        F("Card.CVV", i => i.CreditCardItem, c => c.CVV, (c, v) => c.CVV = v,
          c => (c.EncryptedCvv, c.CvvNonce, c.CvvAuthTag),
          (c, x, n, t) => { c.EncryptedCvv = x; c.CvvNonce = n; c.CvvAuthTag = t; }),
        F("Card.PIN", i => i.CreditCardItem, c => c.PIN, (c, v) => c.PIN = v,
          c => (c.EncryptedPin, c.PinNonce, c.PinAuthTag),
          (c, x, n, t) => { c.EncryptedPin = x; c.PinNonce = n; c.PinAuthTag = t; }),
        F("Card.OnlineBankingPassword", i => i.CreditCardItem, c => c.OnlineBankingPassword, (c, v) => c.OnlineBankingPassword = v,
          c => (c.EncryptedOnlineBankingPassword, c.OnlineBankingPasswordNonce, c.OnlineBankingPasswordAuthTag),
          (c, x, n, t) => { c.EncryptedOnlineBankingPassword = x; c.OnlineBankingPasswordNonce = n; c.OnlineBankingPasswordAuthTag = t; }),

        // Wi-Fi
        F("WiFi.Password", i => i.WiFiItem, w => w.Password, (w, v) => w.Password = v,
          w => (w.EncryptedPassword, w.PasswordNonce, w.PasswordAuthTag),
          (w, x, n, t) => { w.EncryptedPassword = x; w.PasswordNonce = n; w.PasswordAuthTag = t; }),
        F("WiFi.RouterPassword", i => i.WiFiItem, w => w.RouterPassword, (w, v) => w.RouterPassword = v,
          w => (w.EncryptedRouterPassword, w.RouterPasswordNonce, w.RouterPasswordAuthTag),
          (w, x, n, t) => { w.EncryptedRouterPassword = x; w.RouterPasswordNonce = n; w.RouterPasswordAuthTag = t; }),
        F("WiFi.GuestNetworkPassword", i => i.WiFiItem, w => w.GuestNetworkPassword, (w, v) => w.GuestNetworkPassword = v,
          w => (w.EncryptedGuestNetworkPassword, w.GuestNetworkPasswordNonce, w.GuestNetworkPasswordAuthTag),
          (w, x, n, t) => { w.EncryptedGuestNetworkPassword = x; w.GuestNetworkPasswordNonce = n; w.GuestNetworkPasswordAuthTag = t; }),

        // Secure note
        F("Note.Content", i => i.SecureNoteItem, s => s.Content, (s, v) => s.Content = v,
          s => (s.EncryptedContent, s.ContentNonce, s.ContentAuthTag),
          (s, x, n, t) => { s.EncryptedContent = x; s.ContentNonce = n; s.ContentAuthTag = t; }),

        // Passkey
        F("Passkey.CredentialId", i => i.PasskeyItem, p => p.CredentialId, (p, v) => p.CredentialId = v,
          p => (p.EncryptedCredentialId, p.CredentialIdNonce, p.CredentialIdAuthTag),
          (p, x, n, t) => { p.EncryptedCredentialId = x; p.CredentialIdNonce = n; p.CredentialIdAuthTag = t; }),
        F("Passkey.Notes", i => i.PasskeyItem, p => p.Notes, (p, v) => p.Notes = v,
          p => (p.EncryptedNotes, p.NotesNonce, p.NotesAuthTag),
          (p, x, n, t) => { p.EncryptedNotes = x; p.NotesNonce = n; p.NotesAuthTag = t; }),
    };

    /// <summary>Names of every secret field this class manages (for tests / audits).</summary>
    public static IReadOnlyList<string> FieldNames => Fields.Select(f => f.Name).ToArray();

    /// <summary>
    /// Encrypts every non-empty plaintext secret on <paramref name="item"/> into its stored columns.
    /// An empty/null plaintext means "unchanged" — a form that failed to decrypt a value can never wipe it.
    /// Throws if encryption fails, so callers report the problem instead of silently saving without it.
    /// </summary>
    /// <param name="encrypt">plaintext → "cipher|nonce|tag".</param>
    /// <param name="decrypt">
    /// Optional. When given, a field whose stored value already decrypts to the same plaintext is left as is,
    /// so re-saving an unchanged item doesn't rewrite ciphertext (or log a password "change" in history).
    /// </param>
    public static int Seal(PasswordItem item, Func<string, string> encrypt, Func<string, string>? decrypt = null)
    {
        ArgumentNullException.ThrowIfNull(item);
        int sealedCount = 0;
        foreach (var f in Fields)
        {
            var owner = f.Owner(item);
            if (owner == null) continue;
            var plain = f.GetPlain(owner);
            if (string.IsNullOrEmpty(plain)) continue;

            if (decrypt != null)
            {
                var (c, n, t) = f.GetStored(owner);
                var existing = string.IsNullOrEmpty(c) ? null : ToPayload(c, n, t);
                if (existing != null && existing == c + "|" + n + "|" + t) // already in the split layout
                {
                    try { if (decrypt(existing) == plain) continue; }
                    catch (Exception) { /* unreadable → overwrite with the new value */ }
                }
            }

            var parts = encrypt(plain).Split('|');
            if (parts.Length != 3) throw new InvalidOperationException($"Encryption of {f.Name} returned an invalid value.");
            f.SetStored(owner, parts[0], parts[1], parts[2]);
            sealedCount++;
        }
        return sealedCount;
    }

    /// <summary>
    /// Decrypts every stored secret into its plaintext property (skipping ones already populated).
    /// Returns the names of fields that could not be decrypted (wrong key, tampered, locked vault).
    /// </summary>
    /// <param name="decrypt">"cipher|nonce|tag" → plaintext.</param>
    public static IReadOnlyList<string> Open(PasswordItem item, Func<string, string> decrypt)
    {
        ArgumentNullException.ThrowIfNull(item);
        var failed = new List<string>();
        foreach (var f in Fields)
        {
            var owner = f.Owner(item);
            if (owner == null || !string.IsNullOrEmpty(f.GetPlain(owner))) continue;
            var (c, n, t) = f.GetStored(owner);
            if (string.IsNullOrEmpty(c)) continue;

            var payload = ToPayload(c, n, t);
            if (payload == null)
            {
                if (f.LegacyPlaintextInCipherColumn) f.SetPlain(owner, c); // pre-encryption demo data
                else failed.Add(f.Name);
                continue;
            }
            try { f.SetPlain(owner, decrypt(payload)); }
            catch (Exception) { failed.Add(f.Name); } // never log the value or the exception detail here
        }
        return failed;
    }

    /// <summary>
    /// Copies the stored (encrypted) form of every secret from one item to another — for sync/clone code,
    /// which must move ciphertext, never plaintext. Only fields whose owner exists on both sides are copied.
    /// </summary>
    public static void CopyStored(PasswordItem source, PasswordItem target)
    {
        foreach (var f in Fields)
        {
            var from = f.Owner(source);
            var to = f.Owner(target);
            if (from == null || to == null) continue;
            var (c, n, t) = f.GetStored(from);
            f.SetStored(to, c, n, t);
        }
    }

    /// <summary>True if any plaintext secret property on the item is populated (i.e. something to seal).</summary>
    public static bool HasPlaintext(PasswordItem item)
        => Fields.Any(f => f.Owner(item) is { } o && !string.IsNullOrEmpty(f.GetPlain(o)));

    /// <summary>Populated plaintext secrets by field name, e.g. "Card.CVV" → "123" (for an explicit reveal).</summary>
    public static IReadOnlyDictionary<string, string> Plaintexts(PasswordItem item)
        => Fields.Select(f => (f.Name, Owner: f.Owner(item)))
                 .Where(x => x.Owner != null)
                 .Select(x => (x.Name, Value: Fields.First(f => f.Name == x.Name).GetPlain(x.Owner!)))
                 .Where(x => !string.IsNullOrEmpty(x.Value))
                 .ToDictionary(x => x.Name, x => x.Value!);

    /// <summary>Clears all in-memory plaintext secrets (e.g. when a view closes or the vault locks).</summary>
    public static void Forget(PasswordItem item)
    {
        foreach (var f in Fields)
        {
            var owner = f.Owner(item);
            if (owner != null) f.SetPlain(owner, null);
        }
    }

    /// <summary>True when the stored value for the login password is still legacy plaintext.</summary>
    internal static bool IsLegacyPlaintext(string? c, string? n, string? t)
        => !string.IsNullOrEmpty(c) && ToPayload(c, n, t) == null;

    /// <summary>Both stored layouts → "cipher|nonce|tag", or null if there is no nonce/tag at all.</summary>
    public static string? ToPayload(string cipher, string? nonce, string? tag)
    {
        if (cipher.Split('|').Length == 3) return cipher;
        if (!string.IsNullOrEmpty(nonce) && !string.IsNullOrEmpty(tag)) return $"{cipher}|{nonce}|{tag}";
        return null;
    }
}
