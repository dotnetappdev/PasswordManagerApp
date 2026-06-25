using PasswordManager.Models;

namespace PasswordManager.Services.Utilities;

public static class BrandIconHelper
{
    public const string BrandIconCustomFieldName = "Brand Icon";
    public const int MaxBrandIconBytes = 1024 * 1024;

    public static string? GetBrandIconSource(PasswordItem? item)
    {
        var customBrandIcon = GetCustomBrandIconDataUrl(item);
        if (!string.IsNullOrWhiteSpace(customBrandIcon))
        {
            return customBrandIcon;
        }

        var brandSlug = GetBrandSlug(GetWebsite(item), item?.Title);
        return string.IsNullOrWhiteSpace(brandSlug)
            ? null
            : $"https://unpkg.com/simple-icons@v15/icons/{brandSlug}.svg";
    }

    public static string? GetCustomBrandIconDataUrl(PasswordItem? item)
    {
        var value = item?.CustomFields?
            .FirstOrDefault(field => string.Equals(field.Name, BrandIconCustomFieldName, StringComparison.OrdinalIgnoreCase))
            ?.Value;

        return IsImageDataUrl(value) ? value : null;
    }

    public static void SetCustomBrandIcon(PasswordItem item, string? dataUrl)
    {
        ArgumentNullException.ThrowIfNull(item);

        item.CustomFields ??= new List<CustomField>();

        var existingField = item.CustomFields
            .FirstOrDefault(field => string.Equals(field.Name, BrandIconCustomFieldName, StringComparison.OrdinalIgnoreCase));

        if (!IsImageDataUrl(dataUrl))
        {
            if (existingField != null)
            {
                item.CustomFields.Remove(existingField);
            }

            return;
        }

        if (existingField == null)
        {
            item.CustomFields.Add(new CustomField
            {
                Name = BrandIconCustomFieldName,
                Value = dataUrl!,
                Type = CustomFieldType.File,
                DisplayOrder = item.CustomFields.Count == 0 ? 0 : item.CustomFields.Max(field => field.DisplayOrder) + 1,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                PasswordItemId = item.Id
            });

            return;
        }

        existingField.Value = dataUrl!;
        existingField.Type = CustomFieldType.File;
        existingField.LastModified = DateTime.UtcNow;
    }

    public static string? GetWebsite(PasswordItem? item)
    {
        return item?.WebsiteUrl
            ?? item?.LoginItem?.WebsiteUrl
            ?? item?.LoginItem?.Website
            ?? item?.Website
            ?? item?.PasskeyItem?.WebsiteUrl
            ?? item?.PasskeyItem?.Website;
    }

    public static string? GetBrandSlug(string? website, string? title)
    {
        var input = string.IsNullOrWhiteSpace(website) ? title : website;
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var domain = input.Trim().ToLowerInvariant();
        if (domain.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                domain = new Uri(domain).Host;
            }
            catch
            {
                // Fall back to the raw input if it isn't a URL.
            }
        }

        if (domain.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
        {
            domain = domain[4..];
        }

        var parts = domain.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 2)
        {
            domain = parts[^2];
        }
        else if (parts.Length == 2)
        {
            domain = parts[0];
        }

        domain = new string(domain.Where(char.IsLetterOrDigit).ToArray());
        return string.IsNullOrWhiteSpace(domain) ? null : domain;
    }

    public static bool IsImageDataUrl(string? value)
        => !string.IsNullOrWhiteSpace(value)
            && value.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase);
}
