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
    public class VaultService : IVaultService
    {
        private readonly PasswordManagerDbContext _db;
        private readonly IAuthService _authService;

        public VaultService(PasswordManagerDbContext db, IAuthService authService)
        {
            _db = db;
            _authService = authService;
        }

        public async Task<List<Vault>> GetAllAsync()
        {
            var userId = _authService.CurrentUser?.Id;
            if (string.IsNullOrEmpty(userId)) return new List<Vault>();

            var vaults = await _db.Vaults
                .Where(v => v.UserId == userId)
                .OrderByDescending(v => v.IsDefault)
                .ThenBy(v => v.Name)
                .ToListAsync();

            await PopulateItemCountsAsync(vaults, userId);
            return vaults;
        }

        public async Task<Vault?> GetByIdAsync(int id)
        {
            var userId = _authService.CurrentUser?.Id;
            if (string.IsNullOrEmpty(userId)) return null;

            var vault = await _db.Vaults
                .FirstOrDefaultAsync(v => v.Id == id && v.UserId == userId);

            if (vault != null)
                await PopulateItemCountsAsync(new List<Vault> { vault }, userId);

            return vault;
        }

        public async Task<Vault> CreateAsync(Vault vault)
        {
            if (!await _db.Vaults.AnyAsync(v => v.UserId == vault.UserId))
                vault.IsDefault = true;

            vault.Icon = string.IsNullOrWhiteSpace(vault.Icon) ? "🔐" : vault.Icon;
            vault.CreatedAt = DateTime.UtcNow;
            vault.UpdatedAt = DateTime.UtcNow;

            _db.Vaults.Add(vault);
            await _db.SaveChangesAsync();

            // Create a default collection for this vault so items can be assigned to it
            var defaultCollection = new Collection
            {
                Name = vault.Name,
                Description = vault.Description,
                Icon = vault.Icon,
                Color = vault.Color,
                IsDefault = true,
                VaultId = vault.Id,
                UserId = vault.UserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
            _db.Collections.Add(defaultCollection);
            await _db.SaveChangesAsync();

            return vault;
        }

        public async Task<Vault> UpdateAsync(Vault vault)
        {
            var existing = await _db.Vaults.FindAsync(vault.Id)
                ?? throw new InvalidOperationException($"Vault {vault.Id} not found.");

            existing.Name = vault.Name;
            existing.Description = vault.Description;
            existing.Icon = string.IsNullOrWhiteSpace(vault.Icon) ? existing.Icon ?? "🔐" : vault.Icon;
            existing.Color = vault.Color;
            existing.IsDefault = vault.IsDefault;
            existing.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return existing;
        }

        public async Task DeleteAsync(int id)
        {
            var vault = await _db.Vaults.FindAsync(id);
            if (vault == null) return;

            if (vault.IsDefault)
            {
                var newDefault = await _db.Vaults
                    .FirstOrDefaultAsync(v => v.Id != id && v.UserId == vault.UserId);
                if (newDefault != null) { newDefault.IsDefault = true; }
            }

            // Collections with VaultId = id will get VaultId = NULL via SetNull cascade
            // Items inside those collections keep their CollectionId (items are not deleted)
            _db.Vaults.Remove(vault);
            await _db.SaveChangesAsync();
        }

        public async Task<Vault?> GetDefaultVaultAsync()
        {
            var userId = _authService.CurrentUser?.Id;
            if (string.IsNullOrEmpty(userId)) return null;

            var vault = await _db.Vaults
                .FirstOrDefaultAsync(v => v.UserId == userId && v.IsDefault)
                ?? await _db.Vaults.FirstOrDefaultAsync(v => v.UserId == userId);

            if (vault != null)
                await PopulateItemCountsAsync(new List<Vault> { vault }, userId);

            return vault;
        }

        public async Task SetAsDefaultAsync(int id)
        {
            var userId = _authService.CurrentUser?.Id;
            if (string.IsNullOrEmpty(userId)) return;

            var vault = await _db.Vaults.FirstOrDefaultAsync(v => v.Id == id && v.UserId == userId);
            if (vault == null) return;

            var existing = await _db.Vaults.Where(v => v.IsDefault && v.UserId == userId).ToListAsync();
            foreach (var v in existing) v.IsDefault = false;
            vault.IsDefault = true;
            await _db.SaveChangesAsync();
        }

        public async Task<Vault> GetOrCreateDefaultVaultAsync(string userId)
        {
            var vault = await _db.Vaults.FirstOrDefaultAsync(v => v.UserId == userId && v.IsDefault)
                     ?? await _db.Vaults.FirstOrDefaultAsync(v => v.UserId == userId);

            if (vault != null)
            {
                if (!vault.IsDefault) { vault.IsDefault = true; await _db.SaveChangesAsync(); }
                return vault;
            }

            // Create the user's first vault
            var newVault = new Vault
            {
                Name = "Personal",
                Description = "Your personal password vault",
                IsDefault = true,
                Icon = "🔐",
                Color = "#2563EB",
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Vaults.Add(newVault);
            await _db.SaveChangesAsync();

            // Create matching default collection
            _db.Collections.Add(new Collection
            {
                Name = "Personal",
                Description = "Default personal collection",
                Icon = "🔐",
                Color = "#2563EB",
                IsDefault = true,
                VaultId = newVault.Id,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            return newVault;
        }

        // Get items belonging to a specific vault (via its collections)
        public async Task<List<PasswordItem>> GetItemsAsync(int vaultId)
        {
            var userId = _authService.CurrentUser?.Id;
            if (string.IsNullOrEmpty(userId)) return new List<PasswordItem>();

            var collectionIds = await _db.Collections
                .Where(c => c.VaultId == vaultId && c.UserId == userId)
                .Select(c => c.Id)
                .ToListAsync();

            return await _db.PasswordItems
                .Include(p => p.LoginItem)
                .Include(p => p.CreditCardItem)
                .Include(p => p.SecureNoteItem)
                .Include(p => p.Tags)
                .Include(p => p.Category)
                .Where(p => p.UserId == userId
                         && !p.IsDeleted
                         && p.CollectionId != null
                         && collectionIds.Contains(p.CollectionId.Value))
                .OrderByDescending(p => p.LastModified)
                .ToListAsync();
        }

        // Move an item to a vault by assigning it to the vault's default collection
        public async Task MoveItemToVaultAsync(int passwordItemId, int targetVaultId)
        {
            var userId = _authService.CurrentUser?.Id;
            if (string.IsNullOrEmpty(userId)) return;

            var targetCollection = await _db.Collections
                .FirstOrDefaultAsync(c => c.VaultId == targetVaultId
                                       && c.UserId == userId
                                       && c.IsDefault);
            if (targetCollection == null) return;

            var item = await _db.PasswordItems
                .FirstOrDefaultAsync(p => p.Id == passwordItemId && p.UserId == userId);
            if (item == null) return;

            item.CollectionId = targetCollection.Id;
            item.LastModified = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        // Seed default vaults (Personal + Work) for a new user
        public async Task SeedDefaultVaultsAsync(string userId)
        {
            if (await _db.Vaults.AnyAsync(v => v.UserId == userId)) return;

            var defaults = new[]
            {
                new Vault { Name = "Personal",  Description = "Personal passwords and logins",   Icon = "🔐", Color = "#2563EB", IsDefault = true,  UserId = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Vault { Name = "Work",       Description = "Work credentials and tools",      Icon = "💼", Color = "#7C3AED", IsDefault = false, UserId = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Vault { Name = "Finance",    Description = "Banking and financial accounts",  Icon = "🏦", Color = "#059669", IsDefault = false, UserId = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            };
            _db.Vaults.AddRange(defaults);
            await _db.SaveChangesAsync();

            // Create matching collections for each vault
            foreach (var vault in defaults)
            {
                _db.Collections.Add(new Collection
                {
                    Name = vault.Name,
                    Description = vault.Description,
                    Icon = vault.Icon,
                    Color = vault.Color,
                    IsDefault = true,
                    VaultId = vault.Id,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                });
            }
            await _db.SaveChangesAsync();
        }

        // ── Private helpers ─────────────────────────────────────────────────────

        private async Task PopulateItemCountsAsync(List<Vault> vaults, string userId)
        {
            if (!vaults.Any()) return;

            var vaultIds = vaults.Select(v => v.Id).ToList();

            // Get collection IDs grouped by vault
            var collectionsByVault = await _db.Collections
                .Where(c => c.UserId == userId && c.VaultId != null && vaultIds.Contains(c.VaultId.Value))
                .Select(c => new { c.VaultId, c.Id })
                .ToListAsync();

            // Get item counts for each collection
            var collectionIds = collectionsByVault.Select(c => c.Id).ToList();
            var itemCounts = await _db.PasswordItems
                .Where(p => p.UserId == userId
                         && !p.IsDeleted
                         && p.CollectionId != null
                         && collectionIds.Contains(p.CollectionId.Value))
                .GroupBy(p => p.CollectionId!.Value)
                .Select(g => new { CollectionId = g.Key, Count = g.Count() })
                .ToListAsync();

            // Map back to vaults
            foreach (var vault in vaults)
            {
                var vaultCollectionIds = collectionsByVault
                    .Where(c => c.VaultId == vault.Id)
                    .Select(c => c.Id)
                    .ToHashSet();

                vault.ItemCount = itemCounts
                    .Where(ic => vaultCollectionIds.Contains(ic.CollectionId))
                    .Sum(ic => ic.Count);
            }
        }
    }
}
