using Microsoft.EntityFrameworkCore;
using PasswordManager.DAL;
using PasswordManager.Models;
using PasswordManager.Services.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PasswordManager.Services.Services;

public class CustomFieldService : ICustomFieldService
{
    private readonly IDatabaseContextFactory _contextFactory;

    public CustomFieldService(IDatabaseContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<CustomField>> GetByPasswordItemIdAsync(int passwordItemId)
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.CustomFields
            .Where(cf => cf.PasswordItemId == passwordItemId)
            .OrderBy(cf => cf.DisplayOrder)
            .ToListAsync();
    }

    public async Task<CustomField> CreateAsync(CustomField customField)
    {
        using var context = _contextFactory.CreateDbContext();
        customField.CreatedAt = DateTime.UtcNow;
        customField.LastModified = DateTime.UtcNow;
        
        context.CustomFields.Add(customField);
        await context.SaveChangesAsync();
        return customField;
    }

    public async Task<CustomField> UpdateAsync(CustomField customField)
    {
        using var context = _contextFactory.CreateDbContext();
        customField.LastModified = DateTime.UtcNow;
        
        context.CustomFields.Update(customField);
        await context.SaveChangesAsync();
        return customField;
    }

    public async Task DeleteAsync(int id)
    {
        using var context = _contextFactory.CreateDbContext();
        var customField = await context.CustomFields.FindAsync(id);
        if (customField != null)
        {
            context.CustomFields.Remove(customField);
            await context.SaveChangesAsync();
        }
    }

    public async Task<List<CustomField>> GetAllAsync()
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.CustomFields
            .OrderBy(cf => cf.PasswordItemId)
            .ThenBy(cf => cf.DisplayOrder)
            .ToListAsync();
    }
}