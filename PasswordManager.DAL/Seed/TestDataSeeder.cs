using PasswordManager.Models;
using Microsoft.EntityFrameworkCore;

namespace PasswordManager.DAL.Seed;

public static class TestDataSeeder
{
    public static void SeedTestData(PasswordManagerDbContext db)
    {
        if (!db.Categories.Any())
        {
            db.Categories.AddRange(
                new Category { Name = "Personal", Icon = "👤", Color = "#3b82f6" },
                new Category { Name = "Work", Icon = "💼", Color = "#10b981" },
                new Category { Name = "Finance", Icon = "💳", Color = "#f59e0b" },
                new Category { Name = "Social", Icon = "💬", Color = "#ef4444" }
            );
            db.SaveChanges();
        }

        if (!db.Tags.Any())
        {
            db.Tags.AddRange(
                new Tag { Name = "Important", Color = "#ef4444" },
                new Tag { Name = "2FA", Color = "#8b5cf6" },
                new Tag { Name = "Work", Color = "#10b981" },
                new Tag { Name = "Shopping", Color = "#f59e0b" }
            );
            db.SaveChanges();
        }

        if (!db.PasswordItems.Any())
        {
            var categories = db.Categories.ToList();
            var tags = db.Tags.ToList();
            
            // Get default category IDs with fallbacks
            var personalCategoryId = categories.FirstOrDefault(c => c.Name == "Personal")?.Id ?? 1;
            var workCategoryId = categories.FirstOrDefault(c => c.Name == "Work")?.Id ?? 2;
            var shoppingCategoryId = categories.FirstOrDefault(c => c.Name == "Shopping")?.Id ?? 5;
            var financeCategoryId = categories.FirstOrDefault(c => c.Name == "Finance")?.Id ?? 3;
            var socialCategoryId = categories.FirstOrDefault(c => c.Name == "Social")?.Id ?? 4;
            
            db.PasswordItems.AddRange(
                new PasswordItem
                {
                    Title = "Google Account",
                    Type = ItemType.Login,
                    CategoryId = personalCategoryId,
                    LoginItem = new LoginItem
                    {
                        Website = "https://accounts.google.com",
                        Username = "user@gmail.com",
                        Password = "password123",
                        Email = "user@gmail.com"
                    },
                    Tags = tags.Where(t => t.Name == "Important").ToList()
                },
                new PasswordItem
                {
                    Title = "GitHub",
                    Type = ItemType.Login,
                    CategoryId = workCategoryId,
                    LoginItem = new LoginItem
                    {
                        Website = "https://github.com",
                        Username = "devuser",
                        Password = "devpass!@#",
                        Email = "dev@work.com"
                    },
                    Tags = tags.Where(t => t.Name == "Work" || t.Name == "2FA").ToList()
                },
                new PasswordItem
                {
                    Title = "Amazon",
                    Type = ItemType.Login,
                    CategoryId = shoppingCategoryId,
                    LoginItem = new LoginItem
                    {
                        Website = "https://amazon.com",
                        Username = "shopper",
                        Password = "shop1234",
                        Email = "shopper@email.com"
                    },
                    Tags = tags.Where(t => t.Name == "Shopping").ToList()
                },
                new PasswordItem
                {
                    Title = "Bank of America",
                    Type = ItemType.Login,
                    CategoryId = financeCategoryId,
                    LoginItem = new LoginItem
                    {
                        Website = "https://bankofamerica.com",
                        Username = "bankuser",
                        Password = "bankpass!",
                        Email = "bankuser@email.com"
                    },
                    Tags = tags.Where(t => t.Name == "Important").ToList()
                },
                new PasswordItem
                {
                    Title = "Facebook",
                    Type = ItemType.Login,
                    CategoryId = socialCategoryId,
                    LoginItem = new LoginItem
                    {
                        Website = "https://facebook.com",
                        Username = "fbuser",
                        Password = "fbpass!",
                        Email = "fb@email.com"
                    },
                    Tags = tags.Where(t => t.Name == "2FA").ToList()
                }
            );
            db.SaveChanges();
        }
    }
}
