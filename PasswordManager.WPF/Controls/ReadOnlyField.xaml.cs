using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PasswordManager.WPF.Controls
{
    public sealed partial class ReadOnlyField : UserControl
    {
        public ReadOnlyField()
        {
            this.InitializeComponent();
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(ReadOnlyField), new PropertyMetadata(string.Empty));

        // FontFamilyOverride removed - TextBlock uses theme/default font to avoid WinRT binding issues
        // Raw value to copy to clipboard (may differ from displayed Text)
        public string CopyText
        {
            get => (string)GetValue(CopyTextProperty);
            set => SetValue(CopyTextProperty, value);
        }

        public static readonly DependencyProperty CopyTextProperty =
            DependencyProperty.Register("CopyText", typeof(string), typeof(ReadOnlyField), new PropertyMetadata(string.Empty));

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(Text ?? string.Empty);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }

        private void Root_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            CopyButton.Visibility = Visibility.Visible;
        }

        private void Root_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            CopyButton.Visibility = Visibility.Collapsed;
        }

        private void Root_GotFocus(object sender, RoutedEventArgs e)
        {
            CopyButton.Visibility = Visibility.Visible;
        }

        private void Root_LostFocus(object sender, RoutedEventArgs e)
        {
            CopyButton.Visibility = Visibility.Collapsed;
        }

        private void CopyKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            // Copy on Ctrl+C when focused
            var textToCopy = CopyText ?? Text ?? string.Empty;
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(textToCopy);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
            args.Handled = true;
        }
    }
}
