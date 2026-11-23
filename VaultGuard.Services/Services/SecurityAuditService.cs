using VaultGuard.Models;
using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Utilities;

namespace VaultGuard.Services.Services;

/// <summary>
/// "Watchtower"-style vault health analysis: scores the vault and surfaces weak, reused and old
/// passwords plus logins that could add an authenticator code. Pure (operates on the items passed
/// in) so it's identical on every platform and easy to test.
/// </summary>
public sealed class SecurityAuditService : ISecurityAuditService
{
    private readonly IPasswordStrengthService _strength;

    public SecurityAuditService(IPasswordStrengthService strength) => _strength = strength;

    public VaultHealthReport Analyze(IEnumerable<PasswordItem> items, int oldPasswordDays = 365) => Analyze(items, null, oldPasswordDays);

    public VaultHealthReport Analyze(IEnumerable<PasswordItem> items, ISiteSecurityDirectory? directory, int oldPasswordDays = 365)
    {
        var list = (items ?? Enumerable.Empty<PasswordItem>()).Where(i => i != null).ToList();
        var now = DateTime.UtcNow;

        var weak = new List<SecurityAuditEntry>();
        var old = new List<SecurityAuditEntry>();
        var missing2fa = new List<SecurityAuditEntry>();
        var unsecured = new List<SecurityAuditEntry>();
        var passkeys = new List<SecurityAuditEntry>();
        var passkeySites = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var useDirectory = directory?.IsLoaded == true;
        // Sites that already have a saved passkey don't need the suggestion.
        foreach (var pk in list.Where(i => i.PasskeyItem != null))
            if (BrowserLink.SiteMatcher.Host(pk.PasskeyItem!.WebsiteUrl ?? pk.PasskeyItem.Website ?? "") is { } h) passkeySites.Add(h);

        // Map password -> items that use it (for reuse detection). Only credentials with a password.
        var byPassword = new Dictionary<string, List<SecurityAuditEntry>>(StringComparer.Ordinal);
        int credentials = 0;

        foreach (var item in list)
        {
            var password = GetPassword(item);
            if (string.IsNullOrEmpty(password)) continue;

            credentials++;
            var entry = new SecurityAuditEntry { ItemId = item.Id, Title = string.IsNullOrWhiteSpace(item.Title) ? "(untitled)" : item.Title };

            // Weak
            var website = GetWebsite(item);
            var changeUrl = SiteSecurityDirectory.ChangePasswordUrl(website);
            entry = new SecurityAuditEntry { ItemId = item.Id, Title = entry.Title, Link = changeUrl, LinkText = changeUrl == null ? null : "Change password" };

            var strength = _strength.Evaluate(password);
            if (strength.Score <= 1)
                weak.Add(new SecurityAuditEntry { ItemId = item.Id, Title = entry.Title, Detail = strength.Label, Link = changeUrl, LinkText = entry.LinkText });

            // Reused
            if (!byPassword.TryGetValue(password!, out var bucket))
            {
                bucket = new List<SecurityAuditEntry>();
                byPassword[password!] = bucket;
            }
            bucket.Add(entry);

            // Old
            var age = now - item.LastModified;
            if (age.TotalDays > oldPasswordDays)
                old.Add(new SecurityAuditEntry { ItemId = item.Id, Title = entry.Title, Detail = $"{(int)age.TotalDays} days old" });

            // Missing 2FA — a login with a website but no stored authenticator secret. With the 2fa.directory lists,
            // only sites that actually offer authenticator codes are listed, with their set-up instructions.
            var site = useDirectory ? directory!.Lookup(website) : null;
            if (item.Type == ItemType.Login && !string.IsNullOrWhiteSpace(website) && !TotpHelper.HasSecret(item) &&
                (!useDirectory || site?.SupportsAuthenticatorCodes == true))
                missing2fa.Add(new SecurityAuditEntry
                {
                    ItemId = item.Id, Title = entry.Title,
                    Detail = site != null ? $"{site.Domain} offers authenticator codes" : null,
                    Link = site?.TwoFactorHelpUrl, LinkText = site?.TwoFactorHelpUrl == null ? null : "Set up 2FA"
                });

            // Passkeys: the site offers passkey sign-in and no passkey is saved for it yet.
            if (site?.SupportsPasskeySignIn == true && BrowserLink.SiteMatcher.Host(website ?? "") is { } host &&
                !passkeySites.Any(p => p == host || host.EndsWith("." + p) || p.EndsWith("." + host)))
                passkeys.Add(new SecurityAuditEntry
                {
                    ItemId = item.Id, Title = entry.Title, Detail = $"{site.Domain} supports passkeys",
                    Link = site.PasskeyHelpUrl, LinkText = site.PasskeyHelpUrl == null ? null : "Set up a passkey"
                });

            // Unsecured website — the stored URL uses plain http:// instead of https://.
            if (IsUnsecuredUrl(website))
                unsecured.Add(new SecurityAuditEntry { ItemId = item.Id, Title = entry.Title, Detail = "http:// — not encrypted" });
        }

        var reused = byPassword.Values
            .Where(g => g.Count > 1)
            .Select(g => new ReusedPasswordGroup { Items = g })
            .OrderByDescending(g => g.Count)
            .ToList();

        int reusedItemCount = reused.Sum(g => g.Count);

        // Score: penalise weak hardest, then reuse, unsecured sites, then age. Normalised by credential count.
        int score;
        if (credentials == 0)
        {
            score = 100;
        }
        else
        {
            double penalty = (weak.Count * 1.0 + reusedItemCount * 0.8 + unsecured.Count * 0.5 + old.Count * 0.4) / credentials;
            score = Math.Clamp((int)Math.Round(100 * (1 - penalty)), 0, 100);
        }

        var (rating, color) = score switch
        {
            >= 90 => ("Excellent", "#059669"),
            >= 75 => ("Good",      "#16A34A"),
            >= 50 => ("Fair",      "#D97706"),
            >= 25 => ("Poor",      "#EA580C"),
            _     => ("At risk",   "#DC2626"),
        };

        return new VaultHealthReport
        {
            Score = score,
            Rating = rating,
            ColorHex = color,
            TotalCredentials = credentials,
            WeakPasswords = weak,
            ReusedPasswords = reused,
            OldPasswords = old,
            MissingTwoFactor = missing2fa,
            UnsecuredWebsites = unsecured,
            PasskeysAvailable = passkeys,
            TwoFactorFromDirectory = useDirectory
        };
    }

    private static string? GetPassword(PasswordItem item)
        => item.LoginItem?.Password ?? item.Password;

    private static string? GetWebsite(PasswordItem item)
        => item.WebsiteUrl ?? item.LoginItem?.WebsiteUrl ?? item.LoginItem?.Website ?? item.Website;

    // True when the URL explicitly uses the insecure http:// scheme (https:// and
    // scheme-less values are treated as secure to avoid false positives).
    private static bool IsUnsecuredUrl(string? url)
        => !string.IsNullOrWhiteSpace(url)
           && url.TrimStart().StartsWith("http://", StringComparison.OrdinalIgnoreCase);
}
