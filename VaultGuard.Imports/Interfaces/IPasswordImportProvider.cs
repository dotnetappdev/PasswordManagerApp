using VaultGuard.Models;

namespace VaultGuard.Imports.Interfaces;

public interface IPasswordImportProvider
{
    string ProviderName { get; }
    string DisplayName { get; }
    string Version { get; }
    string[] SupportedFileExtensions { get; }
    Task<ImportResult> ImportFromFileAsync(Stream fileStream, string fileName);
}

public class ImportResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<PasswordItem> ImportedItems { get; set; } = new();
    public List<Collection> RequiredCollections { get; set; } = new();
    public List<Category> RequiredCategories { get; set; } = new();
    public List<Tag> RequiredTags { get; set; } = new();
    public int TotalItemsProcessed { get; set; }
    public int SuccessfulImports { get; set; }
    public int FailedImports { get; set; }
    public List<string> Warnings { get; set; } = new();

    /// <summary>One line per item in the file: imported, imported with fields the file didn't have, or failed (and why).
    /// Filled in by ImportService; shown at the end of an import in every app.</summary>
    public List<ImportReportEntry> Report { get; set; } = new();
}

public enum ImportItemOutcome
{
    /// <summary>Saved with everything the item type normally has.</summary>
    Imported,
    /// <summary>Saved, but some usual fields were empty in the file, so there was nothing to carry over.</summary>
    ImportedWithGaps,
    /// <summary>Not saved.</summary>
    Failed
}

public sealed class ImportReportEntry
{
    public string Title { get; set; } = string.Empty;
    /// <summary>Friendly type name, e.g. "Login", "Credit card".</summary>
    public string ItemType { get; set; } = string.Empty;
    public ImportItemOutcome Outcome { get; set; }
    /// <summary>Usual fields the file had no value for, e.g. "password", "website".</summary>
    public List<string> MissingFields { get; set; } = new();
    /// <summary>Why the item wasn't saved (failed items only).</summary>
    public string? Error { get; set; }

    /// <summary>One sentence for the user.</summary>
    public string Summary => Outcome switch
    {
        ImportItemOutcome.Imported => "Imported",
        ImportItemOutcome.ImportedWithGaps => $"Imported. Not in the file: {string.Join(", ", MissingFields)}",
        _ => $"Not imported: {Error ?? "unknown error"}"
    };
}
