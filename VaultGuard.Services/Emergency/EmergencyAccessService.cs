using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VaultGuard.Crypto.Interfaces;
using VaultGuard.DAL;
using VaultGuard.Models;
using VaultGuard.Services.Security;

namespace VaultGuard.Services.Emergency;

public enum EmergencyStatus { Pending, Ready, Requested, Approved, Declined }

/// <summary>One trusted contact: who may ask for read-only access to whose vault, after how long.</summary>
public sealed class EmergencyGrant
{
    public string Id { get; set; } = "";
    public string GrantorId { get; set; } = "";
    public string GrantorEmail { get; set; } = "";
    public string GranteeId { get; set; } = "";
    public string GranteeEmail { get; set; } = "";
    public int WaitDays { get; set; } = 7;
    public EmergencyStatus Status { get; set; } = EmergencyStatus.Pending;
    public DateTime CreatedUtc { get; set; }
    public DateTime? RequestedUtc { get; set; }
    /// <summary>The grantor's vault key, encrypted with the grantee's public key (RSA-OAEP-SHA256). Null until wrapped.</summary>
    public string? WrappedKey { get; set; }
    /// <summary>Which of the grantee's public keys it was wrapped for (they get a new one if they change their master password).</summary>
    public string? GranteeKeyId { get; set; }
    /// <summary>Which of the grantor's vault keys it holds (it changes with the master password).</summary>
    public string? GrantorKeyId { get; set; }

    public DateTime? AvailableUtc => RequestedUtc?.AddDays(WaitDays);
    public bool IsOpen(DateTime now) => Status == EmergencyStatus.Approved ||
                                        (Status == EmergencyStatus.Requested && AvailableUtc <= now);
}

/// <summary>
/// Emergency access (like Bitwarden's): a trusted contact on the same Vault Guard server can ask for read-only access
/// to your vault, and gets it after a waiting period you choose unless you decline (or straight away if you approve).
///
/// Zero-knowledge: every account has an RSA-2048 key pair whose private key is encrypted with that account's own vault
/// key. Adding a contact encrypts your vault key with their public key. The server only hands that to them once
/// access is open, and only they can decrypt it, with their own vault unlocked. The wrapped key is refreshed whenever
/// you unlock (so a new master password carries over), and removed when you revoke the contact or decline.
/// Records are kept as JSON files under the configured directory, independent of the database provider.
/// </summary>
public sealed class EmergencyAccessService
{
    public static readonly int[] AllowedWaitDays = { 1, 2, 7, 14, 30 };

    private sealed class UserKeys
    {
        public string UserId { get; set; } = "";
        public string KeyId { get; set; } = "";
        public string PublicKey { get; set; } = "";          // SubjectPublicKeyInfo, base64
        public EncryptedPasswordData PrivateKey { get; set; } = new(); // PKCS#8 base64, sealed with the user's vault key
    }

    private readonly string _dir;
    private readonly IPasswordCryptoService _crypto;
    private static readonly object Gate = new();

    public EmergencyAccessService(string directory, IPasswordCryptoService crypto)
    {
        _dir = directory;
        _crypto = crypto;
        Directory.CreateDirectory(Path.Combine(_dir, "keys"));
        Directory.CreateDirectory(Path.Combine(_dir, "grants"));
    }

    // ---------------------------------------------------------------- keys and refresh (on every unlock)

    /// <summary>
    /// Called after a user unlocks: makes sure they have a key pair (a new one if their master password changed),
    /// and re-wraps their vault key for each of their trusted contacts whose wrapped copy is missing or out of date.
    /// </summary>
    public void OnUnlocked(string userId, byte[] vaultKey)
    {
        lock (Gate)
        {
            EnsureKeys(userId, vaultKey);
            var keyId = KeyIdOf(vaultKey);
            foreach (var g in Grants().Where(g => g.GrantorId == userId && g.Status != EmergencyStatus.Declined))
            {
                var grantee = LoadKeys(g.GranteeId);
                if (grantee == null) continue; // they haven't signed in since: wrapped later
                if (g.WrappedKey != null && g.GranteeKeyId == grantee.KeyId && g.GrantorKeyId == keyId) continue;
                using var rsa = RSA.Create();
                rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(grantee.PublicKey), out _);
                g.WrappedKey = Convert.ToBase64String(rsa.Encrypt(vaultKey, RSAEncryptionPadding.OaepSHA256));
                g.GranteeKeyId = grantee.KeyId;
                g.GrantorKeyId = keyId;
                if (g.Status == EmergencyStatus.Pending) g.Status = EmergencyStatus.Ready;
                Save(g);
            }
        }
    }

    private void EnsureKeys(string userId, byte[] vaultKey)
    {
        var existing = LoadKeys(userId);
        if (existing != null)
        {
            try { _crypto.DecryptPasswordWithKey(existing.PrivateKey, vaultKey); return; }
            catch (CryptographicException) { /* master password changed: make a new pair below */ }
            catch (FormatException) { }
        }
        using var rsa = RSA.Create(2048);
        var keys = new UserKeys
        {
            UserId = userId,
            KeyId = Convert.ToHexString(RandomNumberGenerator.GetBytes(8)),
            PublicKey = Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo()),
            PrivateKey = _crypto.EncryptPasswordWithKey(Convert.ToBase64String(rsa.ExportPkcs8PrivateKey()), vaultKey)
        };
        File.WriteAllText(KeysPath(userId), JsonSerializer.Serialize(keys));
    }

    // ---------------------------------------------------------------- grantor side

    public (EmergencyGrant? Grant, string? Error) AddContact(VaultGuardDbContext db, ApplicationUser grantor, string granteeEmail, int waitDays)
    {
        granteeEmail = (granteeEmail ?? "").Trim();
        if (!AllowedWaitDays.Contains(waitDays)) return (null, "Choose a waiting period.");
        if (string.Equals(granteeEmail, grantor.Email, StringComparison.OrdinalIgnoreCase)) return (null, "Choose someone other than yourself.");
        var grantee = db.Users.AsNoTracking().FirstOrDefault(u => u.Email == granteeEmail);
        if (grantee == null) return (null, "There's no Vault Guard account with that email on this server. Ask them to create one first.");
        lock (Gate)
        {
            if (Grants().Any(g => g.GrantorId == grantor.Id && g.GranteeId == grantee.Id && g.Status != EmergencyStatus.Declined))
                return (null, "They're already one of your trusted contacts.");
            var grant = new EmergencyGrant
            {
                Id = Convert.ToHexString(RandomNumberGenerator.GetBytes(12)), GrantorId = grantor.Id, GrantorEmail = grantor.Email ?? "",
                GranteeId = grantee.Id, GranteeEmail = grantee.Email ?? granteeEmail, WaitDays = waitDays, CreatedUtc = DateTime.UtcNow
            };
            Save(grant);
            return (grant, null);
        }
    }

    public IReadOnlyList<EmergencyGrant> ForGrantor(string userId) { lock (Gate) return Grants().Where(g => g.GrantorId == userId).ToList(); }
    public IReadOnlyList<EmergencyGrant> ForGrantee(string userId) { lock (Gate) return Grants().Where(g => g.GranteeId == userId && g.Status != EmergencyStatus.Declined).ToList(); }

    /// <summary>Requests waiting for the grantor's answer (to show them when they sign in).</summary>
    public IReadOnlyList<EmergencyGrant> PendingRequestsFor(string grantorId) { lock (Gate) return Grants().Where(g => g.GrantorId == grantorId && g.Status == EmergencyStatus.Requested).ToList(); }

    public bool Decline(string grantId, string grantorId) => Update(grantId, g => g.GrantorId == grantorId && g.Status == EmergencyStatus.Requested,
        g => { g.Status = EmergencyStatus.Ready; g.RequestedUtc = null; });

    public bool Approve(string grantId, string grantorId) => Update(grantId, g => g.GrantorId == grantorId && g.Status == EmergencyStatus.Requested,
        g => g.Status = EmergencyStatus.Approved);

    /// <summary>Removes a trusted contact (and their wrapped key) entirely.</summary>
    public bool Remove(string grantId, string grantorId)
    {
        lock (Gate)
        {
            var g = Load(grantId);
            if (g == null || g.GrantorId != grantorId) return false;
            File.Delete(GrantPath(grantId));
            return true;
        }
    }

    // ---------------------------------------------------------------- grantee side

    public bool Request(string grantId, string granteeId) => Update(grantId,
        g => g.GranteeId == granteeId && g.Status is EmergencyStatus.Ready or EmergencyStatus.Pending,
        g => { g.Status = EmergencyStatus.Requested; g.RequestedUtc = DateTime.UtcNow; });

    /// <summary>
    /// The grantor's vault key, for the grantee, once access is open: decrypted with the grantee's private key, which
    /// is decrypted with the grantee's own vault key. Null if access isn't open (yet) or the key isn't available.
    /// </summary>
    public byte[]? OpenVaultKey(string grantId, string granteeId, byte[] granteeVaultKey, out string? error)
    {
        error = null;
        lock (Gate)
        {
            var g = Load(grantId);
            if (g == null || g.GranteeId != granteeId) { error = "Not found."; return null; }
            if (!g.IsOpen(DateTime.UtcNow)) { error = g.Status == EmergencyStatus.Requested ? $"Access opens on {g.AvailableUtc:f} UTC unless {g.GrantorEmail} declines." : "Request access first."; return null; }
            if (g.WrappedKey == null) { error = $"{g.GrantorEmail} needs to sign in to Vault Guard once more to finish setting this up."; return null; }
            var mine = LoadKeys(granteeId);
            if (mine == null || mine.KeyId != g.GranteeKeyId) { error = $"Your keys changed since {g.GrantorEmail} last signed in; it will work after they next sign in."; return null; }
            try
            {
                using var rsa = RSA.Create();
                rsa.ImportPkcs8PrivateKey(Convert.FromBase64String(_crypto.DecryptPasswordWithKey(mine.PrivateKey, granteeVaultKey)), out _);
                return rsa.Decrypt(Convert.FromBase64String(g.WrappedKey), RSAEncryptionPadding.OaepSHA256);
            }
            catch (CryptographicException) { error = "Couldn't open the key. Sign out and in again, then try again."; return null; }
        }
    }

    /// <summary>The grantor's items, decrypted with their vault key, for read-only viewing.</summary>
    public async Task<List<PasswordItem>> ReadItemsAsync(VaultGuardDbContext db, string grantId, string granteeId, byte[] grantorKey)
    {
        EmergencyGrant? g;
        lock (Gate) g = Load(grantId);
        if (g == null || g.GranteeId != granteeId || !g.IsOpen(DateTime.UtcNow)) return new();
        var items = await db.PasswordItems.AsNoTracking()
            .Where(p => p.UserId == g.GrantorId && !p.IsDeleted)
            .Include(p => p.LoginItem).Include(p => p.CreditCardItem).Include(p => p.SecureNoteItem)
            .Include(p => p.WiFiItem).Include(p => p.CustomFields)
            .OrderBy(p => p.Title).ToListAsync();
        using var cipher = VaultCipher.ForKey(_crypto, (byte[])grantorKey.Clone(), ownsKey: true);
        foreach (var item in items) cipher.Open(item);
        return items;
    }

    // ---------------------------------------------------------------- storage

    public static string KeyIdOf(byte[] vaultKey) => Convert.ToHexString(SHA256.HashData([.. "vg-ea-key-id"u8, .. vaultKey]))[..16];

    private bool Update(string id, Func<EmergencyGrant, bool> allowed, Action<EmergencyGrant> change)
    {
        lock (Gate)
        {
            var g = Load(id);
            if (g == null || !allowed(g)) return false;
            change(g);
            Save(g);
            return true;
        }
    }

    private IEnumerable<EmergencyGrant> Grants()
    {
        foreach (var f in Directory.EnumerateFiles(Path.Combine(_dir, "grants"), "*.json"))
        {
            EmergencyGrant? g = null;
            try { g = JsonSerializer.Deserialize<EmergencyGrant>(File.ReadAllText(f)); } catch { }
            if (g != null) yield return g;
        }
    }

    private EmergencyGrant? Load(string id)
    {
        if (string.IsNullOrEmpty(id) || !id.All(char.IsAsciiHexDigit)) return null;
        var path = GrantPath(id);
        return File.Exists(path) ? JsonSerializer.Deserialize<EmergencyGrant>(File.ReadAllText(path)) : null;
    }

    private void Save(EmergencyGrant g) => File.WriteAllText(GrantPath(g.Id), JsonSerializer.Serialize(g));
    private UserKeys? LoadKeys(string userId) => File.Exists(KeysPath(userId)) ? JsonSerializer.Deserialize<UserKeys>(File.ReadAllText(KeysPath(userId))) : null;
    private string GrantPath(string id) => Path.Combine(_dir, "grants", id + ".json");
    private string KeysPath(string userId) => Path.Combine(_dir, "keys", Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(userId)))[..32] + ".json");
}
