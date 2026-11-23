using System.Security.Cryptography;
using System.Text.Json;

namespace VaultGuard.Services.Sharing;

/// <summary>
/// Server-side storage for secure shares: one small JSON file per share, holding only the encrypted envelope,
/// the expiry and the remaining views (independent of the database provider). A share is deleted when it
/// expires or its last view is used. Shares live at most 30 days and up to 50 views.
/// </summary>
public sealed class FileShareStore
{
    public static readonly TimeSpan MaxLifetime = TimeSpan.FromDays(30);
    public const int MaxViewsLimit = 50;

    public sealed class Record
    {
        public string Id { get; set; } = "";
        public ShareEnvelope Envelope { get; set; } = new();
        public DateTime CreatedUtc { get; set; }
        public DateTime ExpiresUtc { get; set; }
        /// <summary>Null: any number of views until it expires.</summary>
        public int? ViewsLeft { get; set; }
        public string? OwnerUserId { get; set; }
    }

    private readonly string _dir;
    private readonly object _gate = new();

    public FileShareStore(string directory)
    {
        _dir = directory;
        Directory.CreateDirectory(_dir);
    }

    public (string? Id, DateTime ExpiresUtc, string? Error) Create(ShareEnvelope env, TimeSpan expiresIn, int? maxViews, string? ownerUserId)
    {
        if (string.IsNullOrEmpty(env.Ciphertext) || string.IsNullOrEmpty(env.Iv)) return (null, default, "Nothing to share.");
        if (env.Ciphertext.Length > SecureShareCrypto.MaxCiphertextBytes * 4 / 3 + 4) return (null, default, "The share is too large.");
        if (expiresIn <= TimeSpan.Zero) expiresIn = TimeSpan.FromDays(1);
        if (expiresIn > MaxLifetime) expiresIn = MaxLifetime;
        if (maxViews is < 1) maxViews = 1;
        if (maxViews > MaxViewsLimit) maxViews = MaxViewsLimit;
        var rec = new Record
        {
            Id = SecureShareCrypto.B64(RandomNumberGenerator.GetBytes(16)),
            Envelope = env, CreatedUtc = DateTime.UtcNow, ExpiresUtc = DateTime.UtcNow + expiresIn,
            ViewsLeft = maxViews, OwnerUserId = ownerUserId
        };
        lock (_gate)
        {
            Sweep();
            File.WriteAllText(PathFor(rec.Id), JsonSerializer.Serialize(rec));
        }
        return (rec.Id, rec.ExpiresUtc, null);
    }

    /// <summary>Takes one view of a share: its envelope, or null if it doesn't exist, expired or was used up.</summary>
    public Record? TakeView(string id)
    {
        if (!IsValidId(id)) return null;
        lock (_gate)
        {
            var path = PathFor(id);
            if (!File.Exists(path)) return null;
            Record? rec;
            try { rec = JsonSerializer.Deserialize<Record>(File.ReadAllText(path)); }
            catch { File.Delete(path); return null; }
            if (rec == null || rec.ExpiresUtc <= DateTime.UtcNow || rec.ViewsLeft is <= 0) { File.Delete(path); return null; }
            if (rec.ViewsLeft != null)
            {
                rec.ViewsLeft--;
                if (rec.ViewsLeft <= 0) File.Delete(path);
                else File.WriteAllText(path, JsonSerializer.Serialize(rec));
            }
            return rec;
        }
    }

    /// <summary>Removes a share early (its owner only).</summary>
    public bool Delete(string id, string ownerUserId)
    {
        if (!IsValidId(id)) return false;
        lock (_gate)
        {
            var path = PathFor(id);
            if (!File.Exists(path)) return false;
            var rec = JsonSerializer.Deserialize<Record>(File.ReadAllText(path));
            if (rec?.OwnerUserId != ownerUserId) return false;
            File.Delete(path);
            return true;
        }
    }

    private void Sweep()
    {
        foreach (var f in Directory.EnumerateFiles(_dir, "*.json"))
        {
            try
            {
                var rec = JsonSerializer.Deserialize<Record>(File.ReadAllText(f));
                if (rec == null || rec.ExpiresUtc <= DateTime.UtcNow) File.Delete(f);
            }
            catch { /* unreadable or in use: leave it */ }
        }
    }

    private static bool IsValidId(string id) => id.Length is > 10 and < 40 && id.All(c => char.IsLetterOrDigit(c) || c is '-' or '_');
    private string PathFor(string id) => Path.Combine(_dir, id + ".json");
}
