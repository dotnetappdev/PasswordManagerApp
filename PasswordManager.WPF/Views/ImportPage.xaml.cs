using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.WPF.ViewModels;

namespace PasswordManager.WPF.Views;

public sealed partial class ImportPage : Page
{
    private ImportViewModel? _viewModel;
    private IServiceProvider? _serviceProvider;

    public ImportPage()
    {
        this.InitializeComponent();
    }

    public void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
    {
        // Note: WPF Page doesn't have base.OnNavigatedTo
        if (e.ExtraData is IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _viewModel = new ImportViewModel(serviceProvider);
            this.DataContext = _viewModel;
        }
    }

    private void ImportTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // ViewModel binding will handle this automatically
    }

    private async void BrowseFileButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Use WPF's OpenFileDialog instead of WinUI's FilePicker
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Import File",
                Filter = "All Supported Files|*.csv;*.1pux;*.json;*.txt|CSV Files (*.csv)|*.csv|1Password Files (*.1pux)|*.1pux|JSON Files (*.json)|*.json|Text Files (*.txt)|*.txt",
                FilterIndex = 1,
                DefaultExt = ".csv",
                CheckFileExists = true,
                CheckPathExists = true
            };

            var result = dialog.ShowDialog();
            if (result == true && _viewModel != null)
            {
                _viewModel.SelectedFilePath = dialog.FileName;
            }
        }
        catch (Exception ex)
        {
            var errorDialog = new ModernWpf.Controls.ContentDialog
            {
                Title = "File Selection Error",
                Content = $"Failed to open file picker: {ex.Message}",
                CloseButtonText = "OK"
            };

            await errorDialog.ShowAsync();
        }
    }

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            var success = await _viewModel.ImportAsync();

            if (!success && !string.IsNullOrEmpty(_viewModel.ImportStatus))
            {
                var dialog = new ModernWpf.Controls.ContentDialog
                {
                    Title = "Import Error",
                    Content = _viewModel.ImportStatus,
                    CloseButtonText = "OK"
                };

                await dialog.ShowAsync();
            }
        }
    }

    private void ClearResultsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.ClearResults();
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.SelectedFilePath = string.Empty;
            _viewModel.ClearResults();
        }
    }
}
