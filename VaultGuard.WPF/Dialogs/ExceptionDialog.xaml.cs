using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace VaultGuard.WPF.Dialogs;

/// <summary>
/// Generic, reusable exception/error dialog for the WPF app. Informs the user about a failure
/// gracefully and offers an expandable, copyable details view (exception type, message, stack
/// trace, and inner exceptions). Use <see cref="ShowAsync"/> from anywhere a catch block needs to
/// surface a problem to the user.
/// </summary>
public sealed partial class ExceptionDialog : ModernWpf.Controls.ContentDialog
{
    private readonly Exception? _exception;
    private readonly string? _errorMessage;

    public ExceptionDialog(string message, Exception? exception = null, string? title = null, string? errorMessage = null)
    {
        InitializeComponent();

        _exception = exception;
        _errorMessage = errorMessage;

        if (!string.IsNullOrWhiteSpace(title)) Title = title;
        MessageText.Text = message;

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

    /// <summary>
    /// Builds and shows the dialog on the UI thread. Safe to call from anywhere — it marshals onto
    /// the dispatcher and never throws (a failure to show is logged, not rethrown).
    /// </summary>
    public static async Task ShowAsync(string message, Exception? exception = null, string? title = null, string? errorMessage = null)
    {
        try
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null) return;

            await dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    var dialog = new ExceptionDialog(message, exception, title, errorMessage);
                    await dialog.ShowAsync();
                }
                catch (Exception showEx)
                {
                    VaultGuard.Services.Logging.AppLogger.Warning("Failed to show ExceptionDialog", showEx);
                }
            });
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Warning("ExceptionDialog.ShowAsync failed to dispatch", ex);
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
        // Segoe MDL2: ChevronDown (E70D) when open, ChevronRight (E76C) when closed.
        ErrorDetailsChevron.Text = char.ConvertFromUtf32(expanded ? 0xE70D : 0xE76C);
    }

    private void CopyDetailsButton_Click(object sender, RoutedEventArgs e)
    {
        var text = _exception != null
            ? BuildExceptionText(_exception)
            : _errorMessage ?? string.Empty;

        if (!string.IsNullOrEmpty(text))
        {
            try { Clipboard.SetText(text); } catch { /* clipboard can transiently fail */ }
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
