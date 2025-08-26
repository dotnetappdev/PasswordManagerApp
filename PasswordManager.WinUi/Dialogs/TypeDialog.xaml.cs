using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Threading.Tasks;

namespace PasswordManager.WinUi.Dialogs;

public sealed partial class TypeDialog : ContentDialog
{
    private string? _existingTypeName;
    private readonly bool _isEditMode;

    public string? Result { get; private set; }

    public TypeDialog(string? existingTypeName = null)
    {
        this.InitializeComponent();
        _existingTypeName = existingTypeName;
        _isEditMode = !string.IsNullOrEmpty(existingTypeName);

        // Update dialog title
        Title = _isEditMode ? "Edit Type" : "Add Type";
        PrimaryButtonText = _isEditMode ? "Update" : "Create";

        // Wire up events
        TypeColorComboBox.SelectionChanged += TypeColorComboBox_SelectionChanged;
        this.PrimaryButtonClick += TypeDialog_PrimaryButtonClick;

        // Load existing data if editing
        if (_isEditMode && !string.IsNullOrEmpty(_existingTypeName))
        {
            LoadTypeData();
        }
    }

    private void LoadTypeData()
    {
        if (string.IsNullOrEmpty(_existingTypeName)) return;

        TypeNameTextBox.Text = _existingTypeName;
        
        // Set icon based on type name
        var iconIndex = _existingTypeName.ToLower() switch
        {
            "login" => 0,
            "creditcard" => 1,
            "securenote" => 2,
            "wifi" => 3,
            "passkey" => 4,
            "identity" => 5,
            "document" => 6,
            _ => 7 // Other
        };
        
        TypeIconComboBox.SelectedIndex = iconIndex;
        
        // Set default color
        TypeColorComboBox.SelectedIndex = 0; // Default to blue
    }

    private void TypeColorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TypeColorComboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            // Extract color from the selected item
            var stackPanel = selectedItem.Content as StackPanel;
            var ellipse = stackPanel?.Children[0] as  Ellipse;
            if (ellipse?.Fill is SolidColorBrush colorBrush)
            {
                ColorPreview.Fill = colorBrush;
            }
        }
    }

    private async void TypeDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Validate input
        var name = TypeNameTextBox.Text?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            args.Cancel = true;
            await ShowErrorMessage("Type name is required.");
            return;
        }

        // For now, we'll just return the type name
        // In a real implementation, you would save to a database/service
        Result = name;
    }

    private async Task ShowErrorMessage(string message)
    {
        ErrorMessageText.Text = message;
        ErrorMessageBorder.Visibility = Visibility.Visible;
        
        // Auto-hide error after 5 seconds
        await Task.Delay(5000);
        ErrorMessageBorder.Visibility = Visibility.Collapsed;
    }
}