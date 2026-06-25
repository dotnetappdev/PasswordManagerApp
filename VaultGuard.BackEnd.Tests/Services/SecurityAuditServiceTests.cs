using NUnit.Framework;
using PasswordManager.Models;
using PasswordManager.Services.Services;
using PasswordManager.Services.Utilities;

namespace PasswordManager.BackEnd.Tests.Services;

[TestFixture]
public class SecurityAuditServiceTests
{
    private SecurityAuditService _service = null!;

    [SetUp]
    public void Setup() => _service = new SecurityAuditService(new PasswordStrengthService());

    private static PasswordItem Login(int id, string title, string password, string? website = null, DateTime? modified = null)
        => new()
        {
            Id = id,
            Title = title,
            Type = ItemType.Login,
            Website = website,
            LastModified = modified ?? DateTime.UtcNow,
            LoginItem = new LoginItem { Password = password, WebsiteUrl = website }
        };

    [Test]
    public void Analyze_EmptyVault_ScoresPerfect()
    {
        var report = _service.Analyze(Enumerable.Empty<PasswordItem>());
        Assert.That(report.Score, Is.EqualTo(100));
        Assert.That(report.TotalCredentials, Is.EqualTo(0));
        Assert.That(report.TotalIssues, Is.EqualTo(0));
    }

    [Test]
    public void Analyze_DetectsWeakPasswords()
    {
        var items = new[]
        {
            Login(1, "Bank", "123456"),
            Login(2, "Email", "G7#kLm92!qXz&5Rt")
        };

        var report = _service.Analyze(items);

        Assert.That(report.WeakCount, Is.EqualTo(1));
        Assert.That(report.WeakPasswords[0].ItemId, Is.EqualTo(1));
    }

    [Test]
    public void Analyze_DetectsReusedPasswords()
    {
        var items = new[]
        {
            Login(1, "Site A", "ReUsed#Pass123"),
            Login(2, "Site B", "ReUsed#Pass123"),
            Login(3, "Site C", "UniquePass!99")
        };

        var report = _service.Analyze(items);

        Assert.That(report.ReusedPasswords, Has.Count.EqualTo(1));
        Assert.That(report.ReusedPasswords[0].Count, Is.EqualTo(2));
        Assert.That(report.ReusedCount, Is.EqualTo(2));
    }

    [Test]
    public void Analyze_DetectsOldPasswords()
    {
        var items = new[]
        {
            Login(1, "Ancient", "Str0ng&Fresh!2024", modified: DateTime.UtcNow.AddDays(-400)),
            Login(2, "Recent", "Str0ng&Fresh!2024b", modified: DateTime.UtcNow.AddDays(-10))
        };

        var report = _service.Analyze(items, oldPasswordDays: 365);

        Assert.That(report.OldCount, Is.EqualTo(1));
        Assert.That(report.OldPasswords[0].ItemId, Is.EqualTo(1));
    }

    [Test]
    public void Analyze_FlagsLoginsMissingTwoFactor()
    {
        var withTotp = Login(1, "Has 2FA", "Str0ng&Fresh!2024", website: "https://a.com");
        TotpHelper.SetSecret(withTotp, "GEZDGNBVGY3TQOJQ");
        var withoutTotp = Login(2, "No 2FA", "Str0ng&Fresh!2024b", website: "https://b.com");
        var noWebsite = Login(3, "No site", "Str0ng&Fresh!2024c");

        var report = _service.Analyze(new[] { withTotp, withoutTotp, noWebsite });

        Assert.That(report.MissingTwoFactorCount, Is.EqualTo(1));
        Assert.That(report.MissingTwoFactor[0].ItemId, Is.EqualTo(2));
    }

    [Test]
    public void Analyze_AllStrongUniqueFresh_ScoresHigh()
    {
        var items = new[]
        {
            Login(1, "A", "G7#kLm92!qXz&5Rt"),
            Login(2, "B", "9zP!wQ2m#Lx84Vh")
        };

        var report = _service.Analyze(items);

        Assert.That(report.Score, Is.GreaterThanOrEqualTo(90));
        Assert.That(report.Rating, Is.EqualTo("Excellent"));
    }

    [Test]
    public void Analyze_ManyWeakReused_ScoresLow()
    {
        var items = new[]
        {
            Login(1, "A", "123456"),
            Login(2, "B", "123456"),
            Login(3, "C", "password")
        };

        var report = _service.Analyze(items);

        Assert.That(report.Score, Is.LessThan(50));
    }
}
