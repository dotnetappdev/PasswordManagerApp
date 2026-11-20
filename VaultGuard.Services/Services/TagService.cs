
using Microsoft.EntityFrameworkCore;
using VaultGuard.DAL;
using VaultGuard.Services.Interfaces;
using VaultGuard.Models;

namespace VaultGuard.Services;

public class TagService : ITagService
{
    private readonly VaultGuardDbContext _context;

    // Owner scoping: shared tags (no owner) plus the signed-in user's own, and only the caller's items under
    // each tag. Previously every user saw everyone's tags — and, through the PasswordItems include, everyone's
    // items attached to a shared tag.
    private readonly VaultGuard.Services.Security.ICurrentVaultUser? _currentUser;

    public TagService(VaultGuardDbContext context, VaultGuard.Services.Security.ICurrentVaultUser? currentUser = null)
    {
        _context = context;
        _currentUser = currentUser;
    }

    private async Task<string?> UidAsync() => _currentUser == null ? null : await _currentUser.GetUserIdAsync();

    private async Task<IQueryable<Tag>> VisibleAsync(bool includeItems)
    {
        if (_currentUser == null)
            return includeItems ? _context.Tags.Include(t => t.PasswordItems) : _context.Tags;
        var uid = await UidAsync();
        var q = _context.Tags.Where(t => t.UserId == null || t.UserId == uid);
        return includeItems ? q.Include(t => t.PasswordItems.Where(p => p.UserId == uid)) : q;
    }

    public async Task<IEnumerable<Tag>> GetAllAsync()
    {
        return await (await VisibleAsync(includeItems: true))
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<Tag?> GetByIdAsync(int id)
    {
        return await (await VisibleAsync(includeItems: true))
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Tag?> GetByNameAsync(string name)
    {
        return await (await VisibleAsync(includeItems: true))
            .FirstOrDefaultAsync(t => t.Name == name);
    }

    public async Task<Tag> CreateAsync(Tag tag)
    {
        tag.CreatedAt = DateTime.UtcNow;
        if (_currentUser != null && string.IsNullOrEmpty(tag.UserId)) tag.UserId = await UidAsync();
        
        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();
        return tag;
    }

    public async Task<Tag> UpdateAsync(Tag tag)
    {
        if (_currentUser != null)
        {
            var uid = await UidAsync();
            var owner = await _context.Tags.AsNoTracking().Where(t => t.Id == tag.Id).Select(t => t.UserId).FirstOrDefaultAsync();
            if (owner != null && owner != uid) throw new UnauthorizedAccessException("That tag doesn't belong to the signed-in user.");
        }
        _context.Tags.Update(tag);
        await _context.SaveChangesAsync();
        return tag;
    }

    public async Task DeleteAsync(int id)
    {
        var tag = await (await VisibleAsync(includeItems: false)).FirstOrDefaultAsync(t => t.Id == id);
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
        return await (await VisibleAsync(includeItems: false))
            .Where(t => !t.IsSystemTag)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(string name)
    {
        return await (await VisibleAsync(includeItems: false)).AnyAsync(t => t.Name == name);
    }
}
