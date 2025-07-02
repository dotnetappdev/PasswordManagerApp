using PasswordManager.Models;

namespace PasswordManager.Imports.Interfaces;

/// <summary>
/// Interface for password import plugins that can be loaded from external DLLs
/// </summary>
public interface IPasswordImportPlugin : IPasswordImportProvider
{
    /// <summary>
    /// Plugin metadata
    /// </summary>
    PluginMetadata Metadata { get; }
    
    /// <summary>
    /// Initialize the plugin with configuration
    /// </summary>
    /// <param name="configuration">Plugin configuration from JSON</param>
    void Initialize(Dictionary<string, object>? configuration = null);
    
    /// <summary>
    /// Validate that the file can be processed by this plugin
    /// </summary>
    /// <param name="stream">File stream to validate</param>
    /// <param name="fileName">Original filename</param>
    /// <returns>True if file can be processed</returns>
    Task<bool> CanProcessFileAsync(Stream stream, string fileName);
    
    /// <summary>
    /// Get preview of items that would be imported (first 5 items)
    /// </summary>
    /// <param name="stream">File stream</param>
    /// <param name="fileName">Original filename</param>
    /// <returns>Preview of password items</returns>
    Task<IEnumerable<PasswordItem>> GetImportPreviewAsync(Stream stream, string fileName);
}

/// <summary>
/// Plugin metadata information
/// </summary>
public class PluginMetadata
{
    public required string Name { get; set; }
    public required string DisplayName { get; set; }
    public required string Description { get; set; }
    public required string Version { get; set; }
    public required string Author { get; set; }
    public string? Website { get; set; }
    public string? IconUrl { get; set; }
    public DateTime Created { get; set; }
    public DateTime LastUpdated { get; set; }
    public List<string> Tags { get; set; } = new();
}
