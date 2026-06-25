using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using VaultGuard.WinUi.ViewModels;

namespace VaultGuard.WinUi.Views;

public sealed partial class ImportPage : Page
{
    private ImportViewModel? _viewModel;
    private IServiceProvider? _serviceProvider;

    public ImportPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is IServiceProvider serviceProvider)
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
            var filePicker = new Windows.Storage.Pickers.FileOpenPicker();

            // Get the current window's HWND
            var app = App.Current as App;
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(app?.MainWindow);

            // Initialize the file picker with the window handle
            WinRT.Interop.InitializeWithWindow.Initialize(filePicker, hWnd);

            filePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            filePicker.FileTypeFilter.Add(".csv");
            filePicker.FileTypeFilter.Add(".1pux");
            filePicker.FileTypeFilter.Add(".json");
            filePicker.FileTypeFilter.Add(".txt");

            var file = await filePicker.PickSingleFileAsync();
            if (file != null && _viewModel != null)
            {
                _viewModel.SelectedFilePath = file.Path;
            }
        }
        catch (Exception ex)
        {
            var dialog = new ContentDialog
            {
                Title = "File Selection Error",
                Content = $"Failed to open file picker: {ex.Message}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            var success = await _viewModel.ImportAsync();

            if (!success && !string.IsNullOrEmpty(_viewModel.ImportStatus))
            {
                var dialog = new ContentDialog
                {
                    Title = "Import Error",
                    Content = _viewModel.ImportStatus,
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
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