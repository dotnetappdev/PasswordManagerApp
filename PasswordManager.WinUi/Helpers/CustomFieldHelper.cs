using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PasswordManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PasswordManager.WinUi.Helpers;

public static class CustomFieldHelper
{
    public static StackPanel CreateCustomFieldControl(CustomField field, Action<CustomField> onFieldChanged, Action<CustomField> onFieldRemoved)
    {
        var fieldPanel = new StackPanel
        {
            Spacing = 8,
            Margin = new Thickness(0, 0, 0, 12),
            Tag = field
        };

        // Field label and remove button row
        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var nameTextBox = new TextBox
        {
            Text = field.Name,
            PlaceholderText = "Field name",
            Style = Application.Current.Resources["ModernTextBoxStyle"] as Style,
            Margin = new Thickness(0, 0, 8, 0)
        };
        nameTextBox.TextChanged += (s, e) =>
        {
            field.Name = nameTextBox.Text;
            onFieldChanged?.Invoke(field);
        };

        var removeButton = new Button
        {
            Content = "🗑️",
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(8),
            ToolTipService = { ToolTip = "Remove field" }
        };
        removeButton.Click += (s, e) => onFieldRemoved?.Invoke(field);

        Grid.SetColumn(nameTextBox, 0);
        Grid.SetColumn(removeButton, 1);
        headerGrid.Children.Add(nameTextBox);
        headerGrid.Children.Add(removeButton);

        fieldPanel.Children.Add(headerGrid);

        // Field value control based on type
        var valueControl = CreateValueControl(field, onFieldChanged);
        fieldPanel.Children.Add(valueControl);

        return fieldPanel;
    }

    private static FrameworkElement CreateValueControl(CustomField field, Action<CustomField> onFieldChanged)
    {
        switch (field.Type)
        {
            case CustomFieldType.Password:
                var passwordBox = new PasswordBox
                {
                    Password = field.Value,
                    PlaceholderText = "Enter password",
                    Style = Application.Current.Resources["ModernPasswordBoxStyle"] as Style
                };
                passwordBox.PasswordChanged += (s, e) =>
                {
                    field.Value = passwordBox.Password;
                    onFieldChanged?.Invoke(field);
                };
                return passwordBox;

            case CustomFieldType.Date:
                var datePicker = new DatePicker
                {
                    Style = Application.Current.Resources["ModernDatePickerStyle"] as Style
                };
                if (DateTime.TryParse(field.Value, out var date))
                {
                    datePicker.Date = date;
                }
                datePicker.DateChanged += (s, e) =>
                {
                    field.Value = datePicker.Date.ToString("yyyy-MM-dd");
                    onFieldChanged?.Invoke(field);
                };
                return datePicker;

            case CustomFieldType.TextArea:
                var textArea = new TextBox
                {
                    Text = field.Value,
                    PlaceholderText = "Enter text",
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    MaxHeight = 120,
                    Style = Application.Current.Resources["ModernTextBoxStyle"] as Style
                };
                textArea.TextChanged += (s, e) =>
                {
                    field.Value = textArea.Text;
                    onFieldChanged?.Invoke(field);
                };
                return textArea;

            case CustomFieldType.Number:
                var numberBox = new NumberBox
                {
                    Value = double.TryParse(field.Value, out var number) ? number : 0,
                    PlaceholderText = "Enter number",
                    Style = Application.Current.Resources["ModernNumberBoxStyle"] as Style
                };
                numberBox.ValueChanged += (s, e) =>
                {
                    field.Value = numberBox.Value.ToString();
                    onFieldChanged?.Invoke(field);
                };
                return numberBox;

            case CustomFieldType.Email:
            case CustomFieldType.Url:
            case CustomFieldType.Phone:
            case CustomFieldType.Text:
            default:
                var textBox = new TextBox
                {
                    Text = field.Value,
                    PlaceholderText = GetPlaceholderForType(field.Type),
                    Style = Application.Current.Resources["ModernTextBoxStyle"] as Style
                };
                textBox.TextChanged += (s, e) =>
                {
                    field.Value = textBox.Text;
                    onFieldChanged?.Invoke(field);
                };
                return textBox;
        }
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

    public static ComboBox CreateFieldTypeSelector(CustomFieldType selectedType, Action<CustomFieldType> onTypeChanged)
    {
        var comboBox = new ComboBox
        {
            PlaceholderText = "Select field type",
            Style = Application.Current.Resources["ModernComboBoxStyle"] as Style
        };

        var fieldTypes = Enum.GetValues<CustomFieldType>().ToList();
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
            CustomFieldType.Text => "Text",
            CustomFieldType.Password => "Password",
            CustomFieldType.Date => "Date",
            CustomFieldType.Number => "Number",
            CustomFieldType.Email => "Email",
            CustomFieldType.Url => "URL",
            CustomFieldType.TextArea => "Text Area",
            CustomFieldType.Phone => "Phone",
            _ => type.ToString()
        };
    }
}