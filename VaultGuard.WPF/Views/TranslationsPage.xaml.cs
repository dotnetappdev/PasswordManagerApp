using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using VaultGuard.Localization;
using VaultGuard.WPF.Localization;

namespace VaultGuard.WPF.Views;

public sealed partial class TranslationsPage : Page
{
    // A brand-new key from AddKeyButton_Click has no value in any language yet, so it must be
    // tracked independently of any single language's catalog - see TranslationRepository's doc
    // comment for why AddKey/GetAllKeys work off their own registry instead of unioning catalogs.
    private sealed class TranslationRow : INotifyPropertyChanged
    {
        private string _value = string.Empty;
        public string Key { get; init; } = string.Empty;
        public string Value
        {
            get => _value;
            set
            {
                if (_value == value) return;
                _value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private string _selectedLanguageCode = string.Empty;

    public TranslationsPage()
    {
        InitializeComponent();
        Loaded += TranslationsPage_Loaded;
    }

    // No navigation parameter needed - TranslationRepository is a self-contained static store.
    public void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e) { }

    private void TranslationsPage_Loaded(object sender, RoutedEventArgs e) => RefreshLanguageComboBox();

    private void RefreshLanguageComboBox()
    {
        var languages = SupportedLanguages.All.Where(l => l.Code != "en").ToList();
        var previouslySelected = _selectedLanguageCode;

        LanguageComboBox.ItemsSource = languages;

        var toSelect = languages.FirstOrDefault(l => l.Code == previouslySelected) ?? languages.FirstOrDefault();
        LanguageComboBox.SelectedItem = toSelect;

        // No languages left (all removed) - SelectionChanged won't fire for a null selection, so
        // clear the grid explicitly instead of leaving the previous language's rows on screen.
        if (toSelect is null)
        {
            _selectedLanguageCode = string.Empty;
            LoadRows();
        }
    }

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguageComboBox.SelectedItem is LanguageInfo language)
        {
            _selectedLanguageCode = language.Code;
            LoadRows();
        }
    }

    private void LoadRows()
    {
        if (string.IsNullOrEmpty(_selectedLanguageCode))
        {
            TranslationsItemsControl.ItemsSource = null;
            EmptyStateText.Visibility = Visibility.Visible;
            return;
        }

        var catalog = TranslationRepository.GetCatalog(_selectedLanguageCode);
        var keys = TranslationRepository.GetAllKeys();
        var filter = SearchTextBox?.Text?.Trim() ?? string.Empty;

        var rows = keys
            .Where(k => string.IsNullOrEmpty(filter)
                || k.Contains(filter, StringComparison.OrdinalIgnoreCase)
                || (catalog.TryGetValue(k, out var v) && v.Contains(filter, StringComparison.OrdinalIgnoreCase)))
            .Select(k => new TranslationRow { Key = k, Value = catalog.TryGetValue(k, out var v) ? v : string.Empty })
            .ToList();

        TranslationsItemsControl.ItemsSource = new ObservableCollection<TranslationRow>(rows);
        EmptyStateText.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => LoadRows();

    private void TranslationTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is TranslationRow row && !string.IsNullOrEmpty(_selectedLanguageCode))
        {
            TranslationRepository.SetTranslation(_selectedLanguageCode, row.Key, tb.Text);
            LocalizationManager.Instance.RefreshTranslations();
        }
    }

    private async void DeleteKeyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not TranslationRow row) return;

        var dialog = new ModernWpf.Controls.ContentDialog
        {
            Title = "Delete Translation Key",
            Content = $"Delete the key \"{row.Key}\" from every language? This can't be undone.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ModernWpf.Controls.ContentDialogButton.Close
        };

        var result = await dialog.ShowAsync();
        if (result == ModernWpf.Controls.ContentDialogResult.Primary)
        {
            TranslationRepository.DeleteKey(row.Key);
            LocalizationManager.Instance.RefreshTranslations();
            LoadRows();
        }
    }

    private async void AddKeyButton_Click(object sender, RoutedEventArgs e)
    {
        var textBox = new TextBox { MinWidth = 340, Style = (Style)Application.Current.Resources["ModernTextBoxStyle"] };
        ModernWpf.Controls.Primitives.ControlHelper.SetPlaceholderText(textBox, "e.g. \"Export Vault\"");

        var panel = new StackPanel();
        panel.Children.Add(new TextBlock
        {
            Text = "The key is the literal English text shown in the app.",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        });
        panel.Children.Add(textBox);

        var dialog = new ModernWpf.Controls.ContentDialog
        {
            Title = "Add Translation Key",
            Content = panel,
            PrimaryButtonText = "Add",
            CloseButtonText = "Cancel",
            DefaultButton = ModernWpf.Controls.ContentDialogButton.Primary
        };

        var result = await dialog.ShowAsync();
        if (result != ModernWpf.Controls.ContentDialogResult.Primary) return;

        var key = textBox.Text?.Trim();
        if (string.IsNullOrEmpty(key)) return;

        TranslationRepository.AddKey(key);
        LoadRows();
    }

    private async void AddLanguageButton_Click(object sender, RoutedEventArgs e)
    {
        var textBoxStyle = (Style)Application.Current.Resources["ModernTextBoxStyle"];

        var codeBox = new TextBox { MinWidth = 340, Style = textBoxStyle };
        ModernWpf.Controls.Primitives.ControlHelper.SetPlaceholderText(codeBox, "Code, e.g. \"it\"");

        var nativeBox = new TextBox { MinWidth = 340, Style = textBoxStyle, Margin = new Thickness(0, 8, 0, 0) };
        ModernWpf.Controls.Primitives.ControlHelper.SetPlaceholderText(nativeBox, "Native name, e.g. \"Italiano\"");

        var englishBox = new TextBox { MinWidth = 340, Style = textBoxStyle, Margin = new Thickness(0, 8, 0, 0) };
        ModernWpf.Controls.Primitives.ControlHelper.SetPlaceholderText(englishBox, "English name, e.g. \"Italian\"");

        var panel = new StackPanel();
        panel.Children.Add(codeBox);
        panel.Children.Add(nativeBox);
        panel.Children.Add(englishBox);

        var dialog = new ModernWpf.Controls.ContentDialog
        {
            Title = "Add Language",
            Content = panel,
            PrimaryButtonText = "Add",
            CloseButtonText = "Cancel",
            DefaultButton = ModernWpf.Controls.ContentDialogButton.Primary
        };

        var result = await dialog.ShowAsync();
        if (result != ModernWpf.Controls.ContentDialogResult.Primary) return;

        try
        {
            TranslationRepository.AddLanguage(codeBox.Text?.Trim() ?? "", nativeBox.Text?.Trim() ?? "", englishBox.Text?.Trim() ?? "");
            LocalizationManager.Instance.RefreshLanguages();
            RefreshLanguageComboBox();
        }
        catch (Exception ex)
        {
            await ShowErrorDialog(ex.Message);
        }
    }

    private async void RemoveLanguageButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedLanguageCode)) return;
        var codeToRemove = _selectedLanguageCode;

        var dialog = new ModernWpf.Controls.ContentDialog
        {
            Title = "Remove Language",
            Content = $"Remove \"{codeToRemove}\" and all of its translations? This can't be undone.",
            PrimaryButtonText = "Remove",
            CloseButtonText = "Cancel",
            DefaultButton = ModernWpf.Controls.ContentDialogButton.Close
        };

        var result = await dialog.ShowAsync();
        if (result != ModernWpf.Controls.ContentDialogResult.Primary) return;

        TranslationRepository.RemoveLanguage(codeToRemove);
        LocalizationManager.Instance.RefreshLanguages();
        _selectedLanguageCode = string.Empty;
        RefreshLanguageComboBox();
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedLanguageCode)) return;

        var dialog = new SaveFileDialog
        {
            FileName = $"messages.{_selectedLanguageCode}.po",
            Filter = "PO files (*.po)|*.po|All files (*.*)|*.*",
            DefaultExt = ".po"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            File.WriteAllText(dialog.FileName, TranslationRepository.ExportPo(_selectedLanguageCode));
        }
        catch (Exception ex)
        {
            _ = ShowErrorDialog($"Failed to export: {ex.Message}");
        }
    }

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedLanguageCode)) return;

        var openDialog = new OpenFileDialog { Filter = "PO files (*.po)|*.po|All files (*.*)|*.*" };
        if (openDialog.ShowDialog() != true) return;

        var modeDialog = new ModernWpf.Controls.ContentDialog
        {
            Title = "Import .po File",
            Content = "Merge with existing translations, or replace them entirely?",
            PrimaryButtonText = "Merge",
            SecondaryButtonText = "Replace All",
            CloseButtonText = "Cancel"
        };

        var modeResult = await modeDialog.ShowAsync();
        if (modeResult == ModernWpf.Controls.ContentDialogResult.None) return;

        try
        {
            var text = File.ReadAllText(openDialog.FileName);
            TranslationRepository.ImportPo(_selectedLanguageCode, text, replace: modeResult == ModernWpf.Controls.ContentDialogResult.Secondary);
            LocalizationManager.Instance.RefreshTranslations();
            LoadRows();
        }
        catch (Exception ex)
        {
            await ShowErrorDialog($"Failed to import: {ex.Message}");
        }
    }

    private async Task ShowErrorDialog(string message)
    {
        var dialog = new ModernWpf.Controls.ContentDialog { Title = "Error", Content = message, CloseButtonText = "OK" };
        await dialog.ShowAsync();
    }
}
