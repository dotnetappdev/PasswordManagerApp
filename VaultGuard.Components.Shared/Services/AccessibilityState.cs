using Microsoft.JSInterop;

namespace VaultGuard.Components.Shared.Services;

/// <summary>
/// Shared accessibility preferences for the Blazor web app and the MAUI Blazor Hybrid app.
/// Values are persisted in the browser/WebView localStorage and applied to the document via
/// plain DOM interop (no custom JS file needed), so it works in both hosts.
///   • UI zoom   — a 1Password-style zoom on the whole page.
///   • Font size — the root font size used across the UI.
///   • Reduce motion / High contrast — toggled CSS classes on the document element.
/// </summary>
public static class AccessibilityState
{
    public const double MinZoom = 0.8, MaxZoom = 1.6, DefaultZoom = 1.0;
    public const double MinFont = 12, MaxFont = 24, DefaultFont = 16;

    private const string ZoomKey = "vg_a11y_zoom";
    private const string FontKey = "vg_a11y_font";
    private const string ReduceMotionKey = "vg_a11y_reduce_motion";
    private const string HighContrastKey = "vg_a11y_high_contrast";

    public record Prefs(double Zoom, double FontPx, bool ReduceMotion, bool HighContrast);

    /// <summary>
    /// Injects the global stylesheet for the reduce-motion / high-contrast classes. Call once
    /// (e.g. from the main layout's first render). A duplicate &lt;style&gt; would be harmless.
    /// </summary>
    public static async Task EnsureStylesAsync(IJSRuntime js)
    {
        const string style =
            "<style id=\"vg-a11y-styles\">" +
            "html.a11y-reduce-motion *{animation-duration:.001ms !important;animation-iteration-count:1 !important;" +
            "transition-duration:.001ms !important;scroll-behavior:auto !important;}" +
            "html.a11y-high-contrast{filter:contrast(1.18);}" +
            "</style>";
        try
        {
            await js.InvokeVoidAsync("document.head.insertAdjacentHTML", "beforeend", style);
        }
        catch { /* non-browser host — non-fatal */ }
    }

    public static async Task<Prefs> LoadAsync(IJSRuntime js)
    {
        double zoom = DefaultZoom, font = DefaultFont;
        bool reduce = false, contrast = false;
        try
        {
            var z = await js.InvokeAsync<string?>("localStorage.getItem", ZoomKey);
            if (double.TryParse(z, out var zv)) zoom = Clamp(zv, MinZoom, MaxZoom);

            var f = await js.InvokeAsync<string?>("localStorage.getItem", FontKey);
            if (double.TryParse(f, out var fv)) font = Clamp(fv, MinFont, MaxFont);

            reduce = (await js.InvokeAsync<string?>("localStorage.getItem", ReduceMotionKey)) == "true";
            contrast = (await js.InvokeAsync<string?>("localStorage.getItem", HighContrastKey)) == "true";
        }
        catch { /* non-browser host — use defaults */ }
        return new Prefs(zoom, font, reduce, contrast);
    }

    public static async Task ApplyAsync(IJSRuntime js, Prefs p)
    {
        try
        {
            await js.InvokeVoidAsync("document.body.style.setProperty", "zoom",
                p.Zoom.ToString(System.Globalization.CultureInfo.InvariantCulture));
            await js.InvokeVoidAsync("document.documentElement.style.setProperty", "font-size",
                $"{p.FontPx.ToString(System.Globalization.CultureInfo.InvariantCulture)}px");
            await js.InvokeVoidAsync("document.documentElement.classList.toggle", "a11y-reduce-motion", p.ReduceMotion);
            await js.InvokeVoidAsync("document.documentElement.classList.toggle", "a11y-high-contrast", p.HighContrast);
        }
        catch { /* non-browser host — non-fatal */ }
    }

    public static async Task SaveAsync(IJSRuntime js, Prefs p)
    {
        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", ZoomKey, p.Zoom.ToString(System.Globalization.CultureInfo.InvariantCulture));
            await js.InvokeVoidAsync("localStorage.setItem", FontKey, p.FontPx.ToString(System.Globalization.CultureInfo.InvariantCulture));
            await js.InvokeVoidAsync("localStorage.setItem", ReduceMotionKey, p.ReduceMotion ? "true" : "false");
            await js.InvokeVoidAsync("localStorage.setItem", HighContrastKey, p.HighContrast ? "true" : "false");
        }
        catch { /* non-browser host — non-fatal */ }
    }

    private static double Clamp(double v, double min, double max) => v < min ? min : v > max ? max : v;
}
