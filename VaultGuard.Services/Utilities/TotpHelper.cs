using VaultGuard.Models;

namespace VaultGuard.Services.Utilities;

/// <summary>
/// Stores a login's authenticator (TOTP) secret on the item via a reserved custom field, so the
/// Bitwarden-style "Verification code" feature works with no database migration. The value is a
/// Base32 secret or a full otpauth:// URI.
/// </summary>
public static class TotpHelper
{
    public const string TotpCustomFieldName = "TOTP Secret";

    public static string? GetSecret(PasswordItem? item)
    {
        var value = item?.CustomFields?
            .FirstOrDefault(field => string.Equals(field.Name, TotpCustomFieldName, StringComparison.OrdinalIgnoreCase))
            ?.Value;

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public static bool HasSecret(PasswordItem? item) => GetSecret(item) != null;

    public static void SetSecret(PasswordItem item, string? secret)
    {
        ArgumentNullException.ThrowIfNull(item);

        item.CustomFields ??= new List<CustomField>();

        var existing = item.CustomFields
            .FirstOrDefault(field => string.Equals(field.Name, TotpCustomFieldName, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(secret))
        {
            if (existing != null) item.CustomFields.Remove(existing);
            return;
        }

        var trimmed = secret.Trim();

        if (existing == null)
        {
            item.CustomFields.Add(new CustomField
            {
                Name = TotpCustomFieldName,
                Value = trimmed,
                Type = CustomFieldType.Password,
                DisplayOrder = item.CustomFields.Count == 0 ? 0 : item.CustomFields.Max(f => f.DisplayOrder) + 1,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                PasswordItemId = item.Id
            });
            return;
        }

        existing.Value = trimmed;
        existing.Type = CustomFieldType.Password;
        existing.LastModified = DateTime.UtcNow;
    }
}
