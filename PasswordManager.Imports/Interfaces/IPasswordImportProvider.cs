using PasswordManager.Models;

namespace PasswordManager.Imports.Interfaces;

public interface IPasswordImportProvider
{
    string ProviderName { get; }
    string DisplayName { get; }
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
}
