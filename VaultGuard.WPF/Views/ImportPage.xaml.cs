using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using VaultGuard.WPF.ViewModels;
using VaultGuard.WPF.Services;

namespace VaultGuard.WPF.Views;

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
        if (_viewModel == null) return;
        var success = await _viewModel.ImportAsync();

        if (success)
        {
            ToastService.Instance.Show(
                $"Import complete — {_viewModel.ImportedItemsCount} item{(_viewModel.ImportedItemsCount == 1 ? "" : "s")} added to vault.",
                ToastType.Success, "Import");
        }
        else
        {
            ToastService.Instance.Show(
                string.IsNullOrEmpty(_viewModel.ImportStatus) ? "Import failed." : _viewModel.ImportStatus,
                ToastType.Error, "Import Error");
        }
    }

    private void NewImportButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel?.ClearResults();
        if (_viewModel != null)
            _viewModel.SelectedFilePath = string.Empty;
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

    private async void ClearDataButton_Click(object sender, RoutedEventArgs e)
    {
        await RunMaintenanceAsync(
            "Clear Vault Data",
            "This permanently deletes all vault items, categories, collections and tags. User accounts are kept. Continue?",
            service => service.ResetDataTablesAsync());
    }

    private async System.Threading.Tasks.Task RunMaintenanceAsync(
        string title,
        string message,
        Func<VaultGuard.Services.Interfaces.IDatabaseResetService,
             System.Threading.Tasks.Task<VaultGuard.Services.Interfaces.DatabaseResetResult>> operation)
    {
        var serviceProvider = _serviceProvider ?? (App.Current as App)?.Services;
        if (serviceProvider == null) return;

        var confirm = new ModernWpf.Controls.ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = "Yes, continue",
            CloseButtonText = "Cancel"
        };

        if (await confirm.ShowAsync() != ModernWpf.Controls.ContentDialogResult.Primary)
            return;

        try
        {
            using var scope = serviceProvider.CreateScope();
            var resetService = scope.ServiceProvider
                .GetRequiredService<VaultGuard.Services.Interfaces.IDatabaseResetService>();
            var result = await operation(resetService);

            var resultDialog = new ModernWpf.Controls.ContentDialog
            {
                Title = result.Success ? "Done" : "Completed with errors",
                Content = result.Message,
                CloseButtonText = "OK"
            };
            await resultDialog.ShowAsync();
        }
        catch (Exception ex)
        {
            var errorDialog = new ModernWpf.Controls.ContentDialog
            {
                Title = "Error",
                Content = ex.Message,
                CloseButtonText = "OK"
            };
            await errorDialog.ShowAsync();
        }
    }
}
