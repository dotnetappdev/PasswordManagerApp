using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ModernWpf.Controls;
using PasswordManager.Models;

namespace PasswordManager.WPF.Helpers;

public static class CustomFieldHelper
{
    // Win11 dark mode palette colours used for all custom field controls
    private static readonly Color Win11Background    = Color.FromRgb(0x2D, 0x2D, 0x2D);
    private static readonly Color Win11Surface       = Color.FromRgb(0x24, 0x24, 0x24);
    private static readonly Color Win11Border        = Color.FromRgb(0x3A, 0x3A, 0x3A);
    private static readonly Color Win11TextPrimary   = Color.FromRgb(0xE5, 0xE5, 0xE5);
    private static readonly Color Win11TextSecondary = Color.FromRgb(0x9D, 0x9D, 0x9D);
    private static readonly Color Win11Accent        = Color.FromRgb(0x25, 0x63, 0xEB);

    public static StackPanel CreateCustomFieldControl(CustomField field, Action<CustomField> onFieldChanged, Action<CustomField> onFieldRemoved)
    {
        var fieldPanel = new StackPanel
        {
            Margin = new Thickness(0, 0, 0, 12),
            Tag = field
        };

        // ── Header: field name label (left) + type badge (center) + remove (right) ──
        var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Field label input
        var nameTextBox = new System.Windows.Controls.TextBox
        {
            Text = field.Name,
            Background = new SolidColorBrush(Win11Background),
            Foreground = new SolidColorBrush(Win11TextPrimary),
            BorderBrush = new SolidColorBrush(Win11Border),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8, 6, 8, 6),
            FontSize = 13,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        nameTextBox.Tag = "Field label"; // placeholder hint (ControlHelper not exposed in code-behind)

        nameTextBox.TextChanged += (s, e) =>
        {
            field.Name = nameTextBox.Text;
            onFieldChanged?.Invoke(field);
        };

        // Type selector dropdown (compact)
        var typeSelector = CreateFieldTypeSelector(field.Type, (newType) =>
        {
            field.Type = newType;
            if (fieldPanel.Children.Count > 1)
                fieldPanel.Children.RemoveAt(1);
            fieldPanel.Children.Add(CreateValueControl(field, onFieldChanged));
            onFieldChanged?.Invoke(field);
        });
        typeSelector.Margin = new Thickness(0, 0, 8, 0);

        // Remove button
        var removeButton = new System.Windows.Controls.Button
        {
            Content = "", // Segoe MDL2 delete icon
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 14,
            Background = new SolidColorBrush(Colors.Transparent),
            Foreground = new SolidColorBrush(Color.FromRgb(0x9D, 0x9D, 0x9D)),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(8, 4, 8, 4),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        ToolTipService.SetToolTip(removeButton, "Remove field");
        removeButton.Click += (s, e) => onFieldRemoved?.Invoke(field);

        Grid.SetColumn(nameTextBox, 0);
        Grid.SetColumn(typeSelector, 1);
        Grid.SetColumn(removeButton, 2);
        headerGrid.Children.Add(nameTextBox);
        headerGrid.Children.Add(typeSelector);
        headerGrid.Children.Add(removeButton);

        fieldPanel.Children.Add(headerGrid);

        // Value control below the header
        var valueControl = CreateValueControl(field, onFieldChanged);
        fieldPanel.Children.Add(valueControl);

        return fieldPanel;
    }

    private static FrameworkElement CreateValueControl(CustomField field, Action<CustomField> onFieldChanged)
    {
        switch (field.Type)
        {
            case CustomFieldType.Password:
                var pwBorder = new Border
                {
                    Background = new SolidColorBrush(Win11Background),
                    BorderBrush = new SolidColorBrush(Win11Border),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Height = 36
                };
                var passwordBox = new PasswordBox
                {
                    Password = field.Value,
                    Background = new SolidColorBrush(Colors.Transparent),
                    Foreground = new SolidColorBrush(Win11TextPrimary),
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(10, 0, 10, 0),
                    FontSize = 14,
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                pwBorder.Child = passwordBox;
                passwordBox.PasswordChanged += (s, e) =>
                {
                    field.Value = passwordBox.Password;
                    onFieldChanged?.Invoke(field);
                };
                return pwBorder;

            case CustomFieldType.Date:
                var datePicker = new DatePicker
                {
                    Background = new SolidColorBrush(Win11Background),
                    Foreground = new SolidColorBrush(Win11TextPrimary),
                    BorderBrush = new SolidColorBrush(Win11Border),
                    BorderThickness = new Thickness(1),
                    FontSize = 14,
                    Height = 36
                };
                if (DateTime.TryParse(field.Value, out var date))
                    datePicker.SelectedDate = date;
                datePicker.SelectedDateChanged += (s, e) =>
                {
                    if (datePicker.SelectedDate.HasValue)
                    {
                        field.Value = datePicker.SelectedDate.Value.ToString("yyyy-MM-dd");
                        onFieldChanged?.Invoke(field);
                    }
                };
                return datePicker;

            case CustomFieldType.TextArea:
                var textArea = MakeDarkTextBox(field.Value, GetPlaceholderForType(field.Type),
                    acceptsReturn: true, minHeight: 72, maxHeight: 120, fontSize: 14);
                ((System.Windows.Controls.TextBox)textArea).TextChanged += (s, e) =>
                {
                    field.Value = ((System.Windows.Controls.TextBox)textArea).Text;
                    onFieldChanged?.Invoke(field);
                };
                return textArea;

            case CustomFieldType.Number:
                var numberBox = new ModernWpf.Controls.NumberBox
                {
                    Value = double.TryParse(field.Value, out var number) ? number : 0,
                    Background = new SolidColorBrush(Win11Background),
                    Foreground = new SolidColorBrush(Win11TextPrimary),
                    BorderBrush = new SolidColorBrush(Win11Border),
                    FontSize = 14,
                    MinHeight = 36
                };
                numberBox.PlaceholderText = "0";
                numberBox.ValueChanged += (s, e) =>
                {
                    field.Value = double.IsNaN(numberBox.Value) ? "" : numberBox.Value.ToString();
                    onFieldChanged?.Invoke(field);
                };
                return numberBox;

            case CustomFieldType.Toggle:
                var toggleBorder = new Border
                {
                    Background = new SolidColorBrush(Win11Background),
                    BorderBrush = new SolidColorBrush(Win11Border),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10, 8, 10, 8)
                };
                var togglePanel = new StackPanel { Orientation = Orientation.Horizontal };
                var yesRadio = new RadioButton
                {
                    Content = "Yes",
                    Foreground = new SolidColorBrush(Win11TextPrimary),
                    Margin = new Thickness(0, 0, 20, 0),
                    IsChecked = field.Value?.ToLowerInvariant() is "yes" or "true"
                };
                var noRadio = new RadioButton
                {
                    Content = "No",
                    Foreground = new SolidColorBrush(Win11TextPrimary),
                    IsChecked = !(yesRadio.IsChecked == true)
                };
                yesRadio.Checked += (s, e) => { field.Value = "Yes"; onFieldChanged?.Invoke(field); };
                noRadio.Checked  += (s, e) => { field.Value = "No";  onFieldChanged?.Invoke(field); };
                togglePanel.Children.Add(yesRadio);
                togglePanel.Children.Add(noRadio);
                toggleBorder.Child = togglePanel;
                return toggleBorder;

            case CustomFieldType.Email:
            case CustomFieldType.Url:
            case CustomFieldType.Phone:
            case CustomFieldType.Text:
            default:
                var tb = MakeDarkTextBox(field.Value, GetPlaceholderForType(field.Type), fontSize: 14);
                ((System.Windows.Controls.TextBox)tb).TextChanged += (s, e) =>
                {
                    field.Value = ((System.Windows.Controls.TextBox)tb).Text;
                    onFieldChanged?.Invoke(field);
                };
                return tb;
        }
    }

    private static System.Windows.Controls.TextBox MakeDarkTextBox(
        string text, string placeholder,
        bool acceptsReturn = false, double minHeight = 36, double maxHeight = double.NaN, double fontSize = 14)
    {
        var tb = new System.Windows.Controls.TextBox
        {
            Text = text,
            Background = new SolidColorBrush(Win11Background),
            Foreground = new SolidColorBrush(Win11TextPrimary),
            CaretBrush = new SolidColorBrush(Win11TextPrimary),
            SelectionBrush = new SolidColorBrush(Win11Accent),
            BorderBrush = new SolidColorBrush(Win11Border),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10, 0, 10, 0),
            FontSize = fontSize,
            MinHeight = minHeight,
            AcceptsReturn = acceptsReturn,
            TextWrapping = acceptsReturn ? TextWrapping.Wrap : TextWrapping.NoWrap,
            VerticalContentAlignment = acceptsReturn ? VerticalAlignment.Top : VerticalAlignment.Center
        };
        if (!double.IsNaN(maxHeight))
            tb.MaxHeight = maxHeight;
        if (!string.IsNullOrEmpty(placeholder))
            tb.Tag = placeholder; // store as Tag; actual placeholder needs XAML ControlHelper.PlaceholderText
        return tb;
    }

    private static string GetPlaceholderForType(CustomFieldType type)
    {
        return type switch
        {
            CustomFieldType.Email => "Enter email address",
            CustomFieldType.Url => "Enter URL",
            CustomFieldType.Phone => "Enter phone number",
            CustomFieldType.Number => "Enter number",
            CustomFieldType.TextArea => "Enter text",
            _ => "Enter value"
        };
    }

    public static System.Windows.Controls.ComboBox CreateFieldTypeSelector(CustomFieldType selectedType, Action<CustomFieldType> onTypeChanged)
    {
        var comboBox = new System.Windows.Controls.ComboBox
        {
            Background = new SolidColorBrush(Win11Background),
            Foreground = new SolidColorBrush(Win11TextPrimary),
            BorderBrush = new SolidColorBrush(Win11Border),
            BorderThickness = new Thickness(1),
            FontSize = 13,
            MinHeight = 32
        };

        // Exclude File — not useful as a custom field; keep all others including Toggle
        var fieldTypes = Enum.GetValues<CustomFieldType>()
            .Where(t => t != CustomFieldType.File)
            .ToList();
        foreach (var type in fieldTypes)
        {
            var item = new ComboBoxItem
            {
                Content = GetDisplayNameForType(type),
                Tag = type
            };
            comboBox.Items.Add(item);

            if (type == selectedType)
            {
                comboBox.SelectedItem = item;
            }
        }

        comboBox.SelectionChanged += (s, e) =>
        {
            if (comboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is CustomFieldType type)
            {
                onTypeChanged?.Invoke(type);
            }
        };

        return comboBox;
    }

    private static string GetDisplayNameForType(CustomFieldType type)
    {
        return type switch
        {
            CustomFieldType.Text     => "Text",
            CustomFieldType.Password => "Password (hidden)",
            CustomFieldType.TextArea => "Text Area (multiline)",
            CustomFieldType.Toggle   => "Yes / No (toggle)",
            CustomFieldType.Date     => "Date",
            CustomFieldType.Number   => "Number",
            CustomFieldType.Email    => "Email",
            CustomFieldType.Url      => "URL",
            CustomFieldType.Phone    => "Phone",
            _                        => type.ToString()
        };
    }
}
