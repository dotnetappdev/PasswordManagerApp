using PasswordManager.Models;
using Microsoft.EntityFrameworkCore;

namespace PasswordManager.DAL.Seed;

public static class TestDataSeeder
{
    public static void SeedTestData(PasswordManagerDbContext db)
    {
        if (!db.Collections.Any())
        {
            db.Collections.AddRange(
                new Collection { Name = "Banking", Icon = "🏦", Color = "#1f2937", IsDefault = true },
                new Collection { Name = "Insurance", Icon = "🛡️", Color = "#059669", IsDefault = false },
                new Collection { Name = "Utilities", Icon = "⚡", Color = "#dc2626", IsDefault = false },
                new Collection { Name = "Work", Icon = "💼", Color = "#7c3aed", IsDefault = false },
                new Collection { Name = "Personal", Icon = "👤", Color = "#3b82f6", IsDefault = false }
            );
            db.SaveChanges();
        }

        if (!db.Categories.Any())
        {
            db.Categories.AddRange(
                new Category { Name = "Checking Account", Icon = "💳", Color = "#3b82f6", CollectionId = 1 },
                new Category { Name = "Credit Cards", Icon = "💰", Color = "#f59e0b", CollectionId = 1 },
                new Category { Name = "Investment", Icon = "📈", Color = "#10b981", CollectionId = 1 },
                new Category { Name = "Health Insurance", Icon = "🏥", Color = "#ef4444", CollectionId = 2 },
                new Category { Name = "Auto Insurance", Icon = "🚗", Color = "#8b5cf6", CollectionId = 2 },
                new Category { Name = "Home Insurance", Icon = "🏠", Color = "#06b6d4", CollectionId = 2 },
                new Category { Name = "Electric", Icon = "⚡", Color = "#fbbf24", CollectionId = 3 },
                new Category { Name = "Gas", Icon = "🔥", Color = "#f97316", CollectionId = 3 },
                new Category { Name = "Internet", Icon = "🌐", Color = "#6366f1", CollectionId = 3 },
                new Category { Name = "Business", Icon = "🏢", Color = "#7c3aed", CollectionId = 4 },
                new Category { Name = "Email", Icon = "📧", Color = "#10b981", CollectionId = 5 }
            );
            db.SaveChanges();
        }

        if (!db.Tags.Any())
        {
            db.Tags.AddRange(
                new Tag { Name = "Important", Color = "#ef4444" },
                new Tag { Name = "2FA", Color = "#8b5cf6" },
                new Tag { Name = "Monthly Bills", Color = "#10b981" },
                new Tag { Name = "High Security", Color = "#7c3aed" }
            );
            db.SaveChanges();
        }

        if (!db.PasswordItems.Any())
        {
            var categories = db.Categories.ToList();
            var tags = db.Tags.ToList();
            var collections = db.Collections.ToList();
            
            // Get collection IDs with fallbacks
            var bankingCollectionId = collections.FirstOrDefault(c => c.Name == "Banking")?.Id ?? 1;
            var insuranceCollectionId = collections.FirstOrDefault(c => c.Name == "Insurance")?.Id ?? 2;
            var utilitiesCollectionId = collections.FirstOrDefault(c => c.Name == "Utilities")?.Id ?? 3;
            var workCollectionId = collections.FirstOrDefault(c => c.Name == "Work")?.Id ?? 4;
            var personalCollectionId = collections.FirstOrDefault(c => c.Name == "Personal")?.Id ?? 5;
            
            // Get category IDs with fallbacks
            var checkingCategoryId = categories.FirstOrDefault(c => c.Name == "Checking Account")?.Id ?? 1;
            var creditCardCategoryId = categories.FirstOrDefault(c => c.Name == "Credit Cards")?.Id ?? 2;
            var healthInsuranceCategoryId = categories.FirstOrDefault(c => c.Name == "Health Insurance")?.Id ?? 4;
            var autoInsuranceCategoryId = categories.FirstOrDefault(c => c.Name == "Auto Insurance")?.Id ?? 5;
            var electricCategoryId = categories.FirstOrDefault(c => c.Name == "Electric")?.Id ?? 7;
            var internetCategoryId = categories.FirstOrDefault(c => c.Name == "Internet")?.Id ?? 9;
            var businessCategoryId = categories.FirstOrDefault(c => c.Name == "Business")?.Id ?? 10;
            var emailCategoryId = categories.FirstOrDefault(c => c.Name == "Email")?.Id ?? 11;
            
            // NOTE: All passwords are now encrypted. These are sample items without actual passwords.
            // Real passwords will be added through the API with proper encryption using the user's master password.
            db.PasswordItems.AddRange(
                new PasswordItem
                {
                    Title = "Chase Bank",
                    Type = ItemType.Login,
                    CategoryId = checkingCategoryId,
                    CollectionId = bankingCollectionId,
                    LoginItem = new LoginItem
                    {
                        Website = "https://chase.com",
                        Username = "john.doe@email.com",
                        // Password will be encrypted when added through the API
                        Email = "john.doe@email.com"
                    },
                    Tags = tags.Where(t => t.Name == "Important" || t.Name == "High Security").ToList()
                },
                new PasswordItem
                {
                    Title = "Capital One Credit Card",
                    Type = ItemType.Login,
                    CategoryId = creditCardCategoryId,
                    CollectionId = bankingCollectionId,
                    LoginItem = new LoginItem
                    {
                        Website = "https://capitalone.com",
                        Username = "john.doe",
                        // Password will be encrypted when added through the API
                        Email = "john.doe@email.com"
                    },
                    Tags = tags.Where(t => t.Name == "Important" || t.Name == "2FA").ToList()
                },
                new PasswordItem
                {
                    Title = "Blue Cross Blue Shield",
                    Type = ItemType.Login,
                    CategoryId = healthInsuranceCategoryId,
                    CollectionId = insuranceCollectionId,
                    LoginItem = new LoginItem
                    {
                        Website = "https://bcbs.com",
                        Username = "johndoe123",
                        // Password will be encrypted when added through the API
                        Email = "john.doe@email.com"
                    },
                    Tags = tags.Where(t => t.Name == "Important").ToList()
                },
                new PasswordItem
                {
                    Title = "State Farm Auto",
                    Type = ItemType.Login,
                    CategoryId = autoInsuranceCategoryId,
                    CollectionId = insuranceCollectionId,
                    LoginItem = new LoginItem
                    {
                        Website = "https://statefarm.com",
                        Username = "john.doe.sf",
                        // Password will be encrypted when added through the API
                        Email = "john.doe@email.com"
                    },
                    Tags = tags.Where(t => t.Name == "Important").ToList()
                },
                new PasswordItem
                {
                    Title = "Pacific Gas & Electric",
                    Type = ItemType.Login,
                    CategoryId = electricCategoryId,
                    CollectionId = utilitiesCollectionId,
                    LoginItem = new LoginItem
                    {
                        Website = "https://pge.com",
                        Username = "john.doe.pge",
                        // Password will be encrypted when added through the API
                        Email = "john.doe@email.com"
                    },
                    Tags = tags.Where(t => t.Name == "Monthly Bills").ToList()
                },
                new PasswordItem
                {
                    Title = "Comcast Xfinity",
                    Type = ItemType.Login,
                    CategoryId = internetCategoryId,
                    CollectionId = utilitiesCollectionId,
                    LoginItem = new LoginItem
                    {
                        Website = "https://xfinity.com",
                        Username = "johndoe_xfinity",
                        // Password will be encrypted when added through the API
                        Email = "john.doe@email.com"
                    },
                    Tags = tags.Where(t => t.Name == "Monthly Bills").ToList()
                },
                new PasswordItem
                {
                    Title = "Company Portal",
                    Type = ItemType.Login,
                    CategoryId = businessCategoryId,
                    CollectionId = workCollectionId,
                    LoginItem = new LoginItem
                    {
                        Website = "https://portal.company.com",
                        Username = "john.doe",
                        // Password will be encrypted when added through the API
                        Email = "john.doe@company.com"
                    },
                    Tags = tags.Where(t => t.Name == "2FA" || t.Name == "High Security").ToList()
                },
                new PasswordItem
                {
                    Title = "Personal Gmail",
                    Type = ItemType.Login,
                    CategoryId = emailCategoryId,
                    CollectionId = personalCollectionId,
                    LoginItem = new LoginItem
                    {
                        Website = "https://gmail.com",
                        Username = "john.doe@gmail.com",
                        // Password will be encrypted when added through the API
                        Email = "john.doe@gmail.com"
                    },
                    Tags = tags.Where(t => t.Name == "Important" || t.Name == "2FA").ToList()
                }
            );
            db.SaveChanges();
        }
    }
}
