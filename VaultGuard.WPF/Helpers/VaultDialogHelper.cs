using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MwControls = ModernWpf.Controls;

namespace VaultGuard.WPF.Helpers;

// Shared "Create/Edit Vault" dialog (name, description, accent color swatches, icon) —
// used by both the sidebar "+" button (MainWindow) and the Vault Management page (VaultsPage)
// so the two entry points present the exact same form.
public static class VaultDialogHelper
{
    public static async Task<(string? Name, string? Desc, string? Color, string? Icon)> ShowAsync(
        string title,
        string? existingName = null, string? existingDesc = null,
        string? existingColor = null, string? existingIcon = null,
        Action<MwControls.ContentDialog>? configureCentering = null)
    {
        var dialog = new MwControls.ContentDialog
        {
            Title = title,
            PrimaryButtonText = existingName == null ? "Create" : "Save",
            CloseButtonText = "Cancel",
            DefaultButton = MwControls.ContentDialogButton.Primary
        };
        // Apply the polished, rounded dialog chrome by default so this dialog always matches the
        // rest of the app, regardless of whether the caller bothers to pass configureCentering.
        ApplyDialogStyle(dialog);
        configureCentering?.Invoke(dialog);

        var textBoxStyle = Application.Current.Resources["ModernTextBoxStyle"] as Style;

        var nameBox = new TextBox { Text = existingName ?? "", Margin = new Thickness(0, 0, 0, 14), Style = textBoxStyle };
        var descBox = new TextBox
        {
            Text = existingDesc ?? "", AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap, Height = 64,
            Margin = new Thickness(0, 0, 0, 14), Style = textBoxStyle
        };
        var colorBox = new TextBox { Text = existingColor ?? "#2563EB", Margin = new Thickness(0, 0, 0, 14), Style = textBoxStyle };
        var selectedIcon = string.IsNullOrWhiteSpace(existingIcon) ? "🔐" : existingIcon;

        var colorSwatches = new[] { "#2563EB", "#7C3AED", "#059669", "#DC2626", "#D97706", "#0891B2", "#EC4899", "#374151" };
        var swatchPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
        var swatchBorders = new List<Border>();
        foreach (var hex in colorSwatches)
        {
            var swatch = new Border
            {
                Width = 30, Height = 30, CornerRadius = new CornerRadius(8), Margin = new Thickness(0, 0, 8, 8),
                Background = TryParseBrush(hex), Cursor = System.Windows.Input.Cursors.Hand,
                BorderThickness = new Thickness(2), BorderBrush = System.Windows.Media.Brushes.Transparent
            };
            var captured = hex;
            swatchBorders.Add(swatch);
            swatch.MouseLeftButtonDown += (_, _) =>
            {
                colorBox.Text = captured;
                foreach (var b in swatchBorders)
                    b.BorderBrush = System.Windows.Media.Brushes.Transparent;
                swatch.BorderBrush = (Application.Current.Resources["ModernTextPrimaryBrush"] as System.Windows.Media.Brush)
                                     ?? System.Windows.Media.Brushes.White;
            };
            swatchPanel.Children.Add(swatch);
        }
        // Highlight the swatch matching the current/existing color, if any
        var initial = (existingColor ?? "#2563EB").Trim();
        var matchIdx = Array.FindIndex(colorSwatches, c => string.Equals(c, initial, StringComparison.OrdinalIgnoreCase));
        if (matchIdx >= 0)
            swatchBorders[matchIdx].BorderBrush = (Application.Current.Resources["ModernTextPrimaryBrush"] as System.Windows.Media.Brush)
                                                   ?? System.Windows.Media.Brushes.White;

        // Icon picker — preset icons selectable like the color swatches. The stored value stays an
        // emoji (so Web/MAUI render it), but on WPF we display the polished Segoe Fluent Icons glyph
        // via VaultIconHelper so it doesn't look cartoonish next to the Fluent nav icons.
        var iconChoices = VaultIconHelper.PresetEmoji;
        var iconForeground = (Application.Current.Resources["ModernTextPrimaryBrush"] as System.Windows.Media.Brush)
                             ?? System.Windows.Media.Brushes.White;
        var iconPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 4) };
        var iconBorders = new List<Border>();
        foreach (var emoji in iconChoices)
        {
            var glyph = new TextBlock
            {
                FontSize = 16, Foreground = iconForeground,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            };
            VaultIconHelper.Apply(glyph, emoji);
            var tile = new Border
            {
                Width = 34, Height = 34, CornerRadius = new CornerRadius(8), Margin = new Thickness(0, 0, 8, 8),
                Background = (Application.Current.Resources["ModernSurfaceBrush"] as System.Windows.Media.Brush)
                             ?? System.Windows.Media.Brushes.Transparent,
                Cursor = System.Windows.Input.Cursors.Hand,
                BorderThickness = new Thickness(2), BorderBrush = System.Windows.Media.Brushes.Transparent,
                Child = glyph
            };
            var captured = emoji;
            iconBorders.Add(tile);
            tile.MouseLeftButtonDown += (_, _) =>
            {
                selectedIcon = captured;
                foreach (var b in iconBorders)
                    b.BorderBrush = System.Windows.Media.Brushes.Transparent;
                tile.BorderBrush = (Application.Current.Resources["ModernTextPrimaryBrush"] as System.Windows.Media.Brush)
                                    ?? System.Windows.Media.Brushes.White;
            };
            iconPanel.Children.Add(tile);
        }
        var matchIconIdx = Array.FindIndex(iconChoices, i => i == selectedIcon);
        if (matchIconIdx >= 0)
            iconBorders[matchIconIdx].BorderBrush = (Application.Current.Resources["ModernTextPrimaryBrush"] as System.Windows.Media.Brush)
                                                     ?? System.Windows.Media.Brushes.White;

        // Hex box + an old-school "Custom…" colour-picker dialog, so any colour is reachable, not
        // just the preset swatches.
        var customBtn = new Button
        {
            Content = "Custom…",
            Style = Application.Current.Resources["ModernSecondaryButtonStyle"] as Style,
            Margin = new Thickness(8, 0, 0, 14),
            MinWidth = 0, Padding = new Thickness(14, 0, 14, 0)
        };
        customBtn.Click += (_, _) =>
        {
            var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                        ?? Application.Current.MainWindow;
            var picked = ColorPickerDialog.Show(colorBox.Text, owner);
            if (picked != null)
            {
                colorBox.Text = picked;
                foreach (var b in swatchBorders) b.BorderBrush = System.Windows.Media.Brushes.Transparent;
            }
        };
        var colorRow = new Grid();
        colorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        colorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(colorBox, 0);
        Grid.SetColumn(customBtn, 1);
        colorRow.Children.Add(colorBox);
        colorRow.Children.Add(customBtn);

        var panel = new StackPanel { Margin = new Thickness(4), MinWidth = 360 };
        panel.Children.Add(MakeLabel("Vault Name *")); panel.Children.Add(nameBox);
        panel.Children.Add(MakeLabel("Description")); panel.Children.Add(descBox);
        panel.Children.Add(MakeLabel("Accent Color")); panel.Children.Add(swatchPanel);
        panel.Children.Add(colorRow);
        panel.Children.Add(MakeLabel("Icon")); panel.Children.Add(iconPanel);
        dialog.Content = panel;

        var result = await DialogManager.ShowAsync(dialog);
        if (result != MwControls.ContentDialogResult.Primary) return (null, null, null, null);
        var n = nameBox.Text?.Trim();
        if (string.IsNullOrEmpty(n)) return (null, null, null, null);
        return (n, descBox.Text?.Trim(), colorBox.Text?.Trim(), selectedIcon);
    }

    private static void ApplyDialogStyle(MwControls.ContentDialog dialog)
    {
        try
        {
            dialog.Style = dialog.TryFindResource("Modern1PasswordDialogStyle") as Style;
        }
        catch { }
    }

    private static TextBlock MakeLabel(string text) =>
        new() { Text = text, FontSize = 12, FontWeight = FontWeights.SemiBold,
                Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x9D, 0x9D, 0x9D)),
                Margin = new Thickness(0, 0, 0, 4) };

    private static System.Windows.Media.SolidColorBrush TryParseBrush(string hex)
    {
        try { return new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex)); }
        catch { return new(System.Windows.Media.Color.FromRgb(0x37, 0x37, 0x37)); }
    }
}
