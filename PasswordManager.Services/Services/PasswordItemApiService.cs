using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PasswordManager.DAL;
using PasswordManager.Models;
using PasswordManager.Models.DTOs;
using PasswordManager.Services.Helpers;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.Services.Services;

public class PasswordItemApiService : IPasswordItemApiService
{
    private readonly PasswordManagerDbContext _context;
    private readonly ILogger<PasswordItemApiService> _logger;

    public PasswordItemApiService(
        PasswordManagerDbContext context,
        ILogger<PasswordItemApiService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<PasswordItemDto>> GetAllAsync()
    {
        try
        {
            var items = await _context.PasswordItems
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
            var items = await _context.PasswordItems
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
            var items = await _context.PasswordItems
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
            var items = await _context.PasswordItems
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
            var items = await _context.PasswordItems
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

    public async Task<PasswordItemDto> CreateAsync(CreatePasswordItemDto createDto)
    {
        try
        {
            var item = createDto.ToEntity();
            
            // Handle tags
            if (createDto.TagIds.Any())
            {
                var tags = await _context.Tags
                    .Where(t => createDto.TagIds.Contains(t.Id))
                    .ToListAsync();
                item.Tags = tags;
            }

            _context.PasswordItems.Add(item);

            // Handle specific item types
            if (createDto.LoginItem != null)
            {
                var loginItem = createDto.LoginItem.ToEntity();
                loginItem.PasswordItem = item;
                _context.LoginItems.Add(loginItem);
            }
            else if (createDto.CreditCardItem != null)
            {
                var creditCardItem = createDto.CreditCardItem.ToEntity();
                creditCardItem.PasswordItem = item;
                _context.CreditCardItems.Add(creditCardItem);
            }
            else if (createDto.SecureNoteItem != null)
            {
                var secureNoteItem = createDto.SecureNoteItem.ToEntity();
                secureNoteItem.PasswordItem = item;
                _context.SecureNoteItems.Add(secureNoteItem);
            }
            else if (createDto.WiFiItem != null)
            {
                var wifiItem = createDto.WiFiItem.ToEntity();
                wifiItem.PasswordItem = item;
                _context.WiFiItems.Add(wifiItem);
            }

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
            var existingItem = await _context.PasswordItems
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
                var tags = await _context.Tags
                    .Where(t => updateDto.TagIds.Contains(t.Id))
                    .ToListAsync();
                existingItem.Tags = tags;
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
            var item = await _context.PasswordItems.FindAsync(id);
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
            var item = await _context.PasswordItems.FindAsync(id);
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
            var item = await _context.PasswordItems.FindAsync(id);
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
            var item = await _context.PasswordItems.FindAsync(id);
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
            var item = await _context.PasswordItems.FindAsync(id);
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

    public async Task<bool> UnarchiveAsync(int id)
    {
        try
        {
            var item = await _context.PasswordItems.FindAsync(id);
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
}
