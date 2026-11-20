using System.Collections.Concurrent;

namespace VaultGuard.API.Security;

/// <summary>
/// Per-account throttle for master-password checks on anonymous endpoints (API-key issuance). The per-IP
/// rate limiter alone doesn't stop a distributed guesser; this locks the targeted account's password checks
/// for <see cref="LockFor"/> after <see cref="MaxFailures"/> wrong attempts within <see cref="Window"/>.
/// In-memory (per API instance), which is enough to make online guessing impractical.
/// </summary>
public sealed class AccountLockout
{
    public int MaxFailures { get; init; } = 5;
    public TimeSpan Window { get; init; } = TimeSpan.FromMinutes(15);
    public TimeSpan LockFor { get; init; } = TimeSpan.FromMinutes(15);

    private readonly ConcurrentDictionary<string, (int Count, DateTime First, DateTime? LockedUntil)> _state = new();

    private static string Key(string account) => account.Trim().ToUpperInvariant();

    /// <summary>Remaining lock time, or null if attempts are allowed.</summary>
    public TimeSpan? LockedFor(string account)
    {
        if (!_state.TryGetValue(Key(account), out var s) || s.LockedUntil is not { } until) return null;
        var left = until - DateTime.UtcNow;
        return left > TimeSpan.Zero ? left : null;
    }

    public void RecordFailure(string account)
    {
        var now = DateTime.UtcNow;
        _state.AddOrUpdate(Key(account),
            _ => (1, now, null),
            (_, s) =>
            {
                if (s.LockedUntil is { } u && u > now) return s;          // already locked
                if (now - s.First > Window) return (1, now, null);         // window expired: start over
                var count = s.Count + 1;
                return (count, s.First, count >= MaxFailures ? now + LockFor : null);
            });
    }

    public void RecordSuccess(string account) => _state.TryRemove(Key(account), out _);
}
