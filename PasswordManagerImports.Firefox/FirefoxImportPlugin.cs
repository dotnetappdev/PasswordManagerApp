using FileHelpers;
using PasswordManager.Imports.Interfaces;
using PasswordManager.Models;

namespace PasswordManagerImports.Firefox;

/// <summary>
/// Mozilla Firefox browser CSV import plugin
/// </summary>
public class FirefoxImportPlugin : IPasswordImportPlugin
{
    public PluginMetadata Metadata { get; private set; }

    public string ProviderName => "firefox";
    public string DisplayName => "Mozilla Firefox";
    public string Version => Metadata?.Version ?? "1.0.0";
    public string[] SupportedFileExtensions => new[] { ".csv" };

    public FirefoxImportPlugin()
    {
        Metadata = new PluginMetadata
        {
            Name = "firefox",
            DisplayName = "Mozilla Firefox",
            Description = "Import passwords from Firefox CSV export files",
            Version = "1.0.0",
            Author = "PasswordManager Team",
            Website = "https://www.mozilla.org/firefox",
            Created = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            Tags = new List<string> { "firefox", "mozilla", "browser", "csv" }
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

            var engine = new FileHelperEngine<FirefoxCsvRecord>();
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
                    CreatedAt = ParseFirefoxDate(record.TimeCreated),
                    LastModified = ParseFirefoxDate(record.TimePasswordChanged),
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
                ErrorMessage = $"Successfully imported {passwordItems.Count} items from Firefox",
                ImportedItems = passwordItems,
                RequiredCollections = new List<Collection>
                {
                    new Collection
                    {
                        Name = "Firefox Import",
                        Description = "Passwords imported from Mozilla Firefox",
                        Color = "#FF6611" // Firefox orange color
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
                ErrorMessage = $"Failed to import Firefox file: {ex.Message}",
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

            // Firefox CSV header: url,username,password,httpRealm,formActionOrigin,guid,timeCreated,timeLastUsed,timePasswordChanged
            return firstLine?.Contains("url") == true && 
                   firstLine?.Contains("username") == true && 
                   firstLine?.Contains("password") == true &&
                   (firstLine?.Contains("timeCreated") == true || firstLine?.Contains("httpRealm") == true);
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

            var engine = new FileHelperEngine<FirefoxCsvRecord>();
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
            return char.ToUpper(host[0]) + host.Substring(1);
        }
        catch
        {
            return url;
        }
    }

    private DateTime ParseFirefoxDate(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            return DateTime.UtcNow;

        // Firefox exports timestamps in milliseconds since epoch
        if (long.TryParse(dateString, out var timestamp))
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime;
        }

        return DateTime.UtcNow;
    }
}

/// <summary>
/// Firefox CSV record structure
/// </summary>
[DelimitedRecord(",")]
public class FirefoxCsvRecord
{
    [FieldOrder(1)]
    public string? Url { get; set; }

    [FieldOrder(2)]
    public string? Username { get; set; }

    [FieldOrder(3)]
    public string? Password { get; set; }

    [FieldOrder(4)]
    public string? HttpRealm { get; set; }

    [FieldOrder(5)]
    public string? FormActionOrigin { get; set; }

    [FieldOrder(6)]
    public string? Guid { get; set; }

    [FieldOrder(7)]
    public string? TimeCreated { get; set; }

    [FieldOrder(8)]
    public string? TimeLastUsed { get; set; }

    [FieldOrder(9)]
    public string? TimePasswordChanged { get; set; }
}
