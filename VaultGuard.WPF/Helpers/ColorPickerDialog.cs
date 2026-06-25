using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace VaultGuard.WPF.Helpers;

/// <summary>
/// Old-school modal colour picker: a basic-colours palette grid + RGB sliders + a hex box with a
/// live preview, in the spirit of the classic Windows colour dialog.
///
/// Deliberately a <see cref="Window"/> rather than a ModernWpf ContentDialog so it can be opened on
/// top of the vault/category ContentDialogs (ModernWpf forbids nesting ContentDialogs). Returns the
/// chosen colour as a "#RRGGBB" string, or null if cancelled.
/// </summary>
public sealed class ColorPickerDialog : Window
{
    private bool _syncing;
    private readonly Slider _r, _g, _b;
    private readonly TextBox _hex;
    private readonly Border _preview;

    public string? SelectedHex { get; private set; }

    // Classic basic-colours palette (8 columns × 6 rows).
    private static readonly string[] Palette =
    {
        "#000000","#404040","#808080","#C0C0C0","#FFFFFF","#7F0000","#FF0000","#FF7F7F",
        "#7F3F00","#FF7F00","#FFBF7F","#7F7F00","#FFFF00","#FFFF7F","#007F00","#00FF00",
        "#7FFF7F","#007F7F","#00FFFF","#7FFFFF","#00007F","#0000FF","#7F7FFF","#3F007F",
        "#7F00FF","#BF7FFF","#7F007F","#FF00FF","#FF7FFF","#1E293B","#2563EB","#3B82F6",
        "#10B981","#059669","#F59E0B","#D97706","#EF4444","#DC2626","#8B5CF6","#7C3AED",
        "#EC4899","#0891B2","#374151","#6B7280","#14B8A6","#84CC16","#F97316","#A855F7",
    };

    public ColorPickerDialog(string? initialHex)
    {
        Title = "Choose a Colour";
        SizeToContent = SizeToContent.WidthAndHeight;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        Background = ResBrush("ModernBackgroundBrush", Color.FromRgb(0x1A, 0x1A, 0x1A));
        Foreground = ResBrush("ModernTextPrimaryBrush", Colors.White);

        var labelBrush = ResBrush("ModernTextSecondaryBrush", Color.FromRgb(0x9D, 0x9D, 0x9D));

        var root = new Grid { Margin = new Thickness(16) };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });

        // ── Basic colours palette ──────────────────────────────────────────────
        var paletteWrap = new WrapPanel { Width = 8 * 30, VerticalAlignment = VerticalAlignment.Top };
        foreach (var hex in Palette)
        {
            var swatch = new Border
            {
                Width = 26, Height = 26, Margin = new Thickness(2),
                CornerRadius = new CornerRadius(4),
                Background = BrushFrom(hex),
                BorderBrush = ResBrush("ModernBorderBrush", Color.FromRgb(0x3A, 0x3A, 0x3A)),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = hex
            };
            var captured = hex;
            swatch.MouseLeftButtonDown += (_, _) => ApplyHex(captured);
            paletteWrap.Children.Add(swatch);
        }
        var paletteCol = new StackPanel();
        paletteCol.Children.Add(MakeLabel("Basic colours", labelBrush));
        paletteCol.Children.Add(paletteWrap);
        Grid.SetColumn(paletteCol, 0);
        root.Children.Add(paletteCol);

        // ── RGB + hex + preview ────────────────────────────────────────────────
        var right = new StackPanel();
        right.Children.Add(MakeLabel("Preview", labelBrush));
        _preview = new Border
        {
            Height = 56, CornerRadius = new CornerRadius(6),
            BorderBrush = ResBrush("ModernBorderBrush", Color.FromRgb(0x3A, 0x3A, 0x3A)),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 0, 12)
        };
        right.Children.Add(_preview);

        _r = MakeChannel(right, "Red", labelBrush);
        _g = MakeChannel(right, "Green", labelBrush);
        _b = MakeChannel(right, "Blue", labelBrush);

        right.Children.Add(MakeLabel("Hex", labelBrush));
        _hex = new TextBox
        {
            Style = Application.Current.Resources["ModernTextBoxStyle"] as Style,
            MaxLength = 7, Margin = new Thickness(0, 0, 0, 16)
        };
        _hex.LostFocus += (_, _) => { if (TryParse(_hex.Text, out var c)) ApplyColor(c); else SyncFromChannels(); };
        _hex.KeyDown += (s, e) => { if (e.Key == System.Windows.Input.Key.Enter && TryParse(_hex.Text, out var c)) ApplyColor(c); };
        right.Children.Add(_hex);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var ok = new Button { Content = "OK", MinWidth = 80, Margin = new Thickness(0, 0, 8, 0), IsDefault = true,
                              Style = Application.Current.Resources["ModernPrimaryButtonStyle"] as Style };
        var cancel = new Button { Content = "Cancel", MinWidth = 80, IsCancel = true,
                                  Style = Application.Current.Resources["ModernSecondaryButtonStyle"] as Style };
        ok.Click += (_, _) => { SelectedHex = CurrentHex(); DialogResult = true; };
        cancel.Click += (_, _) => { DialogResult = false; };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        right.Children.Add(buttons);

        Grid.SetColumn(right, 2);
        root.Children.Add(right);

        Content = root;

        ApplyHex(string.IsNullOrWhiteSpace(initialHex) ? "#2563EB" : initialHex!);
    }

    /// <summary>Shows the picker modally over <paramref name="owner"/>. Returns hex or null.</summary>
    public static string? Show(string? initialHex, Window? owner)
    {
        var dlg = new ColorPickerDialog(initialHex) { Owner = owner };
        return dlg.ShowDialog() == true ? dlg.SelectedHex : null;
    }

    private Slider MakeChannel(Panel parent, string name, Brush labelBrush)
    {
        parent.Children.Add(MakeLabel(name, labelBrush));
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
        var slider = new Slider { Minimum = 0, Maximum = 255, SmallChange = 1, LargeChange = 16, VerticalAlignment = VerticalAlignment.Center };
        var value = new TextBlock { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Foreground = Foreground };
        slider.ValueChanged += (_, _) => { value.Text = ((int)slider.Value).ToString(); SyncFromChannels(); };
        Grid.SetColumn(slider, 0); Grid.SetColumn(value, 1);
        grid.Children.Add(slider); grid.Children.Add(value);
        parent.Children.Add(grid);
        return slider;
    }

    private static TextBlock MakeLabel(string text, Brush brush) =>
        new() { Text = text, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = brush, Margin = new Thickness(0, 0, 0, 4) };

    private void ApplyHex(string hex)
    {
        if (TryParse(hex, out var c)) ApplyColor(c);
    }

    private void ApplyColor(Color c)
    {
        _syncing = true;
        _r.Value = c.R; _g.Value = c.G; _b.Value = c.B;
        _syncing = false;
        SyncFromChannels();
    }

    private void SyncFromChannels()
    {
        if (_syncing) return;
        var c = Color.FromRgb((byte)_r.Value, (byte)_g.Value, (byte)_b.Value);
        _preview.Background = new SolidColorBrush(c);
        var hex = CurrentHex();
        if (!string.Equals(_hex.Text, hex, StringComparison.OrdinalIgnoreCase))
            _hex.Text = hex;
    }

    private string CurrentHex() => $"#{(byte)_r.Value:X2}{(byte)_g.Value:X2}{(byte)_b.Value:X2}";

    private static bool TryParse(string? hex, out Color color)
    {
        color = Colors.Black;
        if (string.IsNullOrWhiteSpace(hex)) return false;
        hex = hex.Trim();
        if (!hex.StartsWith('#')) hex = "#" + hex;
        try
        {
            var obj = ColorConverter.ConvertFromString(hex);
            if (obj is Color c) { color = c; return true; }
        }
        catch { }
        return false;
    }

    private static SolidColorBrush BrushFrom(string hex) =>
        TryParse(hex, out var c) ? new SolidColorBrush(c) : new SolidColorBrush(Colors.Gray);

    private static Brush ResBrush(string key, Color fallback) =>
        Application.Current?.Resources[key] as Brush ?? new SolidColorBrush(fallback);
}
