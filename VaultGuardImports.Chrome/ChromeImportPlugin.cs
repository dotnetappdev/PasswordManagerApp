using FileHelpers;
using VaultGuard.Imports.Interfaces;
using VaultGuard.Models;

namespace VaultGuardImports.Chrome;

/// <summary>
/// Chrome browser CSV import plugin
/// </summary>
public class ChromeImportPlugin : IPasswordImportPlugin
{
    public PluginMetadata Metadata { get; private set; }

    public string ProviderName => "chrome";
    public string DisplayName => "Google Chrome";
    public string Version => Metadata?.Version ?? "1.0.0";
    public string[] SupportedFileExtensions => new[] { ".csv" };

    public ChromeImportPlugin()
    {
        Metadata = new PluginMetadata
        {
            Name = "chrome",
            DisplayName = "Google Chrome",
            Description = "Import passwords from Chrome CSV export files",
            Version = "1.0.0",
            Author = "VaultGuard Team",
            Website = "https://www.google.com/chrome",
            Created = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            Tags = new List<string> { "chrome", "google", "browser", "csv" }
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

            var engine = new FileHelperEngine<ChromeCsvRecord>();
            var records = engine.ReadString(csvContent);

            var passwordItems = new List<PasswordItem>();

            foreach (var record in records)
            {
                if (string.IsNullOrWhiteSpace(record.Url)) continue;

                // Create a meaningful title from URL
                var title = GetTitleFromUrl(record.Url);

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
                        Notes = null
                    },
                    Tags = new List<Tag>()
                };

                passwordItems.Add(passwordItem);
            }

            return new ImportResult
            {
                Success = true,
                ErrorMessage = $"Successfully imported {passwordItems.Count} items from Chrome",
                ImportedItems = passwordItems,
                RequiredCollections = new List<Collection>
                {
                    new Collection
                    {
                        Name = "Chrome Import",
                        Description = "Passwords imported from Google Chrome",
                        Color = "#4285F4" // Chrome blue color
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
                ErrorMessage = $"Failed to import Chrome file: {ex.Message}",
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

            // Chrome CSV header: name,url,username,password
            return firstLine?.Contains("name") == true && 
                   firstLine?.Contains("url") == true && 
                   firstLine?.Contains("username") == true && 
                   firstLine?.Contains("password") == true;
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

            var engine = new FileHelperEngine<ChromeCsvRecord>();
            var records = engine.ReadString(csvContent).Take(5);

            var previewItems = new List<PasswordItem>();

            foreach (var record in records)
            {
                if (string.IsNullOrWhiteSpace(record.Url)) continue;

                var title = GetTitleFromUrl(record.Url);

                var passwordItem = new PasswordItem
                {
                    Title = title,
                    Type = ItemType.Login,
                    LoginItem = new LoginItem
                    {
                        Username = record.Username ?? string.Empty,
                        Password = "••••••••",
                        Website = record.Url
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
/// Chrome CSV record structure
/// </summary>
[DelimitedRecord(",")]
public class ChromeCsvRecord
{
    [FieldOrder(1)]
    public string? Name { get; set; }

    [FieldOrder(2)]
    public string? Url { get; set; }

    [FieldOrder(3)]
    public string? Username { get; set; }

    [FieldOrder(4)]
    public string? Password { get; set; }
}
