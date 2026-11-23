using NUnit.Framework;
using VaultGuard.Models;
using VaultGuard.Services.Services;

namespace VaultGuard.BackEnd.Tests.Services;

/// <summary>Watchtower: 2FA only where the site offers it, passkey suggestions, and change-password links.</summary>
[TestFixture]
public class WatchtowerTests
{
    // Shaped like api.2fa.directory/v3/tfa.json and passkeys-api.2fa.directory/v1/all.json.
    private const string Tfa = """
        [["GitHub",{"domain":"github.com","tfa":["sms","totp","u2f"],"documentation":"https://docs.github.com/2fa","keywords":[]}],
         ["Example Mail",{"domain":"mail.example","tfa":["sms"],"keywords":[]}],
         ["Big Bank",{"domain":"bigbank.co.uk","additional-domains":["bigbank.com"],"tfa":["totp"],"documentation":"https://bigbank.co.uk/2fa","keywords":[]}]]
        """;
    private const string Passkeys = """
        {"github.com":{"mfa":"allowed","passwordless":"allowed","documentation":"https://docs.github.com/passkeys"},
         "only-mfa.example":{"mfa":"allowed"}}
        """;

    private static PasswordItem Login(int id, string title, string url, string pw, string? totp = null, int ageDays = 1) => new()
    {
        Id = id, Title = title, Type = ItemType.Login, LastModified = DateTime.UtcNow.AddDays(-ageDays),
        LoginItem = new LoginItem { WebsiteUrl = url, Username = "me", Password = pw, TotpSecret = totp }
    };

    private static SecurityAuditService Audit() => new(new PasswordStrengthService());

    [Test]
    public void Directory_MatchesHostsAndParents_NotBareSuffixes()
    {
        var dir = SiteSecurityDirectory.FromJson(Tfa, Passkeys);
        Assert.That(dir.Lookup("https://login.github.com/session")?.SupportsAuthenticatorCodes, Is.True);
        Assert.That(dir.Lookup("github.com")?.SupportsPasskeySignIn, Is.True);
        Assert.That(dir.Lookup("https://www.bigbank.com")?.Domain, Is.EqualTo("bigbank.com"), "additional domains count");
        Assert.That(dir.Lookup("mail.example"), Is.Null, "SMS only isn't listed as authenticator codes");
        Assert.That(dir.Lookup("only-mfa.example"), Is.Null, "passkeys as a second factor only isn't passkey sign-in");
        Assert.That(dir.Lookup("co.uk"), Is.Null);
        Assert.That(dir.Lookup("evilgithub.com"), Is.Null);
    }

    [Test]
    public void Report_Uses_TheDirectory_WhenLoaded()
    {
        var items = new[]
        {
            Login(1, "GitHub", "https://github.com", "S7r0ng-Unique-Pass-1!"),
            Login(2, "Mail", "https://mail.example", "S7r0ng-Unique-Pass-2!"),
            Login(3, "Bank", "https://online.bigbank.co.uk", "S7r0ng-Unique-Pass-3!", totp: "JBSWY3DPEHPK3PXP"),
            Login(4, "Forum", "https://forum.example", "password"),
        };
        var without = Audit().Analyze(items);
        Assert.That(without.MissingTwoFactor.Select(e => e.Title), Is.EquivalentTo(new[] { "GitHub", "Mail", "Forum" }), "old behaviour without the lists");

        var report = Audit().Analyze(items, SiteSecurityDirectory.FromJson(Tfa, Passkeys));
        Assert.That(report.TwoFactorFromDirectory, Is.True);
        var gh = report.MissingTwoFactor.Single();
        Assert.That(gh.Title, Is.EqualTo("GitHub"), "only sites that offer authenticator codes (Bank already has one)");
        Assert.That(gh.Link, Is.EqualTo("https://docs.github.com/2fa"));
        Assert.That(report.PasskeysAvailable.Single().Link, Is.EqualTo("https://docs.github.com/passkeys"));

        var weak = report.WeakPasswords.Single();
        Assert.That(weak.Title, Is.EqualTo("Forum"));
        Assert.That(weak.Link, Is.EqualTo("https://forum.example/.well-known/change-password"));
        Assert.That(weak.LinkText, Is.EqualTo("Change password"));
    }

    [Test]
    public void PasskeySuggestion_SkippedWhenOneIsSaved()
    {
        var items = new[]
        {
            Login(1, "GitHub", "https://github.com", "S7r0ng-Unique-Pass-1!"),
            new PasswordItem { Id = 2, Title = "GitHub passkey", Type = ItemType.Passkey, PasskeyItem = new PasskeyItem { WebsiteUrl = "https://github.com" } }
        };
        Assert.That(Audit().Analyze(items, SiteSecurityDirectory.FromJson(Tfa, Passkeys)).PasskeysAvailable, Is.Empty);
    }

    [Test]
    public void ChangePasswordUrl_IsTheWellKnownAddress()
    {
        Assert.That(SiteSecurityDirectory.ChangePasswordUrl("http://www.example.com/login?x=1"), Is.EqualTo("https://example.com/.well-known/change-password"));
        Assert.That(SiteSecurityDirectory.ChangePasswordUrl(""), Is.Null);
        Assert.That(SiteSecurityDirectory.ChangePasswordUrl("localhost"), Is.Null);
    }
}
