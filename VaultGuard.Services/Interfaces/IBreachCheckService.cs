namespace VaultGuard.Services.Interfaces;

/// <summary>Result of checking one password against the Have I Been Pwned "Pwned Passwords" dataset.</summary>
public sealed class BreachCheckResult
{
    /// <summary>How many times this exact password has appeared in known breaches (0 = not seen).</summary>
    public int TimesSeen { get; init; }

    /// <summary>True when the password appears in at least one known breach corpus.</summary>
    public bool IsBreached => TimesSeen > 0;

    /// <summary>
    /// True when the lookup itself failed (offline, timeout, service down). Treat as "unknown" rather
    /// than "safe" — never tell the user a password is clean when we couldn't actually check it.
    /// </summary>
    public bool CheckFailed { get; init; }

    public static BreachCheckResult Failed { get; } = new() { CheckFailed = true };
}

/// <summary>
/// Privacy-preserving breach check using the Have I Been Pwned <c>Pwned Passwords</c> range API with
/// k-anonymity: the password is SHA-1 hashed locally and only the first five hex characters of the hash
/// are ever sent to the server, which returns every suffix under that prefix. The full password (and its
/// full hash) never leave the device. Shared across WPF, Blazor and the API.
/// </summary>
public interface IBreachCheckService
{
    /// <summary>Check a single password. Returns <see cref="BreachCheckResult.Failed"/> if the lookup errors.</summary>
    Task<BreachCheckResult> CheckPasswordAsync(string password, CancellationToken cancellationToken = default);
}
