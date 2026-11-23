using VaultGuard.DAL;
using VaultGuard.Models;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.Services.Security;

/// <summary>
/// Travel mode (like 1Password's): before crossing a border, mark the vaults that are safe to travel with and turn
/// travel mode on. Everything else disappears from this app on this device (lists, search, Security Center, Quick
/// Access and the browser extension) until travel mode is turned off, which needs the master password.
///
/// Settings (per device, settings.json): TravelMode:On, TravelMode:SafeVaults (vault ids). Items that aren't in any
/// vault are hidden too. Hidden items stay in the encrypted database: travel mode hides them from anyone using the app,
/// it doesn't remove them from the disk. Only the desktop and mobile apps use it; the Web server never turns it on.
/// </summary>
public static class TravelMode
{
    private static volatile int[]? _safeVaults; // null: travel mode is off

    public static bool IsOn => _safeVaults != null;
    public static IReadOnlyList<int> SafeVaultIds => _safeVaults ?? Array.Empty<int>();

    public static void Load(IAppSettingsService settings)
    {
        _safeVaults = settings.GetBool("TravelMode:On") ? Parse(settings.Get("TravelMode:SafeVaults")) : null;
    }

    public static void TurnOn(IAppSettingsService settings, IEnumerable<int> safeVaultIds)
    {
        var ids = safeVaultIds.Distinct().ToArray();
        settings.Set("TravelMode:SafeVaults", string.Join(",", ids));
        settings.Set("TravelMode:On", true);
        settings.Save();
        _safeVaults = ids;
    }

    /// <summary>Turns travel mode off; the caller has checked the master password.</summary>
    public static void TurnOff(IAppSettingsService settings)
    {
        settings.Set("TravelMode:On", false);
        settings.Save();
        _safeVaults = null;
    }

    /// <summary>Limits a query to items in vaults marked safe for travel (no change when travel mode is off).</summary>
    public static IQueryable<PasswordItem> Apply(IQueryable<PasswordItem> items, VaultGuardDbContext db)
    {
        var safe = _safeVaults;
        if (safe == null) return items;
        return items.Where(p => p.CollectionId != null &&
            db.Collections.Any(c => c.Id == p.CollectionId && c.VaultId != null && safe.Contains(c.VaultId.Value)));
    }

    private static int[] Parse(string csv) => (csv ?? "")
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(s => int.TryParse(s, out var i) ? i : (int?)null).OfType<int>().ToArray();
}
