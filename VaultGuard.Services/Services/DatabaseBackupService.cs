using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using VaultGuard.Services.Interfaces;
using VaultGuard.DAL;
using VaultGuard.DAL.Interfaces;
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
    private readonly VaultGuard.ExceptionReporting.IExceptionReporter? _exceptionReporter;

    private static readonly JsonSerializerOptions BackupSerializerOptions = new()
    {
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
    };

    public DatabaseBackupService(
        ILogger<DatabaseBackupService> logger,
        IDatabaseContextFactory contextFactory,
        IPasswordEncryptionService encryptionService,
        IBackupEncryptionService backupEncryption,
        IAuthService? authService = null,
        VaultGuard.ExceptionReporting.IExceptionReporter? exceptionReporter = null)
    {
        _logger = logger;
        _contextFactory = contextFactory;
        _encryptionService = encryptionService;
        _backupEncryption = backupEncryption;
        _authService = authService;
        _exceptionReporter = exceptionReporter;
    }

    // Logs an exception and forwards it to Sentry (when configured) with the operation name as context.
    private void Report(Exception ex, string operation)
    {
        _logger.LogError(ex, "DatabaseBackupService.{Operation} failed", operation);
        _exceptionReporter?.CaptureException(ex, new Dictionary<string, string> { ["operation"] = operation });
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
            string dbPath;
            int passwordCount;
            using (var context = _contextFactory.CreateDbContext())
            {
                dbPath = GetSqliteFilePath(context);
                passwordCount = await context.PasswordItems.CountAsync();
            }

            // The cloud backup is simply the SQLite database file. Take a consistent snapshot via the
            // SQLite online-backup API (safe even while the app holds the database open), optionally
            // gzip it, then encrypt. Restore just decrypts and writes the database back. No per-entity
            // JSON serialization is involved.
            var dbBytes = CreateDatabaseSnapshot(dbPath);
            var finalData = compress ? Compress(dbBytes) : dbBytes;
            var encryptedData = string.IsNullOrEmpty(encryptionKey)
                ? finalData
                : _backupEncryption.Encrypt(finalData, encryptionKey);

            return new DatabaseBackupResult
            {
                Success = true,
                BackupData = encryptedData,
                Metadata = new BackupMetadata
                {
                    CreatedAt = DateTime.UtcNow,
                    DatabaseVersion = "1.0",
                    ApplicationVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
                    IsCompressed = compress,
                    PasswordCount = passwordCount
                }
            };
        }
        catch (Exception ex)
        {
            Report(ex, "CreateBackup");
            return new DatabaseBackupResult
            {
                Success = false,
                ErrorMessage = $"Failed to create backup: {ex.Message}"
            };
        }
    }

    public Task<bool> RestoreBackupAsync(byte[] backupData, string encryptionKey)
        => RestoreFullAsync(backupData, encryptionKey);

    public async Task<BackupContentsDto?> BrowseBackupAsync(byte[] encryptedData, string masterPassword)
    {
        try
        {
            // The backup is a SQLite database file. Open the decrypted copy as a throwaway context and
            // read the items straight out of it so the user can preview / selectively restore them.
            using var handle = await OpenBackupForReadAsync(encryptedData, masterPassword);
            var ctx = handle.Context;

            // Map each CollectionId to the name of the vault it belonged to, so a previewed item can
            // remember its origin vault (PasswordItem itself only stores a CollectionId).
            var vaultNameById = await ctx.Vaults.AsNoTracking()
                .ToDictionaryAsync(v => v.Id, v => v.Name ?? string.Empty);
            var collectionToVaultName = new Dictionary<int, string>();
            foreach (var c in await ctx.Collections.AsNoTracking().ToListAsync())
            {
                if (c.VaultId.HasValue && vaultNameById.TryGetValue(c.VaultId.Value, out var vn) && !string.IsNullOrWhiteSpace(vn))
                    collectionToVaultName[c.Id] = vn;
            }

            var items = await ctx.PasswordItems.AsNoTracking()
                .Include(p => p.LoginItem)
                .Include(p => p.SecureNoteItem)
                .Include(p => p.CreditCardItem)
                .Include(p => p.WiFiItem)
                .ToListAsync();

            var contents = new BackupContentsDto { CreatedAt = DateTime.UtcNow };
            foreach (var pi in items)
            {
                string? vaultName = pi.CollectionId.HasValue
                    && collectionToVaultName.TryGetValue(pi.CollectionId.Value, out var vn)
                        ? vn
                        : null;

                var dto = new BackupItemDto
                {
                    Id = pi.Id.ToString(),
                    Title = string.IsNullOrWhiteSpace(pi.Title) ? "(untitled)" : pi.Title,
                    CreatedAt = pi.CreatedAt,
                    IsSelected = true,
                    VaultName = vaultName,
                    // Keep a serialized copy of the whole item so ImportSelectedItemsAsync can re-insert it.
                    RawJson = JsonSerializer.Serialize(pi, BackupSerializerOptions)
                };

                if (pi.LoginItem != null)
                {
                    dto.ItemType = "Login";
                    dto.Subtitle = pi.LoginItem.Username;
                    contents.LoginItems.Add(dto);
                }
                else if (pi.CreditCardItem != null)
                {
                    dto.ItemType = "Credit Card";
                    dto.Subtitle = pi.CreditCardItem.CardholderName;
                    contents.CreditCards.Add(dto);
                }
                else if (pi.WiFiItem != null)
                {
                    dto.ItemType = "Wi-Fi";
                    dto.Subtitle = pi.WiFiItem.NetworkName;
                    contents.WifiItems.Add(dto);
                }
                else if (pi.SecureNoteItem != null)
                {
                    dto.ItemType = "Secure Note";
                    contents.SecureNotes.Add(dto);
                }
                else
                {
                    dto.ItemType = "Login";
                    contents.LoginItems.Add(dto);
                }
            }
            return contents;
        }
        catch (Exception ex)
        {
            Report(ex, "BrowseBackup");
            return null;
        }
    }

    public async Task<bool> RestoreFullAsync(byte[] encryptedData, string masterPassword)
    {
        try
        {
            var decrypted = string.IsNullOrEmpty(masterPassword)
                ? encryptedData
                : _backupEncryption.Decrypt(encryptedData, masterPassword);
            var dbBytes = DecompressBytesIfNeeded(decrypted);

            if (!IsSqliteDatabase(dbBytes))
                throw new InvalidOperationException("Backup is not a valid database file (wrong master password?).");

            string targetPath;
            using (var context = _contextFactory.CreateDbContext())
                targetPath = GetSqliteFilePath(context);

            // A full restore simply replaces the live database with the backup file's contents,
            // page-for-page, via the SQLite online-backup API.
            RestoreDatabaseSnapshot(dbBytes, targetPath);
            _logger.LogInformation("Database restored from backup into {Path}", targetPath);
            return true;
        }
        catch (Exception ex)
        {
            Report(ex, "RestoreFull");
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

                    // Reset the sub-item primary/foreign keys too so EF inserts fresh rows and links
                    // them to the new PasswordItem instead of colliding with existing keys.
                    if (pi.LoginItem != null) { pi.LoginItem.Id = 0; pi.LoginItem.PasswordItemId = 0; }
                    if (pi.SecureNoteItem != null) { pi.SecureNoteItem.Id = 0; pi.SecureNoteItem.PasswordItemId = 0; }
                    if (pi.CreditCardItem != null) { pi.CreditCardItem.Id = 0; pi.CreditCardItem.PasswordItemId = 0; }
                    if (pi.WiFiItem != null) { pi.WiFiItem.Id = 0; pi.WiFiItem.PasswordItemId = 0; }

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
            Report(ex, "ImportSelectedItems");
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
            Report(ex, $"ExportToBrowser:{format}");
            return new BrowserExportResult
            {
                Success = false,
                ErrorMessage = $"Export failed: {ex.Message}"
            };
        }
    }

    public Task<BackupMetadata?> GetBackupMetadataAsync(byte[] backupData)
    {
        // The backup is the raw (encrypted) database file with no embedded metadata header, so there
        // is nothing to read without the master password. Callers derive timestamp/size from the
        // cloud listing instead.
        return Task.FromResult<BackupMetadata?>(null);
    }

    // ── SQLite backup file helpers ──────────────────────────────────────────────

    private static readonly byte[] SqliteHeader = System.Text.Encoding.ASCII.GetBytes("SQLite format 3\0");

    // Returns the on-disk SQLite file backing the context. Cloud backup only supports SQLite.
    private static string GetSqliteFilePath(IVaultGuardDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        if (connection is SqliteConnection sqlite && !string.IsNullOrWhiteSpace(sqlite.DataSource))
            return sqlite.DataSource;
        throw new InvalidOperationException("Cloud backup is only supported for SQLite databases.");
    }

    // Produces a consistent copy of the live database via the SQLite online-backup API, returning
    // the raw bytes of the snapshot file. Safe to call while the app still has the database open.
    private static byte[] CreateDatabaseSnapshot(string sourceDbPath)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"vgbackup_{Guid.NewGuid():N}.db");
        try
        {
            using (var source = new SqliteConnection($"Data Source={sourceDbPath}"))
            using (var dest = new SqliteConnection($"Data Source={tempPath};Pooling=False"))
            {
                source.Open();
                dest.Open();
                source.BackupDatabase(dest);
            }
            return File.ReadAllBytes(tempPath);
        }
        finally
        {
            TryDeleteFile(tempPath);
        }
    }

    // Writes the snapshot bytes to a temp file and copies it page-for-page into the live database
    // via the online-backup API, replacing all current contents.
    private static void RestoreDatabaseSnapshot(byte[] dbBytes, string targetDbPath)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"vgrestore_{Guid.NewGuid():N}.db");
        try
        {
            File.WriteAllBytes(tempPath, dbBytes);
            using (var source = new SqliteConnection($"Data Source={tempPath};Pooling=False"))
            using (var dest = new SqliteConnection($"Data Source={targetDbPath}"))
            {
                source.Open();
                dest.Open();
                source.BackupDatabase(dest);
            }
            SqliteConnection.ClearAllPools();
        }
        finally
        {
            TryDeleteFile(tempPath);
        }
    }

    // Decrypts + decompresses a backup into a temporary SQLite file and opens it as a read context.
    // The caller MUST dispose the returned handle (disposes the context and deletes the temp file).
    private async Task<BackupReadHandle> OpenBackupForReadAsync(byte[] encryptedData, string masterPassword)
    {
        var decrypted = string.IsNullOrEmpty(masterPassword)
            ? encryptedData
            : _backupEncryption.Decrypt(encryptedData, masterPassword);
        var dbBytes = DecompressBytesIfNeeded(decrypted);

        if (!IsSqliteDatabase(dbBytes))
            throw new InvalidOperationException("Backup is not a valid database file (wrong master password?).");

        var tempPath = Path.Combine(Path.GetTempPath(), $"vgread_{Guid.NewGuid():N}.db");
        await File.WriteAllBytesAsync(tempPath, dbBytes);

        var options = new DbContextOptionsBuilder<VaultGuardDbContext>()
            .UseSqlite($"Data Source={tempPath};Pooling=False")
            .Options;
        return new BackupReadHandle(new VaultGuardDbContext(options), tempPath);
    }

    private static byte[] Compress(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionMode.Compress))
            gzip.Write(data, 0, data.Length);
        return output.ToArray();
    }

    // gzip starts with the magic bytes 0x1F 0x8B; anything else is treated as already-uncompressed.
    private static byte[] DecompressBytesIfNeeded(byte[] data)
    {
        if (data.Length < 2 || data[0] != 0x1f || data[1] != 0x8b)
            return data;

        using var input = new MemoryStream(data);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }

    private static bool IsSqliteDatabase(byte[] data)
    {
        if (data.Length < SqliteHeader.Length) return false;
        for (int i = 0; i < SqliteHeader.Length; i++)
            if (data[i] != SqliteHeader[i]) return false;
        return true;
    }

    private static void TryDeleteFile(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* best effort */ }
    }

    // Holds an open context over a temporary backup database file; disposing cleans up both.
    private sealed class BackupReadHandle : IDisposable
    {
        private readonly string _tempPath;
        public VaultGuardDbContext Context { get; }

        public BackupReadHandle(VaultGuardDbContext context, string tempPath)
        {
            Context = context;
            _tempPath = tempPath;
        }

        public void Dispose()
        {
            Context.Dispose();
            SqliteConnection.ClearAllPools();
            TryDeleteFile(_tempPath);
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