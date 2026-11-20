using Microsoft.EntityFrameworkCore;
using VaultGuard.DAL;
using VaultGuard.Services.Interfaces;
using VaultGuard.Models;

namespace VaultGuard.Services;

public class PasswordItemService : IPasswordItemService
{
    private readonly VaultGuardDbContext _context;
    private readonly VaultGuard.Services.Security.ICurrentVaultUser? _currentUser;

    /// <param name="currentUser">
    /// When supplied (every app registers one), every read and write is limited to the signed-in user's
    /// own items — the database is shared between profiles/users, and previously this service returned
    /// everyone's items. With no signed-in user it returns nothing. Omitted only by tests and tools that
    /// deliberately operate on the whole database.
    /// </param>
    public PasswordItemService(VaultGuardDbContext context, VaultGuard.Services.Security.ICurrentVaultUser? currentUser = null)
    {
        _context = context;
        _currentUser = currentUser;
    }

    /// <summary>The items the caller may see: their own, or none if nobody is signed in.</summary>
    private async Task<IQueryable<PasswordItem>> OwnedAsync()
    {
        if (_currentUser == null) return _context.PasswordItems;
        var userId = await _currentUser.GetUserIdAsync();
        return userId == null
            ? _context.PasswordItems.Where(_ => false)
            : _context.PasswordItems.Where(p => p.UserId == userId);
    }

    private async Task<PasswordItem?> FindOwnedAsync(int id)
        => await (await OwnedAsync()).FirstOrDefaultAsync(p => p.Id == id);

    /// <summary>Throws if a write would create or modify an item that isn't the signed-in user's.</summary>
    private async Task EnsureCanWriteAsync(PasswordItem item, bool isNew)
    {
        if (_currentUser == null) return;
        var userId = await _currentUser.GetUserIdAsync()
                     ?? throw new UnauthorizedAccessException("Sign in to change vault items.");
        if (isNew)
        {
            if (string.IsNullOrEmpty(item.UserId)) item.UserId = userId;
        }
        else
        {
            var owner = await _context.PasswordItems.AsNoTracking().Where(p => p.Id == item.Id).Select(p => p.UserId).FirstOrDefaultAsync();
            if (owner != userId) throw new UnauthorizedAccessException("That item doesn't belong to the signed-in user.");
        }
        if (item.UserId != userId) throw new UnauthorizedAccessException("That item doesn't belong to the signed-in user.");
    }

    /// <summary>
    /// Safety net for every save path (forms, reorders, favourites, imports…): any secret still in plaintext —
    /// including password/one-time-password/protected custom fields and the TOTP secret — is encrypted with the
    /// signed-in user's vault key before it is written. If there is a secret to protect but no unlocked vault,
    /// the save is refused rather than storing it in clear.
    /// </summary>
    private async Task SealSecretsAsync(PasswordItem item)
    {
        if (_currentUser == null || !VaultGuard.Services.Security.VaultItemSecrets.HasPlaintext(item)) return;
        var cipher = await _currentUser.GetSessionCipherAsync()
                     ?? throw new VaultGuard.Services.Security.VaultKeyRequiredException("The vault is locked, so this item's secrets can't be encrypted. Unlock it and try again.");
        cipher.Seal(item);
    }

    public async Task<IEnumerable<PasswordItem>> GetAllAsync()
    {
        return await (await OwnedAsync())
            .Include(p => p.LoginItem)
            .Include(p => p.CreditCardItem)
            .Include(p => p.SecureNoteItem)
            .Include(p => p.WiFiItem)
            .Include(p => p.PasskeyItem)
            .Include(p => p.CustomFields)
            .Include(p => p.Tags)
            .Include(p => p.Category)
            .Include(p => p.Collection)
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.LastModified)
            .ToListAsync();
    }

    public async Task<IEnumerable<PasswordItem>> GetByTypeAsync(ItemType type)
    {
        return await (await OwnedAsync())
            .Include(p => p.LoginItem)
            .Include(p => p.CreditCardItem)
            .Include(p => p.SecureNoteItem)
            .Include(p => p.WiFiItem)
            .Include(p => p.PasskeyItem)
            .Include(p => p.CustomFields)
            .Include(p => p.Tags)
            .Where(p => p.Type == type && !p.IsDeleted)
            .OrderByDescending(p => p.LastModified)
            .ToListAsync();
    }

    public async Task<PasswordItem?> GetByIdAsync(int id)
    {
        return await (await OwnedAsync())
            .Include(p => p.LoginItem)
            .Include(p => p.CreditCardItem)
            .Include(p => p.SecureNoteItem)
            .Include(p => p.WiFiItem)
            .Include(p => p.PasskeyItem)
            .Include(p => p.CustomFields)
            .Include(p => p.Tags)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
    }

    public async Task<PasswordItem> CreateAsync(PasswordItem item)
    {
        await EnsureCanWriteAsync(item, isNew: true);
        await SealSecretsAsync(item);
        item.CreatedAt = DateTime.UtcNow;
        item.LastModified = DateTime.UtcNow;
        
        _context.PasswordItems.Add(item);
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task<PasswordItem> UpdateAsync(PasswordItem item)
    {
        await EnsureCanWriteAsync(item, isNew: false);
        await SealSecretsAsync(item);
        item.LastModified = DateTime.UtcNow;

        // Snapshot the previous password into the item's rolling history (migration-free reserved custom
        // field) whenever it changes, so the user can review or restore an earlier one. Only the existing
        // ciphertext is stored — never plaintext — so this adds no new at-rest exposure.
        try
        {
            var previous = await _context.LoginItems.AsNoTracking()
                .Where(l => l.PasswordItemId == item.Id)
                .Select(l => new { l.EncryptedPassword, l.PasswordNonce, l.PasswordAuthTag })
                .FirstOrDefaultAsync();

            var newEncrypted = item.LoginItem?.EncryptedPassword;
            if (previous != null
                && !string.IsNullOrEmpty(previous.EncryptedPassword)
                && !string.Equals(previous.EncryptedPassword, newEncrypted, StringComparison.Ordinal))
            {
                VaultGuard.Services.Utilities.PasswordHistoryHelper.Append(
                    item, previous.EncryptedPassword, previous.PasswordNonce, previous.PasswordAuthTag);
            }
        }
        catch (Exception ex)
        {
            // History is best-effort — never block a save because the snapshot failed.
            VaultGuard.Services.Logging.AppLogger.Warning("Password history snapshot skipped", ex);
        }

        _context.PasswordItems.Update(item);
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task DeleteAsync(int id)
    {
        var item = await FindOwnedAsync(id);
        if (item != null)
        {
            // Soft delete only — moves the item to Recently Deleted (the trash), recoverable until
            // it's permanently deleted via PermanentlyDeleteAsync.
            item.IsDeleted = true;
            item.LastModified = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> RestoreAsync(int id)
    {
        var item = await FindOwnedAsync(id);
        if (item == null || !item.IsDeleted) return false;

        item.IsDeleted = false;
        item.LastModified = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> PermanentlyDeleteAsync(int id)
    {
        var item = await FindOwnedAsync(id);
        // Only items already in the trash can be permanently removed — prevents skipping the
        // soft-delete safety net by hard-deleting a live item.
        if (item == null || !item.IsDeleted) return false;

        _context.PasswordItems.Remove(item);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<PasswordItem>> SearchAsync(string searchTerm)
    {
        var term = searchTerm.ToLower();
        
        return await (await OwnedAsync())
            .Include(p => p.LoginItem)
            .Include(p => p.CreditCardItem)
            .Include(p => p.SecureNoteItem)
            .Include(p => p.WiFiItem)
            .Include(p => p.PasskeyItem)
            .Include(p => p.CustomFields)
            .Include(p => p.Tags)
            .Where(p => !p.IsDeleted && (
                p.Title.ToLower().Contains(term) ||
                (p.Description != null && p.Description.ToLower().Contains(term)) ||
                (p.LoginItem != null && p.LoginItem.Website != null && p.LoginItem.Website.ToLower().Contains(term)) ||
                (p.LoginItem != null && p.LoginItem.Username != null && p.LoginItem.Username.ToLower().Contains(term)) ||
                p.Tags.Any(t => t.Name.ToLower().Contains(term))
            ))
            .OrderByDescending(p => p.LastModified)
            .ToListAsync();
    }

    public async Task<IEnumerable<PasswordItem>> GetFavoritesAsync()
    {
        return await (await OwnedAsync())
            .Include(p => p.LoginItem)
            .Include(p => p.CreditCardItem)
            .Include(p => p.SecureNoteItem)
            .Include(p => p.WiFiItem)
            .Include(p => p.PasskeyItem)
            .Include(p => p.CustomFields)
            .Include(p => p.Tags)
            .Where(p => p.IsFavorite && !p.IsDeleted)
            .OrderByDescending(p => p.LastModified)
            .ToListAsync();
    }

    public async Task<IEnumerable<PasswordItem>> GetByTagAsync(string tagName)
    {
        return await (await OwnedAsync())
            .Include(p => p.LoginItem)
            .Include(p => p.CreditCardItem)
            .Include(p => p.SecureNoteItem)
            .Include(p => p.WiFiItem)
            .Include(p => p.PasskeyItem)
            .Include(p => p.CustomFields)
            .Include(p => p.Tags)
            .Where(p => !p.IsDeleted && p.Tags.Any(t => t.Name == tagName))
            .OrderByDescending(p => p.LastModified)
            .ToListAsync();
    }

    public async Task<IEnumerable<PasswordItem>> GetRecentlyUsedAsync(int count = 10)
    {
        return await (await OwnedAsync())
            .Include(p => p.LoginItem)
            .Include(p => p.CreditCardItem)
            .Include(p => p.SecureNoteItem)
            .Include(p => p.WiFiItem)
            .Include(p => p.PasskeyItem)
            .Include(p => p.CustomFields)
            .Include(p => p.Tags)
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.LastModified)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IEnumerable<PasswordItem>> GetArchivedAsync()
    {
        return await (await OwnedAsync())
            .Include(p => p.LoginItem)
            .Include(p => p.CreditCardItem)
            .Include(p => p.SecureNoteItem)
            .Include(p => p.WiFiItem)
            .Include(p => p.PasskeyItem)
            .Include(p => p.CustomFields)
            .Include(p => p.Tags)
            .Where(p => p.IsArchived && !p.IsDeleted)
            .OrderByDescending(p => p.LastModified)
            .ToListAsync();
    }

    public async Task<IEnumerable<PasswordItem>> GetDeletedAsync()
    {
        return await (await OwnedAsync())
            .Include(p => p.LoginItem)
            .Include(p => p.CreditCardItem)
            .Include(p => p.SecureNoteItem)
            .Include(p => p.WiFiItem)
            .Include(p => p.PasskeyItem)
            .Include(p => p.CustomFields)
            .Include(p => p.Tags)
            .Where(p => p.IsDeleted)
            .OrderByDescending(p => p.LastModified)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await (await OwnedAsync()).AnyAsync(p => p.Id == id && !p.IsDeleted);
    }

    public async Task<bool> ToggleFavoriteAsync(int id)
    {
        var item = await FindOwnedAsync(id);
        if (item == null || item.IsDeleted)
        {
            return false;
        }

        item.IsFavorite = !item.IsFavorite;
        item.LastModified = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
}
