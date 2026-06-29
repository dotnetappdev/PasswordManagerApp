using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace VaultGuard.WPF.Helpers;

/// <summary>
/// Central accessibility controls for the WPF app. Persists the user's choices to the shared
/// settings.json and applies them live:
///   • UI zoom   — a 1Password-style scale on the whole window content (LayoutTransform).
///   • Font size — a base font size that cascades to menus and text that inherit FontSize.
///   • Reduce motion / High contrast — persisted flags the rest of the app can read.
/// </summary>
public static class AccessibilityManager
{
    public const double MinZoom = 0.8;   // 80%
    public const double MaxZoom = 1.6;   // 160%
    public const double DefaultZoom = 1.0;

    public const double MinFontSize = 11;
    public const double MaxFontSize = 22;
    public const double DefaultFontSize = 14;

    private const string ZoomKey = "AccessibilityUiZoom";
    private const string FontSizeKey = "AccessibilityFontSize";
    private const string ReduceMotionKey = "AccessibilityReduceMotion";
    private const string HighContrastKey = "AccessibilityHighContrast";

    public static double UiZoom { get; private set; } = DefaultZoom;
    public static double BaseFontSize { get; private set; } = DefaultFontSize;
    public static bool ReduceMotion { get; private set; }
    public static bool HighContrast { get; private set; }

    private static string SettingsFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "VaultGuard", "settings.json");

    /// <summary>Loads persisted values. Call once at startup before applying.</summary>
    public static void Load()
    {
        try
        {
            var values = ReadAll();
            if (values.TryGetValue(ZoomKey, out var z) && double.TryParse(z, out var zoom))
                UiZoom = Clamp(zoom, MinZoom, MaxZoom);
            if (values.TryGetValue(FontSizeKey, out var f) && double.TryParse(f, out var font))
                BaseFontSize = Clamp(font, MinFontSize, MaxFontSize);
            if (values.TryGetValue(ReduceMotionKey, out var rm) && bool.TryParse(rm, out var reduce))
                ReduceMotion = reduce;
            if (values.TryGetValue(HighContrastKey, out var hc) && bool.TryParse(hc, out var high))
                HighContrast = high;
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Warning("Failed to load accessibility settings", ex);
        }
    }

    /// <summary>Applies the current zoom + font size to the given window's content.</summary>
    public static void ApplyTo(Window? window)
    {
        if (window == null) return;
        try
        {
            if (window.Content is FrameworkElement root)
            {
                root.LayoutTransform = Math.Abs(UiZoom - 1.0) < 0.001
                    ? Transform.Identity
                    : new ScaleTransform(UiZoom, UiZoom);
            }
            window.FontSize = BaseFontSize;
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Warning("Failed to apply accessibility settings", ex);
        }
    }

    /// <summary>Re-applies to the app's main window (used after a setting changes).</summary>
    public static void ApplyToMainWindow() => ApplyTo(Application.Current?.MainWindow);

    public static void SetZoom(double zoom)
    {
        UiZoom = Clamp(zoom, MinZoom, MaxZoom);
        Save(ZoomKey, UiZoom.ToString("0.00"));
        ApplyToMainWindow();
    }

    public static void SetFontSize(double fontSize)
    {
        BaseFontSize = Clamp(fontSize, MinFontSize, MaxFontSize);
        Save(FontSizeKey, BaseFontSize.ToString("0.#"));
        ApplyToMainWindow();
        // The base size is the global text-size choice; cascade it to the adjustable per-section
        // sizes so {DynamicResource} text (menus, quick actions, details, dialogs) scales too.
        FontScaleManager.SetGlobalFromBaseFont(BaseFontSize);
    }

    public static void SetReduceMotion(bool value)
    {
        ReduceMotion = value;
        Save(ReduceMotionKey, value.ToString());
    }

    public static void SetHighContrast(bool value)
    {
        HighContrast = value;
        Save(HighContrastKey, value.ToString());
        // High-contrast brush swaps are applied by ThemeService on next theme refresh; expose the
        // flag here so the rest of the app can honour it.
    }

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
            VaultGuard.Services.Logging.AppLogger.Warning("Failed to save accessibility setting", ex);
        }
    }
}
