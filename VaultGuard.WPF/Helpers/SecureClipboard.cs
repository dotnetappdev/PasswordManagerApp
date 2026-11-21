using System.Windows;
using System.Windows.Threading;
using VaultGuard.WPF.Services;

namespace VaultGuard.WPF.Helpers;

/// <summary>
/// Copies vault values for pasting, the way a password manager should:
/// <list type="bullet">
/// <item>kept out of Windows clipboard history (Win+V) and cloud clipboard sync, so a password doesn't linger
/// there for days;</item>
/// <item>wiped after the "Clear clipboard after" setting, but only if the clipboard still holds what we put
/// there (never clobbers something the user copied since).</item>
/// </list>
/// Use this for every copy of vault data instead of <see cref="Clipboard.SetText(string)"/>.
/// </summary>
public static class SecureClipboard
{
    private static DispatcherTimer? _timer;
    private static string? _copied;

    /// <summary>Copies <paramref name="text"/>. Returns false if the clipboard was busy.</summary>
    public static bool Copy(string? text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        try
        {
            var data = new DataObject();
            data.SetText(text, TextDataFormat.UnicodeText);
            // Ask Windows not to keep it in clipboard history / sync it to other devices, and tell clipboard
            // managers that honour it to ignore it (same formats other password managers set).
            data.SetData("ExcludeClipboardContentFromMonitorProcessing", new System.IO.MemoryStream(new byte[] { 1, 0, 0, 0 }));
            data.SetData("CanIncludeInClipboardHistory", new System.IO.MemoryStream(new byte[] { 0, 0, 0, 0 }));
            data.SetData("CanUploadToCloudClipboard", new System.IO.MemoryStream(new byte[] { 0, 0, 0, 0 }));
            Clipboard.SetDataObject(data, copy: true);
        }
        catch (Exception ex) when (ex is System.Runtime.InteropServices.COMException or System.Runtime.InteropServices.ExternalException)
        {
            VaultGuard.Services.Logging.AppLogger.Warning("Clipboard was busy; copy failed", ex);
            return false;
        }

        _copied = text;
        _timer?.Stop();
        var seconds = WpfSecuritySettings.ClipboardClearSeconds;
        if (seconds > 0)
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(seconds) };
            _timer.Tick += (_, _) => { _timer?.Stop(); ClearIfStillOurs(); };
            _timer.Start();
        }
        return true;
    }

    /// <summary>"Copied" message including when it will be cleared.</summary>
    public static string CopiedMessage(string what)
    {
        var seconds = WpfSecuritySettings.ClipboardClearSeconds;
        return seconds > 0
            ? $"{what} copied. The clipboard clears in {VaultGuard.Services.Security.VaultSecuritySettings.DescribeClipboard(seconds)}."
            : $"{what} copied to clipboard";
    }

    /// <summary>Wipes the clipboard now if it still holds a value we copied (called when the vault locks).</summary>
    public static void ClearIfStillOurs()
    {
        _timer?.Stop();
        var ours = _copied;
        _copied = null;
        if (ours == null) return;
        try
        {
            if (Clipboard.ContainsText() && Clipboard.GetText() == ours) Clipboard.Clear();
        }
        catch (Exception ex) when (ex is System.Runtime.InteropServices.COMException or System.Runtime.InteropServices.ExternalException)
        {
            VaultGuard.Services.Logging.AppLogger.Warning("Clipboard was busy; couldn't clear it", ex);
        }
    }
}
