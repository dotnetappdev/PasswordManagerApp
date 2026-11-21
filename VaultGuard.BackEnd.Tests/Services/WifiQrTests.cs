using NUnit.Framework;
using VaultGuard.Models;
using VaultGuard.Services.Services;
using VaultGuard.Services.Utilities;

namespace VaultGuard.BackEnd.Tests.Services;

[TestFixture]
public class WifiQrTests
{
    [Test]
    public void Wpa_Network() =>
        Assert.That(WifiQr.Payload("HomeNet", "pa55word", SecurityType.WPA2), Is.EqualTo("WIFI:T:WPA;S:HomeNet;P:pa55word;;"));

    [Test]
    public void SpecialCharacters_AreEscaped() =>
        Assert.That(WifiQr.Payload(@"My;Net:""5G""", @"a\b,c;d", SecurityType.WPA3),
            Is.EqualTo(@"WIFI:T:WPA;S:My\;Net\:\""5G\"";P:a\\b\,c\;d;;"));

    [Test]
    public void OpenNetwork_HasNoPassword()
    {
        Assert.That(WifiQr.Payload("Cafe", null, SecurityType.Open), Is.EqualTo("WIFI:T:nopass;S:Cafe;;"));
        Assert.That(WifiQr.Payload("Cafe", "ignored", SecurityType.Open), Is.EqualTo("WIFI:T:nopass;S:Cafe;;"));
    }

    [Test]
    public void Wep_AndHidden() =>
        Assert.That(WifiQr.Payload("Old", "12345", SecurityType.WEP, hidden: true), Is.EqualTo("WIFI:T:WEP;S:Old;P:12345;H:true;;"));

    [Test]
    public void Networks_IncludeGuest_AndFallBackToTitle()
    {
        var wifi = new WiFiItem { NetworkName = "", Password = "main", GuestNetworkName = "Guests", GuestNetworkPassword = "welcome", SecurityType = SecurityType.WPA2 };
        var nets = WifiQr.NetworksFor(wifi, "Home router");
        Assert.That(nets.Select(n => (n.Label, n.Ssid, n.Password)),
            Is.EqualTo(new[] { ("Main network", "Home router", "main"), ("Guest network", "Guests", "welcome") }));
        Assert.That(WifiQr.NetworksFor(null, "x"), Is.Empty);
    }

    [Test]
    public void QrImage_IsProduced() =>
        Assert.That(new QrCodeService().GeneratePng(WifiQr.Payload("HomeNet", "pw", SecurityType.WPA2)).Length, Is.GreaterThan(100));
}
