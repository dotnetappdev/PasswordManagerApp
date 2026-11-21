using NUnit.Framework;
using VaultGuard.Models;
using VaultGuard.Services.Utilities;

namespace VaultGuard.BackEnd.Tests.Services;

[TestFixture]
public class ExpiryRemindersTests
{
    private static readonly DateOnly Today = new(2026, 11, 21);

    private static PasswordItem Card(int id, string title, string expiry) =>
        new() { Id = id, Title = title, Type = ItemType.CreditCard, CreditCardItem = new CreditCardItem { ExpiryDate = expiry } };

    private static PasswordItem WithField(int id, string title, string name, string value, CustomFieldType type = CustomFieldType.Date) =>
        new() { Id = id, Title = title, Type = ItemType.Login, CustomFields = new List<CustomField> { new() { Name = name, Value = value, Type = type } } };

    [TestCase("12/26", 2026, 12, 31)]
    [TestCase("12/2026", 2026, 12, 31)]
    [TestCase("02-28", 2028, 2, 29)]   // leap year: valid through the 29th
    [TestCase("2027-04", 2027, 4, 30)]
    public void CardExpiry_IsTheLastDayOfTheMonth(string text, int y, int m, int d)
    {
        Assert.That(ExpiryReminders.TryParseCardExpiry(text, out var date), Is.True);
        Assert.That(date, Is.EqualTo(new DateOnly(y, m, d)));
    }

    [TestCase("13/26")]
    [TestCase("hello")]
    [TestCase("")]
    public void CardExpiry_RejectsNonsense(string text) => Assert.That(ExpiryReminders.TryParseCardExpiry(text, out _), Is.False);

    [Test]
    public void Finds_CardsAndExpiryFields_SortedBySoonest_WithStatus()
    {
        var items = new[]
        {
            Card(1, "Visa Travel", "12/26"),                                   // 40 days → upcoming
            Card(2, "Old Amex", "10/26"),                                      // ended 31 Oct → expired
            WithField(3, "Passport", "Expiry date", "2026-12-01"),              // 10 days → soon
            WithField(4, "Gym", "Renewal date", "2027-01-05"),                  // 45 days → upcoming
            WithField(5, "Library", "Member since", "2026-12-01"),              // not an expiry field
            WithField(6, "Licence", "Valid until", "2030-01-01"),               // beyond 90 days
            WithField(7, "Bank", "Expiry", "2026-12-01", CustomFieldType.Password), // secret field: ignored
        };
        var r = ExpiryReminders.Find(items, Today);
        Assert.That(r.Select(x => x.ItemTitle), Is.EqualTo(new[] { "Old Amex", "Passport", "Visa Travel", "Gym" }));
        Assert.That(r.Select(x => x.Status), Is.EqualTo(new[] { ExpiryStatus.Expired, ExpiryStatus.Soon, ExpiryStatus.Upcoming, ExpiryStatus.Upcoming }));
        Assert.That(r[0].When, Is.EqualTo("expired 21 days ago"));
        Assert.That(r[1].When, Is.EqualTo("in 10 days"));
        Assert.That(r[1].What, Is.EqualTo("Expiry date"));
    }

    [Test]
    public void DeletedAndArchivedItems_AreSkipped()
    {
        var deleted = Card(1, "Deleted", "12/26"); deleted.IsDeleted = true;
        Assert.That(ExpiryReminders.Find(new[] { deleted }, Today), Is.Empty);
    }

    [Test]
    public void ImportedOnePasswordFormats_AreUnderstood()
    {
        // 1PUX: card expiry "MM/YYYY", identity/passport "expiry date" as yyyy-MM-dd
        var r = ExpiryReminders.Find(new[] { Card(1, "Visa", "12/2026"), WithField(2, "Passport", "expiry date", "2027-01-31") }, Today);
        Assert.That(r, Has.Count.EqualTo(2));
    }
}
