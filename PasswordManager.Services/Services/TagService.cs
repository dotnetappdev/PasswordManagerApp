
using Microsoft.EntityFrameworkCore;
using PasswordManager.DAL;
using PasswordManager.Interfaces;
using PasswordManager.Models;

namespace PasswordManager.Services;

public class TagService : ITagService
{
    private readonly PasswordManagerDbContext _context;

    public TagService(PasswordManagerDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Tag>> GetAllAsync()
    {
        return await _context.Tags
            .Include(t => t.PasswordItems)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<Tag?> GetByIdAsync(int id)
    {
        return await _context.Tags
            .Include(t => t.PasswordItems)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Tag?> GetByNameAsync(string name)
    {
        return await _context.Tags
            .Include(t => t.PasswordItems)
            .FirstOrDefaultAsync(t => t.Name == name);
    }

    public async Task<Tag> CreateAsync(Tag tag)
    {
        tag.CreatedAt = DateTime.UtcNow;
        
        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();
        return tag;
    }

    public async Task<Tag> UpdateAsync(Tag tag)
    {
        _context.Tags.Update(tag);
        await _context.SaveChangesAsync();
        return tag;
    }

    public async Task DeleteAsync(int id)
    {
        var tag = await _context.Tags.FindAsync(id);
        if (tag != null && !tag.IsSystemTag)
        {
            _context.Tags.Remove(tag);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Tag>> GetSystemTagsAsync()
    {
        return await _context.Tags
            .Where(t => t.IsSystemTag)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Tag>> GetUserTagsAsync()
    {
        return await _context.Tags
            .Where(t => !t.IsSystemTag)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(string name)
    {
        return await _context.Tags.AnyAsync(t => t.Name == name);
    }
}
