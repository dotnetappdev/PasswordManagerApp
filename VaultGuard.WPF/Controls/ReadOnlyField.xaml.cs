using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VaultGuard.WPF.Controls
{
    public sealed partial class ReadOnlyField : UserControl
    {
        public ReadOnlyField()
        {
            this.InitializeComponent();
            UpdateDisplay();
            
            // Add WPF mouse enter/leave handlers
            this.MouseEnter += (s, e) => CopyButton.Visibility = IsEmptyValue(CopyText) && IsEmptyValue(Text) ? Visibility.Collapsed : Visibility.Visible;
            this.MouseLeave += (s, e) => CopyButton.Visibility = Visibility.Collapsed;
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(ReadOnlyField),
                new PropertyMetadata(string.Empty, (d, _) => ((ReadOnlyField)d).UpdateDisplay()));

        /// <summary>What an empty field says. A lone dash didn't tell people the value simply isn't saved.</summary>
        public const string EmptyText = "Not added";

        /// <summary>True for no value, or the "—" placeholder the details page uses for one.</summary>
        public static bool IsEmptyValue(string? text) => string.IsNullOrWhiteSpace(text) || text.Trim() == "—" || text == EmptyText;

        /// <summary>Shows empty values as a muted, italic "Not added" (and nothing to copy) instead of a dash.</summary>
        private void UpdateDisplay()
        {
            if (ValueTextBlock == null) return;
            var empty = IsEmptyValue(Text);
            ValueTextBlock.Text = empty ? EmptyText : Text;
            ValueTextBlock.FontStyle = empty ? FontStyles.Italic : FontStyles.Normal;
            ValueTextBlock.Opacity = empty ? 0.5 : 1.0;
        }

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
            VaultGuard.WPF.Helpers.SecureClipboard.Copy(textToCopy);
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
