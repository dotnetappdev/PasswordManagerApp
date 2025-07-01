using PasswordManager.Models;
using PasswordManager.Services.Interfaces;
using PasswordManager.DAL;
using Microsoft.EntityFrameworkCore;

namespace PasswordManager.Services.Services
{
    public class CategoryService : ICategoryInterface
    {
        private readonly PasswordManagerDbContext _db;
        public CategoryService(PasswordManagerDbContext db)
        {
            _db = db;
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
            _db.Categories.Add(category);
            await _db.SaveChangesAsync();
            return category;
        }

        public async Task<Category> UpdateAsync(Category category)
        {
            _db.Categories.Update(category);
            await _db.SaveChangesAsync();
            return category;
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
    }
}
