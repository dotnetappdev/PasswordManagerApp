using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace VaultGuard.Web.Services;

/// <summary>
/// Bridges a "Link account" click in Settings.razor (running inside an authenticated Blazor Server
/// circuit) to the plain HTTP OIDC challenge/callback round trip in Program.cs, which happens in a
/// separate browser tab with no knowledge of that circuit - VaultGuard.Web never issues a real
/// ASP.NET Identity auth cookie (see IdentityAuthService's doc comment / docs/SSO.md), so the
/// callback request has no other way to know which local user asked for the link.
///
/// Settings.razor mints a single-use, short-lived token for the current user and opens
/// "/login/sso/{id}?linkToken={token}" in a new tab; the callback consumes the token to recover the
/// user id before calling IUserProfileService.LinkExternalLoginAsync. A forged or reused token simply
/// fails to resolve - it can never be used to link an SSO identity to a user other than the one who
/// was signed in in this server process when the token was minted.
/// </summary>
public interface ISsoLinkTokenStore
{
    string CreateToken(string userId);

    /// <summary>Single-use: removes the token whether or not it's still valid, so it can't be replayed.</summary>
    bool TryConsumeToken(string token, out string? userId);
}

public class SsoLinkTokenStore : ISsoLinkTokenStore
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);
    private readonly ConcurrentDictionary<string, (string UserId, DateTimeOffset Expires)> _tokens = new();

    public string CreateToken(string userId)
    {
        PruneExpired();
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        _tokens[token] = (userId, DateTimeOffset.UtcNow.Add(Ttl));
        return token;
    }

    public bool TryConsumeToken(string token, out string? userId)
    {
        userId = null;
        if (!_tokens.TryRemove(token, out var entry))
            return false;
        if (entry.Expires < DateTimeOffset.UtcNow)
            return false;

        userId = entry.UserId;
        return true;
    }

    private void PruneExpired()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var (key, value) in _tokens)
        {
            if (value.Expires < now)
                _tokens.TryRemove(key, out _);
        }
    }
}
