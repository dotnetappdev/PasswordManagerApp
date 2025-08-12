using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Imports.Interfaces;
using PasswordManager.Models;

namespace PasswordManager.WinUi.ViewModels;

public class ImportViewModel : BaseViewModel
{
    private readonly IImportService _importService;
    private string _selectedFilePath = string.Empty;
    private string _importStatus = string.Empty;
    private string _selectedImportType = string.Empty;
    private bool _canImport = false;
    private int _importedItemsCount = 0;
    private int _skippedItemsCount = 0;
    private int _errorItemsCount = 0;

    public ImportViewModel(IServiceProvider serviceProvider)
    {
        _importService = serviceProvider.GetRequiredService<IImportService>();
        
        ImportResults = new ObservableCollection<ImportResultItem>();
        AvailableImportTypes = new List<string> 
        { 
            "1Password CSV", 
            "Bitwarden CSV", 
            "Chrome CSV",
            "Firefox CSV",
            "Safari CSV",
            "KeePass CSV",
            "LastPass CSV"
        };
        
        SelectedImportType = AvailableImportTypes.FirstOrDefault() ?? string.Empty;
    }

    public string SelectedFilePath
    {
        get => _selectedFilePath;
        set
        {
            SetProperty(ref _selectedFilePath, value);
            UpdateCanImport();
        }
    }

    public string ImportStatus
    {
        get => _importStatus;
        set => SetProperty(ref _importStatus, value);
    }

    public string SelectedImportType
    {
        get => _selectedImportType;
        set
        {
            SetProperty(ref _selectedImportType, value);
            UpdateCanImport();
        }
    }

    public bool CanImport
    {
        get => _canImport;
        set => SetProperty(ref _canImport, value);
    }

    public int ImportedItemsCount
    {
        get => _importedItemsCount;
        set => SetProperty(ref _importedItemsCount, value);
    }

    public int SkippedItemsCount
    {
        get => _skippedItemsCount;
        set => SetProperty(ref _skippedItemsCount, value);
    }

    public int ErrorItemsCount
    {
        get => _errorItemsCount;
        set => SetProperty(ref _errorItemsCount, value);
    }

    public ObservableCollection<ImportResultItem> ImportResults { get; }
    public List<string> AvailableImportTypes { get; }

    public bool HasResults => ImportResults.Count > 0;
    public bool HasImportCompleted => ImportedItemsCount > 0 || SkippedItemsCount > 0 || ErrorItemsCount > 0;
    public bool HasNoImport => !IsLoading && !HasImportCompleted && ImportResults.Count == 0;

    private void UpdateCanImport()
    {
        CanImport = !string.IsNullOrWhiteSpace(SelectedFilePath) && 
                    !string.IsNullOrWhiteSpace(SelectedImportType) && 
                    File.Exists(SelectedFilePath) &&
                    !IsLoading;
    }

    public async Task<bool> ImportAsync()
    {
        try
        {
            IsLoading = true;
            CanImport = false;
            ImportStatus = "Starting import...";
            
            // Clear previous results
            ImportResults.Clear();
            ImportedItemsCount = 0;
            SkippedItemsCount = 0;
            ErrorItemsCount = 0;

            // Determine import provider based on selected type
            string providerName = SelectedImportType switch
            {
                "1Password CSV" => "1Password",
                "Bitwarden CSV" => "Bitwarden", 
                "Chrome CSV" => "Chrome",
                "Firefox CSV" => "Firefox",
                "Safari CSV" => "Safari",
                "KeePass CSV" => "KeePass",
                "LastPass CSV" => "LastPass",
                _ => "Generic"
            };

            ImportStatus = $"Importing from {SelectedImportType}...";

            // Perform import
            using var fileStream = new FileStream(SelectedFilePath, FileMode.Open, FileAccess.Read);
            var fileName = Path.GetFileName(SelectedFilePath);
            var result = await _importService.ImportPasswordsAsync(providerName, fileStream, fileName);

            // Process results
            if (result.Success)
            {
                foreach (var item in result.ImportedItems)
                {
                    var resultItem = new ImportResultItem
                    {
                        Title = item.Title ?? "Unknown",
                        Status = "Success",
                        Message = "Imported successfully",
                        ItemType = item.Type.ToString()
                    };
                    ImportResults.Add(resultItem);
                }
                
                ImportedItemsCount = result.SuccessfulImports;
                ErrorItemsCount = result.FailedImports;
                SkippedItemsCount = result.TotalItemsProcessed - result.SuccessfulImports - result.FailedImports;
            }
            else
            {
                var resultItem = new ImportResultItem
                {
                    Title = "Import Failed",
                    Status = "Error",
                    Message = result.ErrorMessage ?? "Unknown error occurred",
                    ItemType = "N/A"
                };
                ImportResults.Add(resultItem);
                ErrorItemsCount = 1;
            }

            ImportStatus = $"Import completed. {ImportedItemsCount} imported, {SkippedItemsCount} skipped, {ErrorItemsCount} errors.";
            
            OnPropertyChanged(nameof(HasResults));
            OnPropertyChanged(nameof(HasImportCompleted));
            
            return ImportedItemsCount > 0;
        }
        catch (Exception ex)
        {
            ImportStatus = $"Import failed: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Import error: {ex}");
            return false;
        }
        finally
        {
            IsLoading = false;
            UpdateCanImport();
        }
    }

    public void ClearResults()
    {
        ImportResults.Clear();
        ImportedItemsCount = 0;
        SkippedItemsCount = 0;
        ErrorItemsCount = 0;
        ImportStatus = string.Empty;
        
        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(HasImportCompleted));
        OnPropertyChanged(nameof(HasNoImport));
    }
}

public class ImportResultItem
{
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
}