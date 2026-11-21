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
