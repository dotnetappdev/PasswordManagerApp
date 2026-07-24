using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using VaultGuard.Models.DTOs.Audit;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.WPF.Views;

public sealed partial class AuditLogsPage : Page
{
    private sealed class LogRow
    {
        public string Action { get; init; } = string.Empty;
        public Brush ActionBrush { get; init; } = Brushes.Gray;
        public string EntityType { get; init; } = string.Empty;
        public string EntityName { get; init; } = string.Empty;
        public string TimestampText { get; init; } = string.Empty;
        public string StatusGlyph { get; init; } = string.Empty;
        public Brush StatusBrush { get; init; } = Brushes.Gray;
        public bool HasChanges { get; init; }
        public string? Changes { get; init; }
        public string? ErrorMessage { get; init; }
    }

    private static readonly string[] ActionOptions = { "All Actions", "Create", "Update", "Delete", "Login", "Logout", "Export", "Import" };
    private static readonly string[] EntityTypeOptions = { "All Types", "PasswordItem", "Vault", "Category", "Collection", "Tag", "User", "Passkey" };

    private IServiceProvider? _serviceProvider;
    private List<AuditLogDto> _allLogs = new();

    public AuditLogsPage()
    {
        InitializeComponent();
        ActionComboBox.ItemsSource = ActionOptions;
        ActionComboBox.SelectedIndex = 0;
        EntityTypeComboBox.ItemsSource = EntityTypeOptions;
        EntityTypeComboBox.SelectedIndex = 0;
    }

    public async void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
    {
        if (e.ExtraData is IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            await LoadLogsAsync();
        }
    }

    private async Task LoadLogsAsync()
    {
        if (_serviceProvider is null) return;

        try
        {
            var authService = _serviceProvider.GetRequiredService<IAuthService>();
            var userId = authService.CurrentUser?.Id ?? await authService.GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                _allLogs = new List<AuditLogDto>();
                ApplyFilter();
                return;
            }

            var auditLogService = _serviceProvider.GetRequiredService<IAuditLogService>();
            var response = await auditLogService.GetAuditLogsAsync(userId, new ListAuditLogsRequestDto
            {
                Page = 1,
                PageSize = 500
            });

            _allLogs = response?.Logs ?? new List<AuditLogDto>();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Error("Failed to load audit logs", ex);
            await ShowErrorDialog($"Failed to load audit logs: {ex.Message}");
        }
    }

    private void ApplyFilter()
    {
        var search = SearchTextBox?.Text?.Trim() ?? string.Empty;
        var action = ActionComboBox.SelectedIndex > 0 ? ActionOptions[ActionComboBox.SelectedIndex] : string.Empty;
        var entityType = EntityTypeComboBox.SelectedIndex > 0 ? EntityTypeOptions[EntityTypeComboBox.SelectedIndex] : string.Empty;

        var filtered = _allLogs.Where(l =>
                (string.IsNullOrEmpty(search)
                    || l.Action.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || (l.EntityType?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (l.EntityName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                && (string.IsNullOrEmpty(action) || string.Equals(l.Action, action, StringComparison.OrdinalIgnoreCase))
                && (string.IsNullOrEmpty(entityType) || string.Equals(l.EntityType, entityType, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(l => l.Timestamp)
            .Select(ToRow)
            .ToList();

        LogsItemsControl.ItemsSource = new ObservableCollection<LogRow>(filtered);
        EmptyStateText.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static LogRow ToRow(AuditLogDto log)
    {
        var successBrush = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
        var failureBrush = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));

        return new LogRow
        {
            Action = log.Action,
            ActionBrush = log.Success ? new SolidColorBrush(Color.FromRgb(0x9D, 0x9D, 0x9D)) : failureBrush,
            EntityType = log.EntityType,
            EntityName = log.EntityName ?? string.Empty,
            TimestampText = log.Timestamp.ToLocalTime().ToString("MMM d, yyyy HH:mm:ss"),
            StatusGlyph = log.Success ? "" : "", // checkmark / error glyphs (Segoe MDL2)
            StatusBrush = log.Success ? successBrush : failureBrush,
            HasChanges = !string.IsNullOrEmpty(log.Changes),
            Changes = log.Changes,
            ErrorMessage = log.ErrorMessage,
        };
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await LoadLogsAsync();

    private void Filter_Changed(object sender, RoutedEventArgs e) => ApplyFilter();

    private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
    {
        SearchTextBox.Text = string.Empty;
        ActionComboBox.SelectedIndex = 0;
        EntityTypeComboBox.SelectedIndex = 0;
        ApplyFilter();
    }

    private async void DetailsButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not LogRow row) return;

        var panel = new StackPanel();
        if (!string.IsNullOrEmpty(row.ErrorMessage))
        {
            panel.Children.Add(new TextBlock
            {
                Text = row.ErrorMessage,
                Foreground = new SolidColorBrush(Color.FromRgb(0xFC, 0xA5, 0xA5)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            });
        }
        panel.Children.Add(new TextBox
        {
            Text = row.Changes,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            MaxHeight = 320,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Style = (Style)Application.Current.Resources["ModernTextBoxStyle"]
        });

        var dialog = new ModernWpf.Controls.ContentDialog
        {
            Title = "Log Entry Details",
            Content = panel,
            CloseButtonText = "Close"
        };
        await dialog.ShowAsync();
    }

    private async Task ShowErrorDialog(string message)
    {
        var dialog = new ModernWpf.Controls.ContentDialog { Title = "Error", Content = message, CloseButtonText = "OK" };
        await dialog.ShowAsync();
    }
}
