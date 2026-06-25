using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

namespace VaultGuard.WPF.Helpers;

/// <summary>
/// Vault icons are stored as emoji so they render identically on Web (Blazor) and mobile (MAUI),
/// where Segoe Fluent Icons' private-use glyphs are unavailable. On WPF, however, flat emoji look
/// cartoonish next to the Fluent nav icons — so here we map each preset emoji to its polished
/// Segoe Fluent Icons glyph and render that instead, while the persisted value stays the emoji.
/// </summary>
public static class VaultIconHelper
{
    // The preset picker set. Source of truth = the emoji (cross-platform safe).
    public static readonly string[] PresetEmoji =
        { "🔐", "🛡️", "💼", "🏠", "🌐", "☁️", "📁", "⭐", "👤", "👥", "💳", "📱" };

    // Parallel Segoe Fluent Icons code points, built into glyphs at runtime so the source
    // stays pure ASCII. Lock, Shield, Work, Home, Globe, Cloud, Folder, Favorite, Contact,
    // People, CreditCard, CellPhone — all standard, well-supported glyphs.
    private static readonly int[] PresetCodePoints =
        { 0xE72E, 0xEA18, 0xE821, 0xE80F, 0xE774, 0xE753,
          0xE8B7, 0xE734, 0xE77B, 0xE716, 0xE8C7, 0xE8EA };

    private const int PuaStart = 0xE000;
    private const int PuaEnd   = 0xF8FF;

    private static readonly string DefaultGlyph = char.ConvertFromUtf32(0xE72E); // Lock
    private static readonly Dictionary<string, string> EmojiToFluent = BuildMap();

    private static Dictionary<string, string> BuildMap()
    {
        var map = new Dictionary<string, string>();
        for (int i = 0; i < PresetEmoji.Length && i < PresetCodePoints.Length; i++)
            map[PresetEmoji[i]] = char.ConvertFromUtf32(PresetCodePoints[i]);
        return map;
    }

    private static readonly FontFamily FluentFont = new("Segoe Fluent Icons, Segoe MDL2 Assets");
    private static readonly FontFamily EmojiFont  = new("Segoe UI Emoji, Segoe UI Symbol, Segoe UI");

    /// <summary>Resolves a stored vault icon to a (glyph, font) pair for display in WPF.</summary>
    public static (string Glyph, FontFamily Font) Resolve(string? icon)
    {
        if (string.IsNullOrWhiteSpace(icon))
            return (DefaultGlyph, FluentFont);

        if (EmojiToFluent.TryGetValue(icon, out var fluent))
            return (fluent, FluentFont);

        // Already a private-use (Fluent) glyph?
        if (icon[0] >= PuaStart && icon[0] <= PuaEnd)
            return (icon, FluentFont);

        // Unknown emoji from an older vault — render it as-is.
        return (icon, EmojiFont);
    }

    /// <summary>Applies the resolved glyph + font to an existing TextBlock.</summary>
    public static void Apply(TextBlock target, string? icon)
    {
        var (glyph, font) = Resolve(icon);
        target.Text = glyph;
        target.FontFamily = font;
    }
}
