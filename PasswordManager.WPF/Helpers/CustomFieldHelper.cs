using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ModernWpf.Controls;
using PasswordManager.Models;

namespace PasswordManager.WPF.Helpers;

public static class CustomFieldHelper
{
    public static StackPanel CreateCustomFieldControl(CustomField field, Action<CustomField> onFieldChanged, Action<CustomField> onFieldRemoved)
    {
        var fieldPanel = new StackPanel
        {
            Margin = new Thickness(0, 0, 0, 12),
            Tag = field
        };

        // Field label and remove button row
        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var nameTextBox = new System.Windows.Controls.TextBox
        {
            Text = field.Name,
            Style = ResourceHelper.GetStyle("ModernTextBoxStyle"),
            Margin = new Thickness(0, 0, 8, 0)
        };
        ModernWpf.Controls.ControlHelper.SetPlaceholderText(nameTextBox, "Field name");
        
        nameTextBox.TextChanged += (s, e) =>
        {
            field.Name = nameTextBox.Text;
            onFieldChanged?.Invoke(field);
        };

        var removeButton = new System.Windows.Controls.Button
        {
            Content = "🗑️",
            Background = new SolidColorBrush(Colors.Transparent),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(8)
        };
        ToolTipService.SetToolTip(removeButton, "Remove field");
        removeButton.Click += (s, e) => onFieldRemoved?.Invoke(field);

        Grid.SetColumn(nameTextBox, 0);
        Grid.SetColumn(removeButton, 1);
        headerGrid.Children.Add(nameTextBox);
        headerGrid.Children.Add(removeButton);

        fieldPanel.Children.Add(headerGrid);

        // Field type selector
        var typeSelector = CreateFieldTypeSelector(field.Type, (newType) =>
        {
            field.Type = newType;

            // Refresh the value control when type changes
            // Remove old value control and add new one
            if (fieldPanel.Children.Count > 2)
            {
                fieldPanel.Children.RemoveAt(2); // Remove old value control
            }
            var newValueControl = CreateValueControl(field, onFieldChanged);
            fieldPanel.Children.Add(newValueControl);

            onFieldChanged?.Invoke(field);
        });
        fieldPanel.Children.Add(typeSelector);

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
                    Style = ResourceHelper.GetStyle("ModernPasswordBoxStyle")
                };
                ModernWpf.Controls.ControlHelper.SetPlaceholderText(passwordBox, "Enter password");
                passwordBox.PasswordChanged += (s, e) =>
                {
                    field.Value = passwordBox.Password;
                    onFieldChanged?.Invoke(field);
                };
                return passwordBox;

            case CustomFieldType.Date:
                var datePicker = new DatePicker
                {
                    Style = ResourceHelper.GetStyle("ModernDatePickerStyle")
                };
                if (DateTime.TryParse(field.Value, out var date))
                {
                    datePicker.SelectedDate = date;
                }
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
                var textArea = new System.Windows.Controls.TextBox
                {
                    Text = field.Value,
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    MaxHeight = 120,
                    Style = ResourceHelper.GetStyle("ModernTextBoxStyle")
                };
                ModernWpf.Controls.ControlHelper.SetPlaceholderText(textArea, "Enter text");
                textArea.TextChanged += (s, e) =>
                {
                    field.Value = textArea.Text;
                    onFieldChanged?.Invoke(field);
                };
                return textArea;

            case CustomFieldType.Number:
                var numberBox = new ModernWpf.Controls.NumberBox
                {
                    Value = double.TryParse(field.Value, out var number) ? number : 0,
                    Style = ResourceHelper.GetStyle("ModernNumberBoxStyle")
                };
                ModernWpf.Controls.ControlHelper.SetPlaceholderText(numberBox, "Enter number");
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
                var textBox = new System.Windows.Controls.TextBox
                {
                    Text = field.Value,
                    Style = ResourceHelper.GetStyle("ModernTextBoxStyle")
                };
                ModernWpf.Controls.ControlHelper.SetPlaceholderText(textBox, GetPlaceholderForType(field.Type));
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

    public static System.Windows.Controls.ComboBox CreateFieldTypeSelector(CustomFieldType selectedType, Action<CustomFieldType> onTypeChanged)
    {
        var comboBox = new System.Windows.Controls.ComboBox
        {
            Style = ResourceHelper.GetStyle("ModernComboBoxStyle")
        };
        ModernWpf.Controls.ControlHelper.SetPlaceholderText(comboBox, "Select field type");

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
