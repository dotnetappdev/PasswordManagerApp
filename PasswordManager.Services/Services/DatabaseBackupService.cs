using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using PasswordManager.Services.Interfaces;
using PasswordManager.DAL;
using PasswordManager.Models.DTOs;
using PasswordManager.Models;
using PasswordManager.Crypto.Interfaces;

namespace PasswordManager.Services.Services;

/// <summary>
/// Service for creating and restoring database backups
/// </summary>
public class DatabaseBackupService : IDatabaseBackupService
{
    private readonly ILogger<DatabaseBackupService> _logger;
    private readonly IDatabaseContextFactory _contextFactory;
    private readonly IPasswordEncryptionService _encryptionService;

    public DatabaseBackupService(
        ILogger<DatabaseBackupService> logger,
        IDatabaseContextFactory contextFactory,
        IPasswordEncryptionService encryptionService)
    {
        _logger = logger;
        _contextFactory = contextFactory;
        _encryptionService = encryptionService;
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

            // Encrypt the data (simplified - in production this would use proper encryption)
            // For now, just return the data as-is
            var encryptedData = finalData;

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
            // Decrypt the data (simplified - in production this would use proper decryption)
            // For now, assume data is already decrypted
            var decryptedData = backupData;

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