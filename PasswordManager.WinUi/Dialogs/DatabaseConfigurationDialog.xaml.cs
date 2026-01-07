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
            // Validate path security
            if (!IsPathSecure(pathToUse))
            {
                args.Cancel = true;
                await ShowErrorAsync("Invalid Path", "The selected path is not allowed. Please choose a location in your user directories.");
                return;
            }
            
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
            await ShowErrorAsync("Error", $"Failed to validate path: {ex.Message}");
        }
    }

    private bool IsPathSecure(string path)
    {
        try
        {
            // Get the full path to resolve any relative paths
            var fullPath = Path.GetFullPath(path);
            
            // Disallow system directories
            var systemDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
            var windowsDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var programFilesDir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesX86Dir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            
            if (fullPath.StartsWith(systemDir, StringComparison.OrdinalIgnoreCase) ||
                fullPath.StartsWith(windowsDir, StringComparison.OrdinalIgnoreCase) ||
                fullPath.StartsWith(programFilesDir, StringComparison.OrdinalIgnoreCase) ||
                fullPath.StartsWith(programFilesX86Dir, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            
            // Disallow UNC paths (network paths)
            if (fullPath.StartsWith(@"\\"))
            {
                return false;
            }
            
            return true;
        }
        catch
        {
            return false;
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
            // Continue anyway - allow app to use defaults
        }
    }

    private async System.Threading.Tasks.Task SaveConfigurationAsync(string databasePath)
    {
        try
        {
            var config = _databaseConfigService.GetDefaultConfiguration();
            
            // Ensure Sqlite config is initialized
            if (config.Sqlite == null)
            {
                config.Sqlite = new PasswordManager.Models.Configuration.SqliteConfig();
            }
            
            config.Sqlite.DatabasePath = databasePath;
            config.IsFirstRun = false;
            
            await _databaseConfigService.SaveConfigurationAsync(config);
            
        }
        catch (Exception ex)
        {
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
