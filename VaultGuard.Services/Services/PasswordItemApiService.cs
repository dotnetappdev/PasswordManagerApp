using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VaultGuard.DAL;
using VaultGuard.Models;
using VaultGuard.Models.DTOs;
using VaultGuard.Services.Helpers;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.Services.Services;

public class PasswordItemApiService : IPasswordItemApiService
{
    private readonly VaultGuardDbContext _context;
    private readonly ILogger<PasswordItemApiService> _logger;
    private readonly VaultGuard.Services.Security.ICurrentVaultUser? _currentUser;
    private readonly VaultGuard.Crypto.Interfaces.IPasswordCryptoService? _crypto;

    public PasswordItemApiService(
        VaultGuardDbContext context,
        ILogger<PasswordItemApiService> logger,
        VaultGuard.Services.Security.ICurrentVaultUser? currentUser = null,
        VaultGuard.Crypto.Interfaces.IPasswordCryptoService? crypto = null)
    {
        _context = context;
        _logger = logger;
        _currentUser = currentUser;
        _crypto = crypto;
    }

    /// <summary>
    /// Items the caller owns (none if unauthenticated). This used to be the whole table, so the list,
    /// search and by-url endpoints returned every user's — and every tenant's — items.
    /// </summary>
    private async Task<IQueryable<PasswordItem>> OwnedAsync()
    {
        if (_currentUser == null) return _context.PasswordItems;
        var userId = await _currentUser.GetUserIdAsync();
        return userId == null ? _context.PasswordItems.Where(_ => false) : _context.PasswordItems.Where(p => p.UserId == userId);
    }

    private async Task<string?> CallerIdAsync() => _currentUser == null ? null : await _currentUser.GetUserIdAsync();

    /// <summary>Tags the owner may attach: their own or shared (UserId null) — never another user's.</summary>
    private Task<List<Tag>> OwnedTagsAsync(IEnumerable<int> ids, string? ownerId)
        => _context.Tags.Where(t => ids.Contains(t.Id) && (t.UserId == null || t.UserId == ownerId)).ToListAsync();

    /// <summary>Drops a category/collection reference that belongs to someone else.</summary>
    private async Task StripForeignReferencesAsync(PasswordItem item, string? ownerId)
    {
        if (item.CategoryId is int cat && await _context.Categories.AnyAsync(c => c.Id == cat && c.UserId != null && c.UserId != ownerId))
            item.CategoryId = null;
        if (item.CollectionId is int col && await _context.Collections.AnyAsync(c => c.Id == col && c.UserId != null && c.UserId != ownerId))
            item.CollectionId = null;
    }

    /// <summary>
    /// Encrypts any secrets on the item with the owner's vault key, derived from the supplied master
    /// password (the same key the apps use, so either side can read it). Secrets are never stored
    /// unencrypted: no/invalid master password → <see cref="VaultGuard.Services.Security.VaultKeyRequiredException"/>.
    /// </summary>
    private async Task SealAsync(PasswordItem item, string ownerId, string? masterPassword)
    {
        if (!VaultGuard.Services.Security.VaultItemSecrets.HasPlaintext(item)) return;
        if (string.IsNullOrEmpty(masterPassword))
            throw new VaultGuard.Services.Security.VaultKeyRequiredException(
                "This item contains secrets. Include \"masterPassword\" so they can be encrypted — they are never stored unencrypted.");
        if (_crypto == null)
            throw new VaultGuard.Services.Security.VaultKeyRequiredException("Encryption is not available on this server.");

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == ownerId);
        if (user == null || string.IsNullOrEmpty(user.UserSalt) || string.IsNullOrEmpty(user.MasterPasswordHash)
            || !_crypto.VerifyMasterPassword(masterPassword, user.MasterPasswordHash, Convert.FromBase64String(user.UserSalt), user.MasterPasswordIterations))
            throw new VaultGuard.Services.Security.VaultKeyRequiredException("Invalid master password.");

        using var cipher = VaultGuard.Services.Security.VaultCipher.ForMasterPassword(_crypto, masterPassword, Convert.FromBase64String(user.UserSalt));
        // API-only users never unlock an app, so upgrade any legacy plaintext of theirs while we hold the key.
        await VaultGuard.Services.Security.VaultSecretsMigrator.MigrateAsync(_context, ownerId, cipher);
        cipher.Seal(item);
        VaultGuard.Services.Security.VaultItemSecrets.Forget(item);
    }

    public async Task<IEnumerable<PasswordItemDto>> GetAllAsync()
    {
        try
        {
            var items = await (await OwnedAsync())
                .Include(p => p.Category)
                .Include(p => p.Collection)
                .Include(p => p.LoginItem)
                .Include(p => p.CreditCardItem)
                .Include(p => p.SecureNoteItem)
                .Include(p => p.WiFiItem)
                .Include(p => p.Tags)
                .Where(p => !p.IsDeleted)
                .ToListAsync();

            return items.Select(item => item.ToDto()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all password items");
            throw;
        }
    }

    public async Task<PasswordItemDto?> GetByIdAsync(int id)
    {
        try
        {
            var item = await _context.PasswordItems
                .Include(p => p.Category)
                .Include(p => p.Collection)
                .Include(p => p.LoginItem)
                .Include(p => p.CreditCardItem)
                .Include(p => p.SecureNoteItem)
                .Include(p => p.WiFiItem)
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            return item?.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting password item with ID {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<PasswordItemDto>> GetByCollectionIdAsync(int collectionId)
    {
        try
        {
            var items = await (await OwnedAsync())
                .Include(p => p.Category)
                .Include(p => p.Collection)
                .Include(p => p.LoginItem)
                .Include(p => p.CreditCardItem)
                .Include(p => p.SecureNoteItem)
                .Include(p => p.WiFiItem)
                .Include(p => p.Tags)
                .Where(p => p.CollectionId == collectionId && !p.IsDeleted)
                .ToListAsync();

            return items.Select(item => item.ToDto()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting password items for collection {CollectionId}", collectionId);
            throw;
        }
    }

    public async Task<IEnumerable<PasswordItemDto>> GetByCategoryIdAsync(int categoryId)
    {
        try
        {
            var items = await (await OwnedAsync())
                .Include(p => p.Category)
                .Include(p => p.Collection)
                .Include(p => p.LoginItem)
                .Include(p => p.CreditCardItem)
                .Include(p => p.SecureNoteItem)
                .Include(p => p.WiFiItem)
                .Include(p => p.Tags)
                .Where(p => p.CategoryId == categoryId && !p.IsDeleted)
                .ToListAsync();

            return items.Select(item => item.ToDto()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting password items for category {CategoryId}", categoryId);
            throw;
        }
    }

    public async Task<IEnumerable<PasswordItemDto>> GetByTagIdAsync(int tagId)
    {
        try
        {
            var items = await (await OwnedAsync())
                .Include(p => p.Category)
                .Include(p => p.Collection)
                .Include(p => p.LoginItem)
                .Include(p => p.CreditCardItem)
                .Include(p => p.SecureNoteItem)
                .Include(p => p.WiFiItem)
                .Include(p => p.Tags)
                .Where(p => p.Tags.Any(t => t.Id == tagId) && !p.IsDeleted)
                .ToListAsync();

            return items.Select(item => item.ToDto()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting password items for tag {TagId}", tagId);
            throw;
        }
    }

    public async Task<IEnumerable<PasswordItemDto>> SearchAsync(string searchTerm)
    {
        try
        {
            var items = await (await OwnedAsync())
                .Include(p => p.Category)
                .Include(p => p.Collection)
                .Include(p => p.LoginItem)
                .Include(p => p.CreditCardItem)
                .Include(p => p.SecureNoteItem)
                .Include(p => p.WiFiItem)
                .Include(p => p.Tags)
                .Where(p => !p.IsDeleted && 
                           (p.Title.Contains(searchTerm) || 
                            (p.Description != null && p.Description.Contains(searchTerm))))
                .ToListAsync();

            return items.Select(item => item.ToDto()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching password items with term {SearchTerm}", searchTerm);
            throw;
        }
    }

    public async Task<PasswordItemDto> CreateAsync(CreatePasswordItemDto createDto, string userId)
    {
        try
        {
            var item = createDto.ToEntity(userId);
            
            // Handle tags
            var callerId = await CallerIdAsync();
            if (_currentUser != null && callerId != userId)
                throw new UnauthorizedAccessException("Items can only be created for the signed-in user.");
            if (createDto.TagIds.Any())
            {
                item.Tags = await OwnedTagsAsync(createDto.TagIds, userId);
            }
            await StripForeignReferencesAsync(item, userId);

            _context.PasswordItems.Add(item);

            // Handle specific item types
            if (createDto.LoginItem != null)
            {
                var loginItem = createDto.LoginItem.ToEntity();
                loginItem.PasswordItem = item;
                item.LoginItem = loginItem;
                loginItem.UserId ??= userId;
                _context.LoginItems.Add(loginItem);
            }
            else if (createDto.CreditCardItem != null)
            {
                var creditCardItem = createDto.CreditCardItem.ToEntity();
                creditCardItem.PasswordItem = item;
                item.CreditCardItem = creditCardItem;
                creditCardItem.UserId ??= userId;
                _context.CreditCardItems.Add(creditCardItem);
            }
            else if (createDto.SecureNoteItem != null)
            {
                var secureNoteItem = createDto.SecureNoteItem.ToEntity();
                secureNoteItem.PasswordItem = item;
                item.SecureNoteItem = secureNoteItem;
                secureNoteItem.UserId ??= userId;
                _context.SecureNoteItems.Add(secureNoteItem);
            }
            else if (createDto.WiFiItem != null)
            {
                var wifiItem = createDto.WiFiItem.ToEntity();
                wifiItem.PasswordItem = item;
                item.WiFiItem = wifiItem;
                wifiItem.UserId ??= userId;
                _context.WiFiItems.Add(wifiItem);
            }

            await SealAsync(item, userId, createDto.MasterPassword);
            await _context.SaveChangesAsync();

            // Reload the item with all navigation properties
            var createdItem = await GetByIdAsync(item.Id);
            return createdItem!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating password item");
            throw;
        }
    }

    public async Task<PasswordItemDto?> UpdateAsync(int id, UpdatePasswordItemDto updateDto)
    {
        try
        {
            var existingItem = await (await OwnedAsync())
                .Include(p => p.Tags)
                .Include(p => p.LoginItem)
                .Include(p => p.CreditCardItem)
                .Include(p => p.SecureNoteItem)
                .Include(p => p.WiFiItem)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (existingItem == null)
                return null;

            existingItem.UpdateFromDto(updateDto);

            // Handle tags
            existingItem.Tags.Clear();
            if (updateDto.TagIds.Any())
            {
                existingItem.Tags = await OwnedTagsAsync(updateDto.TagIds, existingItem.UserId);
            }

            // Handle specific item types
            if (updateDto.LoginItem != null && existingItem.LoginItem != null)
            {
                existingItem.LoginItem.UpdateFromDto(updateDto.LoginItem);
            }
            else if (updateDto.CreditCardItem != null && existingItem.CreditCardItem != null)
            {
                existingItem.CreditCardItem.UpdateFromDto(updateDto.CreditCardItem);
            }
            else if (updateDto.SecureNoteItem != null && existingItem.SecureNoteItem != null)
            {
                existingItem.SecureNoteItem.UpdateFromDto(updateDto.SecureNoteItem);
            }
            else if (updateDto.WiFiItem != null && existingItem.WiFiItem != null)
            {
                existingItem.WiFiItem.UpdateFromDto(updateDto.WiFiItem);
            }

            await StripForeignReferencesAsync(existingItem, existingItem.UserId);
            await SealAsync(existingItem, existingItem.UserId!, updateDto.MasterPassword);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating password item with ID {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var item = await (await OwnedAsync()).FirstOrDefaultAsync(p => p.Id == id);
            if (item == null || item.IsDeleted)
                return false;

            _context.PasswordItems.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting password item with ID {Id}", id);
            throw;
        }
    }

    public async Task<bool> SoftDeleteAsync(int id)
    {
        try
        {
            var item = await (await OwnedAsync()).FirstOrDefaultAsync(p => p.Id == id);
            if (item == null || item.IsDeleted)
                return false;

            item.IsDeleted = true;
            item.LastModified = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error soft deleting password item with ID {Id}", id);
            throw;
        }
    }

    public async Task<bool> RestoreAsync(int id)
    {
        try
        {
            var item = await (await OwnedAsync()).FirstOrDefaultAsync(p => p.Id == id);
            if (item == null || !item.IsDeleted)
                return false;

            item.IsDeleted = false;
            item.LastModified = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring password item with ID {Id}", id);
            throw;
        }
    }

    public async Task<bool> ToggleFavoriteAsync(int id)
    {
        try
        {
            var item = await (await OwnedAsync()).FirstOrDefaultAsync(p => p.Id == id);
            if (item == null || item.IsDeleted)
                return false;

            item.IsFavorite = !item.IsFavorite;
            item.LastModified = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling favorite for password item with ID {Id}", id);
            throw;
        }
    }

    public async Task<bool> ArchiveAsync(int id)
    {
        try
        {
            var item = await (await OwnedAsync()).FirstOrDefaultAsync(p => p.Id == id);
            if (item == null || item.IsDeleted)
                return false;

            item.IsArchived = true;
            item.LastModified = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving password item with ID {Id}", id);
            throw;
        }
    }

    public async Task<IReadOnlyDictionary<string, string>?> RevealSecretsAsync(int id, string masterPassword)
    {
        var item = await (await OwnedAsync())
            .Include(p => p.LoginItem).Include(p => p.CreditCardItem).Include(p => p.WiFiItem)
            .Include(p => p.SecureNoteItem).Include(p => p.PasskeyItem).Include(p => p.CustomFields)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (item == null) return null;
        if (_crypto == null) throw new VaultGuard.Services.Security.VaultKeyRequiredException("Encryption is not available on this server.");

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == item.UserId);
        if (user == null || string.IsNullOrEmpty(masterPassword) || string.IsNullOrEmpty(user.UserSalt) || string.IsNullOrEmpty(user.MasterPasswordHash)
            || !_crypto.VerifyMasterPassword(masterPassword, user.MasterPasswordHash, Convert.FromBase64String(user.UserSalt), user.MasterPasswordIterations))
            throw new VaultGuard.Services.Security.VaultKeyRequiredException("Invalid master password.");

        using var cipher = VaultGuard.Services.Security.VaultCipher.ForMasterPassword(_crypto, masterPassword, Convert.FromBase64String(user.UserSalt));
        if (await VaultGuard.Services.Security.VaultSecretsMigrator.MigrateAsync(_context, user.Id, cipher) > 0)
        {
            // Legacy plaintext was just encrypted — reload so the response includes it.
            item = await _context.PasswordItems.AsNoTracking()
                .Include(p => p.LoginItem).Include(p => p.CreditCardItem).Include(p => p.WiFiItem)
                .Include(p => p.SecureNoteItem).Include(p => p.PasskeyItem).Include(p => p.CustomFields)
                .FirstAsync(p => p.Id == id);
        }
        var failed = cipher.Open(item);
        if (failed.Count > 0)
            _logger.LogWarning("Item {Id}: {Count} secret field(s) could not be decrypted", id, failed.Count); // never log values
        return VaultGuard.Services.Security.VaultItemSecrets.Plaintexts(item);
    }

    public async Task<bool> UnarchiveAsync(int id)
    {
        try
        {
            var item = await (await OwnedAsync()).FirstOrDefaultAsync(p => p.Id == id);
            if (item == null || item.IsDeleted)
                return false;

            item.IsArchived = false;
            item.LastModified = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unarchiving password item with ID {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<PasswordItemDto>> SearchByUrlAsync(string url)
    {
        try
        {
            var hostname = ExtractHostname(url);
            var items = await (await OwnedAsync())
                .Include(p => p.LoginItem)
                .Where(p => !p.IsDeleted && p.LoginItem != null &&
                            p.LoginItem.WebsiteUrl != null &&
                            p.LoginItem.WebsiteUrl.Contains(hostname))
                .ToListAsync();
            return items.Select(p => p.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching by URL {Url}", url);
            return Enumerable.Empty<PasswordItemDto>();
        }
    }

    public async Task<bool> UpdateTotpSecretAsync(int id, string otpauthUri, string? masterPassword = null)
    {
        try
        {
            var item = await (await OwnedAsync())
                .Include(p => p.CustomFields)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (item == null) return false;

            // Store as a custom field named "totp_secret" — same as TotpHelper convention
            const string totpFieldName = "TOTP Secret";
            var existing = item.CustomFields?.FirstOrDefault(f =>
                string.Equals(f.Name, totpFieldName, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.Value = otpauthUri;
                existing.LastModified = DateTime.UtcNow;
            }
            else
            {
                var field = new CustomField
                {
                    PasswordItemId = item.Id,
                    Name = totpFieldName,
                    Value = otpauthUri,
                    Type = CustomFieldType.Password,
                    IsProtected = true,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                };
                item.CustomFields ??= new List<CustomField>();
                item.CustomFields.Add(field);
            }
            item.LastModified = DateTime.UtcNow;
            // The TOTP secret is encrypted like every other secret (needs the owner's master password).
            await SealAsync(item, item.UserId!, masterPassword);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (VaultGuard.Services.Security.VaultKeyRequiredException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating TOTP for item {Id}", id);
            return false;
        }
    }

    private static string ExtractHostname(string url)
    {
        try { return new Uri(url.Contains("://") ? url : $"https://{url}").Host; }
        catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", logEx); return url; }
    }
}
