using System.Text;

namespace VaultGuard.Localization;

/// <summary>
/// Minimal reader for the subset of the gettext .po format VaultGuard actually uses: comment lines
/// (#...), and msgid "..." / msgstr "..." pairs - each optionally continued across several quoted-
/// string lines, which gettext tooling (and every .po editor) concatenates - separated by blank
/// lines. msgctxt and plural forms (msgid_plural/msgstr[n]) are recognized just well enough to skip
/// over without corrupting the entry around them; VaultGuard's UI strings are all simple,
/// non-pluralized labels, so nothing here needs to resolve a plural translation. Deliberately not a
/// full gettext parser (no msgid_plural support, no %-decoding beyond the standard C-style escapes
/// below) - if that's ever needed, swap this file for a real PO library instead of extending it.
/// </summary>
internal static class PoParser
{
    public static IReadOnlyDictionary<string, string> Parse(TextReader reader)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        string? msgid = null;
        string? msgstr = null;
        var readingMsgId = false;
        var readingMsgStr = false;

        void Flush()
        {
            if (!string.IsNullOrEmpty(msgid) && !string.IsNullOrEmpty(msgstr))
                result[msgid] = msgstr;
            msgid = null;
            msgstr = null;
            readingMsgId = false;
            readingMsgStr = false;
        }

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var trimmed = line.Trim();

            if (trimmed.Length == 0)
            {
                Flush();
                continue;
            }

            if (trimmed.StartsWith('#'))
                continue;

            if (trimmed.StartsWith("msgctxt", StringComparison.Ordinal))
            {
                // Contexts aren't a separate lookup key in our single shared catalog - just ignore
                // the line and translate on msgid alone.
                readingMsgId = false;
                readingMsgStr = false;
                continue;
            }

            if (trimmed.StartsWith("msgid_plural", StringComparison.Ordinal))
            {
                // Plural forms aren't supported - stop accumulating so the plural source text
                // doesn't get appended onto msgid.
                readingMsgId = false;
                readingMsgStr = false;
                continue;
            }

            if (trimmed.StartsWith("msgid", StringComparison.Ordinal))
            {
                Flush();
                msgid = ExtractQuoted(trimmed, "msgid");
                readingMsgId = true;
                readingMsgStr = false;
                continue;
            }

            if (trimmed.StartsWith("msgstr[", StringComparison.Ordinal))
            {
                // Only msgstr[0] (singular) is kept as a best-effort translation; later indices are
                // plural forms this catalog has no slot for.
                if (trimmed.StartsWith("msgstr[0]", StringComparison.Ordinal))
                {
                    msgstr = ExtractQuoted(trimmed, "msgstr[0]");
                    readingMsgId = false;
                    readingMsgStr = true;
                }
                else
                {
                    readingMsgId = false;
                    readingMsgStr = false;
                }
                continue;
            }

            if (trimmed.StartsWith("msgstr", StringComparison.Ordinal))
            {
                msgstr = ExtractQuoted(trimmed, "msgstr");
                readingMsgId = false;
                readingMsgStr = true;
                continue;
            }

            if (trimmed.StartsWith('"'))
            {
                var text = ExtractQuotedLiteral(trimmed);
                if (readingMsgId) msgid += text;
                else if (readingMsgStr) msgstr += text;
                continue;
            }

            // Unrecognized line - ignore rather than throw. A malformed or partially-translated
            // catalog should degrade to "missing translation" (falls back to English), not crash
            // the app that loads it.
        }

        Flush();

        // The header entry (msgid "") carries catalog metadata (Content-Type, plural rules, etc.)
        // in its msgstr, not a real translation - drop it so it can't shadow a genuine empty-string
        // lookup (which would never happen in practice, but keep the catalog honest).
        result.Remove(string.Empty);

        return result;
    }

    private static string ExtractQuoted(string line, string keyword)
        => ExtractQuotedLiteral(line[keyword.Length..].TrimStart());

    private static string ExtractQuotedLiteral(string text)
    {
        var start = text.IndexOf('"');
        var end = text.LastIndexOf('"');
        if (start < 0 || end <= start) return string.Empty;

        return Unescape(text[(start + 1)..end]);
    }

    private static string Unescape(string raw)
    {
        if (!raw.Contains('\\')) return raw;

        var sb = new StringBuilder(raw.Length);
        for (var i = 0; i < raw.Length; i++)
        {
            var c = raw[i];
            if (c == '\\' && i + 1 < raw.Length)
            {
                var next = raw[++i];
                sb.Append(next switch
                {
                    'n' => '\n',
                    't' => '\t',
                    'r' => '\r',
                    '"' => '"',
                    '\\' => '\\',
                    _ => next,
                });
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}
