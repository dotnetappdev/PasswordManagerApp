using NUnit.Framework;
using VaultGuard.Services.BrowserLink;

namespace VaultGuard.BackEnd.Tests.Services;

/// <summary>The browser runs its own copy of the helper, so a running helper never locks the app's build output.</summary>
[TestFixture]
public class BrowserHelperStagingTests
{
    private string _dir = null!;

    [SetUp] public void SetUp() => _dir = Path.Combine(Path.GetTempPath(), $"stage-{Guid.NewGuid():N}");
    [TearDown] public void TearDown() { try { Directory.Delete(_dir, true); } catch { } }

    [Test]
    public void Stage_CopiesHelperOutOfTheAppFolder_AndRefreshesWhenItChanges()
    {
        var app = Path.Combine(_dir, "app", "BrowserSupport");
        var root = Path.Combine(_dir, "staged");
        Directory.CreateDirectory(app);
        var exe = Path.Combine(app, "VaultGuard.BrowserExtension.NativeHost.exe");
        File.WriteAllText(exe, "v1");
        File.WriteAllText(Path.Combine(app, "helper.dll"), "lib");

        var first = BrowserHelperStaging.Stage(exe, root);
        Assert.That(first, Does.StartWith(root));
        Assert.That(File.ReadAllText(first), Is.EqualTo("v1"));
        Assert.That(File.Exists(Path.Combine(Path.GetDirectoryName(first)!, "helper.dll")), Is.True);
        Assert.That(BrowserHelperStaging.Stage(exe, root), Is.EqualTo(first), "same build: same copy");

        File.WriteAllText(exe, "v2-rebuilt");
        var second = BrowserHelperStaging.Stage(exe, root);
        Assert.That(second, Is.Not.EqualTo(first));
        Assert.That(File.ReadAllText(second), Is.EqualTo("v2-rebuilt"));
        Assert.That(Directory.Exists(Path.GetDirectoryName(first)!), Is.False, "the old copy is tidied away");
    }

    [Test]
    public void Stage_MissingHelper_ReturnsThePathUnchanged()
    {
        var exe = Path.Combine(_dir, "nope", "VaultGuard.BrowserExtension.NativeHost.exe");
        Assert.That(BrowserHelperStaging.Stage(exe, Path.Combine(_dir, "staged")), Is.EqualTo(exe));
    }
}
