using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace VaultGuard.Services.BrowserLink;

/// <summary>
/// Browsers the user has approved in the app, each with the link key agreed when it was approved (see
/// BrowserLinkCrypto). On Windows the keys are encrypted at rest with DPAPI for the current Windows user, so the
/// file is useless to anyone else. Approvals from before the link was encrypted (protocol 2: a hashed bearer
/// secret) are no longer accepted; those browsers are asked to connect again. Revoking removes every key.
/// </summary>
public sealed class BrowserLinkPairings
{
    public sealed record Pairing(string KeyId, string ProtectedKey, string Label, DateTime CreatedUtc);

    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("VaultGuard.BrowserLink.v3");
    private readonly string _path;
    private readonly object _gate = new();

    public BrowserLinkPairings(string? path = null)
    {
        _path = path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VaultGuard", "BrowserLink", "pairings.json");
    }

    public IReadOnlyList<Pairing> List() { lock (_gate) return Load(); }

    /// <summary>Stores an approved browser's link key; returns the id the browser sends with each request.</summary>
    public string Create(string label, byte[] key)
    {
        var keyId = BrowserLinkCrypto.ToB64(RandomNumberGenerator.GetBytes(16));
        lock (_gate)
        {
            var all = Load();
            all.Add(new Pairing(keyId, Protect(key), label, DateTime.UtcNow));
            Save(all);
        }
        return keyId;
    }

    /// <summary>The link key for an approved browser, or null.</summary>
    public byte[]? KeyFor(string? keyId)
    {
        if (string.IsNullOrEmpty(keyId)) return null;
        Pairing? p;
        lock (_gate) p = Load().FirstOrDefault(x => CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(x.KeyId), Encoding.ASCII.GetBytes(keyId)));
        return p == null ? null : Unprotect(p.ProtectedKey);
    }

    public void RevokeAll() { lock (_gate) Save(new List<Pairing>()); }

    private static string Protect(byte[] key) => OperatingSystem.IsWindows()
        ? "dpapi:" + Convert.ToBase64String(ProtectedData.Protect(key, Entropy, DataProtectionScope.CurrentUser))
        : "raw:" + Convert.ToBase64String(key); // no DPAPI (Linux/macOS dev hosts): the file is in the user's own profile

    private static byte[]? Unprotect(string stored)
    {
        try
        {
            if (stored.StartsWith("dpapi:", StringComparison.Ordinal) && OperatingSystem.IsWindows())
                return ProtectedData.Unprotect(Convert.FromBase64String(stored[6..]), Entropy, DataProtectionScope.CurrentUser);
            if (stored.StartsWith("raw:", StringComparison.Ordinal)) return Convert.FromBase64String(stored[4..]);
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Couldn't read a browser link key", ex); }
        return null;
    }

    private List<Pairing> Load()
    {
        try
        {
            if (!File.Exists(_path)) return new();
            // Older (protocol 2) entries have no KeyId: skip them, so those browsers connect again.
            return (JsonSerializer.Deserialize<List<Pairing>>(File.ReadAllText(_path)) ?? new())
                .Where(p => !string.IsNullOrEmpty(p.KeyId) && !string.IsNullOrEmpty(p.ProtectedKey)).ToList();
        }
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
