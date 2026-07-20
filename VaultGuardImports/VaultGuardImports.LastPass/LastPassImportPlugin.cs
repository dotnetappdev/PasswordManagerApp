using FileHelpers;
using VaultGuard.Imports.Interfaces;
using VaultGuard.Models;

namespace VaultGuardImports.LastPass;

/// <summary>
/// LastPass CSV import plugin
/// </summary>
public class LastPassImportPlugin : IPasswordImportPlugin
{
    public PluginMetadata Metadata { get; private set; }

    public string ProviderName => "lastpass";
    public string DisplayName => "LastPass";
    public string Version => Metadata?.Version ?? "1.0.0";
    public string[] SupportedFileExtensions => new[] { ".csv" };

    public LastPassImportPlugin()
    {
        Metadata = new PluginMetadata
        {
            Name = "lastpass",
            DisplayName = "LastPass",
            Description = "Import passwords from LastPass CSV export files",
            Version = "1.0.0",
            Author = "VaultGuard Team",
            Website = "https://www.lastpass.com",
            Created = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            Tags = new List<string> { "lastpass", "csv", "password-manager" }
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

            var engine = new FileHelperEngine<LastPassCsvRecord>();
            var records = engine.ReadString(csvContent);

            var passwordItems = new List<PasswordItem>();
            var collections = new HashSet<string>();

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
                        Notes = record.Extra
                    },
                    Tags = new List<Tag>()
                };

                // Track collections (folders)
                if (!string.IsNullOrWhiteSpace(record.Grouping))
                {
                    collections.Add(record.Grouping);
                }

                passwordItems.Add(passwordItem);
            }

            // Create required collections
            var requiredCollections = collections.Select(name => new Collection
            {
                Name = name,
                Description = $"Imported from LastPass folder: {name}",
                Color = "#D32D27" // LastPass red color
            }).ToList();

            return new ImportResult
            {
                Success = true,
                ErrorMessage = $"Successfully imported {passwordItems.Count} items from LastPass",
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
                ErrorMessage = $"Failed to import LastPass file: {ex.Message}",
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

            // LastPass CSV header: url,username,password,extra,name,grouping,fav
            return firstLine?.Contains("url") == true && 
                   firstLine?.Contains("username") == true && 
                   firstLine?.Contains("password") == true &&
                   firstLine?.Contains("name") == true;
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

            var engine = new FileHelperEngine<LastPassCsvRecord>();
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
                        Notes = record.Extra != null && record.Extra.Length > 100 
                            ? record.Extra[..100] + "..." 
                            : record.Extra
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
/// LastPass CSV record structure
/// </summary>
[DelimitedRecord(",")]
public class LastPassCsvRecord
{
    [FieldOrder(1)]
    public string? Url { get; set; }

    [FieldOrder(2)]
    public string? Username { get; set; }

    [FieldOrder(3)]
    public string? Password { get; set; }

    [FieldOrder(4)]
    public string? Extra { get; set; }

    [FieldOrder(5)]
    public string? Name { get; set; }

    [FieldOrder(6)]
    public string? Grouping { get; set; }

    [FieldOrder(7)]
    public string? Fav { get; set; }
}
