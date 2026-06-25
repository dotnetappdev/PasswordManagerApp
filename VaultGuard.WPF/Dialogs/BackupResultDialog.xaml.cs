using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace VaultGuard.WPF.Dialogs;

public sealed partial class BackupResultDialog : ModernWpf.Controls.ContentDialog
{
    private readonly Exception? _exception;
    private readonly string? _errorMessage;
    private readonly bool _success;

    public BackupResultDialog(bool success, string message, Exception? exception = null, string? errorMessage = null)
    {
        InitializeComponent();

        _success = success;
        _exception = exception;
        _errorMessage = errorMessage;

        Title = success ? "Backup Created" : "Backup Failed";
        MessageText.Text = message;

        if (success)
        {
            IconBadge.Background = (Brush)FindResource("ModernSuccessBrush");
            IconGlyph.Text = "";
        }
        else
        {
            IconBadge.Background = (Brush)FindResource("ModernErrorBrush");
            IconGlyph.Text = "";

            if (exception != null)
            {
                ErrorDetailsToggle.Visibility = Visibility.Visible;
                CopyDetailsButton.Visibility = Visibility.Visible;
                BuildExceptionTree(exception);
            }
            else if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                SimpleDetailText.Text = $"Details: {errorMessage}";
                SimpleDetailText.Visibility = Visibility.Visible;
                CopyDetailsButton.Visibility = Visibility.Visible;
            }
        }
    }

    private void BuildExceptionTree(Exception exception)
    {
        ExceptionTreeView.Items.Clear();
        ExceptionTreeView.Items.Add(BuildExceptionNode(exception));
    }

    private TreeViewItem BuildExceptionNode(Exception ex)
    {
        var node = new TreeViewItem
        {
            Header = new TextBlock
            {
                Text = $"{ex.GetType().Name}: {ex.Message}",
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)FindResource("ModernErrorBrush")
            }
        };

        if (!string.IsNullOrWhiteSpace(ex.StackTrace))
        {
            var stackNode = new TreeViewItem { Header = "Stack Trace" };
            stackNode.Items.Add(new TextBox
            {
                Text = ex.StackTrace,
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = (Brush)FindResource("ModernTextSecondaryBrush"),
                MaxWidth = 460
            });
            node.Items.Add(stackNode);
        }

        if (ex.InnerException != null)
        {
            node.Items.Add(BuildExceptionNode(ex.InnerException));
        }

        return node;
    }

    private void ErrorDetailsToggle_Click(object sender, RoutedEventArgs e)
    {
        var expanded = ErrorDetailsToggle.IsChecked == true;
        ErrorDetailsPanel.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
        ErrorDetailsChevron.Text = expanded ? "" : "";
    }

    private void CopyDetailsButton_Click(object sender, RoutedEventArgs e)
    {
        var text = _exception != null
            ? BuildExceptionText(_exception)
            : _errorMessage ?? string.Empty;

        if (!string.IsNullOrEmpty(text))
        {
            Clipboard.SetText(text);
        }
    }

    private static string BuildExceptionText(Exception ex)
    {
        var sb = new StringBuilder();
        var current = ex;
        while (current != null)
        {
            sb.AppendLine($"{current.GetType().FullName}: {current.Message}");
            if (!string.IsNullOrWhiteSpace(current.StackTrace))
            {
                sb.AppendLine(current.StackTrace);
            }
            current = current.InnerException;
            if (current != null)
            {
                sb.AppendLine();
                sb.AppendLine("--- Inner Exception ---");
            }
        }
        return sb.ToString();
    }
}
