using PasswordManager.Imports.Interfaces;
using PasswordManager.Models;
using PasswordManagerImports.OnePassword.Providers;

namespace PasswordManagerImports.OnePassword;

/// <summary>
/// 1Password import plugin that wraps the OnePasswordImportProvider
/// </summary>
public class OnePasswordImportPlugin : IPasswordImportPlugin
{
    private readonly OnePasswordImportProvider _provider;

    public PluginMetadata Metadata { get; private set; }

    public string ProviderName => "1Password";
    public string DisplayName => "1Password";
    public string Version => Metadata?.Version ?? "1.0.0";
    public string[] SupportedFileExtensions => new[] { ".csv", ".1pux" };

    public OnePasswordImportPlugin()
    {
        _provider = new OnePasswordImportProvider();
        
        // Metadata will be loaded from plugin.json by the PluginDiscoveryService
        Metadata = new PluginMetadata
        {
            Name = "1password",
            DisplayName = "1Password",
            Description = "Import passwords from 1Password CSV or 1PUX export files",
            Version = "1.0.0",
            Author = "PasswordManager Team",
            Website = "https://1password.com",
            Created = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            Tags = new List<string> { "1password", "csv", "1pux", "password-manager" }
        };
    }

    public void Initialize(Dictionary<string, object>? configuration = null)
    {
        // Plugin initialization logic if needed
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

    public async Task<ImportResult> ImportFromFileAsync(Stream stream, string fileName)
    {
        return await _provider.ImportFromFileAsync(stream, fileName);
    }

    public async Task<bool> CanProcessFileAsync(Stream stream, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        
        if (!SupportedFileExtensions.Contains(extension))
            return false;

        try
        {
            stream.Position = 0;
            
            if (extension == ".1pux")
            {
                // Check if it's a valid ZIP file (1PUX format)
                using var archive = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read, true);
                var hasExportData = archive.Entries.Any(e => e.Name == "export.data");
                stream.Position = 0;
                return hasExportData;
            }
            else if (extension == ".csv")
            {
                // Check if it looks like a 1Password CSV export
                using var reader = new StreamReader(stream, leaveOpen: true);
                var firstLine = await reader.ReadLineAsync();
                stream.Position = 0;

                // 1Password CSV typically starts with: Title,Url,Username,Password...
                return firstLine?.Contains("Title") == true && 
                       firstLine?.Contains("Username") == true && 
                       firstLine?.Contains("Password") == true;
            }
            
            return false;
        }
        catch
        {
            stream.Position = 0;
            return false;
        }
    }

    public async Task<IEnumerable<PasswordItem>> GetImportPreviewAsync(Stream stream, string fileName)
    {
        try
        {
            stream.Position = 0;
            
            // Import full result and return first 5 items for preview
            var result = await _provider.ImportFromFileAsync(stream, fileName);
            
            if (result.Success && result.ImportedItems.Any())
            {
                // Return first 5 items with passwords masked
                return result.ImportedItems.Take(5).Select(item =>
                {
                    var previewItem = new PasswordItem
                    {
                        Title = item.Title,
                        Type = item.Type,
                        CreatedAt = item.CreatedAt,
                        LastModified = item.LastModified,
                        Tags = item.Tags
                    };

                    if (item.LoginItem != null)
                    {
                        previewItem.LoginItem = new LoginItem
                        {
                            Username = item.LoginItem.Username,
                            Password = "••••••••", // Hide password in preview
                            Website = item.LoginItem.Website,
                            WebsiteUrl = item.LoginItem.WebsiteUrl,
                            Email = item.LoginItem.Email,
                            Notes = item.LoginItem.Notes != null && item.LoginItem.Notes.Length > 100 
                                ? item.LoginItem.Notes.Substring(0, 100) + "..." 
                                : item.LoginItem.Notes
                        };
                    }

                    return previewItem;
                });
            }
            
            return new List<PasswordItem>();
        }
        catch
        {
            return new List<PasswordItem>();
        }
    }
}
