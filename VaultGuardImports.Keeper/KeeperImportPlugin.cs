using FileHelpers;
using VaultGuard.Imports.Interfaces;
using VaultGuard.Models;

namespace VaultGuardImports.Keeper;

/// <summary>
/// Keeper CSV import plugin
/// </summary>
public class KeeperImportPlugin : IPasswordImportPlugin
{
    public PluginMetadata Metadata { get; private set; }

    public string ProviderName => "keeper";
    public string DisplayName => "Keeper";
    public string Version => Metadata?.Version ?? "1.0.0";
    public string[] SupportedFileExtensions => new[] { ".csv" };

    public KeeperImportPlugin()
    {
        Metadata = new PluginMetadata
        {
            Name = "keeper",
            DisplayName = "Keeper",
            Description = "Import passwords from Keeper CSV export files",
            Version = "1.0.0",
            Author = "VaultGuard Team",
            Website = "https://keepersecurity.com",
            Created = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            Tags = new List<string> { "keeper", "csv", "password-manager" }
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

            var engine = new FileHelperEngine<KeeperCsvRecord>();
            var records = engine.ReadString(csvContent);

            var passwordItems = new List<PasswordItem>();
            var collections = new HashSet<string>();

            foreach (var record in records)
            {
                if (string.IsNullOrWhiteSpace(record.Title)) continue;

                var passwordItem = new PasswordItem
                {
                    Title = record.Title,
                    Type = ItemType.Login,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    LoginItem = new LoginItem
                    {
                        Username = record.Login ?? string.Empty,
                        Password = record.Password ?? string.Empty,
                        Website = record.WebsiteAddress,
                        WebsiteUrl = record.WebsiteAddress,
                        Notes = record.Notes
                    },
                    Tags = new List<Tag>()
                };

                // Track collections (folders)
                if (!string.IsNullOrWhiteSpace(record.Folder))
                {
                    collections.Add(record.Folder);
                }

                passwordItems.Add(passwordItem);
            }

            var requiredCollections = collections.Select(name => new Collection
            {
                Name = name,
                Description = $"Imported from Keeper folder: {name}",
                Color = "#003580" // Keeper navy color
            }).ToList();

            return new ImportResult
            {
                Success = true,
                ErrorMessage = $"Successfully imported {passwordItems.Count} items from Keeper",
                ImportedItems = passwordItems,
                RequiredCollections = requiredCollections,
                RequiredCategories = new List<Category>(),
                TotalItemsProcessed = passwordItems.Count,
                SuccessfulImports = passwordItems.Count,
                FailedImports = 0
            };
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", ex); return new ImportResult
            {
                Success = false,
                ErrorMessage = $"Failed to import Keeper file: {ex.Message}",
                ImportedItems = new List<PasswordItem>(),
                TotalItemsProcessed = 0,
                SuccessfulImports = 0,
                FailedImports = 1,
                Warnings = new List<string> { ex.Message }
            }; }
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

            // Keeper CSV header: Folder,Title,Login,Password,Website Address,Notes,Two Factor Secret
            return firstLine?.Contains("Title") == true &&
                   firstLine?.Contains("Login") == true &&
                   firstLine?.Contains("Password") == true &&
                   firstLine?.Contains("Website Address") == true;
        }
        catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", logEx); return false; }
    }

    public async Task<IEnumerable<PasswordItem>> GetImportPreviewAsync(Stream stream, string fileName)
    {
        try
        {
            stream.Position = 0;
            using var reader = new StreamReader(stream);
            var csvContent = await reader.ReadToEndAsync();

            var engine = new FileHelperEngine<KeeperCsvRecord>();
            var records = engine.ReadString(csvContent).Take(5);

            var previewItems = new List<PasswordItem>();

            foreach (var record in records)
            {
                if (string.IsNullOrWhiteSpace(record.Title)) continue;

                var passwordItem = new PasswordItem
                {
                    Title = record.Title,
                    Type = ItemType.Login,
                    LoginItem = new LoginItem
                    {
                        Username = record.Login ?? string.Empty,
                        Password = "••••••••",
                        Website = record.WebsiteAddress,
                        Notes = record.Notes != null && record.Notes.Length > 100
                            ? record.Notes[..100] + "..."
                            : record.Notes
                    }
                };

                previewItems.Add(passwordItem);
            }

            return previewItems;
        }
        catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", logEx); return new List<PasswordItem>(); }
    }
}

/// <summary>
/// Keeper CSV record structure: Folder,Title,Login,Password,Website Address,Notes,Two Factor Secret
/// </summary>
[DelimitedRecord(",")]
[IgnoreFirst(1)]
public class KeeperCsvRecord
{
    [FieldOrder(1)]
    public string? Folder { get; set; }

    [FieldOrder(2)]
    public string? Title { get; set; }

    [FieldOrder(3)]
    public string? Login { get; set; }

    [FieldOrder(4)]
    public string? Password { get; set; }

    [FieldOrder(5)]
    public string? WebsiteAddress { get; set; }

    [FieldOrder(6)]
    public string? Notes { get; set; }

    [FieldOrder(7)]
    public string? TwoFactorSecret { get; set; }
}
