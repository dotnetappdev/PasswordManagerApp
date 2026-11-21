using VaultGuard.Imports.Interfaces;
using VaultGuard.Models;
using VaultGuardImports.OnePassword.Models;
using System.IO.Compression;
using System.Text.Json;

namespace VaultGuardImports.OnePassword.Providers;

public class OnePasswordImportProvider : IPasswordImportProvider
{
    public string ProviderName => "1Password";
    public string DisplayName => "1Password Import (CSV or 1PUX)";
    public string Version => "1.0.0";
    public string[] SupportedFileExtensions => new[] { ".csv", ".1pux", ".1pu" };

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

    // Header names 1Password has used across versions (and that people produce by editing exports), lower-cased.
    private static readonly Dictionary<string, string[]> CsvColumns = new()
    {
        ["title"]    = new[] { "title", "name" },
        ["url"]      = new[] { "url", "urls", "website", "login_uri", "login url", "website address" },
        ["username"] = new[] { "username", "user name", "login", "login_username", "email" },
        ["password"] = new[] { "password", "login_password" },
        ["otp"]      = new[] { "otpauth", "otp", "totp", "one-time password", "one time password", "login_totp" },
        ["favorite"] = new[] { "favorite", "favourite" },
        ["archived"] = new[] { "archived" },
        ["tags"]     = new[] { "tags" },
        ["notes"]    = new[] { "notes", "notesplain", "note" },
        ["type"]     = new[] { "type", "category" },
    };

    private async Task<ImportResult> ImportFromCsvAsync(Stream fileStream, string fileName)
    {
        var result = new ImportResult();

        try
        {
            using var reader = new StreamReader(fileStream, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var csvContent = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(csvContent))
            {
                result.Success = false;
                result.ErrorMessage = "The CSV file is empty or contains no valid content.";
                return result;
            }

            var rows = ParseCsv(csvContent.TrimStart('\uFEFF'));
            if (rows.Count == 0)
            {
                result.Success = false;
                result.ErrorMessage = "The CSV file is empty.";
                return result;
            }

            // Map columns by header name, so any column order (and old/new 1Password layouts) works.
            var header = rows[0].Select(h => h.Trim().Trim('\uFEFF').ToLowerInvariant()).ToList();
            var col = new Dictionary<string, int>();
            foreach (var (key, names) in CsvColumns)
            {
                var idx = header.FindIndex(h => names.Contains(h));
                if (idx >= 0) col[key] = idx;
            }
            if (!col.ContainsKey("title") || !col.ContainsKey("password"))
            {
                result.Success = false;
                result.ErrorMessage = "This doesn't look like a 1Password CSV export: it needs at least Title and Password columns.";
                return result;
            }
            // Any other columns are kept as custom fields rather than dropped.
            var known = col.Values.ToHashSet();
            var extraColumns = Enumerable.Range(0, header.Count).Where(i => !known.Contains(i) && rows[0][i].Trim().Length > 0).ToList();

            var collectionsToCreate = new Dictionary<string, Collection>();
            var categoriesToCreate = new Dictionary<string, (Category Category, string CollectionName)>();
            int tempCollId = 1;
            int tempCatId = 1;

            foreach (var row in rows.Skip(1))
            {
                string Get(string key) => col.TryGetValue(key, out var i) && i < row.Count ? row[i] : string.Empty;
                var title = Get("title").Trim();
                var url = Get("url").Trim();
                var username = Get("username").Trim();
                var password = Get("password"); // exactly as exported — spaces can be part of a password
                if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(username) && string.IsNullOrEmpty(password))
                    continue; // blank line
                result.TotalItemsProcessed++;

                try
                {
                    if (string.IsNullOrWhiteSpace(title)) title = !string.IsNullOrWhiteSpace(url) ? ExtractDomain(url) : "Untitled";
                    var collectionName = DetermineCollection(url, title);
                    var categoryName = DetermineCategory(url, title);

                    if (!collectionsToCreate.ContainsKey(collectionName))
                    {
                        collectionsToCreate[collectionName] = new Collection
                        {
                            Id = tempCollId++,
                            Name = collectionName,
                            Icon = GetCollectionIcon(collectionName),
                            Color = GetCollectionColor(collectionName),
                            IsDefault = collectionName == "Banking"
                        };
                    }
                    var collection = collectionsToCreate[collectionName];

                    var categoryKey = $"{collectionName}:{categoryName}";
                    if (!categoriesToCreate.ContainsKey(categoryKey))
                    {
                        categoriesToCreate[categoryKey] = (new Category
                        {
                            Id = tempCatId++,
                            Name = categoryName,
                            CollectionId = collection.Id,
                            Icon = GetCategoryIcon(categoryName),
                            Color = GetCategoryColor(categoryName)
                        }, collectionName);
                    }
                    var category = categoriesToCreate[categoryKey].Category;

                    var notes = Get("notes");
                    var otp = Get("otp").Trim();
                    var type = MapCsvTypeToItemType(Get("type"));
                    var passwordItem = new PasswordItem
                    {
                        Title = title,
                        // A CSV row only carries login columns, so it's always stored with a login body.
                        Type = type is ItemType.Password ? ItemType.Password : ItemType.Login,
                        CollectionId = collection.Id,
                        CategoryId = category.Id,
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow,
                        LoginItem = new LoginItem
                        {
                            Website = url,
                            WebsiteUrl = url,
                            Username = username,
                            Password = password,
                            Email = IsEmail(username) ? username : null,
                            Notes = !string.IsNullOrWhiteSpace(notes) ? notes.Trim() : null,
                            TotpSecret = otp.Length > 0 ? otp : null
                        },
                        Tags = new List<Tag>(),
                        CustomFields = new List<CustomField>()
                    };

                    var order = 0;
                    foreach (var i in extraColumns)
                    {
                        var value = i < row.Count ? row[i] : string.Empty;
                        if (string.IsNullOrWhiteSpace(value)) continue;
                        var name = rows[0][i].Trim();
                        var secret = name.Contains("password", StringComparison.OrdinalIgnoreCase) || name.Contains("pin", StringComparison.OrdinalIgnoreCase)
                                     || name.Contains("secret", StringComparison.OrdinalIgnoreCase) || name.Contains("cvv", StringComparison.OrdinalIgnoreCase);
                        passwordItem.CustomFields.Add(new CustomField
                        {
                            Name = name, Value = value, DisplayOrder = order++,
                            Type = secret ? CustomFieldType.Password : CustomFieldType.Text, IsProtected = secret
                        });
                    }

                    passwordItem.Tags.Add(new Tag { Name = "Imported", Color = "#8b5cf6" });
                    if (IsTrue(Get("favorite"))) passwordItem.Tags.Add(new Tag { Name = "Favorite", Color = "#fbbf24" });
                    if (IsTrue(Get("archived"))) passwordItem.Tags.Add(new Tag { Name = "Archived", Color = "#6b7280" });
                    foreach (var tagName in Get("tags").Split(',', ';').Select(t => t.Trim()).Where(t => t.Length > 0))
                        passwordItem.Tags.Add(new Tag { Name = tagName, Color = "#a855f7" });
                    if (IsHighPriority(title, url))
                        passwordItem.Tags.Add(new Tag { Name = "High Priority", Color = "#ef4444" });

                    result.ImportedItems.Add(passwordItem);
                }
                catch (Exception ex)
                {
                    result.Warnings.Add($"Failed to process record '{title}': {ex.Message}");
                }
            }

            result.RequiredCollections.AddRange(collectionsToCreate.Values);
            result.RequiredCategories.AddRange(categoriesToCreate.Values.Select(c => c.Category));
            result.RequiredTags.Add(new Tag { Name = "Imported", Color = "#8b5cf6" });
            result.RequiredTags.Add(new Tag { Name = "High Priority", Color = "#ef4444" });
            result.RequiredTags.Add(new Tag { Name = "Favorite", Color = "#fbbf24" });
            result.RequiredTags.Add(new Tag { Name = "Archived", Color = "#6b7280" });

            // Leave SuccessfulImports/FailedImports at 0 — ImportService updates them during actual saves
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Failed to read the CSV file. Please make sure it's a 1Password export. Error: {ex.Message}";
        }

        return result;
    }

    private static bool IsTrue(string? v) =>
        !string.IsNullOrWhiteSpace(v) && (v.Trim().Equals("true", StringComparison.OrdinalIgnoreCase) || v.Trim() == "1" || v.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// RFC 4180 CSV: quoted fields may contain commas, line breaks and doubled quotes; CRLF or LF line endings.
    /// Values are returned exactly as written (no trimming), so passwords keep leading/trailing spaces.
    /// </summary>
    internal static List<List<string>> ParseCsv(string text)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new System.Text.StringBuilder();
        var inQuotes = false;
        var fieldStarted = false;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                    else inQuotes = false;
                }
                else field.Append(c);
                continue;
            }

            switch (c)
            {
                case '"' when !fieldStarted || field.Length == 0:
                    inQuotes = true; fieldStarted = true; break;
                case ',':
                    row.Add(field.ToString()); field.Clear(); fieldStarted = false; break;
                case '\r':
                    break;
                case '\n':
                    row.Add(field.ToString()); field.Clear(); fieldStarted = false;
                    if (row.Count > 1 || row[0].Length > 0) rows.Add(row);
                    row = new List<string>();
                    break;
                default:
                    field.Append(c); fieldStarted = true; break;
            }
        }
        if (fieldStarted || field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            if (row.Count > 1 || row[0].Length > 0) rows.Add(row);
        }
        return rows;
    }

    private static ItemType MapCsvTypeToItemType(string? csvType) =>
        csvType?.Trim().ToLowerInvariant() switch
        {
            "login"          => ItemType.Login,
            "credit card"    => ItemType.CreditCard,
            "secure note"    => ItemType.SecureNote,
            "password"       => ItemType.Password,
            "wifi" or "wireless router" => ItemType.WiFi,
            "passkey"        => ItemType.Passkey,
            _                => ItemType.Login
        };

    // 1PUX CategoryUuid values — verified against Bitwarden's open-source 1PUX importer
    private static ItemType MapPuxCategoryToItemType(string? categoryUuid) =>
        categoryUuid?.ToLowerInvariant() switch
        {
            "001" => ItemType.Login,
            "002" => ItemType.CreditCard,
            "003" => ItemType.SecureNote,
            "004" => ItemType.Identity,
            "005" => ItemType.Password,
            "006" => ItemType.Document,
            "100" => ItemType.SoftwareLicense,
            "101" => ItemType.BankAccount,        // Bank Account → treat as CreditCard-like
            "102" => ItemType.Database,
            "103" => ItemType.DriversLicense,
            "104" => ItemType.OutdoorLicense,
            "105" => ItemType.Membership,
            "106" => ItemType.Passport,
            "107" => ItemType.RewardsProgram,
            "108" => ItemType.SocialSecurityNumber,
            "109" => ItemType.WirelessRouter,
            "110" => ItemType.Server,
            "111" => ItemType.EmailAccount,
            "112" => ItemType.ApiCredentials,
            "113" => ItemType.MedicalRecord,
            "114" => ItemType.SshKey,
            _     => ItemType.Login
        };

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
        catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", logEx); return url; }
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

    private string GetVaultIcon(string vaultName)
    {
        // Use vault-specific icons or fallback to folder icon
        // Common vault names: Personal, Work, Shared, Family, etc.
        var vaultLower = vaultName.ToLowerInvariant();

        if (vaultLower.Contains("personal") || vaultLower.Contains("private"))
            return "\uE77B"; // People/Person
        if (vaultLower.Contains("work") || vaultLower.Contains("business"))
            return "\uE821"; // Briefcase
        if (vaultLower.Contains("family") || vaultLower.Contains("shared"))
            return "\uE716"; // People (group)
        if (vaultLower.Contains("finance") || vaultLower.Contains("bank"))
            return "\uE8C7"; // CreditCard

        return "\uE8B7"; // Folder (default)
    }

    private string GetVaultColor(string vaultName)
    {
        // Use vault-specific colors or fallback to neutral color
        var vaultLower = vaultName.ToLowerInvariant();

        if (vaultLower.Contains("personal") || vaultLower.Contains("private"))
            return "#3b82f6"; // Blue
        if (vaultLower.Contains("work") || vaultLower.Contains("business"))
            return "#8b5cf6"; // Purple
        if (vaultLower.Contains("family") || vaultLower.Contains("shared"))
            return "#10b981"; // Green
        if (vaultLower.Contains("finance") || vaultLower.Contains("bank"))
            return "#f59e0b"; // Orange

        return "#6b7280"; // Gray (default)
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
            // Temp IDs start at 1 so ImportService's > 0 check passes and dict keys are unique
            var collectionsToCreate = new Dictionary<string, Collection>();
            var categoriesToCreate = new Dictionary<string, (Category Category, string CollectionName)>();
            int tempCollId = 1;
            int tempCatId = 1;

            var totalItemsAttempted = 0;

            // Process all accounts and vaults
            foreach (var account in puxData.Accounts)
            {
                foreach (var vault in account.Vaults)
                {
                    // Use vault name as category (as per user requirement: vaults are categories)
                    var vaultName = !string.IsNullOrWhiteSpace(vault.Attrs.Name)
                        ? vault.Attrs.Name
                        : "Default";

                    foreach (var item in vault.Items)
                    {
                        totalItemsAttempted++;
                        try
                        {
                            // 1PUX writes explicit nulls for empty values; normalise so nothing below can trip on them.
                            item.Overview ??= new PuxOverview();
                            item.Overview.Title ??= string.Empty;
                            item.Overview.Url ??= string.Empty;
                            item.Overview.Tags ??= new List<string>();
                            item.Overview.Urls ??= new List<PuxUrlObject>();
                            item.Details ??= new PuxDetails();
                            item.Details.LoginFields ??= new List<PuxLoginField>();
                            item.Details.Sections ??= new List<PuxSection>();
                            item.Details.NotesPlain ??= string.Empty;
                            foreach (var sec in item.Details.Sections) { sec.Fields ??= new List<PuxField>(); foreach (var f in sec.Fields) f.Value ??= new PuxFieldValue(); }
                            if (string.IsNullOrWhiteSpace(item.Overview.Title)) item.Overview.Title = "Untitled";

                            // Skip archived items if desired (currently importing all)
                            // if (item.State == "archived") continue;

                            // 1Password stores URLs in an "urls" array; the singular "url" is often empty.
                            var itemUrl = !string.IsNullOrWhiteSpace(item.Overview.Url)
                                ? item.Overview.Url
                                : (item.Overview.Urls?.FirstOrDefault(u => !string.IsNullOrWhiteSpace(u.Url))?.Url ?? string.Empty);

                            // Determine collection based on URL/title analysis
                            var collectionName = DetermineCollection(itemUrl, item.Overview.Title);

                            // Use vault name as the category (1Password vaults map to categories)
                            var categoryName = vaultName;

                            // Ensure collection exists
                            if (!collectionsToCreate.ContainsKey(collectionName))
                            {
                                collectionsToCreate[collectionName] = new Collection
                                {
                                    Id = tempCollId++,
                                    Name = collectionName,
                                    Icon = GetCollectionIcon(collectionName),
                                    Color = GetCollectionColor(collectionName),
                                    IsDefault = collectionName == "Banking"
                                };
                            }

                            var collection = collectionsToCreate[collectionName];

                            // Ensure category exists (using vault name)
                            var categoryKey = $"{collectionName}:{categoryName}";
                            if (!categoriesToCreate.ContainsKey(categoryKey))
                            {
                                var newCategory = new Category
                                {
                                    Id = tempCatId++,
                                    Name = categoryName,
                                    CollectionId = collection.Id,
                                    Icon = GetVaultIcon(categoryName),
                                    Color = GetVaultColor(categoryName)
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
                                var v = loginField.Value ?? string.Empty;
                                if (loginField.Designation == "username")
                                    username = v;
                                else if (loginField.Designation == "password")
                                    password = v;
                                else if (loginField.FieldType == "OTP" || loginField.Type?.ToUpperInvariant() == "TOTP")
                                    totpSecret = v;
                            }
                            // Older exports sometimes leave out designations: fall back to the field's type/name.
                            if (string.IsNullOrEmpty(password))
                                password = item.Details.LoginFields.FirstOrDefault(f => f.FieldType == "P" || string.Equals(f.Name, "password", StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;
                            if (string.IsNullOrEmpty(username))
                                username = item.Details.LoginFields.FirstOrDefault(f => f.FieldType is "E" or "T" && f.Designation != "password"
                                    && (f.Name ?? "").ToLowerInvariant() is "username" or "email" or "login" or "user")?.Value ?? string.Empty;
                            // "Password" category items keep the password in details.password.
                            if (string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(item.Details.Password))
                                password = item.Details.Password;

                            // TOTP may also live in section fields (1Password stores it there for some items)
                            PuxField? totpSectionField = null;
                            if (string.IsNullOrWhiteSpace(totpSecret))
                            {
                                totpSectionField = item.Details.Sections
                                    .SelectMany(s => s.Fields)
                                    .FirstOrDefault(f => !string.IsNullOrWhiteSpace(f.Value.Totp));
                                if (totpSectionField != null)
                                    totpSecret = totpSectionField.Value.Totp;
                            }

                            var itemType = MapPuxCategoryToItemType(item.CategoryUuid);

                            // Build a flat lookup of all section fields by their id for easy access
                            var sectionFields = item.Details.Sections
                                .SelectMany(s => s.Fields)
                                .ToLookup(f => (f.Id ?? string.Empty).ToLowerInvariant());

                            string SectionVal(params string[] ids)
                            {
                                foreach (var id in ids)
                                {
                                    var match = sectionFields[id.ToLowerInvariant()].FirstOrDefault();
                                    if (match is not null)
                                    {
                                        var v = GetFieldValue(match.Value);
                                        if (!string.IsNullOrWhiteSpace(v)) return v;
                                    }
                                }
                                return string.Empty;
                            }

                            // Remap extended types to storage types the DB supports
                            if (itemType == ItemType.BankAccount)
                                itemType = ItemType.CreditCard;     // Bank Account → same fields as Credit Card
                            else if (itemType == ItemType.WirelessRouter)
                                itemType = ItemType.WiFi;
                            else if (itemType != ItemType.Login && itemType != ItemType.CreditCard &&
                                     itemType != ItemType.SecureNote && itemType != ItemType.Password &&
                                     itemType != ItemType.WiFi && itemType != ItemType.Passkey)
                            {
                                // Identity, Document, SoftwareLicense, Database, etc.
                                // — fall back to Login so section fields are captured as custom fields
                                itemType = ItemType.Login;
                            }

                            // Create the password item
                            var passwordItem = new PasswordItem
                            {
                                Title = item.Overview.Title?.Trim() ?? "Untitled",
                                Type = itemType,
                                CollectionId = collection.Id,
                                CategoryId = category.Id,
                                CreatedAt = DateTimeOffset.FromUnixTimeSeconds(item.CreatedAt).UtcDateTime,
                                LastModified = DateTimeOffset.FromUnixTimeSeconds(item.UpdatedAt).UtcDateTime,
                                Tags = new List<Tag>(),
                                CustomFields = new List<CustomField>()
                            };

                            if (itemType == ItemType.CreditCard)
                            {
                                var expiryFormatted = FormatPuxExpiry(SectionVal("expiry"));

                                passwordItem.CreditCardItem = new CreditCardItem
                                {
                                    CardholderName  = SectionVal("cardholder"),
                                    CardNumber      = SectionVal("ccnum"),
                                    ExpiryDate      = expiryFormatted,
                                    CVV             = SectionVal("cvv"),
                                    PIN             = SectionVal("pin"),
                                    CardType        = ParseCardType(SectionVal("type")),
                                    IssuingBank     = SectionVal("bank"),
                                    ValidFrom       = SectionVal("validfrom", "validFrom"),
                                    BankWebsite     = SectionVal("website"),
                                    BankPhoneNumber = SectionVal("phonetollfree", "phoneTollFree", "phonelocal", "phoneLocal"),
                                    Notes           = !string.IsNullOrWhiteSpace(item.Details.NotesPlain) ? item.Details.NotesPlain.Trim() : null,
                                };
                                // No LoginItem — would fail with null Username constraint.
                                // Notes stored directly in CreditCardItem.Notes above.
                            }
                            else if (itemType == ItemType.SecureNote)
                            {
                                // Build full note content: notesPlain first, then any section fields appended
                                var noteBuilder = new System.Text.StringBuilder();
                                if (!string.IsNullOrWhiteSpace(item.Details.NotesPlain))
                                    noteBuilder.Append(item.Details.NotesPlain.Trim());

                                foreach (var section in item.Details.Sections)
                                {
                                    foreach (var field in section.Fields)
                                    {
                                        var fv = GetFieldValue(field.Value);
                                        if (string.IsNullOrWhiteSpace(fv)) continue;
                                        var label = field.Title ?? field.Id ?? string.Empty;
                                        if (noteBuilder.Length > 0) noteBuilder.AppendLine();
                                        if (!string.IsNullOrWhiteSpace(label))
                                            noteBuilder.Append(label).Append(": ");
                                        noteBuilder.Append(fv);
                                    }
                                }

                                // Truncate to 5000 chars to stay within SecureNoteItem.Content MaxLength
                                var noteContent = noteBuilder.Length > 0 ? noteBuilder.ToString() : string.Empty;
                                if (noteContent.Length > 4900)
                                    noteContent = noteContent[..4900] + "\n[truncated]";

                                passwordItem.SecureNoteItem = new SecureNoteItem
                                {
                                    Title   = (item.Overview.Title?.Trim() ?? "Untitled")[..Math.Min(item.Overview.Title?.Trim().Length ?? 8, 100)],
                                    Content = noteContent.Length > 0 ? noteContent : "(empty)",
                                };
                                // No LoginItem — would fail with null Username constraint.
                            }
                            else if (itemType == ItemType.WiFi)
                            {
                                // 1Password "Wireless Router": base station (router admin) + the wireless network itself.
                                passwordItem.WiFiItem = new WiFiItem
                                {
                                    NetworkName    = FirstNonEmpty(SectionVal("network_name"), SectionVal("name"), passwordItem.Title),
                                    Password       = SectionVal("wireless_password"),
                                    RouterPassword = SectionVal("password"),
                                    RouterIP       = SectionVal("server"),
                                    SecurityType   = ParseWifiSecurity(SectionVal("wireless_security")),
                                    Notes          = !string.IsNullOrWhiteSpace(item.Details.NotesPlain) ? item.Details.NotesPlain.Trim() : null,
                                };
                            }
                            else
                            {
                                passwordItem.LoginItem = new LoginItem
                                {
                                    Website    = itemUrl.Trim(),
                                    WebsiteUrl = itemUrl.Trim(),
                                    Username   = username?.Trim() ?? string.Empty,
                                    Password   = password ?? string.Empty, // exactly as exported — spaces can be part of a password
                                    Email      = IsEmail(username?.Trim() ?? string.Empty) ? username!.Trim() : null,
                                    Notes      = !string.IsNullOrWhiteSpace(item.Details.NotesPlain) ? item.Details.NotesPlain.Trim() : null,
                                    TotpSecret = !string.IsNullOrWhiteSpace(totpSecret) ? totpSecret.Trim() : null
                                };
                            }

                            // Add remaining section fields as custom fields
                            // (skip known credit card fields already mapped to CreditCardItem,
                            //  and skip all fields for secure notes since they're in SecureNoteItem.Content)
                            var creditCardKnownIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                            {
                                "ccnum","expiry","cvv","pin","cardholder","type","bank",
                                "validfrom","validFrom","website","phonetollfree","phoneTollFree",
                                "phonelocal","phoneLocal","billingaddress"
                            };

                            var wifiKnownIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                            {
                                "network_name","wireless_password","password","server","wireless_security"
                            };

                            var displayOrder = 0;
                            foreach (var section in item.Details.Sections)
                            {
                                foreach (var field in section.Fields)
                                {
                                    // Secure note fields are already in SecureNoteItem.Content
                                    if (itemType == ItemType.SecureNote) continue;

                                    // Skip known credit card fields already in CreditCardItem
                                    if (itemType == ItemType.CreditCard && creditCardKnownIds.Contains(field.Id ?? string.Empty))
                                        continue;

                                    if (itemType == ItemType.WiFi && wifiKnownIds.Contains(field.Id ?? string.Empty))
                                        continue;

                                    // The authenticator is already on the login (TotpSecret); don't store it twice.
                                    if (ReferenceEquals(field, totpSectionField))
                                        continue;

                                    var fieldValue = GetFieldValue(field.Value);
                                    if (!string.IsNullOrWhiteSpace(fieldValue))
                                    {
                                        passwordItem.CustomFields.Add(new CustomField
                                        {
                                            Name = field.Title ?? field.Id ?? "Field",
                                            Value = fieldValue,
                                            Type = DetermineCustomFieldType(field.Value),
                                            IsProtected = !string.IsNullOrWhiteSpace(field.Value.Concealed),
                                            DisplayOrder = displayOrder++
                                        });
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
                            if (IsHighPriority(item.Overview.Title, itemUrl))
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

            // Leave SuccessfulImports/FailedImports at 0 — ImportService updates them during actual saves
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Failed to import 1PUX file: {ex.Message}";
        }

        return result;
    }

    // 1Password stores expiry as monthYear int (202612), Unix timestamp, or string "2027-04".
    // GetFieldValue already converts monthYear to "MM/YYYY"; this handles the remaining cases.
    private static string FormatPuxExpiry(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        // Already formatted as "MM/YYYY" by GetFieldValue (monthYear path)
        if (raw.Length == 7 && raw[2] == '/')
            return raw;

        // String like "2027-04" → "04/2027"
        if (raw.Length == 7 && raw[4] == '-')
            return raw[5..] + "/" + raw[..4];

        // Unix timestamp fallback
        if (long.TryParse(raw, out var unixSeconds) && unixSeconds > 0)
        {
            var dt = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
            return dt.ToString("MM/yyyy");
        }

        return raw;
    }

    private static string FirstNonEmpty(params string?[] values) => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;

    // 1Password menu values: "none", "wep", "wpa", "wpa2p" (personal), "wpa2e" (enterprise), "wpa3p", "wpa3e"
    private static SecurityType ParseWifiSecurity(string? raw)
    {
        var v = (raw ?? string.Empty).ToLowerInvariant();
        if (v.StartsWith("wpa3")) return SecurityType.WPA3;
        if (v.StartsWith("wpa2")) return SecurityType.WPA2;
        if (v.StartsWith("wpa")) return SecurityType.WPA;
        if (v.StartsWith("wep")) return SecurityType.WEP;
        if (v is "none" or "open") return SecurityType.Open;
        return SecurityType.WPA2;
    }

    private static CardType ParseCardType(string? rawType)
    {
        return rawType?.ToLowerInvariant() switch
        {
            "mc" or "mastercard" or "master card" => CardType.MasterCard,
            "visa"                                 => CardType.Visa,
            "amex" or "american express"           => CardType.AmericanExpress,
            "discover"                             => CardType.Discover,
            "diners" or "diners club"              => CardType.DinersClub,
            "jcb"                                  => CardType.JCB,
            _                                      => CardType.Other
        };
    }

    private string GetFieldValue(PuxFieldValue fieldValue)
    {
        if (!string.IsNullOrWhiteSpace(fieldValue.Totp))
            return fieldValue.Totp;
        if (!string.IsNullOrWhiteSpace(fieldValue.Concealed))
            return fieldValue.Concealed;
        if (!string.IsNullOrWhiteSpace(fieldValue.CreditCardNumber))
            return fieldValue.CreditCardNumber;
        if (!string.IsNullOrWhiteSpace(fieldValue.String))
            return fieldValue.String;
        if (!string.IsNullOrWhiteSpace(fieldValue.CreditCardType))
            return fieldValue.CreditCardType;
        if (!string.IsNullOrWhiteSpace(fieldValue.Menu))
            return fieldValue.Menu;
        if (!string.IsNullOrWhiteSpace(fieldValue.Gender))
            return fieldValue.Gender;
        if (!string.IsNullOrWhiteSpace(fieldValue.Email))
            return fieldValue.Email;
        if (!string.IsNullOrWhiteSpace(fieldValue.Phone))
            return fieldValue.Phone;
        if (!string.IsNullOrWhiteSpace(fieldValue.Url))
            return fieldValue.Url;
        if (!string.IsNullOrWhiteSpace(fieldValue.Address))
            return fieldValue.Address;
        if (!string.IsNullOrWhiteSpace(fieldValue.Reference))
            return fieldValue.Reference;
        if (fieldValue.MonthYear > 0)
        {
            // monthYear is YYYYMM integer, e.g. 202612 → "12/2026"
            var year = fieldValue.MonthYear / 100;
            var month = fieldValue.MonthYear % 100;
            return $"{month:D2}/{year}";
        }
        if (fieldValue.Date > 0)
            return DateTimeOffset.FromUnixTimeSeconds(fieldValue.Date).ToString("yyyy-MM-dd");

        return string.Empty;
    }

    private CustomFieldType DetermineCustomFieldType(PuxFieldValue fieldValue)
    {
        if (!string.IsNullOrWhiteSpace(fieldValue.Totp))
            return CustomFieldType.Password;
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
