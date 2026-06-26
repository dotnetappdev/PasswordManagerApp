using System.Windows.Media;

namespace VaultGuard.WPF.Models;

/// <summary>
/// A single global-search result. Carries everything the AutoSuggestBox item template needs to
/// render an icon (type glyph in a colored tile) alongside the title/subtitle — the same pattern
/// other password managers and OS search bars use so results are recognisable at a glance.
/// </summary>
public sealed class SearchSuggestion
{
    public int ItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;

    /// <summary>Segoe Fluent Icons glyph for the item type.</summary>
    public string Glyph { get; set; } = "";

    /// <summary>Tile background brush — colour-coded by the item's brand.</summary>
    public Brush IconBrush { get; set; } = Brushes.Gray;

    /// <summary>
    /// The item's real brand icon (custom icon or website favicon) shown on top of the tile.
    /// Null when none is available, so the type <see cref="Glyph"/> shows through as a fallback.
    /// </summary>
    public ImageSource? BrandImage { get; set; }

    // AutoSuggestBox falls back to ToString() for its text when a suggestion is chosen.
    public override string ToString() => Title;
}
