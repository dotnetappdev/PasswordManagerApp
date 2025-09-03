using System.Reflection;
using System.Text.Json;
using PasswordManager.Imports.Interfaces;

namespace PasswordManager.Imports.Services;

/// <summary>
/// Service for discovering and loading password import plugins from external DLLs
/// </summary>
public class PluginDiscoveryService
{
    private readonly string _pluginDirectory;
    private readonly List<IPasswordImportPlugin> _loadedPlugins = new();

    public PluginDiscoveryService(string? pluginDirectory = null)
    {
        _pluginDirectory = pluginDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "imports", "otherpasswordmanagers");
    }

    /// <summary>
    /// Discover and load all plugins from the plugin directory
    /// </summary>
    public async Task<IEnumerable<IPasswordImportPlugin>> DiscoverPluginsAsync()
    {
        // If the configured plugin directory exists, load plugins from it.
        if (Directory.Exists(_pluginDirectory))
        {
            var pluginDirectories = Directory.GetDirectories(_pluginDirectory);
            foreach (var pluginDir in pluginDirectories)
            {
                try
                {
                    await LoadPluginFromDirectoryAsync(pluginDir);
                }
                catch (Exception ex)
                {
                    // Log error but continue with other plugins
                    Console.WriteLine($"Failed to load plugin from {pluginDir}: {ex.Message}");
                }
            }
        }

        // Additional: search the application's base directory for any plugin.json files
        // This handles cases where build outputs place provider DLLs/plugin.json in runtime-specific
        // subfolders (e.g. win-x64) or when the app is run from a different working directory.
        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var pluginJsonFiles = Directory.GetFiles(baseDir, "plugin.json", SearchOption.AllDirectories);
            foreach (var metadataFile in pluginJsonFiles)
            {
                var pluginDir = Path.GetDirectoryName(metadataFile);
                if (string.IsNullOrEmpty(pluginDir)) continue;

                // Avoid loading the same plugin twice
                if (_loadedPlugins.Any(p => string.Equals(p.Metadata.DisplayName, Path.GetFileName(pluginDir), StringComparison.OrdinalIgnoreCase)))
                    continue;

                try
                {
                    await LoadPluginFromDirectoryAsync(pluginDir);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load plugin from discovered metadata at {pluginDir}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            // Swallow search errors but log
            Console.WriteLine($"Plugin discovery search error: {ex.Message}");
        }

        return _loadedPlugins;
    }

    private async Task LoadPluginFromDirectoryAsync(string pluginDirectory)
    {
        // Look for plugin.json file
        var metadataFile = Path.Combine(pluginDirectory, "plugin.json");
        if (!File.Exists(metadataFile))
        {
            throw new FileNotFoundException($"Plugin metadata file not found: {metadataFile}");
        }

        // Read plugin metadata
        var metadataJson = await File.ReadAllTextAsync(metadataFile);
        var pluginInfo = JsonSerializer.Deserialize<PluginInfo>(metadataJson, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        if (pluginInfo == null)
        {
            throw new InvalidOperationException("Invalid plugin metadata");
        }

        // Look for the main DLL file
        var dllPath = Path.Combine(pluginDirectory, pluginInfo.MainAssembly);
        if (!File.Exists(dllPath))
        {
            throw new FileNotFoundException($"Plugin assembly not found: {dllPath}");
        }

        // Load the assembly
        var assembly = Assembly.LoadFrom(dllPath);
        
        // Find types that implement IPasswordImportPlugin
        var pluginTypes = assembly.GetTypes()
            .Where(t => typeof(IPasswordImportPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .ToList();

        foreach (var pluginType in pluginTypes)
        {
            var plugin = Activator.CreateInstance(pluginType) as IPasswordImportPlugin;
            if (plugin != null)
            {
                // Update plugin metadata from JSON
                plugin.Metadata.Name = pluginInfo.Name;
                plugin.Metadata.DisplayName = pluginInfo.DisplayName;
                plugin.Metadata.Description = pluginInfo.Description;
                plugin.Metadata.Version = pluginInfo.Version;
                plugin.Metadata.Author = pluginInfo.Author;
                plugin.Metadata.Website = pluginInfo.Website;
                plugin.Metadata.IconUrl = pluginInfo.IconUrl;
                plugin.Metadata.Created = pluginInfo.Created;
                plugin.Metadata.LastUpdated = pluginInfo.LastUpdated;
                plugin.Metadata.Tags = pluginInfo.Tags;
                
                // Initialize plugin with configuration if provided
                var configuration = pluginInfo.Configuration?.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
                plugin.Initialize(configuration);
                
                _loadedPlugins.Add(plugin);
            }
        }
    }

    public IEnumerable<IPasswordImportPlugin> GetLoadedPlugins() => _loadedPlugins;
}

/// <summary>
/// Plugin information from plugin.json file
/// </summary>
public class PluginInfo
{
    public required string Name { get; set; }
    public required string DisplayName { get; set; }
    public required string Description { get; set; }
    public required string Version { get; set; }
    public required string Author { get; set; }
    public string? Website { get; set; }
    public string? IconUrl { get; set; }
    public required string MainAssembly { get; set; }
    public string? MainClass { get; set; }
    public DateTime Created { get; set; }
    public DateTime LastUpdated { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<string> SupportedFileExtensions { get; set; } = new();
    public Dictionary<string, JsonElement>? Configuration { get; set; }
    public PluginRequirements? Requirements { get; set; }
    public PluginHelp? Help { get; set; }
}

/// <summary>
/// Plugin requirements
/// </summary>
public class PluginRequirements
{
    public string? MinFrameworkVersion { get; set; }
    public List<string> Dependencies { get; set; } = new();
}

/// <summary>
/// Plugin help information
/// </summary>
public class PluginHelp
{
    public string? Instructions { get; set; }
    public List<string> ExportSteps { get; set; } = new();
}
