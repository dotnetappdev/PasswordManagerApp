using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MwControls = ModernWpf.Controls;

namespace VaultGuard.WPF.Helpers;

/// <summary>
/// Polished, Windows 11-style destructive confirmation dialog. Centralises the look of every
/// "are you sure you want to delete…" prompt so they are consistent: a warning glyph in a tinted
/// circle, the message, a red primary "Delete" button, "Cancel" as the default (safe) action, and
/// the app's rounded dialog chrome. Shown through <see cref="DialogManager"/> so it never collides
/// with another open dialog.
/// </summary>
public static class ConfirmDialog
{
    // Segoe Fluent Icons "Warning" glyph.
    private const string WarningGlyph = "";

    /// <summary>Shows a destructive confirmation. Returns true only if the user confirmed.</summary>
    public static async Task<bool> ShowDeleteAsync(string title, string message, string confirmText = "Delete")
    {
        var dialog = new MwControls.ContentDialog
        {
            Title = title,
            Content = BuildContent(message),
            PrimaryButtonText = confirmText,
            CloseButtonText = "Cancel",
            // Cancel is the default so an accidental Enter never deletes.
            DefaultButton = MwControls.ContentDialogButton.Close
        };

        try { dialog.Style = dialog.TryFindResource("Modern1PasswordDialogStyle") as Style; }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error("Failed to apply confirm dialog style", ex); }

        // Make the confirming action read as destructive (red), per Windows 11 guidance.
        if (Application.Current?.Resources["ModernDangerButtonStyle"] is Style danger)
        {
            dialog.PrimaryButtonStyle = danger;
        }

        return await DialogManager.ShowAsync(dialog) == MwControls.ContentDialogResult.Primary;
    }

    private static UIElement BuildContent(string message)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            MinWidth = 360,
            Margin = new Thickness(0, 4, 0, 0)
        };

        var iconWrap = new Border
        {
            Width = 40,
            Height = 40,
            CornerRadius = new CornerRadius(20),
            Background = new SolidColorBrush(Color.FromArgb(0x26, 0xEF, 0x44, 0x44)), // ~15% red
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 0, 14, 0),
            Child = new TextBlock
            {
                Text = WarningGlyph,
                FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                FontSize = 18,
                Foreground = (Application.Current?.Resources["ModernErrorBrush"] as Brush) ?? Brushes.Red,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        var text = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 320,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (Application.Current?.Resources["ModernTextPrimaryBrush"] as Brush) ?? Brushes.White,
            FontSize = 14
        };

        panel.Children.Add(iconWrap);
        panel.Children.Add(text);
        return panel;
    }
}
