using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PasswordManager.Services.Interfaces;
using System;
using System.IO;
using Windows.Storage.Pickers;

namespace PasswordManager.WinUi.Dialogs;

public sealed partial class DatabaseConfigurationDialog : ContentDialog
{
    private readonly IPlatformService _platformService;
    private readonly IDatabaseConfigurationService _databaseConfigService;
    private string _selectedPath = string.Empty;
    private string _defaultPath = string.Empty;

    public string SelectedDatabasePath => string.IsNullOrWhiteSpace(_selectedPath) ? _defaultPath : _selectedPath;

    public string PreviewPath => SelectedDatabasePath;

    public DatabaseConfigurationDialog(IPlatformService platformService, IDatabaseConfigurationService databaseConfigService)
    {
        _platformService = platformService;
        _databaseConfigService = databaseConfigService;
        this.InitializeComponent();
        
        // Set default path
        _defaultPath = Path.Combine(_platformService.GetAppDataDirectory(), "passwordmanager.db");
        DefaultPathTextBlock.Text = _defaultPath;
        PreviewPathTextBlock.Text = _defaultPath;
    }

    private async void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var folderPicker = new FolderPicker();
            
            // Get the window handle
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Current.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
            
            folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            folderPicker.FileTypeFilter.Add("*");

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                _selectedPath = Path.Combine(folder.Path, "passwordmanager.db");
                CustomPathTextBox.Text = _selectedPath;
                CustomPathWarning.IsOpen = true;
                PreviewPathTextBlock.Text = _selectedPath;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error picking folder: {ex.Message}");
            await ShowErrorAsync("Error", $"Failed to select folder: {ex.Message}");
        }
    }

    private void CustomPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _selectedPath = CustomPathTextBox.Text?.Trim() ?? string.Empty;
        CustomPathWarning.IsOpen = !string.IsNullOrWhiteSpace(_selectedPath);
        PreviewPathTextBlock.Text = string.IsNullOrWhiteSpace(_selectedPath) ? _defaultPath : _selectedPath;
    }

    private async void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Validate the path
        var pathToUse = SelectedDatabasePath;
        
        try
        {
            // Ensure the directory exists
            var directory = Path.GetDirectoryName(pathToUse);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                // Ask for confirmation to create the directory
                args.Cancel = true;
                
                var confirmDialog = new ContentDialog
                {
                    Title = "Create Directory?",
                    Content = $"The directory does not exist:\n\n{directory}\n\nDo you want to create it?",
                    PrimaryButtonText = "Create",
                    CloseButtonText = "Cancel",
                    XamlRoot = this.XamlRoot
                };

                var result = await confirmDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    Directory.CreateDirectory(directory);
                    // Save configuration
                    await SaveConfigurationAsync(pathToUse);
                    this.Hide();
                }
            }
            else
            {
                // Directory exists, save configuration
                await SaveConfigurationAsync(pathToUse);
            }
        }
        catch (Exception ex)
        {
            args.Cancel = true;
            System.Diagnostics.Debug.WriteLine($"Error validating path: {ex.Message}");
            await ShowErrorAsync("Error", $"Failed to validate path: {ex.Message}");
        }
    }

    private async void OnCloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Use default path
        try
        {
            await SaveConfigurationAsync(_defaultPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving default configuration: {ex.Message}");
            // Continue anyway - allow app to use defaults
        }
    }

    private async System.Threading.Tasks.Task SaveConfigurationAsync(string databasePath)
    {
        try
        {
            var config = _databaseConfigService.GetDefaultConfiguration();
            config.Sqlite!.DatabasePath = databasePath;
            config.IsFirstRun = false;
            
            await _databaseConfigService.SaveConfigurationAsync(config);
            
            System.Diagnostics.Debug.WriteLine($"Database configuration saved: {databasePath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving database configuration: {ex.Message}");
            throw;
        }
    }

    private async System.Threading.Tasks.Task ShowErrorAsync(string title, string message)
    {
        var errorDialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        await errorDialog.ShowAsync();
    }
}
