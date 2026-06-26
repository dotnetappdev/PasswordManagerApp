using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace VaultGuard.WPF.Helpers;

/// <summary>
/// Applies Windows 11 desktop window polish via DWM: the Mica system backdrop, an immersive
/// dark title bar that matches the app's dark theme, and rounded window corners.
///
/// <para>
/// Everything degrades gracefully: on Windows 10 only the dark title bar applies, and on older
/// builds the calls are skipped. All P/Invoke is wrapped so a failure never affects the app.
/// </para>
/// </summary>
public static class Win11Chrome
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;          // Win10 2004+ / Win11
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;         // Win11
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;              // Win11 22H2+
    private const int DWMWA_MICA_EFFECT = 1029;                    // Win11 21H2 (pre-22H2) undocumented

    private const int DWMWCP_ROUND = 2;                            // rounded corners
    private const int DWMSBT_MAINWINDOW = 2;                       // Mica

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int pvAttribute, int cbAttribute);

    /// <summary>
    /// Applies Mica + dark title bar + rounded corners to a window. Safe to call before or after
    /// the window has a handle — if there is no handle yet it defers until <c>SourceInitialized</c>.
    /// </summary>
    public static void Apply(Window window, bool dark = true)
    {
        if (window == null) return;

        var helper = new WindowInteropHelper(window);
        if (helper.Handle == IntPtr.Zero)
        {
            window.SourceInitialized += OnSourceInitialized;
            void OnSourceInitialized(object? sender, EventArgs e)
            {
                window.SourceInitialized -= OnSourceInitialized;
                ApplyCore(new WindowInteropHelper(window).Handle, window, dark);
            }
            return;
        }

        ApplyCore(helper.Handle, window, dark);
    }

    private static void ApplyCore(IntPtr hwnd, Window window, bool dark)
    {
        if (hwnd == IntPtr.Zero) return;

        try
        {
            var build = Environment.OSVersion.Version.Build;

            // Dark title bar — Windows 10 2004 (19041) and up.
            int useDark = dark ? 1 : 0;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));

            // Rounded corners + Mica are Windows 11 only (build 22000+).
            if (build >= 22000)
            {
                int corner = DWMWCP_ROUND;
                DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));

                if (build >= 22621)
                {
                    // 22H2+: the documented backdrop-type attribute.
                    int backdrop = DWMSBT_MAINWINDOW;
                    DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));
                }
                else
                {
                    // 21H2: the earlier Mica toggle.
                    int mica = 1;
                    DwmSetWindowAttribute(hwnd, DWMWA_MICA_EFFECT, ref mica, sizeof(int));
                }

                // Let the Mica material show through: a transparent window background composites
                // with the DWM backdrop. Opaque child surfaces (e.g. the content area) still cover
                // it, so only the intentionally-translucent regions (the nav pane) reveal Mica.
                window.Background = Brushes.Transparent;
            }
        }
        catch
        {
            // Window polish is purely cosmetic — never let it disrupt startup.
        }
    }
}
