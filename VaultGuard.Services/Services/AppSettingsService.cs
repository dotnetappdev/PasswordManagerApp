using System.Text.Json;
using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Logging;

namespace VaultGuard.Services.Services;

/// <summary>
/// File-backed <see cref="IAppSettingsService"/>. Persists to the same
/// <c>%LocalAppData%\VaultGuard\settings.json</c> the WPF app writes, so preferences
/// are shared between the desktop and web builds on one machine.
/// The on-disk format is a flat <c>Dictionary&lt;string,string&gt;</c> — identical to the
/// WPF <c>SettingsViewModel</c> — so neither app clobbers the other's schema.
/// </summary>
public sealed class AppSettingsService : IAppSettingsService
{
    private readonly object _sync = new();
    private Dictionary<string, string> _values;

    public AppSettingsService()
    {
        _values = LoadFromDisk(SettingsFilePath);
    }

    public string SettingsFilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "VaultGuard", "settings.json");

    public string Get(string key, string fallback = "")
    {
        lock (_sync)
            return _values.TryGetValue(key, out var v) && v is not null ? v : fallback;
    }

    public bool GetBool(string key, bool fallback = false)
        => bool.TryParse(Get(key), out var v) ? v : fallback;

    public int GetInt(string key, int fallback = 0)
        => int.TryParse(Get(key), out var v) ? v : fallback;

    public void Set(string key, string value)
    {
        lock (_sync) _values[key] = value ?? string.Empty;
    }

    public void Set(string key, bool value) => Set(key, value.ToString());

    public void Set(string key, int value) => Set(key, value.ToString());

    public bool Contains(string key)
    {
        lock (_sync) return _values.ContainsKey(key);
    }

    public void Remove(string key)
    {
        lock (_sync) _values.Remove(key);
    }

    public void Reload()
    {
        lock (_sync) _values = LoadFromDisk(SettingsFilePath);
    }

    public bool Save()
    {
        try
        {
            lock (_sync)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath)!);
                // Merge onto whatever is already on disk so we never drop keys another
                // client (e.g. the WPF app) may have written since we last loaded.
                var onDisk = LoadFromDisk(SettingsFilePath);
                foreach (var kv in _values) onDisk[kv.Key] = kv.Value;
                _values = onDisk;
                File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(_values));
            }
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.Error("Failed to save app settings", ex);
            return false;
        }
    }

    private static Dictionary<string, string> LoadFromDisk(string path)
    {
        try
        {
            if (File.Exists(path))
                return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))
                       ?? new Dictionary<string, string>();
        }
        catch (Exception ex)
        {
            AppLogger.Error("Failed to load app settings", ex);
        }
        return new Dictionary<string, string>();
    }
}
