using VaultGuard.Models;

namespace VaultGuard.Services.Utilities;

/// <summary>
/// Marks an item (typically a secure note) as "protected", so its sensitive content is masked in
/// the UI until the user explicitly reveals it (and, on platforms that support it, re-authenticates
/// with Windows Hello / a passcode). Stored on the item via a reserved custom field so no database
/// migration is needed — same pattern as <see cref="TotpHelper"/> and BrandIconHelper.
/// </summary>
public static class ProtectedItemHelper
{
    public const string ProtectedCustomFieldName = "VaultGuard.Protected";

    public static bool IsProtected(PasswordItem? item)
    {
        var value = item?.CustomFields?
            .FirstOrDefault(f => string.Equals(f.Name, ProtectedCustomFieldName, StringComparison.OrdinalIgnoreCase))
            ?.Value;

        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";
    }

    public static void SetProtected(PasswordItem item, bool isProtected)
    {
        ArgumentNullException.ThrowIfNull(item);
        item.CustomFields ??= new List<CustomField>();

        var existing = item.CustomFields
            .FirstOrDefault(f => string.Equals(f.Name, ProtectedCustomFieldName, StringComparison.OrdinalIgnoreCase));

        if (!isProtected)
        {
            if (existing != null) item.CustomFields.Remove(existing);
            return;
        }

        if (existing == null)
        {
            item.CustomFields.Add(new CustomField
            {
                Name = ProtectedCustomFieldName,
                Value = "true",
                Type = CustomFieldType.Text,
                DisplayOrder = item.CustomFields.Count == 0 ? 0 : item.CustomFields.Max(f => f.DisplayOrder) + 1,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                PasswordItemId = item.Id
            });
            return;
        }

        existing.Value = "true";
        existing.LastModified = DateTime.UtcNow;
    }
}
