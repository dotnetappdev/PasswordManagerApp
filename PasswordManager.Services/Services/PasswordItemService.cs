using Microsoft.EntityFrameworkCore;
using PasswordManager.DAL;
using PasswordManager.Interfaces;
using PasswordManager.Models;

namespace PasswordManager.Services;

public class PasswordItemService : IPasswordItemService
{
    private readonly PasswordManagerDbContext _context;

    public PasswordItemService(PasswordManagerDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PasswordItem>> GetAllAsync()
    {
        return await _context.PasswordItems
            .Include(p => p.LoginItem)
             .Include(p => p.CreditCardItem)
           
            .Include(p => p.SecureNoteItem)
            .Include(p => p.WiFiItem)
            .Include(p => p.Tags)
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.LastModified)
            .ToListAsync();
    }

    public async Task<IEnumerable<PasswordItem>> GetByTypeAsync(ItemType type)
    {
        return await _context.PasswordItems
            .Include(p => p.LoginItem)
            .Include(p => p.CreditCardItem)
            .Include(p => p.SecureNoteItem)
            .Include(p => p.WiFiItem)
            .Include(p => p.Tags)
            .Where(p => p.Type == type && !p.IsDeleted)
            .OrderByDescending(p => p.LastModified)
            .ToListAsync();
    }

    public async Task<PasswordItem?> GetByIdAsync(int id)
    {
        return await _context.PasswordItems
            .Include(p => p.LoginItem)
            .Include(p => p.CreditCardItem)
            .Include(p => p.SecureNoteItem)
            .Include(p => p.WiFiItem)
            .Include(p => p.Tags)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
    }

    public async Task<PasswordItem> CreateAsync(PasswordItem item)
    {
        item.CreatedAt = DateTime.UtcNow;
        item.LastModified = DateTime.UtcNow;
        
        _context.PasswordItems.Add(item);
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task<PasswordItem> UpdateAsync(PasswordItem item)
    {
        item.LastModified = DateTime.UtcNow;
        
        _context.PasswordItems.Update(item);
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task DeleteAsync(int id)
    {
        var item = await _context.PasswordItems.FindAsync(id);
        if (item != null)
        {
            item.IsDeleted = true;
            item.LastModified = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<PasswordItem>> SearchAsync(string searchTerm)
    {
        var term = searchTerm.ToLower();
        
        return await _context.PasswordItems
            .Include(p => p.LoginItem)
            .Include(p => p.CreditCardItem)
            .Include(p => p.SecureNoteItem)
            .Include(p => p.WiFiItem)
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
        return await _context.PasswordItems
            .Include(p => p.LoginItem)
            .Include(p => p.CreditCardItem)
            .Include(p => p.SecureNoteItem)
            .Include(p => p.WiFiItem)
            .Include(p => p.Tags)
            .Where(p => p.IsFavorite && !p.IsDeleted)
            .OrderByDescending(p => p.LastModified)
            .ToListAsync();
    }

    public async Task<IEnumerable<PasswordItem>> GetByTagAsync(string tagName)
    {
        return await _context.PasswordItems
            .Include(p => p.LoginItem)
            .Include(p => p.CreditCardItem)
            .Include(p => p.SecureNoteItem)
            .Include(p => p.WiFiItem)
            .Include(p => p.Tags)
            .Where(p => !p.IsDeleted && p.Tags.Any(t => t.Name == tagName))
            .OrderByDescending(p => p.LastModified)
            .ToListAsync();
    }

    public async Task<IEnumerable<PasswordItem>> GetRecentlyUsedAsync(int count = 10)
    {
        return await _context.PasswordItems
            .Include(p => p.LoginItem)
            .Include(p => p.CreditCardItem)
            .Include(p => p.SecureNoteItem)
            .Include(p => p.WiFiItem)
            .Include(p => p.Tags)
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.LastModified)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IEnumerable<PasswordItem>> GetArchivedAsync()
    {
        return await _context.PasswordItems
            .Include(p => p.LoginItem)
            .Include(p => p.CreditCardItem)
            .Include(p => p.SecureNoteItem)
            .Include(p => p.WiFiItem)
            .Include(p => p.Tags)
            .Where(p => p.IsArchived && !p.IsDeleted)
            .OrderByDescending(p => p.LastModified)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.PasswordItems.AnyAsync(p => p.Id == id && !p.IsDeleted);
    }
}
