using System.Globalization;
using System.Text.RegularExpressions;
using VaultGuard.Models;

namespace VaultGuard.Services.Utilities;

public enum ExpiryStatus { Expired, Soon, Upcoming }

/// <summary>Something in the vault that expires or renews: a card, a passport, a licence, a membership…</summary>
public sealed record ExpiryReminder(int ItemId, string ItemTitle, ItemType ItemType, string What, DateOnly Date, int DaysLeft, ExpiryStatus Status)
{
    public string When => DaysLeft switch
    {
        < -1 => $"expired {-DaysLeft} days ago",
        -1 => "expired yesterday",
        0 => "expires today",
        1 => "expires tomorrow",
        < 60 => $"in {DaysLeft} days",
        _ => $"on {Date:d MMM yyyy}"
    };
}

/// <summary>
/// Finds dates coming up in the vault: card expiry dates, and any date field whose name means expiry or renewal
/// ("Expiry date", "Expires", "Valid until", "Renewal date", "Due"…) — e.g. passports and licences imported from
/// 1Password, or fields people add themselves. None of these are secrets, so nothing needs decrypting.
/// </summary>
public static class ExpiryReminders
{
    public const int SoonDays = 30;
    public const int DefaultHorizonDays = 90;

    private static readonly Regex DateFieldName = new(
        @"expir|valid\s*(until|to|thru|through)|renew|due\s*date|^due$|end\s*date|good\s*thru",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <param name="horizonDays">How far ahead to look; things that already expired are included for this long too.</param>
    public static IReadOnlyList<ExpiryReminder> Find(IEnumerable<PasswordItem> items, DateOnly today, int horizonDays = DefaultHorizonDays)
    {
        var list = new List<ExpiryReminder>();
        foreach (var item in items)
        {
            if (item.IsDeleted || item.IsArchived) continue;

            if (item.CreditCardItem?.ExpiryDate is { Length: > 0 } exp && TryParseCardExpiry(exp, out var cardDate))
                Add(list, item, "Card expires", cardDate, today, horizonDays);

            foreach (var f in item.CustomFields ?? Enumerable.Empty<CustomField>())
            {
                if (f.IsSecret || string.IsNullOrWhiteSpace(f.Name) || !DateFieldName.IsMatch(f.Name)) continue;
                var value = f.Value;
                if (string.IsNullOrWhiteSpace(value)) continue;
                if (TryParseDate(value, out var d) || TryParseCardExpiry(value, out d))
                    Add(list, item, Capitalize(f.Name.Trim()), d, today, horizonDays);
            }
        }
        return list.OrderBy(r => r.Date).ThenBy(r => r.ItemTitle, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static void Add(List<ExpiryReminder> list, PasswordItem item, string what, DateOnly date, DateOnly today, int horizon)
    {
        var days = date.DayNumber - today.DayNumber;
        if (days > horizon || days < -horizon) return;
        var status = days < 0 ? ExpiryStatus.Expired : days <= SoonDays ? ExpiryStatus.Soon : ExpiryStatus.Upcoming;
        list.Add(new ExpiryReminder(item.Id, item.Title ?? "(untitled)", item.Type, what, date, days, status));
    }

    /// <summary>Card style "MM/YY", "MM/YYYY", "MM-YY", "YYYY-MM": valid through the last day of that month.</summary>
    public static bool TryParseCardExpiry(string value, out DateOnly date)
    {
        date = default;
        var m = Regex.Match(value.Trim(), @"^(?<m>\d{1,2})\s*[/\-.]\s*(?<y>\d{2}|\d{4})$");
        int month, year;
        if (m.Success) { month = int.Parse(m.Groups["m"].Value); year = int.Parse(m.Groups["y"].Value); }
        else
        {
            m = Regex.Match(value.Trim(), @"^(?<y>\d{4})\s*[/\-.]\s*(?<m>\d{1,2})$");
            if (!m.Success) return false;
            month = int.Parse(m.Groups["m"].Value); year = int.Parse(m.Groups["y"].Value);
        }
        if (year < 100) year += 2000;
        if (month is < 1 or > 12 || year is < 2000 or > 2100) return false;
        date = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        return true;
    }

    /// <summary>Full dates: ISO (2027-04-30) first, then the usual written forms.</summary>
    public static bool TryParseDate(string value, out DateOnly date)
    {
        var v = value.Trim();
        string[] formats = { "yyyy-MM-dd", "yyyy/MM/dd", "d MMM yyyy", "d MMMM yyyy", "MMM d, yyyy", "MMMM d, yyyy", "dd/MM/yyyy", "d/M/yyyy", "dd.MM.yyyy" };
        if (DateOnly.TryParseExact(v, formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out date)) return true;
        if (DateTime.TryParse(v, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var dt) && v.Any(char.IsLetter) == false && v.Length >= 8)
        {
            date = DateOnly.FromDateTime(dt);
            return true;
        }
        return false;
    }

    private static string Capitalize(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];
}
