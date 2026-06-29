using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace VaultGuard.WPF.Helpers;

/// <summary>
/// Drives the app's adjustable font sizes. Every scalable size is exposed as a Double resource
/// (see App.xaml) that XAML references with {DynamicResource ...}. The effective size is:
///
///     effective = baseSize × RegionScale[region]                       (icon regions)
///     effective = baseSize × GlobalScale × RegionScale[region]         (text regions)
///
///   • GlobalScale  — the overall "Text size" control (Accessibility tab). 1.0 == default. Only
///                    applies to text; icon regions (MenuIcons, CardIcons) are exempt — see Apply().
///   • RegionScale  — a per-area multiplier so the user can size Menu, Quick Actions, Details and
///                    Dialogs independently of each other and of the global size. Icon regions use a
///                    much wider range (0.5–4.0 vs 0.8–1.6) since a 16-18px glyph needs real headroom
///                    to reach something like 64px.
///
/// Values persist to the same settings.json used by <see cref="AccessibilityManager"/>. Updating a
/// resource value live re-renders every control bound to it via DynamicResource — no restart needed.
/// </summary>
public static class FontScaleManager
{
    public const double MinScale = 0.8;
    public const double MaxScale = 1.6;
    public const double DefaultScale = 1.0;

    // Icon regions get a much wider range than text: a 16-18px glyph needs to reach ~64px for
    // "make the icons big" requests, and icons aren't a readability concern the way text is — so
    // they're also exempt from the global "Text size" multiplier (see Apply()) to avoid the global
    // slider quietly cancelling out a per-section icon boost (0.8 global × 1.5 region ≈ no change).
    public const double IconMinScale = 0.5;
    public const double IconMaxScale = 4.0;

    // Per-section keys used by the Settings selectors. Each targets one area and stacks on top of
    // the global "Text size" control, so the user gets a global choice AND per-section control.
    public const string Menu = "Menu";
    public const string MenuIcons = "MenuIcons";
    public const string QuickActions = "QuickActions";
    public const string Details = "Details";
    public const string Dialogs = "Dialogs";
    public const string Global = "Global";
    public const string CardIcons = "CardIcons";

    public static readonly string[] AllRegions = { Menu, MenuIcons, QuickActions, Details, Dialogs, Global, CardIcons };

    // Regions whose keys use IconMinScale/IconMaxScale and skip the global text-size multiplier.
    private static readonly HashSet<string> IconRegions = new() { MenuIcons, CardIcons };

    // Resource key → its unscaled base size in points.
    private static readonly Dictionary<string, double> BaseSizes = new()
    {
        // Global body size (no region → global scale only). Drives app-wide controls like dropdowns.
        ["AppFontSize"] = 14,
        ["MenuFontSize"] = 14,
        ["MenuIconSize"] = 16,
        ["QuickActionFontSize"] = 14,
        ["QuickActionIconSize"] = 16,
        ["DetailTitleFontSize"] = 15,
        ["DetailValueFontSize"] = 14,
        ["DetailLabelFontSize"] = 11,
        ["DialogLabelFontSize"] = 12,
        ["DialogFontSize"] = 14,
        ["DialogTitleFontSize"] = 16,
        // Item cards (the password/item list rows) — controlled by the "Global" selector.
        ["CardTitleFontSize"] = 15,
        ["CardSubtitleFontSize"] = 13,
        ["CardTagFontSize"] = 12,
        ["CardIconFontSize"] = 18,
        // Row height for the item list — without this, bigger card text just gets cramped/clipped
        // inside a fixed-height row instead of the row growing to fit it.
        ["CardRowHeight"] = 60,
    };

    // Resource key → which region's scale applies to it (besides General, which applies to all).
    private static readonly Dictionary<string, string> KeyRegion = new()
    {
        ["MenuFontSize"] = Menu,
        ["MenuIconSize"] = MenuIcons,
        ["QuickActionFontSize"] = QuickActions,
        ["QuickActionIconSize"] = QuickActions,
        ["DetailTitleFontSize"] = Details,
        ["DetailValueFontSize"] = Details,
        ["DetailLabelFontSize"] = Details,
        ["DialogLabelFontSize"] = Dialogs,
        ["DialogFontSize"] = Dialogs,
        ["DialogTitleFontSize"] = Dialogs,
        ["CardTitleFontSize"] = Global,
        ["CardSubtitleFontSize"] = Global,
        ["CardTagFontSize"] = Global,
        ["CardIconFontSize"] = CardIcons,
        ["CardRowHeight"] = Global,
    };

    private static readonly Dictionary<string, double> _regionScale = new()
    {
        [Menu] = DefaultScale,
        [MenuIcons] = DefaultScale,
        [QuickActions] = DefaultScale,
        [Details] = DefaultScale,
        [Dialogs] = DefaultScale,
        [Global] = DefaultScale,
        [CardIcons] = DefaultScale,
    };

    /// <summary>Overall (global) text-size multiplier, kept in sync with the base font-size control.</summary>
    public static double GlobalScale { get; private set; } = DefaultScale;

    private static string SettingsFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "VaultGuard", "settings.json");

    /// <summary>Loads persisted scales and applies them to the resource dictionary. Call once at startup.</summary>
    public static void Initialize()
    {
        try
        {
            var values = ReadAll();
            // Global scale tracks the saved base font size relative to the default.
            if (values.TryGetValue("AccessibilityFontSize", out var f) && double.TryParse(f, out var font))
                GlobalScale = Clamp(font / AccessibilityManager.DefaultFontSize, MinScale, MaxScale);

            foreach (var region in AllRegions)
            {
                if (values.TryGetValue(RegionKey(region), out var s) && double.TryParse(s, out var scale))
                {
                    var (min, max) = ClampRangeFor(region);
                    _regionScale[region] = Clamp(scale, min, max);
                }
            }
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Warning("Failed to load font scales", ex);
        }
        Apply();
    }

    public static double GetRegionScale(string region) =>
        _regionScale.TryGetValue(region, out var s) ? s : DefaultScale;

    public static void SetRegionScale(string region, double scale)
    {
        if (!_regionScale.ContainsKey(region)) return;
        var (min, max) = ClampRangeFor(region);
        _regionScale[region] = Clamp(scale, min, max);
        Save(RegionKey(region), _regionScale[region].ToString("0.00"));
        Apply();
    }

    private static (double Min, double Max) ClampRangeFor(string region) =>
        IconRegions.Contains(region) ? (IconMinScale, IconMaxScale) : (MinScale, MaxScale);

    /// <summary>Called when the base font-size control changes so the global scale follows it.</summary>
    public static void SetGlobalFromBaseFont(double baseFontSize)
    {
        GlobalScale = Clamp(baseFontSize / AccessibilityManager.DefaultFontSize, MinScale, MaxScale);
        Apply();
    }

    public static void ResetAll()
    {
        foreach (var region in AllRegions)
        {
            _regionScale[region] = DefaultScale;
            Save(RegionKey(region), DefaultScale.ToString("0.00"));
        }
        Apply();
    }

    /// <summary>Recomputes every adjustable size and pushes it into Application resources.</summary>
    public static void Apply()
    {
        var res = Application.Current?.Resources;
        if (res == null) return;
        try
        {
            foreach (var (key, baseSize) in BaseSizes)
            {
                string? region = KeyRegion.TryGetValue(key, out var r) ? r : null;
                double regionScale = region != null && _regionScale.TryGetValue(region, out var rs)
                    ? rs : DefaultScale;
                // Icon sizes are controlled purely by their own region — the global "Text size"
                // slider is about reading text, and multiplying it in let a low global value
                // silently cancel out a large per-section icon boost.
                double globalFactor = region != null && IconRegions.Contains(region) ? 1.0 : GlobalScale;
                double effective = baseSize * globalFactor * regionScale;
                // Keep a sane floor so nothing becomes unreadable.
                res[key] = Math.Round(Math.Max(8.0, effective), 1);
            }
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Warning("Failed to apply font scales", ex);
        }
    }

    /// <summary>The effective dialog scale (global × Dialogs section).</summary>
    public static double DialogScale => GlobalScale * GetRegionScale(Dialogs);

    /// <summary>
    /// Scales a dialog window's content so its text/layout grow with the Dialogs section setting.
    /// Dialogs hardcode dozens of font sizes each, so a proportional layout scale (the same trick the
    /// UI-zoom uses) is the reliable way to resize them without touching every TextBlock. The app's
    /// main window is left alone — it has its own accessibility zoom + per-section font resources.
    /// </summary>
    /// <summary>
    /// Dialogs that scale their text properly via {DynamicResource Dialog*FontSize} mark their root
    /// Window with Tag="FontScaled" so they opt out of the proportional layout scale — otherwise the
    /// font would grow twice (once from the resource, once from the transform).
    /// </summary>
    public const string FontScaledTag = "FontScaled";

    public static void ApplyDialogWindowScale(Window? window)
    {
        if (window == null) return;
        if (ReferenceEquals(window, Application.Current?.MainWindow)) return;
        if (window.Tag as string == FontScaledTag) return;
        try
        {
            if (window.Content is FrameworkElement root)
            {
                var scale = DialogScale;
                root.LayoutTransform = Math.Abs(scale - 1.0) < 0.001
                    ? Transform.Identity
                    : new ScaleTransform(scale, scale);
            }
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Warning("Failed to scale dialog window", ex);
        }
    }

    private static string RegionKey(string region) => $"FontScale_{region}";

    private static double Clamp(double v, double min, double max) => v < min ? min : v > max ? max : v;

    private static Dictionary<string, string> ReadAll()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
            }
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Failed to read settings.json", ex); }
        return new();
    }

    private static void Save(string key, string value)
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var values = ReadAll();
            values[key] = value;
            File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(values, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Warning("Failed to save font scale", ex);
        }
    }
}
