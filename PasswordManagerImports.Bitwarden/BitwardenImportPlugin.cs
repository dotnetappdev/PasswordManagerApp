using Microsoft.Extensions.Configuration;
using FileHelpers;
using PasswordManager.Imports.Interfaces;
using PasswordManager.Models;

namespace PasswordManagerImports.Bitwarden;

/// <summary>
/// Bitwarden CSV import plugin
/// </summary>
public class BitwardenImportPlugin : IPasswordImportPlugin
{
    public PluginMetadata Metadata { get; private set; }

    public string ProviderName => "bitwarden";
    public string DisplayName => "Bitwarden";
    public string[] SupportedFileExtensions => new[] { ".csv" };

    public BitwardenImportPlugin()
    {
        // Metadata will be loaded from plugin.json by the PluginDiscoveryService
        Metadata = new PluginMetadata
        {
            Name = "bitwarden",
            DisplayName = "Bitwarden",
            Description = "Import passwords from Bitwarden CSV export files",
            Version = "1.0.0",
            Author = "PasswordManager Team",
            Website = "https://bitwarden.com",
            Created = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            Tags = new List<string> { "bitwarden", "csv", "password-manager" }
        };
    }

    public void Initialize(Dictionary<string, object>? configuration = null)
    {
        // Plugin initialization logic if needed
        // Configuration could contain custom settings from plugin.json
        if (configuration != null)
        {
            // Update metadata from JSON configuration if provided
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

    public async Task<ImportResult> ImportPasswordsAsync(Stream stream, string fileName)
    {
        try
        {
            using var reader = new StreamReader(stream);
            var csvContent = await reader.ReadToEndAsync();
            
            var engine = new FileHelperEngine<BitwardenCsvRecord>();
            var records = engine.ReadString(csvContent);
            
            var passwordItems = new List<PasswordItem>();
            var collections = new HashSet<string>();
            var categories = new HashSet<string>();
            
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
                        Password = record.Password ?? string.Empty, // Temporary property for import
                        Website = record.Url,
                        Notes = record.Notes // Temporary property for import
                    },
                    Tags = new List<Tag>()
                };
                
                // Track collections and categories for creation
                if (!string.IsNullOrWhiteSpace(record.Folder))
                {
                    collections.Add(record.Folder);
                }
                if (!string.IsNullOrWhiteSpace(record.Type))
                {
                    categories.Add(record.Type);
                }
                
                passwordItems.Add(passwordItem);
            }
            
            // Create required collections and categories
            var requiredCollections = collections.Select(name => new Collection 
            { 
                Name = name,
                Description = $"Imported from Bitwarden folder: {name}",
                Color = "#ffffff"
            }).ToList();
            
            var requiredCategories = categories.Select(name => new Category 
            { 
                Name = name,
                CollectionId = 1 // Will be updated during import
            }).ToList();
            
            return new ImportResult
            {
                Success = true,
                Message = $"Successfully imported {passwordItems.Count} items from Bitwarden",
                ImportedItems = passwordItems,
                RequiredCollections = requiredCollections,
                RequiredCategories = requiredCategories,
                TotalItems = passwordItems.Count,
                ProcessedItems = passwordItems.Count,
                SuccessfulImports = passwordItems.Count
            };
        }
        catch (Exception ex)
        {
            return new ImportResult
            {
                Success = false,
                ErrorMessage = $"Failed to import Bitwarden file: {ex.Message}",
                Errors = new List<string> { ex.Message }
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
            
            // Check if it looks like a Bitwarden CSV export
            return firstLine?.Contains("folder,favorite,type,name,notes,fields,reprompt,login_uri,login_username,login_password,login_totp") == true;
        }
        catch
        {
            return false;
        }
    }
                if (configuration.TryGetValue("displayName", out var displayName))
                    Metadata.DisplayName = displayName != null ? displayName.ToString() : Metadata.DisplayName;
                if (configuration.TryGetValue("description", out var description))
                    Metadata.Description = description != null ? description.ToString() : Metadata.Description;
                if (configuration.TryGetValue("version", out var version))
                    Metadata.Version = version != null ? version.ToString() : Metadata.Version;
                if (configuration.TryGetValue("author", out var author))
                    Metadata.Author = author != null ? author.ToString() : Metadata.Author;
            stream.Position = 0;
            
            var engine = new FileHelperEngine<BitwardenCsvRecord>();
            var records = engine.ReadString(csvContent).Take(5); // Preview first 5 items
            
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
                        Password = "••••••••", // Hide password in preview (temporary property)
                        Website = record.Url,
                        Notes = record.Notes // Temporary property for import
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
/// Bitwarden CSV record structure
/// </summary>
[DelimitedRecord(",")]
public class BitwardenCsvRecord
{
    [FieldOrder(1)]
    public string? Folder { get; set; }
    
    [FieldOrder(2)]
    public string? Favorite { get; set; }
    
    [FieldOrder(3)]
    public string? Type { get; set; }
    
    [FieldOrder(4)]
    public string? Name { get; set; }
    
    [FieldOrder(5)]
    public string? Notes { get; set; }
    
    [FieldOrder(6)]
    public string? Fields { get; set; }
    
    [FieldOrder(7)]
    public string? Reprompt { get; set; }
    
    [FieldOrder(8)]
    public string? Url { get; set; }
    
    [FieldOrder(9)]
    public string? Username { get; set; }
    
    [FieldOrder(10)]
    public string? Password { get; set; }
    
    [FieldOrder(11)]
    public string? Totp { get; set; }
}
