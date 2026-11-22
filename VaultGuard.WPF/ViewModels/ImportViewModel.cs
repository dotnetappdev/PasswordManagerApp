using System.Collections.ObjectModel;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using VaultGuard.Imports.Interfaces;
using VaultGuard.Models;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.WPF.ViewModels;

public class ImportViewModel : BaseViewModel
{
    private readonly IImportService _importService;
    private readonly IServiceProvider _serviceProvider;
    private string _selectedFilePath = string.Empty;
    private string _importStatus = string.Empty;
    private string _selectedImportType = string.Empty;
    private bool _canImport = false;
    private int _importedItemsCount = 0;
    private int _skippedItemsCount = 0;
    private int _errorItemsCount = 0;
    private int _importProgress = 0;

    public ImportViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _importService = serviceProvider.GetRequiredService<IImportService>();

        ImportResults = new ObservableCollection<ImportResultItem>();
        AvailableImportTypes = new List<string>
        {
            "1Password CSV",
            "1Password 1PUX",
            "Bitwarden CSV",
            "Chrome CSV",
            "Edge CSV",
            "Firefox CSV",
            "Safari CSV",
            "Dashlane CSV",
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
            OnPropertyChanged(nameof(SelectedFileFormat));
            UpdateCanImport();
        }
    }

    // Short bracketed format label (e.g. "(1PUX)" / "(CSV)") so 1PUX and CSV files are easy to tell apart.
    public string SelectedFileFormat
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_selectedFilePath))
                return string.Empty;

            var ext = Path.GetExtension(_selectedFilePath).TrimStart('.').ToUpperInvariant();
            return ext switch
            {
                "1PUX" or "1PU" => "(1PUX)",
                "" => string.Empty,
                _ => $"({ext})"
            };
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

    public int ImportProgress
    {
        get => _importProgress;
        set
        {
            SetProperty(ref _importProgress, value);
            OnPropertyChanged(nameof(ImportProgressText));
        }
    }

    public string ImportProgressText => _importProgress > 0 ? $"{_importProgress}%" : string.Empty;

    public ObservableCollection<ImportResultItem> ImportResults { get; }
    public List<string> AvailableImportTypes { get; }

    public bool HasResults => ImportResults.Count > 0;
    public bool HasImportCompleted => ImportedItemsCount > 0 || SkippedItemsCount > 0 || ErrorItemsCount > 0;
    public bool HasNoImport => !IsLoading && !HasImportCompleted && ImportResults.Count == 0;

    // Completion banner properties
    public string CompletionBannerColor => ErrorItemsCount > 0 && ImportedItemsCount == 0
        ? "#EF4444"  // red — all failed
        : ErrorItemsCount > 0
            ? "#F59E0B"  // amber — partial
            : "#10B981"; // green — all good

    public string CompletionIcon => ErrorItemsCount > 0 && ImportedItemsCount == 0
        ? ""   // Error
        : ErrorItemsCount > 0
            ? ""  // Warning
            : ""; // CheckMark

    public string CompletionTitle => ErrorItemsCount > 0 && ImportedItemsCount == 0
        ? "Import failed"
        : ErrorItemsCount > 0
            ? $"Import completed with {ErrorItemsCount} error{(ErrorItemsCount == 1 ? "" : "s")}"
            : $"Import complete — {ImportedItemsCount} item{(ImportedItemsCount == 1 ? "" : "s")} added";

    public string CompletionSubtitle
    {
        get
        {
            var parts = new List<string>();
            if (ImportedItemsCount > 0) parts.Add($"{ImportedItemsCount} imported");
            if (SkippedItemsCount > 0)  parts.Add($"{SkippedItemsCount} missing some fields");
            if (ErrorItemsCount > 0)    parts.Add($"{ErrorItemsCount} failed");
            return string.Join(" · ", parts);
        }
    }

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
            ImportProgress = 0;

            // Clear previous results
            ImportResults.Clear();
            ImportedItemsCount = 0;
            SkippedItemsCount = 0;
            ErrorItemsCount = 0;

            // Check available providers for debugging
            var availableProviders = await _importService.GetAvailableProvidersAsync();
            var providerNames = string.Join(", ", availableProviders.Select(p => p.ProviderName));

            // Determine import provider based on selected type
            string providerName = SelectedImportType switch
            {
                "1Password CSV" => "1Password",
                "1Password 1PUX" => "1Password",
                "Bitwarden CSV" => "Bitwarden",
                "Chrome CSV" => "Chrome",
                "Edge CSV" => "Edge",
                "Firefox CSV" => "Firefox",
                "Safari CSV" => "Safari",
                "Dashlane CSV" => "Dashlane",
                "KeePass CSV" => "KeePass",
                "LastPass CSV" => "LastPass",
                _ => "Generic"
            };

            ImportStatus = $"Importing from {SelectedImportType}...";

            // Resolve the current user so imported items are owned by (and visible to) them.
            // Without this the items are saved with no UserId and never appear in the user-filtered list.
            string? userId = null;
            try
            {
                var authService = _serviceProvider.GetService<VaultGuard.Services.Interfaces.IAuthService>();
                userId = authService?.CurrentUser?.Id;
                if (string.IsNullOrEmpty(userId) && authService != null)
                    userId = await authService.GetCurrentUserIdAsync();
            }
            catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Failed to resolve current user for import", ex); }

            // Perform import (report a live percentage in the status text)
            using var fileStream = new FileStream(SelectedFilePath, FileMode.Open, FileAccess.Read);
            var fileName = Path.GetFileName(SelectedFilePath);
            var progress = new System.Progress<int>(p =>
            {
                ImportProgress = p;
                ImportStatus = p >= 100 ? "Saving to vault…" : $"Processing items… {p}%";
            });
            // Get the current vault session so the import service can encrypt passwords
            string? sessionId = null;
            try
            {
                var secureStorage = _serviceProvider.GetService<ISecureStorageService>();
                if (secureStorage != null)
                    sessionId = await secureStorage.GetAsync("sessionId");
            }
            catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Failed to get vault session id", ex); }

            var result = await _importService.ImportPasswordsAsync(providerName, fileStream, fileName, userId, progress, sessionId);

            // Process results: one row per item from the import report (what carried over, what the file
            // didn't have, and what failed and why).
            LastResult = result;
            LastFileName = fileName;
            if (result.Success)
            {
                foreach (var entry in result.Report.OrderBy(r => r.Outcome == VaultGuard.Imports.Interfaces.ImportItemOutcome.Failed ? 0
                                                              : r.Outcome == VaultGuard.Imports.Interfaces.ImportItemOutcome.ImportedWithGaps ? 1 : 2)
                                                   .ThenBy(r => r.Title, StringComparer.CurrentCultureIgnoreCase))
                {
                    ImportResults.Add(new ImportResultItem
                    {
                        Title = entry.Title,
                        ItemType = entry.ItemType,
                        Status = entry.Outcome switch
                        {
                            VaultGuard.Imports.Interfaces.ImportItemOutcome.Imported => "Success",
                            VaultGuard.Imports.Interfaces.ImportItemOutcome.ImportedWithGaps => "Warning",
                            _ => "Error"
                        },
                        Message = entry.Summary
                    });
                }

                ImportedItemsCount = result.SuccessfulImports;
                ErrorItemsCount = result.FailedImports;
                // "Missing fields": imported, but the file had nothing for some usual fields.
                SkippedItemsCount = result.Report.Count(r => r.Outcome == VaultGuard.Imports.Interfaces.ImportItemOutcome.ImportedWithGaps);
            }
            else
            {
                ImportResults.Add(new ImportResultItem
                {
                    Title = "The file couldn't be imported",
                    Status = "Error",
                    Message = result.ErrorMessage ?? "No reason was given.",
                    ItemType = "File"
                });
                ErrorItemsCount = 1;
            }

            ImportProgress = 100;
            ImportStatus = $"Done: {ImportedItemsCount} imported ({SkippedItemsCount} with missing fields), {ErrorItemsCount} not imported.";
            OnPropertyChanged(nameof(HasReport));

            OnPropertyChanged(nameof(HasResults));
            OnPropertyChanged(nameof(HasImportCompleted));
            OnPropertyChanged(nameof(CompletionBannerColor));
            OnPropertyChanged(nameof(CompletionIcon));
            OnPropertyChanged(nameof(CompletionTitle));
            OnPropertyChanged(nameof(CompletionSubtitle));

            return ImportedItemsCount > 0;
        }
        catch (Exception ex)
        {
            ImportStatus = $"Import failed: {ex.Message}";
            return false;
        }
        finally
        {
            IsLoading = false;
            UpdateCanImport();
        }
    }

    /// <summary>The last import's full result, for "Save report".</summary>
    public VaultGuard.Imports.Interfaces.ImportResult? LastResult { get; private set; }
    public string? LastFileName { get; private set; }
    public bool HasReport => LastResult != null;

    /// <summary>The last import as plain text (no secrets: titles, types and field names only).</summary>
    public string? BuildReportText() => LastResult == null ? null
        : VaultGuard.Imports.Services.ImportReport.ToText(LastResult, LastFileName, SelectedImportType);

    public void ClearResults()
    {
        LastResult = null;
        OnPropertyChanged(nameof(HasReport));
        ImportResults.Clear();
        ImportedItemsCount = 0;
        SkippedItemsCount = 0;
        ErrorItemsCount = 0;
        ImportProgress = 0;
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
