using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.WinUi.ViewModels;

namespace PasswordManager.WinUi.Views;

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
        // This would normally open a file picker
        // For demonstration, we'll simulate file selection
        var dialog = new ContentDialog
        {
            Title = "File Selection",
            Content = "This would open a file picker to select import file.\nFor demo purposes, simulating file selection.",
            PrimaryButtonText = "Simulate Selection",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && _viewModel != null)
        {
            // Simulate a selected file path
            _viewModel.SelectedFilePath = @"C:\Users\Demo\passwords_export.csv";
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
}