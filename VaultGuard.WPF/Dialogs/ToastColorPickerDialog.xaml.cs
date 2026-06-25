using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VaultGuard.WPF.Services;

namespace VaultGuard.WPF.Dialogs
{
    /// <summary>
    /// Old-school (MS Paint style) colour picker for a single toast type. Lets the user choose a
    /// primary (accent) colour, a secondary (background) colour and an icon glyph, with a live preview.
    /// </summary>
    public partial class ToastColorPickerDialog : Window
    {
        // The classic 48-colour Windows palette (8 columns x 6 rows).
        private static readonly string[] BasicPalette =
        {
            "#000000","#808080","#800000","#808000","#008000","#008080","#000080","#800080",
            "#808040","#004040","#0080FF","#004080","#8000FF","#804000","#FFFFFF","#C0C0C0",
            "#FF0000","#FFFF00","#00FF00","#00FFFF","#0000FF","#FF00FF","#FFFF80","#00FF80",
            "#80FFFF","#8080FF","#FF0080","#FF8040","#404040","#A0A0A0","#10B981","#EF4444",
            "#F59E0B","#60A5FA","#0A2E20","#2D0A0A","#2D1E06","#0A1929","#5B4FCF","#059669",
            "#DC2626","#8B5CF6","#EC4899","#3B82F6","#1F2937","#111827","#0F172A","#E5E7EB"
        };

        // Curated, verified Segoe MDL2 Assets glyphs that read well as notification icons.
        private static readonly int[] IconGlyphs =
        {
            0xE73E, // CheckMark
            0xE930, // Completed (check in circle)
            0xE8FB, // Accept (bold check)
            0xE946, // Info (i in circle)
            0xE783, // Error (! in circle)
            0xEA39, // ErrorBadge
            0xE7BA, // Warning (! in triangle outline)
            0xE814, // IncidentTriangle (filled warning)
            0xE72E, // Lock
            0xE713, // Settings (gear)
            0xE7C1, // Flag
            0xE8BD, // Message
            0xEB51, // Heart
            0xE735  // FavoriteStarFill
        };

        private string _primary;
        private string _secondary;
        private int _iconGlyph;

        /// <summary>The result the caller should apply if ShowDialog() returned true.</summary>
        public ToastTheme Result { get; }

        public ToastColorPickerDialog(string title, ToastTheme current)
        {
            InitializeComponent();
            HeaderText.Text = title;

            _primary = current.Accent;
            _secondary = current.Background;
            _iconGlyph = current.IconGlyph;
            Result = current.Clone();

            BuildPalette();
            BuildIconPicker();

            PrimaryHexBox.Text = _primary;
            SecondaryHexBox.Text = _secondary;
            RefreshSwatches();
            RefreshPreview();
        }

        private void BuildPalette()
        {
            foreach (var hex in BasicPalette)
            {
                var brush = SafeBrush(hex);
                var swatch = new Border
                {
                    Width = 22,
                    Height = 22,
                    Margin = new Thickness(1),
                    Background = brush,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x20)),
                    BorderThickness = new Thickness(1),
                    Cursor = Cursors.Hand,
                    ToolTip = hex
                };
                swatch.MouseLeftButtonUp += (_, _) => SetPrimary(hex);
                swatch.MouseRightButtonUp += (_, e) => { e.Handled = true; SetSecondary(hex); };
                PaletteHost.Children.Add(swatch);
            }
        }

        private void BuildIconPicker()
        {
            foreach (var glyph in IconGlyphs)
            {
                var btn = new Button
                {
                    Width = 34,
                    Height = 34,
                    Margin = new Thickness(2),
                    Padding = new Thickness(0),
                    Content = char.ConvertFromUtf32(glyph),
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    FontSize = 16,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Cursor = Cursors.Hand,
                    Tag = glyph
                };
                btn.Click += (_, _) => { _iconGlyph = glyph; RefreshPreview(); HighlightSelectedIcon(); };
                IconHost.Children.Add(btn);
            }
            HighlightSelectedIcon();
        }

        private void HighlightSelectedIcon()
        {
            foreach (var child in IconHost.Children)
            {
                if (child is Button b && b.Tag is int g)
                    b.BorderBrush = g == _iconGlyph
                        ? SafeBrush(_primary)
                        : (Brush)new SolidColorBrush(Color.FromArgb(0x40, 0x80, 0x80, 0x80));
            }
        }

        private void SetPrimary(string hex)
        {
            _primary = hex;
            PrimaryHexBox.Text = hex;
            RefreshSwatches();
            RefreshPreview();
            HighlightSelectedIcon();
        }

        private void SetSecondary(string hex)
        {
            _secondary = hex;
            SecondaryHexBox.Text = hex;
            RefreshSwatches();
            RefreshPreview();
        }

        private void HexBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox tb) return;
            var hex = (tb.Text ?? "").Trim();
            if (!ToastSettings.IsHex(hex))
            {
                // Revert to the last valid value.
                tb.Text = (string)tb.Tag == "primary" ? _primary : _secondary;
                return;
            }
            if ((string)tb.Tag == "primary") SetPrimary(hex);
            else SetSecondary(hex);
        }

        private void RefreshSwatches()
        {
            PrimarySwatch.Background = SafeBrush(_primary);
            SecondarySwatch.Background = SafeBrush(_secondary);
        }

        private void RefreshPreview()
        {
            var accent = SafeBrush(_primary);
            var bg = SafeBrush(_secondary);

            var icon = new TextBlock
            {
                Text = char.ConvertFromUtf32(_iconGlyph),
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 18,
                Foreground = accent,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            var title = new TextBlock
            {
                Text = "Notification title",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xE5, 0xE5, 0xE5))
            };
            var msg = new TextBlock
            {
                Text = "This is how your toast will look.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(0x9D, 0x9D, 0x9D))
            };
            var textStack = new StackPanel();
            textStack.Children.Add(title);
            textStack.Children.Add(msg);

            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(icon);
            row.Children.Add(textStack);

            var accentBar = new Border { Width = 3, Background = accent, CornerRadius = new CornerRadius(2, 0, 0, 2) };
            var content = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(12) };
            content.Children.Add(row);

            var inner = new DockPanel();
            DockPanel.SetDock(accentBar, Dock.Left);
            inner.Children.Add(accentBar);
            inner.Children.Add(content);

            PreviewHost.Child = new Border
            {
                Background = bg,
                CornerRadius = new CornerRadius(10),
                BorderBrush = accent,
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Left,
                MinWidth = 300,
                Child = inner
            };
        }

        private static Brush SafeBrush(string hex)
        {
            try { return (Brush)new BrushConverter().ConvertFrom(hex)!; }
            catch { return Brushes.Gray; }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Result.Accent = _primary;
            Result.Background = _secondary;
            Result.IconGlyph = _iconGlyph;
            DialogResult = true;
            Close();
        }
    }
}
