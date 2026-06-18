using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ModernWpf.Controls;
using PasswordManager.Models.DTOs;

namespace PasswordManager.WPF.Dialogs;

public partial class CloudBackupBrowseDialog : ContentDialog
{
    private BackupContentsDto _contents;
    private List<ItemRow> _allRows = new();

    public IReadOnlyList<string> SelectedIds { get; private set; } = Array.Empty<string>();

    public CloudBackupBrowseDialog(BackupContentsDto contents)
    {
        InitializeComponent();
        this.PrimaryButtonClick += (_, args) =>
        {
            SelectedIds = _allRows.Where(r => r.Item.IsSelected).Select(r => r.Item.Id).ToList();
        };
        _contents = contents;
        BackupDateText.Text = contents.CreatedAt.ToLocalTime().ToString("MMMM d, yyyy  h:mm tt");
        BackupCountText.Text = $"{contents.TotalCount} items in this backup";
        BuildList(string.Empty);
        UpdateCount();
    }

    private void BuildList(string filter)
    {
        ItemsPanel.Children.Clear();
        _allRows.Clear();

        AddGroup("Logins", "", "#2563EB", _contents.LoginItems, filter);
        AddGroup("Secure Notes", "", "#7C3AED", _contents.SecureNotes, filter);
        AddGroup("Credit Cards", "", "#059669", _contents.CreditCards, filter);
        AddGroup("Wi-Fi", "", "#D97706", _contents.WifiItems, filter);
    }

    private void AddGroup(string label, string icon, string color, List<BackupItemDto> items, string filter)
    {
        var visible = string.IsNullOrWhiteSpace(filter)
            ? items
            : items.Where(i => i.Title.Contains(filter, StringComparison.OrdinalIgnoreCase)
                            || (i.Subtitle?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false))
                   .ToList();
        if (!visible.Any()) return;

        // Group header
        var header = new Border
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0D1B2A")),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 6, 10, 6),
            Margin = new Thickness(0, 8, 0, 4)
        };
        var hRow = new StackPanel { Orientation = Orientation.Horizontal };
        hRow.Children.Add(new TextBlock
        {
            Text = icon, FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 13, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
            Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center
        });
        hRow.Children.Add(new TextBlock
        {
            Text = $"{label}  ({visible.Count})",
            FontWeight = FontWeights.SemiBold, FontSize = 13,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
            VerticalAlignment = VerticalAlignment.Center
        });
        header.Child = hRow;
        ItemsPanel.Children.Add(header);

        foreach (var item in visible)
        {
            var row = new ItemRow(item);
            _allRows.Add(row);

            var card = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(4, 6, 4, 6)
            };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var check = new CheckBox { IsChecked = item.IsSelected, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
            check.Checked += (_, _) => { item.IsSelected = true; UpdateCount(); };
            check.Unchecked += (_, _) => { item.IsSelected = false; UpdateCount(); };
            row.CheckBox = check;
            Grid.SetColumn(check, 0);

            var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            info.Children.Add(new TextBlock { Text = item.Title, FontWeight = FontWeights.SemiBold, FontSize = 13 });
            if (!string.IsNullOrEmpty(item.Subtitle))
                info.Children.Add(new TextBlock { Text = item.Subtitle, FontSize = 11, Opacity = 0.6 });
            Grid.SetColumn(info, 1);

            grid.Children.Add(check);
            grid.Children.Add(info);
            card.Child = grid;
            ItemsPanel.Children.Add(card);
        }
    }

    private void UpdateCount()
    {
        var count = _allRows.Count(r => r.Item.IsSelected);
        SelectedCountText.Text = $"{count} selected";

        var all = _allRows.All(r => r.Item.IsSelected);
        var none = _allRows.All(r => !r.Item.IsSelected);
        SelectAllCheck.Checked -= SelectAllCheck_Changed;
        SelectAllCheck.Unchecked -= SelectAllCheck_Changed;
        SelectAllCheck.IsChecked = all ? true : (none ? false : null);
        SelectAllCheck.Checked += SelectAllCheck_Changed;
        SelectAllCheck.Unchecked += SelectAllCheck_Changed;

        IsPrimaryButtonEnabled = count > 0;
    }

    private void SelectAllCheck_Changed(object sender, RoutedEventArgs e)
    {
        var check = (CheckBox)sender;
        var val = check.IsChecked == true;
        foreach (var row in _allRows)
        {
            row.Item.IsSelected = val;
            if (row.CheckBox != null) row.CheckBox.IsChecked = val;
        }
        UpdateCount();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        BuildList(SearchBox.Text);
        foreach (var row in _allRows)
            row.CheckBox?.InvalidateVisual();
        UpdateCount();
    }

    private class ItemRow
    {
        public BackupItemDto Item { get; }
        public CheckBox? CheckBox { get; set; }
        public ItemRow(BackupItemDto item) { Item = item; }
    }
}
