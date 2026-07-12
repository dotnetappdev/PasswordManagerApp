using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Logging;

namespace VaultGuard.Services.Services;

/// <summary>
/// <see cref="IBreachCheckService"/> backed by the Have I Been Pwned Pwned Passwords range API.
///
/// Privacy model (k-anonymity): SHA-1 the password locally, take the first 5 hex chars of the hash as the
/// "prefix", and request <c>https://api.pwnedpasswords.com/range/{prefix}</c>. The response lists every
/// hash suffix (and breach count) that shares that prefix; we match the remaining 35 chars locally. The
/// server never sees the password or its full hash. <c>Add-Padding</c> is sent so the response size can't
/// leak whether a match exists. Results are cached in-memory for the session (keyed by full hash).
///
/// No API key required for the Pwned Passwords range endpoint. Failures are treated as "unknown", never
/// as "safe".
/// </summary>
public sealed class HibpBreachCheckService : IBreachCheckService
{
    private const string RangeUrl = "https://api.pwnedpasswords.com/range/";

    private static readonly HttpClient Http = CreateClient();
    private readonly ConcurrentDictionary<string, int> _cache = new();

    private static HttpClient CreateClient()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        // HIBP asks callers to identify themselves with a User-Agent.
        http.DefaultRequestHeaders.UserAgent.ParseAdd("VaultGuard-PasswordManager");
        return http;
    }

    public async Task<BreachCheckResult> CheckPasswordAsync(string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(password))
            return new BreachCheckResult { TimesSeen = 0 };

        try
        {
            var hash = Sha1Hex(password);
            if (_cache.TryGetValue(hash, out var cached))
                return new BreachCheckResult { TimesSeen = cached };

            var prefix = hash[..5];
            var suffix = hash[5..];

            using var request = new HttpRequestMessage(HttpMethod.Get, RangeUrl + prefix);
            // Pad the response with random decoy entries so its length reveals nothing.
            request.Headers.Add("Add-Padding", "true");

            using var response = await Http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            var count = ParseCount(body, suffix);
            _cache[hash] = count;
            return new BreachCheckResult { TimesSeen = count };
        }
        catch (Exception ex)
        {
            // Offline / timeout / service error — report as unknown so callers don't imply "safe".
            AppLogger.Warning("Pwned Passwords breach check failed", ex);
            return BreachCheckResult.Failed;
        }
    }

    // Each response line is "SUFFIX:COUNT"; padded decoy lines have COUNT 0. Find our exact suffix.
    private static int ParseCount(string body, string suffix)
    {
        foreach (var line in body.Split('\n'))
        {
            var separator = line.IndexOf(':');
            if (separator <= 0) continue;

            if (line.AsSpan(0, separator).Trim().Equals(suffix, StringComparison.OrdinalIgnoreCase))
            {
                _ = int.TryParse(line.AsSpan(separator + 1).Trim(), out var count);
                return count;
            }
        }
        return 0;
    }

    private static string Sha1Hex(string input)
    {
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(input));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
            sb.Append(b.ToString("X2"));
        return sb.ToString();
    }
}
