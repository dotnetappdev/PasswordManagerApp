using FileHelpers;
using PasswordManager.Imports.Interfaces;
using PasswordManager.Models;
using PasswordManagerImports.OnePassword.Models;

namespace PasswordManagerImports.OnePassword.Providers;

public class OnePasswordImportProvider : IPasswordImportProvider
{
    public string ProviderName => "1Password";
    public string DisplayName => "1Password CSV Import";
    public string[] SupportedFileExtensions => new[] { ".csv" };

    public async Task<ImportResult> ImportFromFileAsync(Stream fileStream, string fileName)
    {
        var result = new ImportResult();

        try
        {
            using var reader = new StreamReader(fileStream);
            var csvContent = await reader.ReadToEndAsync();

            // Check if the CSV content is valid
            if (string.IsNullOrWhiteSpace(csvContent))
            {
                result.Success = false;
                result.ErrorMessage = "The CSV file is empty or contains no valid content.";
                return result;
            }

            var engine = new FileHelperEngine<OnePasswordCsvRecord>();
            OnePasswordCsvRecord[] records;
            
            try
            {
                records = engine.ReadString(csvContent);
            }
            catch (Exception csvEx)
            {
                result.Success = false;
                result.ErrorMessage = $"Failed to parse CSV file. Please ensure it's a valid 1Password export file. Error: {csvEx.Message}";
                return result;
            }

            result.TotalItemsProcessed = records.Length;

            // Track collections and categories we need to create
            var collectionsToCreate = new Dictionary<string, Collection>();
            var categoriesToCreate = new Dictionary<string, Category>();

            foreach (var record in records)
            {
                try
                {
                    // Skip empty records
                    if (string.IsNullOrWhiteSpace(record.Title) && 
                        string.IsNullOrWhiteSpace(record.Username) && 
                        string.IsNullOrWhiteSpace(record.Password))
                    {
                        continue;
                    }

                    // Determine collection based on URL/title analysis (no folder in new format)
                    var collectionName = DetermineCollection(record.Url, record.Title);
                    var categoryName = DetermineCategory(record.Url, record.Title);

                    // Ensure collection exists
                    if (!collectionsToCreate.ContainsKey(collectionName))
                    {
                        collectionsToCreate[collectionName] = new Collection
                        {
                            Name = collectionName,
                            Icon = GetCollectionIcon(collectionName),
                            Color = GetCollectionColor(collectionName),
                            IsDefault = collectionName == "Banking"
                        };
                    }

                    var collection = collectionsToCreate[collectionName];

                    // Ensure category exists
                    var categoryKey = $"{collectionName}:{categoryName}";
                    if (!categoriesToCreate.ContainsKey(categoryKey))
                    {
                        categoriesToCreate[categoryKey] = new Category
                        {
                            Name = categoryName,
                            Icon = GetCategoryIcon(categoryName),
                            Color = GetCategoryColor(categoryName),
                            CollectionId = collection.Id // This will be set properly after collection creation
                        };
                    }

                    var category = categoriesToCreate[categoryKey];

                    // Create the password item
                    var passwordItem = new PasswordItem
                    {
                        Title = record.Title.Trim(),
                        Type = ItemType.Login,
                        CollectionId = collection.Id, // Will be updated after collection creation
                        CategoryId = category.Id, // Will be updated after category creation
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow,
                        LoginItem = new LoginItem
                        {
                            Website = record.Url.Trim(),
                            Username = record.Username.Trim(),
                            Password = record.Password.Trim(), // Temporary property for import
                            Email = IsEmail(record.Username.Trim()) ? record.Username.Trim() : null,
                            Notes = record.Notes.Trim(), // Temporary property for import
                            TotpSecret = !string.IsNullOrWhiteSpace(record.OTPAuth) ? record.OTPAuth.Trim() : null // Handle OTPAuth field
                        },
                        Tags = new List<Tag>()
                    };

                    // Add import tag
                    var importTag = new Tag
                    {
                        Name = "Imported",
                        Color = "#8b5cf6"
                    };

                    passwordItem.Tags.Add(importTag);
                    
                    // Handle favorite items
                    if (!string.IsNullOrWhiteSpace(record.Favorite) && 
                        (record.Favorite.Equals("true", StringComparison.OrdinalIgnoreCase) || record.Favorite == "1"))
                    {
                        var favoriteTag = new Tag
                        {
                            Name = "Favorite",
                            Color = "#fbbf24"
                        };
                        passwordItem.Tags.Add(favoriteTag);
                    }
                    
                    // Handle archived items
                    if (!string.IsNullOrWhiteSpace(record.Archived) && 
                        (record.Archived.Equals("true", StringComparison.OrdinalIgnoreCase) || record.Archived == "1"))
                    {
                        var archivedTag = new Tag
                        {
                            Name = "Archived",
                            Color = "#6b7280"
                        };
                        passwordItem.Tags.Add(archivedTag);
                    }
                    
                    // Handle custom tags from 1Password
                    if (!string.IsNullOrWhiteSpace(record.Tags))
                    {
                        var tags = record.Tags.Split(',', ';')
                            .Where(t => !string.IsNullOrWhiteSpace(t))
                            .Select(t => t.Trim());
                            
                        foreach (var tagName in tags)
                        {
                            var customTag = new Tag
                            {
                                Name = tagName,
                                Color = "#a855f7" // Purple for custom tags
                            };
                            passwordItem.Tags.Add(customTag);
                        }
                    }
                    
                    // Add high priority tag if it looks important
                    if (IsHighPriority(record.Title, record.Url))
                    {
                        var highPriorityTag = new Tag
                        {
                            Name = "High Priority",
                            Color = "#ef4444"
                        };
                        passwordItem.Tags.Add(highPriorityTag);
                    }

                    result.ImportedItems.Add(passwordItem);
                }
                catch (Exception ex)
                {
                    result.Warnings.Add($"Failed to process record '{record.Title}': {ex.Message}");
                }
            }

            // Add required collections, categories, and tags to result
            result.RequiredCollections.AddRange(collectionsToCreate.Values);
            result.RequiredCategories.AddRange(categoriesToCreate.Values);
            
            // Add standard tags
            result.RequiredTags.Add(new Tag { Name = "Imported", Color = "#8b5cf6" });
            result.RequiredTags.Add(new Tag { Name = "High Priority", Color = "#ef4444" });
            result.RequiredTags.Add(new Tag { Name = "Favorite", Color = "#fbbf24" });
            result.RequiredTags.Add(new Tag { Name = "Archived", Color = "#6b7280" });

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    private string DetermineCollection(string url, string title)
    {
        var domain = ExtractDomain(url).ToLowerInvariant();
        var titleLower = title.ToLowerInvariant();

        // Banking keywords
        if (IsBankingDomain(domain) || titleLower.Contains("bank") || titleLower.Contains("credit") || 
            titleLower.Contains("loan") || titleLower.Contains("mortgage"))
        {
            return "Banking";
        }

        // Insurance keywords
        if (IsInsuranceDomain(domain) || titleLower.Contains("insurance") || titleLower.Contains("health") ||
            titleLower.Contains("auto insurance") || titleLower.Contains("life insurance"))
        {
            return "Insurance";
        }

        // Utilities keywords
        if (IsUtilityDomain(domain) || titleLower.Contains("electric") || titleLower.Contains("gas") ||
            titleLower.Contains("water") || titleLower.Contains("internet") || titleLower.Contains("utility"))
        {
            return "Utilities";
        }

        // Default to General
        return "General";
    }

    private string DetermineCategory(string url, string title)
    {
        var domain = ExtractDomain(url).ToLowerInvariant();
        var titleLower = title.ToLowerInvariant();

        // Banking categories
        if (titleLower.Contains("checking") || titleLower.Contains("checking account"))
            return "Checking";
        if (titleLower.Contains("savings") || titleLower.Contains("savings account"))
            return "Savings";
        if (titleLower.Contains("credit card") || titleLower.Contains("credit"))
            return "Credit Cards";
        if (titleLower.Contains("loan") || titleLower.Contains("mortgage"))
            return "Loans";

        // Insurance categories
        if (titleLower.Contains("auto") || titleLower.Contains("car"))
            return "Auto";
        if (titleLower.Contains("health") || titleLower.Contains("medical"))
            return "Health";
        if (titleLower.Contains("home") || titleLower.Contains("property"))
            return "Home";
        if (titleLower.Contains("life"))
            return "Life";

        // Utility categories
        if (titleLower.Contains("electric") || titleLower.Contains("power"))
            return "Electric";
        if (titleLower.Contains("gas"))
            return "Gas";
        if (titleLower.Contains("water") || titleLower.Contains("sewer"))
            return "Water";
        if (titleLower.Contains("internet") || titleLower.Contains("cable") || titleLower.Contains("phone"))
            return "Internet";

        // Default based on collection
        var collection = DetermineCollection(url, title);
        return collection switch
        {
            "Banking" => "Checking",
            "Insurance" => "Auto",
            "Utilities" => "Electric",
            _ => "General"
        };
    }

    private bool IsBankingDomain(string domain)
    {
        var bankingDomains = new[] { "bankofamerica", "chase", "wellsfargo", "citibank", "usbank", "pnc", "capitalone", "americanexpress" };
        return bankingDomains.Any(bd => domain.Contains(bd));
    }

    private bool IsInsuranceDomain(string domain)
    {
        var insuranceDomains = new[] { "statefarm", "geico", "progressive", "allstate", "bluecross", "aetna", "anthem" };
        return insuranceDomains.Any(id => domain.Contains(id));
    }

    private bool IsUtilityDomain(string domain)
    {
        var utilityDomains = new[] { "comed", "xfinity", "verizon", "att", "tmobile", "comcast", "spectrum" };
        return utilityDomains.Any(ud => domain.Contains(ud));
    }

    private string ExtractDomain(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        try
        {
            if (!url.StartsWith("http"))
                url = "https://" + url;

            var uri = new Uri(url);
            return uri.Host.Replace("www.", "");
        }
        catch
        {
            return url;
        }
    }

    private bool IsEmail(string input)
    {
        return !string.IsNullOrWhiteSpace(input) && input.Contains("@") && input.Contains(".");
    }

    private bool IsHighPriority(string title, string url)
    {
        var titleLower = title.ToLowerInvariant();
        var urlLower = url.ToLowerInvariant();

        return titleLower.Contains("bank") || titleLower.Contains("root") || titleLower.Contains("admin") ||
               urlLower.Contains("aws") || urlLower.Contains("azure") || urlLower.Contains("google");
    }

    private string GetCollectionIcon(string collectionName)
    {
        return collectionName switch
        {
            "Banking" => "🏦",
            "Insurance" => "🛡️",
            "Utilities" => "⚡",
            _ => "📁"
        };
    }

    private string GetCollectionColor(string collectionName)
    {
        return collectionName switch
        {
            "Banking" => "#3b82f6",
            "Insurance" => "#10b981",
            "Utilities" => "#f59e0b",
            _ => "#6b7280"
        };
    }

    private string GetCategoryIcon(string categoryName)
    {
        return categoryName switch
        {
            "Checking" => "💳",
            "Savings" => "💰",
            "Credit Cards" => "💎",
            "Loans" => "📋",
            "Auto" => "🚗",
            "Health" => "🏥",
            "Home" => "🏠",
            "Life" => "👨‍👩‍👧‍👦",
            "Electric" => "⚡",
            "Gas" => "🔥",
            "Water" => "💧",
            "Internet" => "🌐",
            _ => "📄"
        };
    }

    private string GetCategoryColor(string categoryName)
    {
        return categoryName switch
        {
            "Checking" => "#3b82f6",
            "Savings" => "#10b981",
            "Credit Cards" => "#8b5cf6",
            "Loans" => "#f59e0b",
            "Auto" => "#ef4444",
            "Health" => "#06b6d4",
            "Home" => "#84cc16",
            "Life" => "#ec4899",
            "Electric" => "#f59e0b",
            "Gas" => "#ef4444",
            "Water" => "#06b6d4",
            "Internet" => "#8b5cf6",
            _ => "#6b7280"
        };
    }
}
