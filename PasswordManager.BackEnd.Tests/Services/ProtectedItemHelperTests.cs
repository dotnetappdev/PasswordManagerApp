using NUnit.Framework;
using PasswordManager.Models;
using PasswordManager.Services.Utilities;

namespace PasswordManager.BackEnd.Tests.Services;

[TestFixture]
public class ProtectedItemHelperTests
{
    [Test]
    public void SetProtected_True_MarksItemProtected()
    {
        var item = new PasswordItem { Title = "Secret note" };

        ProtectedItemHelper.SetProtected(item, true);

        Assert.That(ProtectedItemHelper.IsProtected(item), Is.True);
        Assert.That(item.CustomFields.Count(f => f.Name == ProtectedItemHelper.ProtectedCustomFieldName), Is.EqualTo(1));
    }

    [Test]
    public void SetProtected_False_RemovesFlag()
    {
        var item = new PasswordItem { Title = "Secret note" };
        ProtectedItemHelper.SetProtected(item, true);

        ProtectedItemHelper.SetProtected(item, false);

        Assert.That(ProtectedItemHelper.IsProtected(item), Is.False);
        Assert.That(item.CustomFields.Any(f => f.Name == ProtectedItemHelper.ProtectedCustomFieldName), Is.False);
    }

    [Test]
    public void SetProtected_Twice_DoesNotDuplicate()
    {
        var item = new PasswordItem { Title = "Note" };
        ProtectedItemHelper.SetProtected(item, true);
        ProtectedItemHelper.SetProtected(item, true);

        Assert.That(item.CustomFields.Count(f => f.Name == ProtectedItemHelper.ProtectedCustomFieldName), Is.EqualTo(1));
    }

    [Test]
    public void IsProtected_NullOrUnmarked_ReturnsFalse()
    {
        Assert.That(ProtectedItemHelper.IsProtected(null), Is.False);
        Assert.That(ProtectedItemHelper.IsProtected(new PasswordItem()), Is.False);
    }
}
