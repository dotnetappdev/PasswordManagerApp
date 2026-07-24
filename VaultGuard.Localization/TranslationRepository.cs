using System.Text;
using System.Text.Json;

namespace VaultGuard.Localization;

/// <summary>
/// Mutable, file-backed translation store - the editable counterpart to the read-only embedded
/// .po catalogs in Resources/ (see <see cref="PoCatalogStore"/>). Both WPF's LocalizationManager
/// and Blazor's <see cref="PoStringLocalizer"/> read live through this class instead of the
/// embedded-only store, so edits made in the Translations management page (WPF:
/// Views/TranslationsPage.xaml, Blazor: Components/Pages/TranslationsAdmin.razor) take effect
/// immediately - no rebuild, no restart.
///
/// Storage: <c>%LocalAppData%\VaultGuard\Localization\</c> - the same machine-shared directory
/// convention as <c>IAppSettingsService</c>'s settings.json (VaultGuard.Services). On first use,
/// the built-in embedded catalogs are copied out here so they're immediately editable; from then on
/// this directory is the source of truth and the embedded resources are only read again if a
/// catalog file here goes missing.
///
/// Keys are tracked independently of any single language's catalog (<c>keys.json</c>), so a newly
/// added key shows up as an empty row to fill in for every language, rather than only appearing
/// once the first language happens to translate it.
/// </summary>
public static class TranslationRepository
{
    private static readonly object _sync = new();
    private static bool _seeded;

    // cultureCode -> (msgid -> msgstr)
    private static readonly Dictionary<string, Dictionary<string, string>> _catalogs =
        new(StringComparer.OrdinalIgnoreCase);

    private static List<LanguageInfo> _customLanguages = new();
    private static SortedSet<string> _knownKeys = new(StringComparer.Ordinal);

    public static string LocalizationDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "VaultGuard", "Localization");

    private static string CatalogPath(string cultureCode) => Path.Combine(LocalizationDirectory, $"messages.{cultureCode}.po");
    private static string LanguagesRegistryPath => Path.Combine(LocalizationDirectory, "languages.json");
    private static string KeysRegistryPath => Path.Combine(LocalizationDirectory, "keys.json");

    // ── Reading (used by PoStringLocalizer / WPF's LocalizationManager) ────────────────────

    public static string? GetTranslation(string cultureCode, string msgid)
    {
        if (string.IsNullOrEmpty(msgid) || string.IsNullOrEmpty(cultureCode)) return null;
        if (string.Equals(cultureCode, "en", StringComparison.OrdinalIgnoreCase)) return null;

        EnsureSeeded();
        lock (_sync)
        {
            return _catalogs.TryGetValue(cultureCode, out var catalog)
                && catalog.TryGetValue(msgid, out var value)
                && !string.IsNullOrEmpty(value)
                ? value
                : null;
        }
    }

    // ── CRUD surface for the Translations management page ──────────────────────────────────

    /// <summary>English plus every language that has an editable catalog - this is what
    /// <see cref="SupportedLanguages.All"/> delegates to, so both language pickers and the CRUD
    /// page always agree.</summary>
    public static IReadOnlyList<LanguageInfo> GetLanguages()
    {
        EnsureSeeded();
        lock (_sync)
        {
            var all = new List<LanguageInfo> { SupportedLanguages.English };
            all.AddRange(_customLanguages);
            return all;
        }
    }

    /// <summary>Every known translation key, one row per key in the CRUD grid regardless of which
    /// languages have actually translated it yet.</summary>
    public static IReadOnlyList<string> GetAllKeys()
    {
        EnsureSeeded();
        lock (_sync)
            return _knownKeys.ToList();
    }

    public static IReadOnlyDictionary<string, string> GetCatalog(string cultureCode)
    {
        EnsureSeeded();
        lock (_sync)
        {
            return _catalogs.TryGetValue(cultureCode, out var catalog)
                ? new Dictionary<string, string>(catalog, StringComparer.Ordinal)
                : new Dictionary<string, string>();
        }
    }

    /// <summary>Registers a new translatable key with no value in any language yet - it appears as
    /// an empty row in the CRUD grid, ready to fill in. No-op if the key already exists.</summary>
    public static void AddKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        EnsureSeeded();
        lock (_sync)
        {
            if (_knownKeys.Add(key))
                SaveKeysRegistry();
        }
    }

    /// <summary>Adds/updates a single key's translation for one language. An empty/whitespace
    /// value clears the cell (the key stays known - see <see cref="AddKey"/> - it just has no
    /// translation for this language yet). Implicitly registers the key if it wasn't already
    /// known, so pasting/typing a brand-new key directly into a language's value also works.</summary>
    public static void SetTranslation(string cultureCode, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        if (string.Equals(cultureCode, "en", StringComparison.OrdinalIgnoreCase)) return;

        EnsureSeeded();
        lock (_sync)
        {
            if (_knownKeys.Add(key))
                SaveKeysRegistry();

            if (!_catalogs.TryGetValue(cultureCode, out var catalog))
                _catalogs[cultureCode] = catalog = new Dictionary<string, string>(StringComparer.Ordinal);

            if (string.IsNullOrEmpty(value)) catalog.Remove(key);
            else catalog[key] = value;

            SaveCatalogInternal(cultureCode, catalog);
        }
    }

    /// <summary>Removes a key entirely - from the known-keys registry and every language's
    /// catalog. This deletes the whole row from the CRUD grid (as opposed to clearing one
    /// language's cell, which is <see cref="SetTranslation"/> with an empty value).</summary>
    public static void DeleteKey(string key)
    {
        EnsureSeeded();
        lock (_sync)
        {
            if (_knownKeys.Remove(key))
                SaveKeysRegistry();

            foreach (var code in _catalogs.Keys.ToList())
            {
                if (_catalogs[code].Remove(key))
                    SaveCatalogInternal(code, _catalogs[code]);
            }
        }
    }

    public static void AddLanguage(string code, string nativeName, string englishName)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Language code is required.", nameof(code));
        code = code.Trim().ToLowerInvariant();
        if (string.Equals(code, "en", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("English is the built-in source language and can't be added again.");

        EnsureSeeded();
        lock (_sync)
        {
            if (_customLanguages.Any(l => string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Language '{code}' already exists.");

            _customLanguages.Add(new LanguageInfo(code, nativeName, englishName));
            if (!_catalogs.ContainsKey(code))
                _catalogs[code] = new Dictionary<string, string>(StringComparer.Ordinal);

            SaveLanguageRegistry();
            SaveCatalogInternal(code, _catalogs[code]);
        }
    }

    /// <summary>Deletes a language entirely - its registry entry and its .po file. Known keys and
    /// other languages' translations are untouched. English can't be removed (it isn't a catalog
    /// entry to begin with - see <see cref="SupportedLanguages"/>).</summary>
    public static void RemoveLanguage(string code)
    {
        EnsureSeeded();
        lock (_sync)
        {
            _customLanguages.RemoveAll(l => string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase));
            _catalogs.Remove(code);
            SaveLanguageRegistry();
            try
            {
                var path = CatalogPath(code);
                if (File.Exists(path)) File.Delete(path);
            }
            catch { /* best effort - a stray file left behind isn't worth failing the operation over */ }
        }
    }

    public static string ExportPo(string cultureCode)
    {
        EnsureSeeded();
        lock (_sync)
        {
            var catalog = _catalogs.TryGetValue(cultureCode, out var c) ? c : new Dictionary<string, string>();
            return PoWriter.Write(catalog, cultureCode);
        }
    }

    /// <param name="replace">true clears every existing translation for this language first (the
    /// imported file becomes the whole catalog); false merges the imported entries on top of
    /// what's already there (imported values win on key collisions).</param>
    public static void ImportPo(string cultureCode, string poText, bool replace)
    {
        if (string.Equals(cultureCode, "en", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("English is the source language and has no catalog to import into.");

        EnsureSeeded();
        var imported = PoParser.Parse(new StringReader(poText));

        lock (_sync)
        {
            if (!_catalogs.TryGetValue(cultureCode, out var catalog))
                _catalogs[cultureCode] = catalog = new Dictionary<string, string>(StringComparer.Ordinal);

            if (replace) catalog.Clear();

            var keysChanged = false;
            foreach (var entry in imported)
            {
                catalog[entry.Key] = entry.Value;
                if (_knownKeys.Add(entry.Key)) keysChanged = true;
            }

            if (keysChanged) SaveKeysRegistry();
            SaveCatalogInternal(cultureCode, catalog);
        }
    }

    // ── Persistence ──────────────────────────────────────────────────────────────────────

    private static void EnsureSeeded()
    {
        if (_seeded) return;
        lock (_sync)
        {
            if (_seeded) return;

            try { Directory.CreateDirectory(LocalizationDirectory); } catch { /* best effort */ }

            _customLanguages = LoadLanguageRegistry();

            foreach (var language in _customLanguages)
                _catalogs[language.Code] = LoadOrSeedCatalog(language.Code);

            _knownKeys = LoadOrSeedKeysRegistry();

            _seeded = true;
        }
    }

    private static Dictionary<string, string> LoadOrSeedCatalog(string cultureCode)
    {
        var path = CatalogPath(cultureCode);
        try
        {
            if (File.Exists(path))
            {
                using var reader = new StreamReader(path, Encoding.UTF8);
                return new Dictionary<string, string>(PoParser.Parse(reader), StringComparer.Ordinal);
            }
        }
        catch { /* fall through and re-seed from the embedded default below */ }

        // Not on disk (first run, or the file was deleted) - seed from the built-in embedded
        // catalog if this is one of the languages VaultGuard ships translations for out of the
        // box (empty dictionary otherwise), then persist it so it's immediately editable.
        var seeded = new Dictionary<string, string>(PoCatalogStore.LoadEmbeddedCatalog(cultureCode), StringComparer.Ordinal);
        SaveCatalogInternal(cultureCode, seeded);
        return seeded;
    }

    private static SortedSet<string> LoadOrSeedKeysRegistry()
    {
        try
        {
            if (File.Exists(KeysRegistryPath))
            {
                var stored = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(KeysRegistryPath)) ?? new List<string>();
                return new SortedSet<string>(stored, StringComparer.Ordinal);
            }
        }
        catch { /* fall through and re-seed below */ }

        // First run (or the registry file was lost) - every key that exists in any already-loaded
        // language catalog is, by definition, a known key.
        var keys = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var catalog in _catalogs.Values)
            foreach (var key in catalog.Keys)
                keys.Add(key);

        SaveKeysRegistryInternal(keys);
        return keys;
    }

    private static void SaveCatalogInternal(string cultureCode, Dictionary<string, string> catalog)
    {
        try
        {
            Directory.CreateDirectory(LocalizationDirectory);
            File.WriteAllText(CatalogPath(cultureCode), PoWriter.Write(catalog, cultureCode), Encoding.UTF8);
        }
        catch { /* best effort - a failed write shouldn't crash the app, just won't persist */ }
    }

    private static void SaveKeysRegistry() => SaveKeysRegistryInternal(_knownKeys);

    private static void SaveKeysRegistryInternal(SortedSet<string> keys)
    {
        try
        {
            Directory.CreateDirectory(LocalizationDirectory);
            File.WriteAllText(KeysRegistryPath, JsonSerializer.Serialize(keys.ToList()));
        }
        catch { /* best effort */ }
    }

    private static List<LanguageInfo> LoadLanguageRegistry()
    {
        try
        {
            if (File.Exists(LanguagesRegistryPath))
            {
                var stored = JsonSerializer.Deserialize<List<LanguageRegistryEntry>>(File.ReadAllText(LanguagesRegistryPath))
                    ?? new List<LanguageRegistryEntry>();
                return stored.Select(e => new LanguageInfo(e.Code, e.NativeName, e.EnglishName)).ToList();
            }
        }
        catch { /* fall through to defaults */ }

        // First run: seed the registry with the languages VaultGuard ships built-in translations
        // for (SupportedLanguages.Defaults, NOT .All - .All reads back through this class, and
        // using it here would recurse). So they show up immediately without the user having to
        // manually "add" a language that already has real content.
        var defaults = SupportedLanguages.Defaults.Where(l => l.Code != "en").ToList();
        SaveLanguageRegistry(defaults);
        return defaults;
    }

    private static void SaveLanguageRegistry() => SaveLanguageRegistry(_customLanguages);

    private static void SaveLanguageRegistry(List<LanguageInfo> languages)
    {
        try
        {
            Directory.CreateDirectory(LocalizationDirectory);
            var entries = languages.Select(l => new LanguageRegistryEntry(l.Code, l.NativeName, l.EnglishName)).ToList();
            File.WriteAllText(LanguagesRegistryPath, JsonSerializer.Serialize(entries));
        }
        catch { /* best effort */ }
    }

    private sealed record LanguageRegistryEntry(string Code, string NativeName, string EnglishName);
}
