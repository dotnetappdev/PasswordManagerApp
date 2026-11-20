using Microsoft.EntityFrameworkCore;
using VaultGuard.DAL;
using VaultGuard.Models;
using VaultGuard.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VaultGuard.Services.Services
{
    public class CollectionService : ICollectionService
    {
        private readonly VaultGuardDbContext _db;

        public CollectionService(VaultGuardDbContext db, VaultGuard.Services.Security.ICurrentVaultUser? currentUser = null)
        {
            _db = db;
            _currentUser = currentUser;
        }

        // Owner scoping: shared rows (no owner) plus the signed-in user's own — never another user's.
        // Previously every user saw (and could edit/delete by id) everyone's collections.
        private readonly VaultGuard.Services.Security.ICurrentVaultUser? _currentUser;
        private async Task<IQueryable<Collection>> VisibleAsync()
        {
            if (_currentUser == null) return _db.Collections;
            var uid = await _currentUser.GetUserIdAsync();
            return _db.Collections.Where(x => x.UserId == null || x.UserId == uid);
        }


        public async Task<List<Collection>> GetAllAsync()
        {
            return await (await VisibleAsync())
                .Include(c => c.Categories)
                .ToListAsync();
        }

        public async Task<Collection?> GetByIdAsync(int id)
        {
            return await (await VisibleAsync())
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
            // Ensure Icon is not null to satisfy DB constraints / UI expectations
            if (string.IsNullOrWhiteSpace(collection.Icon))
            {
                collection.Icon = "folder"; // default icon
            }

            if (_currentUser != null && string.IsNullOrEmpty(collection.UserId))
                collection.UserId = await _currentUser.GetUserIdAsync();
            _db.Collections.Add(collection);
            await _db.SaveChangesAsync();
            return collection;
        }

        public async Task<Collection> UpdateAsync(Collection collection)
        {
            // Find the existing entity to avoid tracking conflicts
            var existingCollection = await (await VisibleAsync()).FirstOrDefaultAsync(c => c.Id == collection.Id);
            if (existingCollection != null)
            {
                // Update the properties manually
                existingCollection.Name = collection.Name;
                existingCollection.Description = collection.Description;
                existingCollection.Icon = string.IsNullOrWhiteSpace(collection.Icon) ? existingCollection.Icon ?? "folder" : collection.Icon;
                existingCollection.Color = collection.Color;
                existingCollection.ParentCollectionId = collection.ParentCollectionId;
                existingCollection.IsDefault = collection.IsDefault;

                await _db.SaveChangesAsync();
                return existingCollection;
            }

            // If not found, throw an exception
            throw new InvalidOperationException($"Collection with ID {collection.Id} not found.");
        }

        public async Task DeleteAsync(int id)
        {
            var collection = await (await VisibleAsync()).FirstOrDefaultAsync(c => c.Id == id);
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
            // Get all collections without tracking to avoid conflicts
            var collections = await _db.Collections.AsNoTracking().ToListAsync();

            // Update each collection's IsDefault property
            foreach (var collection in collections)
            {
                var entity = _db.Collections.Find(collection.Id);
                if (entity != null)
                {
                    entity.IsDefault = (collection.Id == id);
                }
            }

            await _db.SaveChangesAsync();
        }
    }
}
