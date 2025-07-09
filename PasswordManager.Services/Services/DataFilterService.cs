using PasswordManager.Models;

namespace PasswordManager.Services.Services;

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
                Password = item.LoginItem.Password, // In production, consider encrypting this
                Website = item.LoginItem.Website,
                Notes = FilterNotes(item.LoginItem.Notes),
                LastAutoFill = item.LoginItem.LastAutoFill,
                RequiresMasterPassword = item.LoginItem.RequiresMasterPassword,
                PasswordId = item.LoginItem.PasswordId
            };
        }

        // Filter credit card items - be extra careful with financial data
        if (item.CreditCardItem != null)
        {
            filteredItem.CreditCardItem = new CreditCardItem
            {
                Id = item.CreditCardItem.Id,
                CardholderName = item.CreditCardItem.CardholderName,
                CardNumber = item.CreditCardItem.CardNumber, // In production, encrypt or mask this
                ExpirationMonth = item.CreditCardItem.ExpirationMonth,
                ExpirationYear = item.CreditCardItem.ExpirationYear,
                SecurityCode = item.CreditCardItem.SecurityCode, // In production, encrypt or exclude this
                CardType = item.CreditCardItem.CardType,
                Notes = FilterNotes(item.CreditCardItem.Notes),
                RequiresMasterPassword = item.CreditCardItem.RequiresMasterPassword,
                PasswordId = item.CreditCardItem.PasswordId
            };
        }

        // Filter secure notes - these often contain sensitive information
        if (item.SecureNoteItem != null)
        {
            filteredItem.SecureNoteItem = new SecureNoteItem
            {
                Id = item.SecureNoteItem.Id,
                Content = FilterNotes(item.SecureNoteItem.Content),
                RequiresMasterPassword = item.SecureNoteItem.RequiresMasterPassword,
                PasswordId = item.SecureNoteItem.PasswordId
            };
        }

        // Filter WiFi items
        if (item.WiFiItem != null)
        {
            filteredItem.WiFiItem = new WiFiItem
            {
                Id = item.WiFiItem.Id,
                NetworkName = item.WiFiItem.NetworkName,
                Password = item.WiFiItem.Password, // In production, encrypt this
                SecurityType = item.WiFiItem.SecurityType,
                Notes = FilterNotes(item.WiFiItem.Notes),
                RequiresMasterPassword = item.WiFiItem.RequiresMasterPassword,
                PasswordId = item.WiFiItem.PasswordId
            };
        }

        return filteredItem;
    }

    /// <summary>
    /// Determines whether an item appears to be a master password
    /// </summary>
    private static bool IsMasterPassword(PasswordItem item)
    {
        // Check if title contains keywords suggesting it's a master password
        var titleLower = item.Title?.ToLower() ?? "";
        var keywordsList = new[] { "master", "password manager", "vault", "primary", "main password" };
        
        if (keywordsList.Any(keyword => titleLower.Contains(keyword)))
        {
            return true;
        }

        // Check if website refers to password manager services
        if (item.LoginItem?.Website != null)
        {
            var websiteLower = item.LoginItem.Website.ToLower();
            var passwordManagerSites = new[] 
            { 
                "bitwarden", 
                "1password", 
                "lastpass", 
                "dashlane", 
                "keeper", 
                "nordpass", 
                "passwordmanager" 
            };
            
            if (passwordManagerSites.Any(site => websiteLower.Contains(site)))
            {
                return true;
            }
        }

        // Implement additional checks based on your requirements
        return false;
    }

    /// <summary>
    /// Filters out sensitive information from notes
    /// </summary>
    private static string FilterNotes(string notes)
    {
        if (string.IsNullOrEmpty(notes))
        {
            return notes;
        }

        // Implement filtering logic - for example:
        // - Remove SSN/National ID patterns
        // - Mask financial account numbers
        // - Remove specific keywords or sections marked "private"

        // Simple SSN pattern (US)
        notes = System.Text.RegularExpressions.Regex.Replace(
            notes, 
            @"\b\d{3}[-.]?\d{2}[-.]?\d{4}\b", 
            "[REDACTED-SSN]"
        );

        // Credit card numbers
        notes = System.Text.RegularExpressions.Regex.Replace(
            notes, 
            @"\b(?:\d[ -]*?){13,16}\b", 
            "[REDACTED-CC]"
        );

        // Bank account numbers (simple pattern)
        notes = System.Text.RegularExpressions.Regex.Replace(
            notes, 
            @"\b(?:acc(?:oun)?t\.?\s*(?:no|num|number)?\.?:?\s*)?\d{8,12}\b", 
            "[REDACTED-ACCOUNT]"
        );

        // Private sections
        notes = System.Text.RegularExpressions.Regex.Replace(
            notes,
            @"(?:--PRIVATE--|===PRIVATE===|BEGIN PRIVATE).*?(?:--END PRIVATE--|===END PRIVATE===|END PRIVATE)",
            "[REDACTED-PRIVATE]",
            System.Text.RegularExpressions.RegexOptions.Singleline
        );

        return notes;
    }
}
