using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace VaultGuard.Services.BrowserLink;

/// <summary>
/// Browsers the user has approved in the app. A browser gets a random 256-bit secret when it is approved and
/// sends it with every request; only its SHA-256 is stored (a high-entropy secret needs no slow hash), so the
/// file reveals nothing usable. Revoking removes the hash and the browser must be approved again.
/// </summary>
public sealed class BrowserLinkPairings
{
    public sealed record Pairing(string Hash, string Label, DateTime CreatedUtc);

    private readonly string _path;
    private readonly object _gate = new();

    public BrowserLinkPairings(string? path = null)
    {
        _path = path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VaultGuard", "BrowserLink", "pairings.json");
    }

    public IReadOnlyList<Pairing> List() { lock (_gate) return Load(); }

    /// <summary>Approves a browser and returns the secret it must present from now on.</summary>
    public string Create(string label)
    {
        var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        lock (_gate)
        {
            var all = Load();
            all.Add(new Pairing(Hash(secret), label, DateTime.UtcNow));
            Save(all);
        }
        return secret;
    }

    public bool IsPaired(string? secret)
    {
        if (string.IsNullOrEmpty(secret)) return false;
        var h = Encoding.ASCII.GetBytes(Hash(secret));
        lock (_gate)
            return Load().Any(p => CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(p.Hash), h));
    }

    public void RevokeAll() { lock (_gate) Save(new List<Pairing>()); }

    private static string Hash(string secret) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));

    private List<Pairing> Load()
    {
        try { return File.Exists(_path) ? JsonSerializer.Deserialize<List<Pairing>>(File.ReadAllText(_path)) ?? new() : new(); }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Couldn't read browser pairings", ex); return new(); }
    }

    private void Save(List<Pairing> all)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, JsonSerializer.Serialize(all));
    }
}

/// <summary>Asks the user to allow a browser to use the vault (WPF dialog / MAUI alert).</summary>
public interface IBrowserLinkApprover
{
    Task<bool> ApproveAsync(string browserDescription, CancellationToken ct);
}
