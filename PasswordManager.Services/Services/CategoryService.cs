using PasswordManager.Models;
using PasswordManager.Services.Interfaces;
using PasswordManager.DAL;
using Microsoft.EntityFrameworkCore;

namespace PasswordManager.Services.Services
{
    public class CategoryService : ICategoryInterface
    {
        private readonly PasswordManagerDbContext _db;
        private readonly PasswordManager.Services.Interfaces.IAuthService _authService;

        public CategoryService(PasswordManagerDbContext db, PasswordManager.Services.Interfaces.IAuthService authService)
        {
            _db = db;
            _authService = authService;
        }

        public async Task<List<Category>> GetAllAsync()
        {
            return await _db.Categories.ToListAsync();
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _db.Categories.FindAsync(id);
        }

        public async Task<Category> CreateAsync(Category category)
        {
            // Ensure any tracked collections have required fields set to avoid DB NOT NULL errors
            foreach (var entry in _db.ChangeTracker.Entries<Collection>())
            {
                var col = entry.Entity;
                if (col != null)
                {
                    if (string.IsNullOrWhiteSpace(col.Icon))
                        col.Icon = "folder";
                    if (string.IsNullOrWhiteSpace(col.Color))
                        col.Color = "#ffffff";
                }
            }

            // Ensure the category has defaults and UserId (required by EF model)
            if (string.IsNullOrWhiteSpace(category.Icon))
                category.Icon = "📁";
            if (string.IsNullOrWhiteSpace(category.Color))
                category.Color = "#3b82f6";

            if (string.IsNullOrWhiteSpace(category.UserId))
            {
                category.UserId = await EnsureUserIdAsync();
            }

            _db.Categories.Add(category);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Re-throw with additional context to help debugging
                throw new InvalidOperationException($"Failed to save Category: {ex.Message}", ex);
            }

            return category;
        }

        public async Task<Category> UpdateAsync(Category category)
        {
            category.UpdatedAt = DateTime.UtcNow;
            _db.Categories.Update(category);
            await _db.SaveChangesAsync();
            return category;
        }

        // Helper to provide a UserId when none is supplied (used by seeding/UI flows)
        private async Task<string> EnsureUserIdAsync()
        {
            try
            {
                // Prefer the authenticated user if available
                var current = _authService?.CurrentUser;
                if (current != null && !string.IsNullOrWhiteSpace(current.Id))
                    return current.Id;

                // Fallback: return the first user in the database if present
                var existingUser = await _db.Users.FirstOrDefaultAsync();
                if (existingUser != null)
                    return existingUser.Id;

                // As a last resort, create a minimal demo user record so required FK exists
                var demo = new ApplicationUser
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = "demo@local",
                    UserName = "demo@local",
                    FirstName = "Demo",
                    LastName = "User",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.Users.Add(demo);
                await _db.SaveChangesAsync();
                return demo.Id;
            }
            catch
            {
                // If anything fails, throw a helpful exception to make the root cause visible to callers
                throw new InvalidOperationException("Unable to determine or create a default user for Category creation.");
            }
        }

        public async Task DeleteAsync(int id)
        {
            var cat = await _db.Categories.FindAsync(id);
            if (cat != null)
            {
                _db.Categories.Remove(cat);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<bool> HasPasswordItemsAsync(int categoryId)
        {
            return await _db.PasswordItems.AnyAsync(pi => pi.CategoryId == categoryId);
        }

        public async Task<int> GetPasswordItemCountAsync(int categoryId)
        {
            return await _db.PasswordItems.CountAsync(pi => pi.CategoryId == categoryId);
        }
    }
}
