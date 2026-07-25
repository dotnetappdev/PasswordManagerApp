using System.Text;

namespace VaultGuard.Localization;

/// <summary>
/// Serializes a key/value translation catalog back to gettext .po text - the inverse of
/// <see cref="PoParser"/>. Used both to persist <see cref="TranslationRepository"/>'s edits to disk
/// and to generate the file the Translations CRUD page's "Export" button downloads.
/// </summary>
internal static class PoWriter
{
    private static readonly char[] SpecialChars = { '\\', '"', '\n', '\r', '\t' };

    public static string Write(IEnumerable<KeyValuePair<string, string>> entries, string cultureCode)
    {
        var sb = new StringBuilder();
        sb.Append("msgid \"\"\n");
        sb.Append("msgstr \"\"\n");
        sb.Append("\"Project-Id-Version: VaultGuard\\n\"\n");
        sb.Append("\"MIME-Version: 1.0\\n\"\n");
        sb.Append("\"Content-Type: text/plain; charset=UTF-8\\n\"\n");
        sb.Append("\"Content-Transfer-Encoding: 8bit\\n\"\n");
        sb.Append("\"Language: ").Append(cultureCode).Append("\\n\"\n\n");

        foreach (var entry in entries.OrderBy(e => e.Key, StringComparer.Ordinal))
        {
            sb.Append("msgid \"").Append(Escape(entry.Key)).Append("\"\n");
            sb.Append("msgstr \"").Append(Escape(entry.Value)).Append("\"\n\n");
        }

        return sb.ToString();
    }

    private static string Escape(string value)
    {
        if (value.IndexOfAny(SpecialChars) < 0) return value;

        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }
}
