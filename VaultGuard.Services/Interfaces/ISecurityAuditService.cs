using VaultGuard.Models;

namespace VaultGuard.Services.Interfaces;

/// <summary>
/// A single flagged item in a vault health report (Watchtower-style).
/// </summary>
public sealed class SecurityAuditEntry
{
    public int ItemId { get; init; }
    public string Title { get; init; } = "";
    public string? Detail { get; init; }
    /// <summary>Where to fix it, e.g. the site's change-password page or its 2FA set-up instructions.</summary>
    public string? Link { get; init; }
    public string? LinkText { get; init; }
}

/// <summary>
/// A set of items that share the exact same password.
/// </summary>
public sealed class ReusedPasswordGroup
{
    public int Count => Items.Count;
    public List<SecurityAuditEntry> Items { get; init; } = new();
}

/// <summary>
/// The result of auditing a vault: an overall score plus the items that need attention.
/// </summary>
public sealed class VaultHealthReport
{
    /// <summary>Overall vault health, 0–100 (higher is better).</summary>
    public int Score { get; init; }
    public string Rating { get; init; } = "";
    public string ColorHex { get; init; } = "#6A6A6A";

    public int TotalCredentials { get; init; }
    public IReadOnlyList<SecurityAuditEntry> WeakPasswords { get; init; } = System.Array.Empty<SecurityAuditEntry>();
    public IReadOnlyList<ReusedPasswordGroup> ReusedPasswords { get; init; } = System.Array.Empty<ReusedPasswordGroup>();
    public IReadOnlyList<SecurityAuditEntry> OldPasswords { get; init; } = System.Array.Empty<SecurityAuditEntry>();
    public IReadOnlyList<SecurityAuditEntry> MissingTwoFactor { get; init; } = System.Array.Empty<SecurityAuditEntry>();

    /// <summary>Saved sites that offer passkey sign-in (a suggestion; not counted as an issue).</summary>
    public IReadOnlyList<SecurityAuditEntry> PasskeysAvailable { get; init; } = System.Array.Empty<SecurityAuditEntry>();

    /// <summary>True when "missing 2FA" is based on which sites actually offer it (2fa.directory).</summary>
    public bool TwoFactorFromDirectory { get; init; }

    /// <summary>Logins whose website uses an insecure http:// URL (Watchtower "Unsecured Websites").</summary>
    public IReadOnlyList<SecurityAuditEntry> UnsecuredWebsites { get; init; } = System.Array.Empty<SecurityAuditEntry>();

    public int WeakCount => WeakPasswords.Count;
    public int ReusedCount => ReusedPasswords.Sum(g => g.Count);
    public int OldCount => OldPasswords.Count;
    public int MissingTwoFactorCount => MissingTwoFactor.Count;
    public int UnsecuredCount => UnsecuredWebsites.Count;

    /// <summary>Total number of flagged credentials across every concern category.</summary>
    public int TotalIssues => WeakCount + ReusedCount + OldCount + UnsecuredCount + MissingTwoFactorCount;
}

public interface ISecurityAuditService
{
    /// <summary>
    /// Analyses a set of items for weak / reused / old passwords and logins missing 2FA.
    /// </summary>
    VaultHealthReport Analyze(IEnumerable<PasswordItem> items, int oldPasswordDays = 365);

    /// <summary>
    /// As <see cref="Analyze(IEnumerable{PasswordItem}, int)"/>, but with what each site supports: "missing 2FA" then
    /// only lists sites that offer authenticator codes (with a link to set it up), passkey-capable sites are listed,
    /// and weak/reused logins get a link to the site's change-password page.
    /// </summary>
    VaultHealthReport Analyze(IEnumerable<PasswordItem> items, VaultGuard.Services.Services.ISiteSecurityDirectory? directory, int oldPasswordDays = 365)
        => Analyze(items, oldPasswordDays);
}
