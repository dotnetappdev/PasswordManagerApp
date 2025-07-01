using Microsoft.EntityFrameworkCore;
using PasswordManager.DAL;
using PasswordManager.Models;
using PasswordManager.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PasswordManager.Services.Services
{
    public class CollectionService : ICollectionService
    {
        private readonly PasswordManagerDbContext _db;

        public CollectionService(PasswordManagerDbContext db)
        {
            _db = db;
        }

        public async Task<List<Collection>> GetAllAsync()
        {
            return await _db.Collections
                .Include(c => c.Categories)
                .ToListAsync();
        }

        public async Task<Collection?> GetByIdAsync(int id)
        {
            return await _db.Collections
                .Include(c => c.Categories)
                .Include(c => c.PasswordItems)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Collection> CreateAsync(Collection collection)
        {
            // If this is the first collection, mark it as default
            if (!await _db.Collections.AnyAsync())
            {
                collection.IsDefault = true;
            }

            _db.Collections.Add(collection);
            await _db.SaveChangesAsync();
            return collection;
        }

        public async Task<Collection> UpdateAsync(Collection collection)
        {
            _db.Collections.Update(collection);
            await _db.SaveChangesAsync();
            return collection;
        }

        public async Task DeleteAsync(int id)
        {
            var collection = await _db.Collections.FindAsync(id);
            if (collection != null)
            {
                // Check if it's the default collection
                if (collection.IsDefault)
                {
                    // Find another collection to mark as default
                    var newDefault = await _db.Collections
                        .Where(c => c.Id != id)
                        .FirstOrDefaultAsync();
                        
                    if (newDefault != null)
                    {
                        newDefault.IsDefault = true;
                    }
                }

                // Get all password items in this collection
                var passwordItems = await _db.PasswordItems
                    .Where(p => p.CollectionId == id)
                    .ToListAsync();

                // Move them to default collection if exists
                var defaultCollection = await GetDefaultCollectionAsync();
                if (defaultCollection != null && defaultCollection.Id != id)
                {
                    foreach (var item in passwordItems)
                    {
                        item.CollectionId = defaultCollection.Id;
                    }
                }

                _db.Collections.Remove(collection);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<Collection?> GetDefaultCollectionAsync()
        {
            var defaultCollection = await _db.Collections
                .FirstOrDefaultAsync(c => c.IsDefault);

            // If no default collection exists but collections exist, set the first one as default
            if (defaultCollection == null)
            {
                defaultCollection = await _db.Collections.FirstOrDefaultAsync();
                if (defaultCollection != null)
                {
                    defaultCollection.IsDefault = true;
                    await _db.SaveChangesAsync();
                }
            }

            return defaultCollection;
        }

        public async Task SetAsDefaultAsync(int id)
        {
            var collections = await _db.Collections.ToListAsync();
            foreach (var collection in collections)
            {
                collection.IsDefault = (collection.Id == id);
            }
            await _db.SaveChangesAsync();
        }
    }
}
