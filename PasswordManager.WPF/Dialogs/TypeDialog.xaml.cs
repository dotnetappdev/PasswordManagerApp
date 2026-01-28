using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System;
using System.Threading.Tasks;

namespace PasswordManager.WPF.Dialogs;

public sealed partial class TypeDialog : ModernWpf.Controls.ContentDialog
{
    private string? _existingTypeName;
    private readonly bool _isEditMode;
    private readonly bool _isReadOnly;

    public string? Result { get; private set; }

    public TypeDialog(string? existingTypeName = null, bool isReadOnly = false)
    {
        this.InitializeComponent();
        _existingTypeName = existingTypeName;
        _isEditMode = !string.IsNullOrEmpty(existingTypeName);
        _isReadOnly = isReadOnly;

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

        if (_isReadOnly)
        {
            // populate display controls and hide inputs
            TypeNameTextDisplay.Text = TypeNameTextBox.Text;
            TypeNameTextDisplay.CopyText = TypeNameTextBox.Text;
            TypeDescriptionTextDisplay.Text = TypeDescriptionTextBox.Text;
            TypeDescriptionTextDisplay.CopyText = TypeDescriptionTextBox.Text;
            TypeNameTextBox.Visibility = Visibility.Collapsed;
            TypeNameTextDisplay.Visibility = Visibility.Visible;
            TypeDescriptionTextBox.Visibility = Visibility.Collapsed;
            TypeDescriptionTextDisplay.Visibility = Visibility.Visible;

            // Populate and show icon/color display
            var iconText = "";
            if (TypeIconComboBox.SelectedItem is ComboBoxItem sel && sel.Content is StackPanel sp && sp.Children.Count > 1 && sp.Children[1] is TextBlock tb)
            {
                iconText = tb.Text;
            }
            else if (TypeIconComboBox.SelectedItem is ComboBoxItem sel2 && sel2.Content is StackPanel sp3 && sp3.Children.Count > 1 && sp3.Children[1] is TextBlock tb3)
            {
                iconText = tb3.Text;
            }
            TypeIconTextDisplay.Text = iconText;
            TypeIconTextDisplay.CopyText = iconText;
            TypeIconComboBox.Visibility = Visibility.Collapsed;
            TypeIconTextDisplay.Visibility = Visibility.Visible;

            var colorText = "";
            if (TypeColorComboBox.SelectedItem is ComboBoxItem colorSel && colorSel.Content is StackPanel csp && csp.Children.Count > 1 && csp.Children[1] is TextBlock ctb)
                colorText = ctb.Text;
            TypeColorTextDisplay.Text = colorText;
            TypeColorTextDisplay.CopyText = colorText;
            TypeColorComboBox.Visibility = Visibility.Collapsed;
            TypeColorTextDisplay.Visibility = Visibility.Visible;

            PrimaryButtonText = "Close";
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
            var ellipse = stackPanel?.Children[0] as Ellipse;
            if (ellipse?.Fill is SolidColorBrush colorBrush)
            {
                ColorPreview.Fill = colorBrush;
            }
        }
    }

    private async void TypeDialog_PrimaryButtonClick(ModernWpf.Controls.ContentDialog sender, ModernWpf.Controls.ContentDialogButtonClickEventArgs args)
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
