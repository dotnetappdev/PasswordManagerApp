using System.Text.Json;
using VaultGuard.Models;

namespace VaultGuard.Services.Utilities;

/// <summary>
/// Keeps a rolling history of an item's previous passwords so the user can view when a password changed
/// and restore an earlier one. Stored — like <see cref="TotpHelper"/> — in a single reserved custom field
/// as JSON, so it needs <b>no database migration</b> and works on every provider immediately.
///
/// Only the <b>already-encrypted</b> password blobs (ciphertext + nonce + tag) are stored, never
/// plaintext, so this adds no new at-rest exposure. The reserved field is hidden from the custom-field UI
/// (callers filter on <see cref="HistoryFieldName"/>, the same way the TOTP field is hidden).
/// </summary>
public static class PasswordHistoryHelper
{
    /// <summary>Reserved custom-field name that holds the JSON history. Hidden from the normal fields UI.</summary>
    public const string HistoryFieldName = "vaultguard.password-history";

    /// <summary>Cap the history so a frequently-changed item can't grow without bound.</summary>
    private const int MaxEntries = 25;

    public sealed class Entry
    {
        public string Enc { get; set; } = string.Empty;
        public string? Nonce { get; set; }
        public string? Tag { get; set; }
        public DateTime At { get; set; }
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public static IReadOnlyList<Entry> GetHistory(PasswordItem? item)
    {
        var raw = item?.CustomFields?
            .FirstOrDefault(f => string.Equals(f.Name, HistoryFieldName, StringComparison.OrdinalIgnoreCase))
            ?.Value;

        if (string.IsNullOrWhiteSpace(raw)) return Array.Empty<Entry>();
        try
        {
            return JsonSerializer.Deserialize<List<Entry>>(raw!, JsonOptions) ?? new List<Entry>();
        }
        catch
        {
            return Array.Empty<Entry>();
        }
    }

    public static bool HasHistory(PasswordItem? item) => GetHistory(item).Count > 0;

    /// <summary>Prepend a previous encrypted password to the history (newest first), capped at the max.</summary>
    public static void Append(PasswordItem item, string encryptedPassword, string? nonce, string? tag)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (string.IsNullOrEmpty(encryptedPassword)) return;

        var list = GetHistory(item).ToList();

        // Skip if the newest entry is already this exact blob (idempotent on repeated saves).
        if (list.Count > 0 && string.Equals(list[0].Enc, encryptedPassword, StringComparison.Ordinal))
            return;

        list.Insert(0, new Entry { Enc = encryptedPassword, Nonce = nonce, Tag = tag, At = DateTime.UtcNow });
        if (list.Count > MaxEntries) list = list.Take(MaxEntries).ToList();

        Save(item, list);
    }

    private static void Save(PasswordItem item, List<Entry> list)
    {
        item.CustomFields ??= new List<CustomField>();
        var json = JsonSerializer.Serialize(list, JsonOptions);

        var existing = item.CustomFields
            .FirstOrDefault(f => string.Equals(f.Name, HistoryFieldName, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
        {
            item.CustomFields.Add(new CustomField
            {
                Name = HistoryFieldName,
                Value = json,
                Type = CustomFieldType.Text,
                DisplayOrder = int.MaxValue, // always last; it's hidden anyway
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                PasswordItemId = item.Id
            });
        }
        else
        {
            existing.Value = json;
            existing.LastModified = DateTime.UtcNow;
        }
    }
}
