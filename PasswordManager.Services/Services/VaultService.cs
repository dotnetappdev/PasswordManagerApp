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
            if (string.IsNullOrEmpty(userId))
            {
                return new List<Vault>();
            }

            return await _db.Vaults
                .Where(v => v.UserId == userId)
                .Include(v => v.Categories)
                .Include(v => v.PasswordItems)
                .ToListAsync();
        }

        public async Task<Vault?> GetByIdAsync(int id)
        {
            var userId = _authService.CurrentUser?.Id;
            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            return await _db.Vaults
                .Where(v => v.UserId == userId)
                .Include(v => v.Categories)
                .Include(v => v.PasswordItems)
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<Vault> CreateAsync(Vault vault)
        {
            // If this is the first vault for the user, mark it as default
            if (!await _db.Vaults.AnyAsync(v => v.UserId == vault.UserId))
            {
                vault.IsDefault = true;
            }

            // Ensure Icon is not null to satisfy DB constraints / UI expectations
            if (string.IsNullOrWhiteSpace(vault.Icon))
            {
                vault.Icon = "🔐"; // default vault icon
            }

            vault.CreatedAt = DateTime.UtcNow;
            vault.UpdatedAt = DateTime.UtcNow;

            _db.Vaults.Add(vault);
            await _db.SaveChangesAsync();
            return vault;
        }

        public async Task<Vault> UpdateAsync(Vault vault)
        {
            // Find the existing entity to avoid tracking conflicts
            var existingVault = await _db.Vaults.FindAsync(vault.Id);
            if (existingVault != null)
            {
                // Update the properties manually
                existingVault.Name = vault.Name;
                existingVault.Description = vault.Description;
                existingVault.Icon = string.IsNullOrWhiteSpace(vault.Icon) ? existingVault.Icon ?? "🔐" : vault.Icon;
                existingVault.Color = vault.Color;
                existingVault.IsDefault = vault.IsDefault;
                existingVault.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                return existingVault;
            }

            // If not found, throw an exception
            throw new InvalidOperationException($"Unable to update vault: Vault with ID {vault.Id} not found.");
        }

        public async Task DeleteAsync(int id)
        {
            var vault = await _db.Vaults.FindAsync(id);
            if (vault != null)
            {
                // Check if it's the default vault
                if (vault.IsDefault)
                {
                    // Find another vault to mark as default
                    var newDefault = await _db.Vaults
                        .Where(v => v.Id != id && v.UserId == vault.UserId)
                        .FirstOrDefaultAsync();

                    if (newDefault != null)
                    {
                        newDefault.IsDefault = true;
                    }
                }

                // Get all password items in this vault
                var passwordItems = await _db.PasswordItems
                    .Where(p => p.VaultId == id)
                    .ToListAsync();

                // Move them to default vault if exists
                var defaultVault = await _db.Vaults
                    .Where(v => v.IsDefault && v.Id != id && v.UserId == vault.UserId)
                    .FirstOrDefaultAsync();

                if (defaultVault != null)
                {
                    foreach (var item in passwordItems)
                    {
                        item.VaultId = defaultVault.Id;
                    }
                }
                else
                {
                    // If no default vault, just clear the VaultId
                    foreach (var item in passwordItems)
                    {
                        item.VaultId = null;
                    }
                }

                // Get all categories in this vault
                var categories = await _db.Categories
                    .Where(c => c.VaultId == id)
                    .ToListAsync();

                if (defaultVault != null)
                {
                    foreach (var category in categories)
                    {
                        category.VaultId = defaultVault.Id;
                    }
                }
                else
                {
                    // If no default vault, just clear the VaultId
                    foreach (var category in categories)
                    {
                        category.VaultId = null;
                    }
                }

                _db.Vaults.Remove(vault);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<Vault?> GetDefaultVaultAsync()
        {
            var userId = _authService.CurrentUser?.Id;
            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            return await _db.Vaults
                .Where(v => v.UserId == userId)
                .Include(v => v.Categories)
                .Include(v => v.PasswordItems)
                .FirstOrDefaultAsync(v => v.IsDefault);
        }

        public async Task SetAsDefaultAsync(int id)
        {
            var userId = _authService.CurrentUser?.Id;
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            var vault = await _db.Vaults
                .FirstOrDefaultAsync(v => v.Id == id && v.UserId == userId);
            if (vault != null)
            {
                // Clear any existing default vault for this user
                var existingDefaults = await _db.Vaults
                    .Where(v => v.IsDefault && v.UserId == vault.UserId)
                    .ToListAsync();

                foreach (var existingDefault in existingDefaults)
                {
                    existingDefault.IsDefault = false;
                }

                vault.IsDefault = true;
                await _db.SaveChangesAsync();
            }
        }

        public async Task<Vault> GetOrCreateDefaultVaultAsync(string userId)
        {
            // Try to get existing default vault for user
            var defaultVault = await _db.Vaults
                .FirstOrDefaultAsync(v => v.UserId == userId && v.IsDefault);

            if (defaultVault != null)
            {
                return defaultVault;
            }

            // Try to get any vault for user
            var anyVault = await _db.Vaults
                .FirstOrDefaultAsync(v => v.UserId == userId);

            if (anyVault != null)
            {
                anyVault.IsDefault = true;
                await _db.SaveChangesAsync();
                return anyVault;
            }

            // Create a new default "Personal" vault
            var newVault = new Vault
            {
                Name = "Personal",
                Description = "Your personal password vault",
                IsDefault = true,
                Icon = "🔐",
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Vaults.Add(newVault);
            await _db.SaveChangesAsync();

            return newVault;
        }
    }
}
