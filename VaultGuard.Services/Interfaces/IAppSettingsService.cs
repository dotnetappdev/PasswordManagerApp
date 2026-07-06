namespace VaultGuard.Services.Interfaces;

/// <summary>
/// Cross-platform application settings that persist to the same
/// <c>%LocalAppData%\VaultGuard\settings.json</c> file the WPF desktop app uses,
/// so the desktop and web builds share one set of preferences (theme, accent,
/// database location, and so on) on the same machine.
/// Values are stored as a flat string dictionary; typed helpers are provided for
/// booleans and integers.
/// </summary>
public interface IAppSettingsService
{
    /// <summary>Absolute path of the shared settings.json file.</summary>
    string SettingsFilePath { get; }

    /// <summary>Reads a raw string setting, returning <paramref name="fallback"/> when absent.</summary>
    string Get(string key, string fallback = "");

    /// <summary>Reads a boolean setting, returning <paramref name="fallback"/> when absent or unparseable.</summary>
    bool GetBool(string key, bool fallback = false);

    /// <summary>Reads an integer setting, returning <paramref name="fallback"/> when absent or unparseable.</summary>
    int GetInt(string key, int fallback = 0);

    /// <summary>Sets a value in memory (does not write to disk until <see cref="Save"/>).</summary>
    void Set(string key, string value);

    void Set(string key, bool value);
    void Set(string key, int value);

    /// <summary>True when the key exists.</summary>
    bool Contains(string key);

    /// <summary>Removes a key if present.</summary>
    void Remove(string key);

    /// <summary>Re-reads settings.json from disk, discarding unsaved in-memory changes.</summary>
    void Reload();

    /// <summary>Writes the current in-memory settings to disk. Returns false on I/O failure.</summary>
    bool Save();
}
