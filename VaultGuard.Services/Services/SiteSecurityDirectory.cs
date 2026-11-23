using System.Text.Json;

namespace VaultGuard.Services.Services;

/// <summary>What a website supports, from the public 2fa.directory lists.</summary>
public sealed record SiteSecurityInfo(string Domain, bool SupportsAuthenticatorCodes, string? TwoFactorHelpUrl,
    bool SupportsPasskeySignIn, string? PasskeyHelpUrl);

public interface ISiteSecurityDirectory
{
    /// <summary>Loads the lists (from the device's cache if fresh, else downloads them). False when unavailable.</summary>
    Task<bool> EnsureLoadedAsync(CancellationToken ct = default);

    /// <summary>True once the lists are loaded.</summary>
    bool IsLoaded { get; }

    /// <summary>The site, or its parent domain, if listed (call <see cref="EnsureLoadedAsync"/> first).</summary>
    SiteSecurityInfo? Lookup(string? urlOrHost);
}

/// <summary>
/// Which sites offer authenticator-app 2FA and passkey sign-in, from 2fa.directory's public lists (api.2fa.directory
/// and passkeys-api.2fa.directory). Only the whole lists are downloaded; nothing about your vault is sent, and
/// matching happens on the device. Cached for 7 days in %LocalAppData%\VaultGuard\cache, and the cache is used when
/// offline.
/// </summary>
public sealed class SiteSecurityDirectory : ISiteSecurityDirectory
{
    public const string TwoFactorListUrl = "https://api.2fa.directory/v3/tfa.json";
    public const string PasskeyListUrl = "https://passkeys-api.2fa.directory/v1/all.json";
    private static readonly TimeSpan MaxAge = TimeSpan.FromDays(7);
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };

    private readonly string _cacheDir;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Dictionary<string, SiteSecurityInfo>? _sites;

    public SiteSecurityDirectory(string? cacheDir = null)
    {
        _cacheDir = cacheDir ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VaultGuard", "cache");
    }

    public async Task<bool> EnsureLoadedAsync(CancellationToken ct = default)
    {
        if (_sites != null) return true;
        await _gate.WaitAsync(ct);
        try
        {
            if (_sites != null) return true;
            var tfa = await GetListAsync(TwoFactorListUrl, "2fa-directory-tfa.json", ct);
            var pk = await GetListAsync(PasskeyListUrl, "2fa-directory-passkeys.json", ct);
            if (tfa == null && pk == null) return false;
            _sites = Build(tfa, pk);
            return true;
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Warning("Couldn't load the 2fa.directory lists", ex);
            return false;
        }
        finally { _gate.Release(); }
    }

    public bool IsLoaded => _sites != null;

    /// <summary>For tests: use lists already in memory.</summary>
    public static SiteSecurityDirectory FromJson(string? tfaJson, string? passkeyJson) => new() { _sites = Build(tfaJson, passkeyJson) };

    public SiteSecurityInfo? Lookup(string? urlOrHost)
    {
        if (_sites == null) return null;
        var host = VaultGuard.Services.BrowserLink.SiteMatcher.Host(urlOrHost ?? "");
        // Try the host, then each parent domain (login.example.co.uk → example.co.uk), never a bare suffix.
        while (!string.IsNullOrEmpty(host) && host.Contains('.'))
        {
            if (_sites.TryGetValue(host, out var info)) return info;
            host = host[(host.IndexOf('.') + 1)..];
        }
        return null;
    }

    /// <summary>Builds the lookup from the two lists' JSON (public for tests).</summary>
    public static Dictionary<string, SiteSecurityInfo> Build(string? tfaJson, string? passkeyJson)
    {
        var sites = new Dictionary<string, SiteSecurityInfo>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(tfaJson))
        {
            using var doc = JsonDocument.Parse(tfaJson);
            foreach (var entry in doc.RootElement.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Array || entry.GetArrayLength() < 2) continue;
                var e = entry[1];
                var totp = e.TryGetProperty("tfa", out var t) && t.ValueKind == JsonValueKind.Array && t.EnumerateArray().Any(x => x.GetString() == "totp");
                if (!totp) continue;
                var docs = e.TryGetProperty("documentation", out var d) ? d.GetString() : null;
                var domains = new List<string>();
                if (e.TryGetProperty("domain", out var dom) && dom.GetString() is { } main) domains.Add(main);
                if (e.TryGetProperty("additional-domains", out var more) && more.ValueKind == JsonValueKind.Array)
                    domains.AddRange(more.EnumerateArray().Select(x => x.GetString()).OfType<string>());
                foreach (var host in domains.Select(h => h.ToLowerInvariant().TrimStart('.')))
                    sites[host] = new SiteSecurityInfo(host, true, docs, false, null);
            }
        }
        if (!string.IsNullOrEmpty(passkeyJson))
        {
            using var doc = JsonDocument.Parse(passkeyJson);
            foreach (var p in doc.RootElement.EnumerateObject())
            {
                var signIn = p.Value.TryGetProperty("passwordless", out var pw) && pw.GetString() is "allowed" or "required";
                if (!signIn) continue;
                var host = p.Name.ToLowerInvariant();
                var docs = p.Value.TryGetProperty("documentation", out var d) ? d.GetString() : null;
                sites[host] = sites.TryGetValue(host, out var existing)
                    ? existing with { SupportsPasskeySignIn = true, PasskeyHelpUrl = docs }
                    : new SiteSecurityInfo(host, false, null, true, docs);
            }
        }
        return sites;
    }

    private async Task<string?> GetListAsync(string url, string cacheName, CancellationToken ct)
    {
        var path = Path.Combine(_cacheDir, cacheName);
        if (File.Exists(path) && DateTime.UtcNow - File.GetLastWriteTimeUtc(path) < MaxAge) return await File.ReadAllTextAsync(path, ct);
        try
        {
            var json = await Http.GetStringAsync(url, ct);
            Directory.CreateDirectory(_cacheDir);
            await File.WriteAllTextAsync(path, json, ct);
            return json;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
        {
            return File.Exists(path) ? await File.ReadAllTextAsync(path, ct) : null; // offline: an older copy is fine
        }
    }

    /// <summary>The site's standard "change password" address (RFC 8615 well-known URL); sites redirect it.</summary>
    public static string? ChangePasswordUrl(string? website)
    {
        var host = VaultGuard.Services.BrowserLink.SiteMatcher.Host(website ?? "");
        return string.IsNullOrEmpty(host) || !host.Contains('.') ? null : $"https://{host}/.well-known/change-password";
    }
}
