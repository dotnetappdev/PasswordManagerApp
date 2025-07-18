using PasswordManager.Models;

namespace PasswordManager.API.Services;

public static class DataFilterService
{
    /// <summary>
    /// Filters sensitive data from password items before syncing
    /// </summary>
    public static PasswordItem FilterSensitiveData(PasswordItem item)
    {
        var filteredItem = new PasswordItem
        {
            Id = item.Id,
            Title = item.Title,
            Description = item.Description,
            Type = item.Type,
            CreatedAt = item.CreatedAt,
            LastModified = item.LastModified,
            IsFavorite = item.IsFavorite,
            IsArchived = item.IsArchived,
            IsDeleted = item.IsDeleted,
            CategoryId = item.CategoryId,
            CollectionId = item.CollectionId
        };

        // Filter login items - exclude master passwords and highly sensitive data
        if (item.LoginItem != null)
        {
            // Skip items that might be master passwords (based on title or website)
            if (IsMasterPassword(item))
            {
                return null; // Don't sync master passwords
            }

            filteredItem.LoginItem = new LoginItem
            {
                Id = item.LoginItem.Id,
                Username = item.LoginItem.Username,
                Email = item.LoginItem.Email,
                Password = item.LoginItem.Password, // In production, consider encrypting this
                Website = item.LoginItem.Website,
                TotpSecret = item.LoginItem.TotpSecret,
                Notes = FilterNotes(item.LoginItem.Notes),
                PasswordItemId = item.LoginItem.PasswordItemId
            };
        }

        // Filter credit card items - be extra careful with financial data
        if (item.CreditCardItem != null)
        {
            filteredItem.CreditCardItem = new CreditCardItem
            {
                Id = item.CreditCardItem.Id,
                CardholderName = item.CreditCardItem.CardholderName,
                CardNumber = MaskCreditCardNumber(item.CreditCardItem.CardNumber),
                CVV = null, // Never sync CVV
                ExpiryDate = item.CreditCardItem.ExpiryDate,
                PIN = null, // Never sync PIN
                CardType = item.CreditCardItem.CardType,
                Notes = FilterNotes(item.CreditCardItem.Notes),
                PasswordItemId = item.CreditCardItem.PasswordItemId
            };
        }

        // Filter secure notes - check for sensitive content
        if (item.SecureNoteItem != null)
        {
            filteredItem.SecureNoteItem = new SecureNoteItem
            {
                Id = item.SecureNoteItem.Id,
                Content = FilterSecureNoteContent(item.SecureNoteItem.Content),
                PasswordItemId = item.SecureNoteItem.PasswordItemId
            };
        }

        // WiFi items - generally safe to sync
        if (item.WiFiItem != null)
        {
            filteredItem.WiFiItem = new WiFiItem
            {
                Id = item.WiFiItem.Id,
                NetworkName = item.WiFiItem.NetworkName,
                Password = item.WiFiItem.Password,
                SecurityType = item.WiFiItem.SecurityType,
                Frequency = item.WiFiItem.Frequency,
                RouterPassword = item.WiFiItem.RouterPassword,
                Notes = FilterNotes(item.WiFiItem.Notes),
                PasswordItemId = item.WiFiItem.PasswordItemId
            };
        }

        return filteredItem;
    }

    /// <summary>
    /// Determines if an item is likely a master password
    /// </summary>
    private static bool IsMasterPassword(PasswordItem item)
    {
        if (item.Title == null) return false;

        var title = item.Title.ToLowerInvariant();
        var masterPasswordKeywords = new[]
        {
            "master password", "master", "main password", "primary password",
            "vault password", "password manager", "keystore", "master key"
        };

        return masterPasswordKeywords.Any(keyword => title.Contains(keyword));
    }

    /// <summary>
    /// Masks credit card numbers for sync
    /// </summary>
    private static string? MaskCreditCardNumber(string? cardNumber)
    {
        if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 8)
            return cardNumber;

        // Show only last 4 digits
        var digitsOnly = new string(cardNumber.Where(char.IsDigit).ToArray());
        if (digitsOnly.Length < 4) return cardNumber;

        var lastFour = digitsOnly.Substring(digitsOnly.Length - 4);
        var masked = new string('*', digitsOnly.Length - 4) + lastFour;
        
        // Preserve original formatting
        var result = cardNumber;
        var digitIndex = 0;
        for (int i = 0; i < result.Length && digitIndex < masked.Length; i++)
        {
            if (char.IsDigit(result[i]))
            {
                result = result.Remove(i, 1).Insert(i, masked[digitIndex].ToString());
                digitIndex++;
            }
        }

        return result;
    }

    /// <summary>
    /// Filters notes to remove sensitive information
    /// </summary>
    private static string? FilterNotes(string? notes)
    {
        if (string.IsNullOrEmpty(notes)) return notes;

        // Remove lines that might contain sensitive information
        var lines = notes.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var filteredLines = lines.Where(line =>
        {
            var lowerLine = line.ToLowerInvariant();
            return !lowerLine.Contains("ssn") &&
                   !lowerLine.Contains("social security") &&
                   !lowerLine.Contains("passport") &&
                   !lowerLine.Contains("license number") &&
                   !lowerLine.Contains("driver's license");
        });

        return string.Join('\n', filteredLines);
    }

    /// <summary>
    /// Filters secure note content
    /// </summary>
    private static string? FilterSecureNoteContent(string? content)
    {
        if (string.IsNullOrEmpty(content)) return content;

        // For now, just filter notes like other content
        // In production, you might want more sophisticated filtering
        return FilterNotes(content);
    }

    /// <summary>
    /// Determines if a category should be synced
    /// </summary>
    public static bool ShouldSyncCategory(Category category)
    {
        if (category.Name == null) return true;

        var name = category.Name.ToLowerInvariant();
        var excludedCategories = new[]
        {
            "master passwords", "local only", "private", "do not sync"
        };

        return !excludedCategories.Any(excluded => name.Contains(excluded));
    }

    /// <summary>
    /// Determines if a collection should be synced
    /// </summary>
    public static bool ShouldSyncCollection(Collection collection)
    {
        if (collection.Name == null) return true;

        var name = collection.Name.ToLowerInvariant();
        var excludedCollections = new[]
        {
            "local only", "private", "do not sync", "offline"
        };

        return !excludedCollections.Any(excluded => name.Contains(excluded));
    }

    /// <summary>
    /// Determines if a tag should be synced
    /// </summary>
    public static bool ShouldSyncTag(Tag tag)
    {
        if (tag.Name == null) return true;

        var name = tag.Name.ToLowerInvariant();
        var excludedTags = new[]
        {
            "local only", "private", "do not sync", "sensitive", "master"
        };

        return !excludedTags.Any(excluded => name.Contains(excluded));
    }
}
