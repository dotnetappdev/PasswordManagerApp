using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using ModernWpf.Controls;
using PasswordManager.Models;

namespace PasswordManager.WPF.Helpers;

/// <summary>
/// Builds 1Password-style custom field rows:
///   [≡ drag]  [Label (editable)]           [⊖]
///             [Value input               ]
/// </summary>
public static class CustomFieldHelper
{
    // ── Palette ────────────────────────────────────────────────────────────
    private static readonly SolidColorBrush BrushBg        = new(Color.FromRgb(0x2D, 0x2D, 0x2D));
    private static readonly SolidColorBrush BrushSurface   = new(Color.FromRgb(0x24, 0x24, 0x24));
    private static readonly SolidColorBrush BrushBorder    = new(Color.FromRgb(0x3A, 0x3A, 0x3A));
    private static readonly SolidColorBrush BrushPrimary   = new(Color.FromRgb(0xE5, 0xE5, 0xE5));
    private static readonly SolidColorBrush BrushSecondary = new(Color.FromRgb(0x9D, 0x9D, 0x9D));
    private static readonly SolidColorBrush BrushAccent    = new(Color.FromRgb(0x25, 0x63, 0xEB));
    private static readonly SolidColorBrush BrushDanger    = new(Color.FromRgb(0xEF, 0x44, 0x44));
    private static readonly SolidColorBrush BrushTransp    = new(Colors.Transparent);

    // ── Field menu definitions (1Password order) ───────────────────────────
    private static readonly (string Label, string Icon, CustomFieldType Type)[] MenuItems =
    [
        ("Text",             "", CustomFieldType.Text),
        ("URL",              "", CustomFieldType.Url),
        ("Email",            "", CustomFieldType.Email),
        ("Address",          "", CustomFieldType.Address),
        ("Date",             "", CustomFieldType.Date),
        ("One-Time Password","", CustomFieldType.OneTimePassword),
        ("Password",         "", CustomFieldType.Password),
        ("Phone",            "", CustomFieldType.Phone),
        ("Number",           "", CustomFieldType.Number),
        ("Yes / No",         "", CustomFieldType.Toggle),
        ("Multiline text",   "", CustomFieldType.TextArea),
        ("Sign in with",     "", CustomFieldType.SignInWith),
    ];

    // ── Drag-drop state ────────────────────────────────────────────────────
    private static CustomField? _dragSource;

    // ── Public: popup type menu ─────────────────────────────────────────────
    /// <summary>
    /// Opens a 1Password-style dropdown below <paramref name="anchor"/>.
    /// Uses a plain <see cref="Popup"/> rather than <see cref="ContextMenu"/> — ModernWpf's
    /// ContextMenu template includes an animated ScrollBar whose visual-state storyboard can
    /// throw "VerifyPathIsAnimatable" if it fires while the popup's visual tree is still settling.
    /// A bare Popup has no ScrollBar in its template, so that race can't happen.
    /// </summary>
    public static void ShowFieldTypeMenu(FrameworkElement anchor, Action<CustomFieldType> onPicked)
    {
        var popup = new Popup
        {
            PlacementTarget = anchor,
            Placement = PlacementMode.Bottom,
            StaysOpen = false,
            AllowsTransparency = true,
            PopupAnimation = PopupAnimation.None,
        };

        var border = new Border
        {
            Background = BrushSurface,
            BorderBrush = BrushBorder,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(0, 4, 0, 4),
        };

        var stack = new StackPanel();

        foreach (var (label, icon, type) in MenuItems)
        {
            var row = new Grid { Margin = new Thickness(0) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var iconTb = new TextBlock
            {
                Text = icon,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 13,
                Foreground = BrushSecondary,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var labelTb = new TextBlock
            {
                Text = label,
                FontSize = 13,
                Foreground = BrushPrimary,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(iconTb, 0);
            Grid.SetColumn(labelTb, 1);
            row.Children.Add(iconTb);
            row.Children.Add(labelTb);

            var item = new System.Windows.Controls.Button
            {
                Content = row,
                Background = BrushTransp,
                Foreground = BrushPrimary,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(12, 7, 16, 7),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Cursor = Cursors.Hand,
                Tag = type,
            };
            item.MouseEnter += (_, _) => item.Background = BrushBg;
            item.MouseLeave += (_, _) => item.Background = BrushTransp;
            item.Click += (_, _) =>
            {
                popup.IsOpen = false;
                onPicked(type);
            };
            stack.Children.Add(item);
        }

        border.Child = stack;
        popup.Child = border;
        popup.IsOpen = true;
    }

    // ── Public: create one 1Password-style field row ────────────────────────
    /// <param name="container">Parent panel — used to resolve drop position during drag.</param>
    /// <param name="reorderCallback">Called with (draggedField, newIndex) when the user drops.</param>
    public static FrameworkElement CreateCustomFieldRow(
        CustomField field,
        Panel container,
        Action<CustomField> onChanged,
        Action<CustomField> onRemoved,
        Action<CustomField, int> reorderCallback)
    {
        // ── Outer border (full row) ────────────────────────────────────────
        var outer = new Border
        {
            Background      = BrushBg,
            BorderBrush     = BrushBorder,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding         = new Thickness(0, 10, 8, 10),
            AllowDrop       = true,
            Tag             = field,
        };

        var outerGrid = new Grid();
        outerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
        outerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        outerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // ── Drag handle ───────────────────────────────────────────────────
        var handle = new TextBlock
        {
            Text                = "", // Segoe MDL2 "List" / drag dots icon
            FontFamily          = new FontFamily("Segoe MDL2 Assets"),
            FontSize            = 14,
            Foreground          = BrushSecondary,
            VerticalAlignment   = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Cursor              = Cursors.SizeAll,
            ToolTip             = "Drag to reorder",
        };

        handle.PreviewMouseLeftButtonDown += (_, _) => _dragSource = field;
        handle.PreviewMouseMove += (_, e) =>
        {
            if (e.LeftButton == MouseButtonState.Pressed && _dragSource == field)
                DragDrop.DoDragDrop(handle, field, DragDropEffects.Move);
        };

        // ── Drop handling on this row ─────────────────────────────────────
        outer.DragEnter += (_, e) =>
        {
            if (e.Data.GetDataPresent(typeof(CustomField)))
                outer.BorderBrush = BrushAccent;
        };
        outer.DragLeave += (_, _) => outer.BorderBrush = BrushBorder;
        outer.Drop += (_, e) =>
        {
            outer.BorderBrush = BrushBorder;
            if (e.Data.GetData(typeof(CustomField)) is not CustomField dragged) return;
            if (dragged == field) return;

            // Compute the new index = index of this target row
            int targetIdx = 0;
            foreach (UIElement child in container.Children)
            {
                if (child is FrameworkElement fe && fe.Tag is CustomField cf && cf == field)
                    break;
                targetIdx++;
            }
            reorderCallback(dragged, targetIdx);
        };

        // ── Label + value column ──────────────────────────────────────────
        var innerStack = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };

        var nameBox = new System.Windows.Controls.TextBox
        {
            Text                   = field.Name,
            Background             = BrushTransp,
            Foreground             = BrushSecondary,
            CaretBrush             = BrushPrimary,
            BorderThickness        = new Thickness(0),
            Padding                = new Thickness(0),
            FontSize               = 11,
            FontWeight             = FontWeights.SemiBold,
            VerticalContentAlignment = VerticalAlignment.Center,
        };
        nameBox.TextChanged += (_, _) => { field.Name = nameBox.Text; onChanged?.Invoke(field); };

        var valueControl = BuildValueControl(field, onChanged);
        innerStack.Children.Add(nameBox);
        innerStack.Children.Add(valueControl);

        // ── Delete button (⊖ red) ─────────────────────────────────────────
        var deleteBtn = new System.Windows.Controls.Button
        {
            Content         = "", // Segoe MDL2 "Remove" circle minus
            FontFamily      = new FontFamily("Segoe MDL2 Assets"),
            FontSize        = 16,
            Background      = BrushTransp,
            Foreground      = BrushDanger,
            BorderThickness = new Thickness(0),
            Padding         = new Thickness(4),
            VerticalAlignment = VerticalAlignment.Center,
            Cursor          = Cursors.Hand,
            ToolTip         = "Remove field",
        };
        deleteBtn.Click += (_, _) => onRemoved?.Invoke(field);

        Grid.SetColumn(handle,    0);
        Grid.SetColumn(innerStack, 1);
        Grid.SetColumn(deleteBtn, 2);
        outerGrid.Children.Add(handle);
        outerGrid.Children.Add(innerStack);
        outerGrid.Children.Add(deleteBtn);
        outer.Child = outerGrid;
        return outer;
    }

    // ── Legacy overload used by AddPasswordDialog (no reorder callback) ────
    public static StackPanel CreateCustomFieldControl(
        CustomField field,
        Action<CustomField> onFieldChanged,
        Action<CustomField> onFieldRemoved)
    {
        var wrapper = new StackPanel { Margin = new Thickness(0, 0, 0, 2), Tag = field };
        var row = CreateCustomFieldRow(field, wrapper, onFieldChanged, onFieldRemoved, (_, _) => { });
        wrapper.Children.Add(row);
        return wrapper;
    }

    // ── Build value input for given type ──────────────────────────────────
    private static FrameworkElement BuildValueControl(CustomField field, Action<CustomField> onChanged)
    {
        switch (field.Type)
        {
            case CustomFieldType.Password:
            {
                var pw = new PasswordBox
                {
                    Password              = field.Value,
                    Background            = BrushTransp,
                    Foreground            = BrushPrimary,
                    BorderThickness       = new Thickness(0),
                    Padding               = new Thickness(0),
                    FontSize              = 14,
                    VerticalContentAlignment = VerticalAlignment.Center,
                };
                pw.PasswordChanged += (_, _) => { field.Value = pw.Password; onChanged?.Invoke(field); };
                return pw;
            }

            case CustomFieldType.Date:
            {
                var dp = new DatePicker
                {
                    Background      = BrushTransp,
                    Foreground      = BrushPrimary,
                    BorderThickness = new Thickness(0),
                    FontSize        = 14,
                    Height          = 32,
                };
                if (DateTime.TryParse(field.Value, out var d)) dp.SelectedDate = d;
                dp.SelectedDateChanged += (_, _) =>
                {
                    if (dp.SelectedDate.HasValue)
                    { field.Value = dp.SelectedDate.Value.ToString("yyyy-MM-dd"); onChanged?.Invoke(field); }
                };
                return dp;
            }

            case CustomFieldType.Toggle:
            {
                var panel = new StackPanel { Orientation = Orientation.Horizontal };
                var yes = new RadioButton
                {
                    Content = "Yes", Foreground = BrushPrimary, Margin = new Thickness(0, 0, 16, 0),
                    IsChecked = field.Value?.ToLowerInvariant() is "yes" or "true"
                };
                var no = new RadioButton { Content = "No", Foreground = BrushPrimary, IsChecked = !(yes.IsChecked == true) };
                yes.Checked += (_, _) => { field.Value = "Yes"; onChanged?.Invoke(field); };
                no.Checked  += (_, _) => { field.Value = "No";  onChanged?.Invoke(field); };
                panel.Children.Add(yes);
                panel.Children.Add(no);
                return panel;
            }

            case CustomFieldType.Number:
            {
                var nb = new ModernWpf.Controls.NumberBox
                {
                    Value           = double.TryParse(field.Value, out var n) ? n : 0,
                    Background      = BrushTransp,
                    Foreground      = BrushPrimary,
                    BorderThickness = new Thickness(0),
                    FontSize        = 14,
                    PlaceholderText = "0",
                };
                nb.ValueChanged += (_, _) => { field.Value = double.IsNaN(nb.Value) ? "" : nb.Value.ToString(); onChanged?.Invoke(field); };
                return nb;
            }

            case CustomFieldType.Address:
            case CustomFieldType.TextArea:
            {
                var ta = PlainTextBox(field.Value, acceptsReturn: true, minHeight: 60);
                ta.TextChanged += (_, _) => { field.Value = ta.Text; onChanged?.Invoke(field); };
                return ta;
            }

            case CustomFieldType.OneTimePassword:
            {
                var tb = PlainTextBox(field.Value, placeholder: "otpauth:// URI or Base32 secret");
                tb.TextChanged += (_, _) => { field.Value = tb.Text; onChanged?.Invoke(field); };
                return tb;
            }

            default: // Text, Email, Url, Phone, SignInWith, …
            {
                var placeholder = field.Type switch
                {
                    CustomFieldType.Email      => "email@example.com",
                    CustomFieldType.Url        => "https://",
                    CustomFieldType.Phone      => "+1 (555) 000-0000",
                    CustomFieldType.SignInWith  => "Provider name (e.g. Google)",
                    _                          => "",
                };
                var tb = PlainTextBox(field.Value, placeholder);
                tb.TextChanged += (_, _) => { field.Value = tb.Text; onChanged?.Invoke(field); };
                return tb;
            }
        }
    }

    private static System.Windows.Controls.TextBox PlainTextBox(
        string text, string placeholder = "", bool acceptsReturn = false, double minHeight = 28)
    {
        var tb = new System.Windows.Controls.TextBox
        {
            Text                   = text,
            Background             = BrushTransp,
            Foreground             = BrushPrimary,
            CaretBrush             = BrushPrimary,
            SelectionBrush         = BrushAccent,
            BorderThickness        = new Thickness(0),
            Padding                = new Thickness(0),
            FontSize               = 14,
            MinHeight              = minHeight,
            AcceptsReturn          = acceptsReturn,
            TextWrapping           = acceptsReturn ? TextWrapping.Wrap : TextWrapping.NoWrap,
            VerticalContentAlignment = acceptsReturn ? VerticalAlignment.Top : VerticalAlignment.Center,
        };
        if (!string.IsNullOrEmpty(placeholder))
            tb.Tag = placeholder;
        return tb;
    }

    // ── Helpers used by AddPasswordDialog type picker ─────────────────────
    public static string GetDefaultFieldName(CustomFieldType type) => type switch
    {
        CustomFieldType.Password        => "Password",
        CustomFieldType.Email           => "Email",
        CustomFieldType.Url             => "URL",
        CustomFieldType.Phone           => "Phone",
        CustomFieldType.Number          => "Number",
        CustomFieldType.Date            => "Date",
        CustomFieldType.Toggle          => "Yes / No",
        CustomFieldType.TextArea        => "Notes",
        CustomFieldType.Address         => "Address",
        CustomFieldType.OneTimePassword => "One-Time Password",
        CustomFieldType.SignInWith      => "Sign in with",
        _                               => "Text field",
    };

    /// <summary>Legacy type-selector ComboBox — kept for any callers that still use it.</summary>
    public static System.Windows.Controls.ComboBox CreateFieldTypeSelector(
        CustomFieldType selectedType, Action<CustomFieldType> onTypeChanged)
    {
        var cb = new System.Windows.Controls.ComboBox
        {
            Background = BrushBg, Foreground = BrushPrimary,
            BorderBrush = BrushBorder, BorderThickness = new Thickness(1),
            FontSize = 13, MinHeight = 32
        };
        foreach (var (label, _, type) in MenuItems)
        {
            var item = new ComboBoxItem { Content = label, Tag = type };
            cb.Items.Add(item);
            if (type == selectedType) cb.SelectedItem = item;
        }
        cb.SelectionChanged += (_, _) =>
        {
            if (cb.SelectedItem is ComboBoxItem si && si.Tag is CustomFieldType t)
                onTypeChanged(t);
        };
        return cb;
    }
}
