using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using VaultGuard.Services.Interfaces;
using VaultGuard.DAL;
using VaultGuard.Models.DTOs;
using VaultGuard.Models;
using VaultGuard.Crypto.Interfaces;

namespace VaultGuard.Services.Services;

/// <summary>
/// Service for creating and restoring database backups
/// </summary>
public class DatabaseBackupService : IDatabaseBackupService
{
    private readonly ILogger<DatabaseBackupService> _logger;
    private readonly IDatabaseContextFactory _contextFactory;
    private readonly IPasswordEncryptionService _encryptionService;
    private readonly IBackupEncryptionService _backupEncryption;
    private readonly IAuthService? _authService;

    public DatabaseBackupService(
        ILogger<DatabaseBackupService> logger,
        IDatabaseContextFactory contextFactory,
        IPasswordEncryptionService encryptionService,
        IBackupEncryptionService backupEncryption,
        IAuthService? authService = null)
    {
        _logger = logger;
        _contextFactory = contextFactory;
        _encryptionService = encryptionService;
        _backupEncryption = backupEncryption;
        _authService = authService;
    }

    // Finds (or creates) the vault + its default collection for the given name, so a restored item
    // lands back in the vault it came from instead of wherever its old, possibly-stale CollectionId
    // happens to point. Returns null if there's no vault name to resolve or no signed-in user.
    private async Task<int?> ResolveCollectionIdForVaultNameAsync(VaultGuard.DAL.Interfaces.IVaultGuardDbContext context, string? vaultName)
    {
        if (string.IsNullOrWhiteSpace(vaultName)) return null;
        var userId = _authService?.CurrentUser?.Id;
        if (string.IsNullOrEmpty(userId)) return null;

        var vault = await context.Vaults.FirstOrDefaultAsync(v => v.UserId == userId && v.Name == vaultName);
        if (vault == null)
        {
            vault = new Vault
            {
                Name = vaultName,
                Description = $"Restored from backup",
                Icon = "🔐",
                Color = "#2563EB",
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Vaults.Add(vault);
            await context.SaveChangesAsync();
        }

        var collection = await context.Collections.FirstOrDefaultAsync(c => c.VaultId == vault.Id && c.IsDefault)
                       ?? await context.Collections.FirstOrDefaultAsync(c => c.VaultId == vault.Id);
        if (collection == null)
        {
            collection = new Collection
            {
                Name = vault.Name,
                Description = vault.Description,
                Icon = vault.Icon,
                Color = vault.Color,
                IsDefault = true,
                VaultId = vault.Id,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
            context.Collections.Add(collection);
            await context.SaveChangesAsync();
        }

        return collection.Id;
    }

    public async Task<DatabaseBackupResult> CreateBackupAsync(string encryptionKey, bool compress = true)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            
            // Create backup data
            var backupData = new
            {
                metadata = new BackupMetadata
                {
                    CreatedAt = DateTime.UtcNow,
                    DatabaseVersion = "1.0", // Version from database schema
                    ApplicationVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
                    IsCompressed = compress,
                    PasswordCount = await context.PasswordItems.CountAsync()
                },
                passwordItems = await context.PasswordItems.ToListAsync(),
                categories = await context.Categories.ToListAsync(),
                tags = await context.Tags.ToListAsync(),
                vaults = await context.Vaults.ToListAsync(),
                collections = await context.Collections.ToListAsync(),
                customFields = await context.CustomFields.ToListAsync(),
                loginItems = await context.LoginItems.ToListAsync(),
                secureNoteItems = await context.SecureNoteItems.ToListAsync(),
                creditCardItems = await context.CreditCardItems.ToListAsync(),
                wifiItems = await context.WiFiItems.ToListAsync(),
                userPasskeys = await context.UserPasskeys.ToListAsync()
            };

            // Serialize to JSON
            var jsonData = JsonSerializer.Serialize(backupData);
            var jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonData);

            // Compress if requested
            byte[] finalData;
            if (compress)
            {
                using var compressedStream = new MemoryStream();
                using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Compress))
                {
                    await gzipStream.WriteAsync(jsonBytes);
                }
                finalData = compressedStream.ToArray();
            }
            else
            {
                finalData = jsonBytes;
            }

            var encryptedData = string.IsNullOrEmpty(encryptionKey)
                ? finalData
                : _backupEncryption.Encrypt(finalData, encryptionKey);

            return new DatabaseBackupResult
            {
                Success = true,
                BackupData = encryptedData,
                Metadata = backupData.metadata
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create database backup");
            return new DatabaseBackupResult
            {
                Success = false,
                ErrorMessage = $"Failed to create backup: {ex.Message}"
            };
        }
    }

    public async Task<bool> RestoreBackupAsync(byte[] backupData, string encryptionKey)
    {
        try
        {
            var decryptedData = string.IsNullOrEmpty(encryptionKey)
                ? backupData
                : _backupEncryption.Decrypt(backupData, encryptionKey);

            // Check if compressed and decompress
            var jsonData = await DecompressIfNeeded(decryptedData);

            // Deserialize backup data
            var backupObject = JsonSerializer.Deserialize<JsonElement>(jsonData);
            
            // Validate backup structure
            if (!backupObject.TryGetProperty("metadata", out var metadataElement))
            {
                throw new InvalidOperationException("Invalid backup format: missing metadata");
            }

            // TODO: Implement full restore logic
            // This would need careful transaction handling to replace database contents
            _logger.LogInformation("Backup restore would be implemented here");
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore database backup");
            return false;
        }
    }

    public async Task<BackupContentsDto?> BrowseBackupAsync(byte[] encryptedData, string masterPassword)
    {
        try
        {
            var decrypted = string.IsNullOrEmpty(masterPassword)
                ? encryptedData
                : _backupEncryption.Decrypt(encryptedData, masterPassword);
            var json = await DecompressIfNeeded(decrypted);
            var root = JsonSerializer.Deserialize<JsonElement>(json);

            var contents = new BackupContentsDto
            {
                CreatedAt = root.TryGetProperty("metadata", out var meta) && meta.TryGetProperty("CreatedAt", out var ts)
                    ? ts.GetDateTime()
                    : DateTime.UtcNow
            };

            // Build CollectionId -> VaultName so each item can remember which vault it came from,
            // even though PasswordItem itself only stores a CollectionId.
            var collectionToVaultName = BuildCollectionToVaultNameMap(root);

            if (root.TryGetProperty("loginItems", out var logins))
            {
                foreach (var el in logins.EnumerateArray())
                    contents.LoginItems.Add(MapElement(el, "Login", collectionToVaultName));
            }
            if (root.TryGetProperty("secureNoteItems", out var notes))
            {
                foreach (var el in notes.EnumerateArray())
                    contents.SecureNotes.Add(MapElement(el, "Secure Note", collectionToVaultName));
            }
            if (root.TryGetProperty("creditCardItems", out var cards))
            {
                foreach (var el in cards.EnumerateArray())
                    contents.CreditCards.Add(MapElement(el, "Credit Card", collectionToVaultName));
            }
            if (root.TryGetProperty("wifiItems", out var wifi))
            {
                foreach (var el in wifi.EnumerateArray())
                    contents.WifiItems.Add(MapElement(el, "Wi-Fi", collectionToVaultName));
            }
            // Also pull top-level passwordItems that may not have sub-type rows
            if (root.TryGetProperty("passwordItems", out var passwords))
            {
                foreach (var el in passwords.EnumerateArray())
                {
                    // Only add if not already represented via a sub-type above
                    var idProp = el.TryGetProperty("Id", out var idEl);
                    var id = idProp ? (idEl.ValueKind == JsonValueKind.Number ? idEl.GetInt32().ToString() : (idEl.GetString() ?? "")) : "";
                    bool exists = contents.LoginItems.Any(x => x.Id == id)
                               || contents.SecureNotes.Any(x => x.Id == id)
                               || contents.CreditCards.Any(x => x.Id == id)
                               || contents.WifiItems.Any(x => x.Id == id);
                    if (!exists)
                        contents.LoginItems.Add(MapElement(el, "Login", collectionToVaultName));
                }
            }
            return contents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to browse backup");
            return null;
        }
    }

    // Maps Collection.Id -> the name of the vault it belongs to, by cross-referencing the backup's
    // "collections" (which carry VaultId) against its "vaults" (which carry the vault Name).
    private static Dictionary<int, string> BuildCollectionToVaultNameMap(JsonElement root)
    {
        var map = new Dictionary<int, string>();
        try
        {
            if (!root.TryGetProperty("vaults", out var vaultsEl) || !root.TryGetProperty("collections", out var collectionsEl))
                return map;

            var vaultNameById = new Dictionary<int, string>();
            foreach (var v in vaultsEl.EnumerateArray())
            {
                if (v.TryGetProperty("Id", out var vid) && v.TryGetProperty("Name", out var vname))
                    vaultNameById[vid.GetInt32()] = vname.GetString() ?? "";
            }

            foreach (var c in collectionsEl.EnumerateArray())
            {
                if (!c.TryGetProperty("Id", out var cid)) continue;
                if (c.TryGetProperty("VaultId", out var vIdEl) && vIdEl.ValueKind == JsonValueKind.Number
                    && vaultNameById.TryGetValue(vIdEl.GetInt32(), out var vaultName) && !string.IsNullOrWhiteSpace(vaultName))
                {
                    map[cid.GetInt32()] = vaultName;
                }
            }
        }
        catch { /* best-effort — items just won't carry a vault name */ }
        return map;
    }

    private static BackupItemDto MapElement(JsonElement el, string itemType, Dictionary<int, string>? collectionToVaultName = null)
    {
        // Id is int in the DB — use it as string for the DTO key
        string id;
        if (el.TryGetProperty("Id", out var idEl))
            id = idEl.ValueKind == JsonValueKind.Number ? idEl.GetInt32().ToString() : (idEl.GetString() ?? Guid.NewGuid().ToString());
        else
            id = Guid.NewGuid().ToString();

        var title = el.TryGetProperty("Title", out var t) ? t.GetString() ?? "(untitled)" : "(untitled)";
        string? subtitle = null;
        if (el.TryGetProperty("Username", out var u)) subtitle = u.GetString();
        else if (el.TryGetProperty("CardHolderName", out var ch)) subtitle = ch.GetString();
        else if (el.TryGetProperty("NetworkName", out var nn)) subtitle = nn.GetString();
        DateTime created = el.TryGetProperty("CreatedAt", out var c) ? c.GetDateTime() : DateTime.UtcNow;

        string? vaultName = null;
        if (collectionToVaultName != null
            && el.TryGetProperty("CollectionId", out var collIdEl)
            && collIdEl.ValueKind == JsonValueKind.Number)
        {
            collectionToVaultName.TryGetValue(collIdEl.GetInt32(), out vaultName);
        }

        return new BackupItemDto
        {
            Id = id, Title = title, Subtitle = subtitle,
            ItemType = itemType, CreatedAt = created,
            IsSelected = true, RawJson = el.GetRawText(),
            VaultName = vaultName
        };
    }

    public async Task<bool> RestoreFullAsync(byte[] encryptedData, string masterPassword)
    {
        try
        {
            var decrypted = string.IsNullOrEmpty(masterPassword)
                ? encryptedData
                : _backupEncryption.Decrypt(encryptedData, masterPassword);
            var json = await DecompressIfNeeded(decrypted);
            var root = JsonSerializer.Deserialize<JsonElement>(json);

            using var context = _contextFactory.CreateDbContext();

            // Clear existing data in dependency order
            context.CustomFields.RemoveRange(context.CustomFields);
            context.LoginItems.RemoveRange(context.LoginItems);
            context.SecureNoteItems.RemoveRange(context.SecureNoteItems);
            context.CreditCardItems.RemoveRange(context.CreditCardItems);
            context.WiFiItems.RemoveRange(context.WiFiItems);
            context.PasswordItems.RemoveRange(context.PasswordItems);
            await context.SaveChangesAsync();

            // Re-insert password items (with sub-types inline)
            if (root.TryGetProperty("passwordItems", out var items))
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var list = JsonSerializer.Deserialize<List<PasswordItem>>(items.GetRawText(), opts);
                if (list != null)
                {
                    // RestoreFullAsync wipes items but leaves Collections/Vaults untouched, so an item's
                    // old CollectionId usually still resolves correctly. But when restoring into a fresh
                    // database (or one where those collections were since deleted), re-home the item by
                    // vault name instead of leaving it pointing at a row that no longer exists.
                    var collectionToVaultName = BuildCollectionToVaultNameMap(root);
                    var existingCollectionIds = (await context.Collections.Select(c => c.Id).ToListAsync()).ToHashSet();
                    var resolvedByVaultName = new Dictionary<string, int>();

                    foreach (var pi in list)
                    {
                        if (pi.CollectionId.HasValue && existingCollectionIds.Contains(pi.CollectionId.Value))
                            continue; // still valid, leave as-is

                        var vaultName = pi.CollectionId.HasValue && collectionToVaultName.TryGetValue(pi.CollectionId.Value, out var vn)
                            ? vn
                            : null;
                        if (string.IsNullOrWhiteSpace(vaultName)) continue;

                        if (!resolvedByVaultName.TryGetValue(vaultName, out var newCollectionId))
                        {
                            var resolved = await ResolveCollectionIdForVaultNameAsync(context, vaultName);
                            if (!resolved.HasValue) continue;
                            newCollectionId = resolved.Value;
                            resolvedByVaultName[vaultName] = newCollectionId;
                            existingCollectionIds.Add(newCollectionId);
                        }
                        pi.CollectionId = newCollectionId;
                    }

                    context.PasswordItems.AddRange(list);
                }
            }
            if (root.TryGetProperty("loginItems", out var li))
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var list = JsonSerializer.Deserialize<List<LoginItem>>(li.GetRawText(), opts);
                if (list != null) context.LoginItems.AddRange(list);
            }
            if (root.TryGetProperty("secureNoteItems", out var sn))
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var list = JsonSerializer.Deserialize<List<SecureNoteItem>>(sn.GetRawText(), opts);
                if (list != null) context.SecureNoteItems.AddRange(list);
            }
            if (root.TryGetProperty("creditCardItems", out var cc))
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var list = JsonSerializer.Deserialize<List<CreditCardItem>>(cc.GetRawText(), opts);
                if (list != null) context.CreditCardItems.AddRange(list);
            }
            if (root.TryGetProperty("wifiItems", out var wi))
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var list = JsonSerializer.Deserialize<List<WiFiItem>>(wi.GetRawText(), opts);
                if (list != null) context.WiFiItems.AddRange(list);
            }

            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RestoreFullAsync failed");
            return false;
        }
    }

    public async Task<int> ImportSelectedItemsAsync(BackupContentsDto contents, IEnumerable<string> selectedIds)
    {
        try
        {
            var idSet = new HashSet<string>(selectedIds);
            var selected = contents.LoginItems.Concat(contents.SecureNotes)
                                              .Concat(contents.CreditCards)
                                              .Concat(contents.WifiItems)
                                              .Where(x => idSet.Contains(x.Id))
                                              .ToList();
            if (!selected.Any()) return 0;

            using var context = _contextFactory.CreateDbContext();
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            int count = 0;

            foreach (var item in selected)
            {
                try
                {
                    var pi = JsonSerializer.Deserialize<PasswordItem>(item.RawJson, opts);
                    if (pi == null) continue;
                    pi.Id = 0; // let EF assign a new PK
                    pi.Title = $"{pi.Title} (restored)";

                    // Put it back in the vault it came from (creating the vault if it no longer
                    // exists) rather than trusting its old CollectionId, which may point at nothing.
                    var resolvedCollectionId = await ResolveCollectionIdForVaultNameAsync(context, item.VaultName);
                    if (resolvedCollectionId.HasValue)
                        pi.CollectionId = resolvedCollectionId.Value;

                    context.PasswordItems.Add(pi);
                    count++;
                }
                catch { /* skip malformed items */ }
            }

            await context.SaveChangesAsync();
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ImportSelectedItemsAsync failed");
            return 0;
        }
    }

    public async Task<BrowserExportResult> ExportToBrowserAsync(BrowserExportFormat format)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var passwordItems = await context.PasswordItems.Include(p => p.LoginItem).ToListAsync();

            var exportData = format switch
            {
                BrowserExportFormat.Chrome => await ExportToChromeFormat(passwordItems),
                BrowserExportFormat.Edge => await ExportToEdgeFormat(passwordItems),
                BrowserExportFormat.Firefox => await ExportToFirefoxFormat(passwordItems),
                _ => throw new ArgumentException($"Unsupported browser format: {format}")
            };

            return new BrowserExportResult
            {
                Success = true,
                ExportData = exportData,
                ExportedCount = passwordItems.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export to browser format {Format}", format);
            return new BrowserExportResult
            {
                Success = false,
                ErrorMessage = $"Export failed: {ex.Message}"
            };
        }
    }

    public async Task<BackupMetadata?> GetBackupMetadataAsync(byte[] backupData)
    {
        try
        {
            // For now, we'll need to decrypt and parse to get metadata
            // In a real implementation, we might store metadata separately
            _logger.LogInformation("Backup metadata extraction would be implemented here");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract backup metadata");
            return null;
        }
    }

    private async Task<string> DecompressIfNeeded(byte[] data)
    {
        try
        {
            // Try to decompress first
            using var compressedStream = new MemoryStream(data);
            using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var resultStream = new MemoryStream();
            await gzipStream.CopyToAsync(resultStream);
            return System.Text.Encoding.UTF8.GetString(resultStream.ToArray());
        }
        catch
        {
            // If decompression fails, assume it's uncompressed
            return System.Text.Encoding.UTF8.GetString(data);
        }
    }

    private async Task<string> ExportToChromeFormat(List<PasswordItem> items)
    {
        var csvLines = new List<string>
        {
            "name,url,username,password,note"
        };

        foreach (var item in items)
        {
            if (item.LoginItem != null)
            {
                var name = EscapeCsvField(item.Title ?? "");
                var url = EscapeCsvField(item.LoginItem.WebsiteUrl ?? "");
                var username = EscapeCsvField(item.LoginItem.Username ?? "");
                var password = EscapeCsvField(item.LoginItem.Password ?? "");
                var note = EscapeCsvField(item.Description ?? "");

                csvLines.Add($"{name},{url},{username},{password},{note}");
            }
        }

        return string.Join(Environment.NewLine, csvLines);
    }

    private async Task<string> ExportToEdgeFormat(List<PasswordItem> items)
    {
        // Edge uses similar format to Chrome
        return await ExportToChromeFormat(items);
    }

    private async Task<string> ExportToFirefoxFormat(List<PasswordItem> items)
    {
        var csvLines = new List<string>
        {
            "url,username,password,httpRealm,formActionOrigin,guid,timeCreated,timeLastUsed,timePasswordChanged"
        };

        foreach (var item in items)
        {
            if (item.LoginItem != null)
            {
                var url = EscapeCsvField(item.LoginItem.WebsiteUrl ?? "");
                var username = EscapeCsvField(item.LoginItem.Username ?? "");
                var password = EscapeCsvField(item.LoginItem.Password ?? "");
                var guid = Guid.NewGuid().ToString();
                var timeCreated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                csvLines.Add($"{url},{username},{password},,{url},{guid},{timeCreated},{timeCreated},{timeCreated}");
            }
        }

        return string.Join(Environment.NewLine, csvLines);
    }

    private static string EscapeCsvField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }
}