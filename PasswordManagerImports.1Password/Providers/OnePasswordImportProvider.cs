using FileHelpers;
using PasswordManager.Imports.Interfaces;
using PasswordManager.Models;
using PasswordManagerImports.OnePassword.Models;
using System.IO.Compression;
using System.Text.Json;

namespace PasswordManagerImports.OnePassword.Providers;

public class OnePasswordImportProvider : IPasswordImportProvider
{
    public string ProviderName => "1Password";
    public string DisplayName => "1Password Import (CSV or 1PUX)";
    public string Version => "1.0.0";
    public string[] SupportedFileExtensions => new[] { ".csv", ".1pux" };

    public async Task<ImportResult> ImportFromFileAsync(Stream fileStream, string fileName)
    {
        var result = new ImportResult();

        try
        {
            // Determine format based on file extension
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            if (extension == ".1pux")
            {
                return await ImportFrom1PuxAsync(fileStream, fileName);
            }
            else // .csv
            {
                return await ImportFromCsvAsync(fileStream, fileName);
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    private async Task<ImportResult> ImportFromCsvAsync(Stream fileStream, string fileName)
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
            var categoriesToCreate = new Dictionary<string, (Category Category, string CollectionName)>();

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
                        var newCategory = new Category
                        {
                            Name = categoryName,
                            Icon = GetCategoryIcon(categoryName),
                            Color = GetCategoryColor(categoryName)
                            // CollectionId will be set after collection is created by ImportService
                        };
                        categoriesToCreate[categoryKey] = (newCategory, collectionName);
                    }

                    var category = categoriesToCreate[categoryKey].Category;

                    // Create the password item with notes properly set
                    var passwordItem = new PasswordItem
                    {
                        Title = record.Title?.Trim() ?? string.Empty,
                        Type = ItemType.Login,
                        CollectionId = collection.Id, // Will be updated after collection creation
                        CategoryId = category.Id, // Will be updated after category creation
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow,
                        LoginItem = new LoginItem
                        {
                            Website = record.Url?.Trim() ?? string.Empty,
                            WebsiteUrl = record.Url?.Trim() ?? string.Empty,
                            Username = record.Username?.Trim() ?? string.Empty,
                            Password = record.Password?.Trim() ?? string.Empty, // NotMapped - will be encrypted by service
                            Email = IsEmail(record.Username?.Trim() ?? string.Empty) ? record.Username.Trim() : null,
                            Notes = !string.IsNullOrWhiteSpace(record.Notes) ? record.Notes.Trim() : null, // NotMapped - will be encrypted by service
                            TotpSecret = !string.IsNullOrWhiteSpace(record.OTPAuth) ? record.OTPAuth.Trim() : null // NotMapped - will be encrypted by service
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
            result.RequiredCategories.AddRange(categoriesToCreate.Values.Select(c => c.Category));

            // Add standard tags
            result.RequiredTags.Add(new Tag { Name = "Imported", Color = "#8b5cf6" });
            result.RequiredTags.Add(new Tag { Name = "High Priority", Color = "#ef4444" });
            result.RequiredTags.Add(new Tag { Name = "Favorite", Color = "#fbbf24" });
            result.RequiredTags.Add(new Tag { Name = "Archived", Color = "#6b7280" });

            // Set success and failure counts
            result.SuccessfulImports = result.ImportedItems.Count;
            result.FailedImports = result.TotalItemsProcessed - result.SuccessfulImports;

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
        // Using Segoe MDL2 Assets glyphs for system icons
        return collectionName switch
        {
            "Banking" => "\uE8C7",      // CreditCard
            "Insurance" => "\uE72F",    // Lock (security/protection)
            "Utilities" => "\uE945",    // Flashlight/Lightning
            "General" => "\uE8B7",      // Folder
            _ => "\uE8B7"                // Folder (default)
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
        // Using Segoe MDL2 Assets glyphs for system icons
        return categoryName switch
        {
            "Checking" => "\uE8C7",      // CreditCard
            "Savings" => "\uE7EF",       // Money
            "Credit Cards" => "\uE8C7",  // CreditCard
            "Loans" => "\uE8A5",         // List
            "Auto" => "\uE804",          // Car
            "Health" => "\uE95E",        // Health
            "Home" => "\uE80F",          // Home
            "Life" => "\uE77B",          // People
            "Electric" => "\uE945",      // Flashlight/Lightning
            "Gas" => "\uE7C1",           // Flame
            "Water" => "\uE7F0",         // Emoji2
            "Internet" => "\uE701",      // Globe
            "General" => "\uE8B7",       // Folder
            _ => "\uE8B7"                 // Folder (default)
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

    private async Task<ImportResult> ImportFrom1PuxAsync(Stream fileStream, string fileName)
    {
        var result = new ImportResult();

        try
        {
            // 1PUX is a ZIP archive containing export.data JSON file
            using var archive = new System.IO.Compression.ZipArchive(fileStream, System.IO.Compression.ZipArchiveMode.Read);

            // Find the export.data entry
            var dataEntry = archive.Entries.FirstOrDefault(e => e.Name == "export.data");
            if (dataEntry == null)
            {
                result.Success = false;
                result.ErrorMessage = "Invalid 1PUX file: export.data not found.";
                return result;
            }

            // Read and parse the JSON
            using var dataStream = dataEntry.Open();

            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
            };

            var puxData = await System.Text.Json.JsonSerializer.DeserializeAsync<OnePasswordPuxExport>(dataStream, options);

            if (puxData == null || !puxData.Accounts.Any())
            {
                result.Success = false;
                result.ErrorMessage = "No accounts found in 1PUX file.";
                return result;
            }

            // Track collections and categories we need to create
            var collectionsToCreate = new Dictionary<string, Collection>();
            var categoriesToCreate = new Dictionary<string, (Category Category, string CollectionName)>();
            
            var totalItemsAttempted = 0;

            // Process all accounts and vaults
            foreach (var account in puxData.Accounts)
            {
                foreach (var vault in account.Vaults)
                {
                    foreach (var item in vault.Items)
                    {
                        totalItemsAttempted++;
                        try
                        {
                            // Skip archived items if desired (currently importing all)
                            // if (item.State == "archived") continue;

                            // Determine collection and category
                            var collectionName = DetermineCollection(item.Overview.Url, item.Overview.Title);
                            var categoryName = DetermineCategory(item.Overview.Url, item.Overview.Title);

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
                                var newCategory = new Category
                                {
                                    Name = categoryName,
                                    Icon = GetCategoryIcon(categoryName),
                                    Color = GetCategoryColor(categoryName)
                                };
                                categoriesToCreate[categoryKey] = (newCategory, collectionName);
                            }

                            var category = categoriesToCreate[categoryKey].Category;

                            // Extract username and password from login fields
                            var username = string.Empty;
                            var password = string.Empty;
                            var totpSecret = string.Empty;

                            foreach (var loginField in item.Details.LoginFields)
                            {
                                if (loginField.Designation == "username")
                                {
                                    username = loginField.Value;
                                }
                                else if (loginField.Designation == "password")
                                {
                                    password = loginField.Value;
                                }
                                // Check for TOTP in field type
                                else if (loginField.FieldType == "OTP" || loginField.Type?.ToUpperInvariant() == "TOTP")
                                {
                                    totpSecret = loginField.Value;
                                }
                            }

                            // Create the password item
                            var passwordItem = new PasswordItem
                            {
                                Title = item.Overview.Title?.Trim() ?? "Untitled",
                                Type = ItemType.Login,
                                CollectionId = collection.Id,
                                CategoryId = category.Id,
                                CreatedAt = DateTimeOffset.FromUnixTimeSeconds(item.CreatedAt).UtcDateTime,
                                LastModified = DateTimeOffset.FromUnixTimeSeconds(item.UpdatedAt).UtcDateTime,
                                LoginItem = new LoginItem
                                {
                                    Website = item.Overview.Url?.Trim() ?? string.Empty,
                                    WebsiteUrl = item.Overview.Url?.Trim() ?? string.Empty,
                                    Username = username?.Trim() ?? string.Empty,
                                    Password = password?.Trim() ?? string.Empty,
                                    Email = IsEmail(username?.Trim() ?? string.Empty) ? username.Trim() : null,
                                    Notes = !string.IsNullOrWhiteSpace(item.Details.NotesPlain) ? item.Details.NotesPlain.Trim() : null,
                                    TotpSecret = !string.IsNullOrWhiteSpace(totpSecret) ? totpSecret.Trim() : null
                                },
                                Tags = new List<Tag>(),
                                CustomFields = new List<CustomField>()
                            };

                            // Add custom fields from sections
                            var displayOrder = 0;
                            foreach (var section in item.Details.Sections)
                            {
                                foreach (var field in section.Fields)
                                {
                                    var fieldValue = GetFieldValue(field.Value);
                                    if (!string.IsNullOrWhiteSpace(fieldValue))
                                    {
                                        var customField = new CustomField
                                        {
                                            Name = field.Title ?? field.Id ?? "Field",
                                            Value = fieldValue,
                                            Type = DetermineCustomFieldType(field.Value),
                                            IsProtected = !string.IsNullOrWhiteSpace(field.Value.Concealed),
                                            DisplayOrder = displayOrder++
                                        };
                                        passwordItem.CustomFields.Add(customField);
                                    }
                                }
                            }

                            // Add import tag
                            passwordItem.Tags.Add(new Tag { Name = "Imported", Color = "#8b5cf6" });

                            // Handle favorite items
                            if (item.FavIndex > 0)
                            {
                                passwordItem.Tags.Add(new Tag { Name = "Favorite", Color = "#fbbf24" });
                            }

                            // Handle archived items
                            if (item.State == "archived")
                            {
                                passwordItem.Tags.Add(new Tag { Name = "Archived", Color = "#6b7280" });
                            }

                            // Handle custom tags
                            foreach (var tagName in item.Overview.Tags)
                            {
                                if (!string.IsNullOrWhiteSpace(tagName))
                                {
                                    passwordItem.Tags.Add(new Tag { Name = tagName.Trim(), Color = "#a855f7" });
                                }
                            }

                            // Add high priority tag if applicable
                            if (IsHighPriority(item.Overview.Title, item.Overview.Url))
                            {
                                passwordItem.Tags.Add(new Tag { Name = "High Priority", Color = "#ef4444" });
                            }

                            result.ImportedItems.Add(passwordItem);
                        }
                        catch (Exception ex)
                        {
                            result.Warnings.Add($"Failed to process item '{item.Overview.Title}': {ex.Message}");
                        }
                    }
                }
            }

            result.TotalItemsProcessed = totalItemsAttempted;

            // Add required collections, categories, and tags to result
            result.RequiredCollections.AddRange(collectionsToCreate.Values);
            result.RequiredCategories.AddRange(categoriesToCreate.Values.Select(c => c.Category));

            // Add standard tags
            result.RequiredTags.Add(new Tag { Name = "Imported", Color = "#8b5cf6" });
            result.RequiredTags.Add(new Tag { Name = "High Priority", Color = "#ef4444" });
            result.RequiredTags.Add(new Tag { Name = "Favorite", Color = "#fbbf24" });
            result.RequiredTags.Add(new Tag { Name = "Archived", Color = "#6b7280" });

            // Set success and failure counts
            result.SuccessfulImports = result.ImportedItems.Count;
            result.FailedImports = result.TotalItemsProcessed - result.SuccessfulImports;

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Failed to import 1PUX file: {ex.Message}";
        }

        return result;
    }

    private string GetFieldValue(PuxFieldValue fieldValue)
    {
        if (!string.IsNullOrWhiteSpace(fieldValue.Concealed))
            return fieldValue.Concealed;
        if (!string.IsNullOrWhiteSpace(fieldValue.String))
            return fieldValue.String;
        if (!string.IsNullOrWhiteSpace(fieldValue.Email))
            return fieldValue.Email;
        if (!string.IsNullOrWhiteSpace(fieldValue.Phone))
            return fieldValue.Phone;
        if (!string.IsNullOrWhiteSpace(fieldValue.Url))
            return fieldValue.Url;
        if (fieldValue.Date > 0)
            return DateTimeOffset.FromUnixTimeSeconds(fieldValue.Date).ToString("yyyy-MM-dd");

        return string.Empty;
    }

    private CustomFieldType DetermineCustomFieldType(PuxFieldValue fieldValue)
    {
        if (!string.IsNullOrWhiteSpace(fieldValue.Concealed))
            return CustomFieldType.Password;
        if (!string.IsNullOrWhiteSpace(fieldValue.Email))
            return CustomFieldType.Email;
        if (!string.IsNullOrWhiteSpace(fieldValue.Phone))
            return CustomFieldType.Phone;
        if (!string.IsNullOrWhiteSpace(fieldValue.Url))
            return CustomFieldType.Url;
        if (fieldValue.Date > 0)
            return CustomFieldType.Date;

        return CustomFieldType.Text;
    }

    /// <summary>
    /// Sets the import result statistics (success/failure counts)
    /// </summary>
    private void SetImportResultStatistics(ImportResult result)
    {
        result.SuccessfulImports = result.ImportedItems.Count;
        result.FailedImports = result.TotalItemsProcessed - result.SuccessfulImports;
    }
}
