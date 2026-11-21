using NUnit.Framework;
using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Security;

namespace VaultGuard.BackEnd.Tests.Services;

[TestFixture]
public class VaultSecuritySettingsTests
{
    private sealed class FakeSettings : IAppSettingsService
    {
        public Dictionary<string, string> Values { get; } = new();
        public string SettingsFilePath => "";
        public string Get(string key, string fallback = "") => Values.TryGetValue(key, out var v) ? v : fallback;
        public bool GetBool(string key, bool fallback = false) => Values.TryGetValue(key, out var v) && bool.TryParse(v, out var b) ? b : fallback;
        public int GetInt(string key, int fallback = 0) => Values.TryGetValue(key, out var v) && int.TryParse(v, out var i) ? i : fallback;
        public void Set(string key, string value) => Values[key] = value;
        public void Set(string key, bool value) => Values[key] = value.ToString();
        public void Set(string key, int value) => Values[key] = value.ToString();
        public bool Save() => true;
        public bool Contains(string key) => Values.ContainsKey(key);
        public void Remove(string key) => Values.Remove(key);
        public void Reload() { }
    }

    [Test]
    public void Defaults_WhenNothingIsSaved()
    {
        var s = new FakeSettings();
        Assert.That(VaultSecuritySettings.AutoLockMinutes(s), Is.EqualTo(VaultSecuritySettings.DefaultAutoLockMinutes));
        Assert.That(VaultSecuritySettings.ClipboardClearSeconds(s), Is.EqualTo(VaultSecuritySettings.DefaultClipboardClearSeconds));
    }

    [TestCase("0", 0)]          // never
    [TestCase("1", 1)]
    [TestCase("240", 240)]
    [TestCase("-5", 15)]        // nonsense falls back to the default instead of locking instantly or never
    [TestCase("999999", 15)]
    public void AutoLock_IsReadAndSanitized(string stored, int expected)
    {
        var s = new FakeSettings(); s.Values[VaultSecuritySettings.AutoLockMinutesKey] = stored;
        Assert.That(VaultSecuritySettings.AutoLockMinutes(s), Is.EqualTo(expected));
    }

    [Test]
    public void EveryChoice_HasALabel_AndSurvivesSanitizing()
    {
        foreach (var m in VaultSecuritySettings.AutoLockChoices)
        {
            var s = new FakeSettings(); s.Set(VaultSecuritySettings.AutoLockMinutesKey, m);
            Assert.That(VaultSecuritySettings.AutoLockMinutes(s), Is.EqualTo(m));
            Assert.That(VaultSecuritySettings.DescribeAutoLock(m), Is.Not.Empty);
        }
        foreach (var sec in VaultSecuritySettings.ClipboardChoices)
        {
            var s = new FakeSettings(); s.Set(VaultSecuritySettings.ClipboardClearSecondsKey, sec);
            Assert.That(VaultSecuritySettings.ClipboardClearSeconds(s), Is.EqualTo(sec));
            Assert.That(VaultSecuritySettings.DescribeClipboard(sec), Is.Not.Empty);
        }
        Assert.That(VaultSecuritySettings.DescribeAutoLock(0), Is.EqualTo("Never"));
        Assert.That(VaultSecuritySettings.DescribeClipboard(90), Is.EqualTo("90 seconds"));
        Assert.That(VaultSecuritySettings.DescribeClipboard(120), Is.EqualTo("2 minutes"));
    }
}

[TestFixture]
public class LockPolicyResolutionTests
{
    private static readonly VaultGuardPolicy NoPolicy = VaultGuardPolicy.None;

    [Test]
    public void SameAsWindows_FollowsTheWindowsTimeout()
    {
        var win = new WindowsLockInfo(TimeSpan.FromMinutes(10), "the Windows screen saver lock");
        var r = VaultSecuritySettings.ResolveAutoLock(VaultSecuritySettings.SameAsWindows, win, NoPolicy);
        Assert.That(r.Timeout, Is.EqualTo(TimeSpan.FromMinutes(10)));
        Assert.That(r.Explanation, Does.Contain("like Windows").And.Contain("screen saver"));
        Assert.That(r.LimitedByPolicy, Is.False);
    }

    [Test]
    public void SameAsWindows_WhenWindowsNeverLocks_UsesTheDefault()
    {
        var r = VaultSecuritySettings.ResolveAutoLock(VaultSecuritySettings.SameAsWindows, WindowsLockInfo.None, NoPolicy);
        Assert.That(r.Timeout, Is.EqualTo(TimeSpan.FromMinutes(VaultSecuritySettings.DefaultAutoLockMinutes)));
        Assert.That(r.Explanation, Does.Contain("isn't set to lock"));
    }

    [Test]
    public void Windows_ShorterOfInactivityPolicyAndScreenSaverWins()
    {
        Assert.That(WindowsLockPolicy.Combine(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10)).Timeout, Is.EqualTo(TimeSpan.FromMinutes(5)));
        Assert.That(WindowsLockPolicy.Combine(TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(10)).Source, Does.Contain("screen saver"));
        Assert.That(WindowsLockPolicy.Combine(null, TimeSpan.FromSeconds(90)).Timeout, Is.EqualTo(TimeSpan.FromSeconds(90)));
        Assert.That(WindowsLockPolicy.Combine(TimeSpan.FromMinutes(5), null).Source, Does.Contain("policy"));
        Assert.That(WindowsLockPolicy.Combine(null, null).Timeout, Is.Null);
    }

    [TestCase(0, 10, 10)]    // "Never" isn't allowed under a policy
    [TestCase(60, 10, 10)]   // longer than allowed → capped
    [TestCase(5, 10, 5)]     // shorter is fine
    public void OrganizationPolicy_CapsAutoLock(int chosen, int max, int expected)
    {
        var r = VaultSecuritySettings.ResolveAutoLock(chosen, WindowsLockInfo.None, new VaultGuardPolicy(max, null, false));
        Assert.That(r.Timeout, Is.EqualTo(TimeSpan.FromMinutes(expected)));
        Assert.That(r.LimitedByPolicy, Is.EqualTo(expected != chosen));
    }

    [Test]
    public void OrganizationPolicy_CapsSameAsWindowsToo()
    {
        var win = new WindowsLockInfo(TimeSpan.FromMinutes(30), "x");
        var r = VaultSecuritySettings.ResolveAutoLock(VaultSecuritySettings.SameAsWindows, win, new VaultGuardPolicy(10, null, false));
        Assert.That(r.Timeout, Is.EqualTo(TimeSpan.FromMinutes(10)));
        Assert.That(r.Explanation, Does.StartWith("Your organization"));
    }

    [Test]
    public void OrganizationPolicy_RemovesLongerChoicesAndNever()
    {
        var p = new VaultGuardPolicy(20, 45, true);
        var lockChoices = VaultSecuritySettings.AutoLockChoicesFor(windows: true, p);
        Assert.That(lockChoices[0], Is.EqualTo(VaultSecuritySettings.SameAsWindows));
        Assert.That(lockChoices, Does.Not.Contain(0).And.Not.Contain(30).And.Contain(20).And.Contain(15));
        var clip = VaultSecuritySettings.ClipboardChoicesFor(p);
        Assert.That(clip, Does.Not.Contain(0).And.Not.Contain(60).And.Contain(45).And.Contain(30));
        Assert.That(VaultSecuritySettings.ResolveClipboard(0, p).Seconds, Is.EqualTo(45));
        Assert.That(VaultSecuritySettings.ResolveLockOnSystemLock(false, p), Is.True);
        Assert.That(VaultSecuritySettings.AutoLockChoicesFor(windows: false), Does.Not.Contain(VaultSecuritySettings.SameAsWindows));
    }

    [Test]
    public void Labels()
    {
        Assert.That(VaultSecuritySettings.DescribeAutoLock(VaultSecuritySettings.SameAsWindows), Is.EqualTo("Same as Windows"));
        Assert.That(VaultSecuritySettings.DescribeAutoLock(90), Is.EqualTo("90 minutes"));
        Assert.That(VaultSecuritySettings.DescribeAutoLock(120), Is.EqualTo("2 hours"));
    }
}
