using NUnit.Framework;
using VaultGuard.Models;
using VaultGuard.Services.Utilities;

namespace VaultGuard.BackEnd.Tests.Services;

[TestFixture]
public class TotpHelperTests
{
    private const string Secret = "GEZDGNBVGY3TQOJQ";

    [Test]
    public void SetSecret_AddsCustomField_GetSecretReturnsIt()
    {
        var item = new PasswordItem { Title = "Login" };

        TotpHelper.SetSecret(item, Secret);

        Assert.That(TotpHelper.GetSecret(item), Is.EqualTo(Secret));
        Assert.That(TotpHelper.HasSecret(item), Is.True);
        Assert.That(item.CustomFields, Has.Exactly(1)
            .Matches<CustomField>(f => f.Name == TotpHelper.TotpCustomFieldName));
    }

    [Test]
    public void SetSecret_Trims_Whitespace()
    {
        var item = new PasswordItem { Title = "Login" };
        TotpHelper.SetSecret(item, "   " + Secret + "  ");
        Assert.That(TotpHelper.GetSecret(item), Is.EqualTo(Secret));
    }

    [Test]
    public void SetSecret_Updates_ExistingField_WithoutDuplicating()
    {
        var item = new PasswordItem { Title = "Login" };
        TotpHelper.SetSecret(item, Secret);
        TotpHelper.SetSecret(item, "NEWSECRET234567");

        Assert.That(TotpHelper.GetSecret(item), Is.EqualTo("NEWSECRET234567"));
        Assert.That(item.CustomFields.Count(f => f.Name == TotpHelper.TotpCustomFieldName), Is.EqualTo(1));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void SetSecret_EmptyValue_RemovesField(string? empty)
    {
        var item = new PasswordItem { Title = "Login" };
        TotpHelper.SetSecret(item, Secret);

        TotpHelper.SetSecret(item, empty);

        Assert.That(TotpHelper.GetSecret(item), Is.Null);
        Assert.That(TotpHelper.HasSecret(item), Is.False);
        Assert.That(item.CustomFields.Any(f => f.Name == TotpHelper.TotpCustomFieldName), Is.False);
    }

    [Test]
    public void GetSecret_NullItem_ReturnsNull()
    {
        Assert.That(TotpHelper.GetSecret(null), Is.Null);
        Assert.That(TotpHelper.HasSecret(null), Is.False);
    }
}
