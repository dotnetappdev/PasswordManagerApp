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
            
            // Add WPF mouse enter/leave handlers
            this.MouseEnter += (s, e) => CopyButton.Visibility = Visibility.Visible;
            this.MouseLeave += (s, e) => CopyButton.Visibility = Visibility.Collapsed;
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(ReadOnlyField), new PropertyMetadata(string.Empty));

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
            // Use WPF Clipboard API
            var textToCopy = CopyText ?? Text ?? string.Empty;
            System.Windows.Clipboard.SetText(textToCopy);
        }

        private void Root_GotFocus(object sender, RoutedEventArgs e)
        {
            CopyButton.Visibility = Visibility.Visible;
        }

        private void Root_LostFocus(object sender, RoutedEventArgs e)
        {
            CopyButton.Visibility = Visibility.Collapsed;
        }
    }
}
