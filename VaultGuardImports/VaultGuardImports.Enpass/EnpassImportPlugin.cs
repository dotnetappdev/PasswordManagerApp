using System.Text.Json;
using System.Text.Json.Serialization;
using VaultGuard.Imports.Interfaces;
using VaultGuard.Models;

namespace VaultGuardImports.Enpass;

/// <summary>
/// Enpass JSON import plugin
/// </summary>
public class EnpassImportPlugin : IPasswordImportPlugin
{
    public PluginMetadata Metadata { get; private set; }

    public string ProviderName => "enpass";
    public string DisplayName => "Enpass";
    public string Version => Metadata?.Version ?? "1.0.0";
    public string[] SupportedFileExtensions => new[] { ".json" };

    public EnpassImportPlugin()
    {
        Metadata = new PluginMetadata
        {
            Name = "enpass",
            DisplayName = "Enpass",
            Description = "Import passwords from Enpass JSON export files",
            Version = "1.0.0",
            Author = "VaultGuard Team",
            Website = "https://enpass.io",
            Created = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            Tags = new List<string> { "enpass", "json", "password-manager" }
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
            var jsonContent = await reader.ReadToEndAsync();

            var export = JsonSerializer.Deserialize<EnpassExport>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (export?.Items == null)
            {
                return new ImportResult
                {
                    Success = false,
                    ErrorMessage = "No items found in Enpass export file",
                    ImportedItems = new List<PasswordItem>(),
                    TotalItemsProcessed = 0,
                    SuccessfulImports = 0,
                    FailedImports = 0
                };
            }

            var passwordItems = new List<PasswordItem>();
            var collections = new HashSet<string>();

            foreach (var item in export.Items)
            {
                if (string.IsNullOrWhiteSpace(item.Title)) continue;

                // Extract login fields
                var usernameField = item.Fields?.FirstOrDefault(f =>
                    f.Type?.Equals("username", StringComparison.OrdinalIgnoreCase) == true ||
                    f.Label?.Equals("username", StringComparison.OrdinalIgnoreCase) == true ||
                    f.Label?.Equals("email", StringComparison.OrdinalIgnoreCase) == true);

                var passwordField = item.Fields?.FirstOrDefault(f =>
                    f.Type?.Equals("password", StringComparison.OrdinalIgnoreCase) == true ||
                    f.Label?.Equals("password", StringComparison.OrdinalIgnoreCase) == true);

                var urlField = item.Fields?.FirstOrDefault(f =>
                    f.Type?.Equals("url", StringComparison.OrdinalIgnoreCase) == true ||
                    f.Label?.Equals("url", StringComparison.OrdinalIgnoreCase) == true ||
                    f.Label?.Equals("website", StringComparison.OrdinalIgnoreCase) == true);

                var passwordItem = new PasswordItem
                {
                    Title = item.Title,
                    Type = ItemType.Login,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    LoginItem = new LoginItem
                    {
                        Username = usernameField?.Value ?? item.Subtitle ?? string.Empty,
                        Password = passwordField?.Value ?? string.Empty,
                        Website = urlField?.Value,
                        WebsiteUrl = urlField?.Value,
                        Notes = item.Note
                    },
                    Tags = new List<Tag>()
                };

                // Track collections from tags
                if (item.Tags != null)
                {
                    foreach (var tag in item.Tags)
                    {
                        if (!string.IsNullOrWhiteSpace(tag))
                            collections.Add(tag);
                    }
                }

                passwordItems.Add(passwordItem);
            }

            var requiredCollections = collections.Select(name => new Collection
            {
                Name = name,
                Description = $"Imported from Enpass tag: {name}",
                Color = "#3478C6" // Enpass blue color
            }).ToList();

            return new ImportResult
            {
                Success = true,
                ErrorMessage = $"Successfully imported {passwordItems.Count} items from Enpass",
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
                ErrorMessage = $"Failed to import Enpass file: {ex.Message}",
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
            var content = await reader.ReadToEndAsync();
            stream.Position = 0;

            // Enpass JSON export contains an "items" array at the top level
            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.TryGetProperty("items", out _);
        }
        catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", logEx); return false; }
    }

    public async Task<IEnumerable<PasswordItem>> GetImportPreviewAsync(Stream stream, string fileName)
    {
        try
        {
            stream.Position = 0;
            using var reader = new StreamReader(stream);
            var jsonContent = await reader.ReadToEndAsync();

            var export = JsonSerializer.Deserialize<EnpassExport>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (export?.Items == null) return new List<PasswordItem>();

            var previewItems = new List<PasswordItem>();

            foreach (var item in export.Items.Take(5))
            {
                if (string.IsNullOrWhiteSpace(item.Title)) continue;

                var usernameField = item.Fields?.FirstOrDefault(f =>
                    f.Type?.Equals("username", StringComparison.OrdinalIgnoreCase) == true ||
                    f.Label?.Equals("username", StringComparison.OrdinalIgnoreCase) == true ||
                    f.Label?.Equals("email", StringComparison.OrdinalIgnoreCase) == true);

                var urlField = item.Fields?.FirstOrDefault(f =>
                    f.Type?.Equals("url", StringComparison.OrdinalIgnoreCase) == true ||
                    f.Label?.Equals("url", StringComparison.OrdinalIgnoreCase) == true ||
                    f.Label?.Equals("website", StringComparison.OrdinalIgnoreCase) == true);

                var passwordItem = new PasswordItem
                {
                    Title = item.Title,
                    Type = ItemType.Login,
                    LoginItem = new LoginItem
                    {
                        Username = usernameField?.Value ?? item.Subtitle ?? string.Empty,
                        Password = "••••••••",
                        Website = urlField?.Value,
                        Notes = item.Note != null && item.Note.Length > 100
                            ? item.Note[..100] + "..."
                            : item.Note
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
/// Enpass JSON export root structure
/// </summary>
public class EnpassExport
{
    [JsonPropertyName("items")]
    public List<EnpassItem>? Items { get; set; }
}

/// <summary>
/// Enpass item structure
/// </summary>
public class EnpassItem
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("subtitle")]
    public string? Subtitle { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("fields")]
    public List<EnpassField>? Fields { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }
}

/// <summary>
/// Enpass field structure
/// </summary>
public class EnpassField
{
    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}
