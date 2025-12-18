using FileHelpers;
using PasswordManager.Imports.Interfaces;
using PasswordManager.Models;

namespace PasswordManagerImports.Safari;

/// <summary>
/// Apple Safari browser CSV import plugin
/// </summary>
public class SafariImportPlugin : IPasswordImportPlugin
{
    public PluginMetadata Metadata { get; private set; }

    public string ProviderName => "safari";
    public string DisplayName => "Apple Safari";
    public string Version => Metadata?.Version ?? "1.0.0";
    public string[] SupportedFileExtensions => new[] { ".csv" };

    public SafariImportPlugin()
    {
        Metadata = new PluginMetadata
        {
            Name = "safari",
            DisplayName = "Apple Safari",
            Description = "Import passwords from Safari CSV export files",
            Version = "1.0.0",
            Author = "PasswordManager Team",
            Website = "https://www.apple.com/safari",
            Created = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            Tags = new List<string> { "safari", "apple", "browser", "csv" }
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

            var engine = new FileHelperEngine<SafariCsvRecord>();
            var records = engine.ReadString(csvContent);

            var passwordItems = new List<PasswordItem>();

            foreach (var record in records)
            {
                if (string.IsNullOrWhiteSpace(record.Url)) continue;

                // Create a meaningful title from URL or use title if available
                var title = !string.IsNullOrWhiteSpace(record.Title) 
                    ? record.Title 
                    : GetTitleFromUrl(record.Url);

                var passwordItem = new PasswordItem
                {
                    Title = title,
                    Type = ItemType.Login,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    LoginItem = new LoginItem
                    {
                        Username = record.Username ?? string.Empty,
                        Password = record.Password ?? string.Empty,
                        Website = record.Url,
                        WebsiteUrl = record.Url,
                        Notes = record.Notes
                    },
                    Tags = new List<Tag>()
                };

                passwordItems.Add(passwordItem);
            }

            return new ImportResult
            {
                Success = true,
                ErrorMessage = $"Successfully imported {passwordItems.Count} items from Safari",
                ImportedItems = passwordItems,
                RequiredCollections = new List<Collection>
                {
                    new Collection
                    {
                        Name = "Safari Import",
                        Description = "Passwords imported from Apple Safari",
                        Color = "#006CFF" // Safari blue color
                    }
                },
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
                ErrorMessage = $"Failed to import Safari file: {ex.Message}",
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

            // Safari CSV header: Title,URL,Username,Password,Notes,OTPAuth
            return firstLine?.Contains("Title") == true && 
                   firstLine?.Contains("URL") == true && 
                   firstLine?.Contains("Username") == true && 
                   firstLine?.Contains("Password") == true;
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

            var engine = new FileHelperEngine<SafariCsvRecord>();
            var records = engine.ReadString(csvContent).Take(5);

            var previewItems = new List<PasswordItem>();

            foreach (var record in records)
            {
                if (string.IsNullOrWhiteSpace(record.Url)) continue;

                var title = !string.IsNullOrWhiteSpace(record.Title) 
                    ? record.Title 
                    : GetTitleFromUrl(record.Url);

                var passwordItem = new PasswordItem
                {
                    Title = title,
                    Type = ItemType.Login,
                    LoginItem = new LoginItem
                    {
                        Username = record.Username ?? string.Empty,
                        Password = "••••••••",
                        Website = record.Url,
                        Notes = record.Notes != null && record.Notes.Length > 100 
                            ? record.Notes[..100] + "..." 
                            : record.Notes
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

    private string GetTitleFromUrl(string url)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(url))
                return "Untitled";

            var uri = new Uri(url);
            var host = uri.Host.Replace("www.", "");
            
            // Capitalize first letter
            return char.ToUpper(host[0]) + host[1..];
        }
        catch
        {
            return url;
        }
    }
}

/// <summary>
/// Safari CSV record structure
/// </summary>
[DelimitedRecord(",")]
public class SafariCsvRecord
{
    [FieldOrder(1)]
    public string? Title { get; set; }

    [FieldOrder(2)]
    public string? Url { get; set; }

    [FieldOrder(3)]
    public string? Username { get; set; }

    [FieldOrder(4)]
    public string? Password { get; set; }

    [FieldOrder(5)]
    public string? Notes { get; set; }

    [FieldOrder(6)]
    public string? OtpAuth { get; set; }
}
