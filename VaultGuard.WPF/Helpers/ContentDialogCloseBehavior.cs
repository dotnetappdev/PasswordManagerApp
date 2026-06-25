using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ModernWpf.Controls;

namespace VaultGuard.WPF.Helpers;

// Lets a plain Button placed inside a ContentDialog's control template act as the
// dialog's title-bar "X" close button, without needing code-behind in the XAML
// resource dictionary that defines the dialog's template.
public static class ContentDialogCloseBehavior
{
    public static readonly DependencyProperty IsCloseButtonProperty =
        DependencyProperty.RegisterAttached(
            "IsCloseButton",
            typeof(bool),
            typeof(ContentDialogCloseBehavior),
            new PropertyMetadata(false, OnIsCloseButtonChanged));

    public static bool GetIsCloseButton(DependencyObject obj) => (bool)obj.GetValue(IsCloseButtonProperty);
    public static void SetIsCloseButton(DependencyObject obj, bool value) => obj.SetValue(IsCloseButtonProperty, value);

    private static void OnIsCloseButtonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Button button) return;

        button.Click -= Button_Click;
        if ((bool)e.NewValue)
            button.Click += Button_Click;
    }

    private static void Button_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        var dialog = FindAncestorContentDialog(button);
        dialog?.Hide();
    }

    private static ContentDialog? FindAncestorContentDialog(DependencyObject child)
    {
        var current = child;
        while (current != null)
        {
            if (current is ContentDialog dialog) return dialog;
            current = VisualTreeHelper.GetParent(current) ?? LogicalTreeHelper.GetParent(current);
        }
        return null;
    }
}
