using System.Text;
using VaultGuard.Models;

namespace VaultGuard.Services.Utilities;

/// <summary>
/// Builds the "join this Wi-Fi" text that phone cameras understand (Android and iOS both read it from a QR code):
/// <c>WIFI:T:WPA;S:MyNetwork;P:secret;;</c>. Special characters are escaped as the format requires.
/// </summary>
public static class WifiQr
{
    public sealed record Network(string Ssid, string? Password, SecurityType Security, bool Hidden, string Label);

    /// <summary>The networks a Wi-Fi item can share: the main one and, if set up, the guest network.</summary>
    public static IReadOnlyList<Network> NetworksFor(WiFiItem? wifi, string fallbackName)
    {
        var list = new List<Network>();
        if (wifi == null) return list;
        var ssid = string.IsNullOrWhiteSpace(wifi.NetworkName) ? fallbackName : wifi.NetworkName!;
        if (!string.IsNullOrWhiteSpace(ssid))
            list.Add(new Network(ssid, wifi.Password, wifi.SecurityType, wifi.IsHidden, "Main network"));
        if (!string.IsNullOrWhiteSpace(wifi.GuestNetworkName))
            list.Add(new Network(wifi.GuestNetworkName!, wifi.GuestNetworkPassword, wifi.SecurityType, false, "Guest network"));
        return list;
    }

    /// <summary>The QR payload. Open networks (or no password) are shared as "nopass".</summary>
    public static string Payload(Network n) => Payload(n.Ssid, n.Password, n.Security, n.Hidden);

    public static string Payload(string ssid, string? password, SecurityType security, bool hidden = false)
    {
        var open = security == SecurityType.Open || string.IsNullOrEmpty(password);
        // WPA covers WPA/WPA2 and WPA3 transition networks; it's what phone cameras expect.
        var type = open ? "nopass" : security == SecurityType.WEP ? "WEP" : "WPA";
        var sb = new StringBuilder("WIFI:");
        sb.Append("T:").Append(type).Append(';');
        sb.Append("S:").Append(Escape(ssid)).Append(';');
        if (!open) sb.Append("P:").Append(Escape(password!)).Append(';');
        if (hidden) sb.Append("H:true;");
        sb.Append(';');
        return sb.ToString();
    }

    /// <summary>Backslash-escapes \ ; , : and " as the Wi-Fi QR format requires.</summary>
    public static string Escape(string value)
    {
        var sb = new StringBuilder(value.Length + 8);
        foreach (var c in value)
        {
            if (c is '\\' or ';' or ',' or ':' or '"') sb.Append('\\');
            sb.Append(c);
        }
        return sb.ToString();
    }
}
