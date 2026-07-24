using System.Collections.Concurrent;
using System.Text;

namespace VaultGuard.Localization;

/// <summary>
/// Loads and caches the embedded .po catalogs (VaultGuard.Localization/Resources/messages.&lt;code&gt;.po)
/// shipped inside this assembly. English has no catalog file - <see cref="GetTranslation"/> simply
/// returns null for "en" (or any culture with no matching entry, or an entry missing this
/// particular msgid), and callers fall back to the msgid itself - which IS the English text, since
/// English is the source language (see docs/LOCALIZATION.md).
/// </summary>
public static class PoCatalogStore
{
    private static readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> _catalogs =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Returns the translated string, or null if this culture has no catalog / no entry for it.</summary>
    public static string? GetTranslation(string cultureCode, string msgid)
    {
        if (string.IsNullOrEmpty(msgid) || string.IsNullOrEmpty(cultureCode)) return null;
        if (string.Equals(cultureCode, "en", StringComparison.OrdinalIgnoreCase)) return null;

        var catalog = _catalogs.GetOrAdd(cultureCode, LoadCatalog);
        return catalog.TryGetValue(msgid, out var translated) && !string.IsNullOrEmpty(translated)
            ? translated
            : null;
    }

    private static IReadOnlyDictionary<string, string> LoadCatalog(string cultureCode)
    {
        var assembly = typeof(PoCatalogStore).Assembly;

        // Expected MSBuild logical name for Resources/messages.<code>.po given RootNamespace
        // "VaultGuard.Localization". Falls back to a suffix search below in case the toolchain ever
        // names it slightly differently - a wrong assumption here should degrade to "no translation
        // found" (English fallback), never crash the app that loads it.
        var expected = $"{typeof(PoCatalogStore).Namespace}.Resources.messages.{cultureCode}.po";

        var resourceName = Array.Find(assembly.GetManifestResourceNames(),
            n => string.Equals(n, expected, StringComparison.OrdinalIgnoreCase))
            ?? Array.Find(assembly.GetManifestResourceNames(),
                n => n.EndsWith($".messages.{cultureCode}.po", StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
            return new Dictionary<string, string>();

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            return new Dictionary<string, string>();

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return PoParser.Parse(reader);
    }
}
