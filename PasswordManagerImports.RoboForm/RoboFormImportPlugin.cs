using FileHelpers;
using PasswordManager.Imports.Interfaces;
using PasswordManager.Models;

namespace PasswordManagerImports.RoboForm;

/// <summary>
/// RoboForm CSV import plugin
/// </summary>
public class RoboFormImportPlugin : IPasswordImportPlugin
{
    public PluginMetadata Metadata { get; private set; }

    public string ProviderName => "roboform";
    public string DisplayName => "RoboForm";
    public string Version => Metadata?.Version ?? "1.0.0";
    public string[] SupportedFileExtensions => new[] { ".csv" };

    public RoboFormImportPlugin()
    {
        Metadata = new PluginMetadata
        {
            Name = "roboform",
            DisplayName = "RoboForm",
            Description = "Import passwords from RoboForm CSV export files",
            Version = "1.0.0",
            Author = "PasswordManager Team",
            Website = "https://roboform.com",
            Created = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            Tags = new List<string> { "roboform", "csv", "password-manager" }
        };
    }

    public void Initialize(Dictionary<string, object>? configuration = null)
    {
        if (configuration != null)
        {
            if (configuration.TryGetValue("displayName", out var displayName))
                Metadata.DisplayName = displayName.ToString() ?? Metadata.DisplayName;
            if (configuration.TryGetValue("description", out var description))
                Metadata.Description = description.ToString() ?? Metadata.Description;
            if (configuration.TryGetValue("version", out var version))
                Metadata.Version = version.ToString() ?? Metadata.Version;
            if (configuration.TryGetValue("author", out var author))
                Metadata.Author = author.ToString() ?? Metadata.Author;
            if (configuration.TryGetValue("website", out var website))
                Metadata.Website = website.ToString();
        }
    }

    public async Task<ImportResult> ImportFromFileAsync(Stream stream, string fileName)
    {
        try
        {
            using var reader = new StreamReader(stream);
            var csvContent = await reader.ReadToEndAsync();

            var engine = new FileHelperEngine<RoboFormCsvRecord>();
            var records = engine.ReadString(csvContent);

            var passwordItems = new List<PasswordItem>();

            foreach (var record in records)
            {
                if (string.IsNullOrWhiteSpace(record.Name)) continue;

                var passwordItem = new PasswordItem
                {
                    Title = record.Name,
                    Type = ItemType.Login,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    LoginItem = new LoginItem
                    {
                        Username = record.Username ?? string.Empty,
                        Password = record.Password ?? string.Empty,
                        Website = record.Url,
                        WebsiteUrl = record.Url,
                        Notes = record.Note
                    },
                    Tags = new List<Tag>()
                };

                passwordItems.Add(passwordItem);
            }

            return new ImportResult
            {
                Success = true,
                ErrorMessage = $"Successfully imported {passwordItems.Count} items from RoboForm",
                ImportedItems = passwordItems,
                RequiredCollections = new List<Collection>(),
                RequiredCategories = new List<Category>(),
                TotalItemsProcessed = passwordItems.Count,
                SuccessfulImports = passwordItems.Count,
                FailedImports = 0
            };
        }
        catch (Exception ex)
        {
            return new ImportResult
            {
                Success = false,
                ErrorMessage = $"Failed to import RoboForm file: {ex.Message}",
                ImportedItems = new List<PasswordItem>(),
                TotalItemsProcessed = 0,
                SuccessfulImports = 0,
                FailedImports = 1,
                Warnings = new List<string> { ex.Message }
            };
        }
    }

    public async Task<bool> CanProcessFileAsync(Stream stream, string fileName)
    {
        if (!SupportedFileExtensions.Contains(Path.GetExtension(fileName).ToLowerInvariant()))
            return false;

        try
        {
            stream.Position = 0;
            using var reader = new StreamReader(stream);
            var firstLine = await reader.ReadLineAsync();
            stream.Position = 0;

            // RoboForm CSV header: Name,Url,Username,Password,Note
            return firstLine?.Contains("Name") == true &&
                   firstLine?.Contains("Url") == true &&
                   firstLine?.Contains("Username") == true &&
                   firstLine?.Contains("Password") == true &&
                   firstLine?.Contains("Note") == true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<PasswordItem>> GetImportPreviewAsync(Stream stream, string fileName)
    {
        try
        {
            stream.Position = 0;
            using var reader = new StreamReader(stream);
            var csvContent = await reader.ReadToEndAsync();

            var engine = new FileHelperEngine<RoboFormCsvRecord>();
            var records = engine.ReadString(csvContent).Take(5);

            var previewItems = new List<PasswordItem>();

            foreach (var record in records)
            {
                if (string.IsNullOrWhiteSpace(record.Name)) continue;

                var passwordItem = new PasswordItem
                {
                    Title = record.Name,
                    Type = ItemType.Login,
                    LoginItem = new LoginItem
                    {
                        Username = record.Username ?? string.Empty,
                        Password = "••••••••",
                        Website = record.Url,
                        Notes = record.Note != null && record.Note.Length > 100
                            ? record.Note[..100] + "..."
                            : record.Note
                    }
                };

                previewItems.Add(passwordItem);
            }

            return previewItems;
        }
        catch
        {
            return new List<PasswordItem>();
        }
    }
}

/// <summary>
/// RoboForm CSV record structure: Name,Url,Username,Password,Note
/// </summary>
[DelimitedRecord(",")]
[IgnoreFirst(1)]
public class RoboFormCsvRecord
{
    [FieldOrder(1)]
    public string? Name { get; set; }

    [FieldOrder(2)]
    public string? Url { get; set; }

    [FieldOrder(3)]
    public string? Username { get; set; }

    [FieldOrder(4)]
    public string? Password { get; set; }

    [FieldOrder(5)]
    public string? Note { get; set; }
}
