using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Services.Interfaces;
using PasswordManager.Models;
using Microsoft.EntityFrameworkCore;
using PasswordManager.DAL;
using PasswordManager.Crypto.Interfaces;
using System;
using System.Threading.Tasks;

namespace PasswordManager.WPF.Helpers
{
    public static class SampleDataSeeder
    {
        public static async Task SeedSampleDataAsync(IServiceProvider serviceProvider)
        {
            try
            {
                var categoryService = serviceProvider.GetRequiredService<ICategoryInterface>();
                var passwordItemService = serviceProvider.GetRequiredService<IPasswordItemService>();
                var collectionService = serviceProvider.GetRequiredService<ICollectionService>();
                var cryptoService = serviceProvider.GetRequiredService<IPasswordCryptoService>();
                var db = serviceProvider.GetService<PasswordManager.DAL.PasswordManagerDbContext>();
                // Ensure there is at least one user to own seeded data
                string seedUserId;
                if (db != null)
                {
                    var existingUser = await db.Users.FirstOrDefaultAsync();
                    if (existingUser == null)
                    {
                        // Create demo user with proper cryptographic setup
                        const string demoMasterPassword = "DemoPassword123!";

                        // Generate user salt for cryptographic operations
                        var userSalt = cryptoService.GenerateUserSalt();

                        // Create master password hash for authentication
                        var masterPasswordHash = cryptoService.CreateMasterPasswordHash(demoMasterPassword, userSalt);

                        // Create master key identifier for lookup during master key login
                        var masterKeyIdentifier = cryptoService.CreateMasterKeyIdentifier(demoMasterPassword, userSalt);

                        var demoUser = new PasswordManager.Models.ApplicationUser
                        {
                            Id = Guid.NewGuid().ToString(),
                            Email = "demo@local",
                            UserName = "demo@local",
                            FirstName = "Demo",
                            LastName = "User",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            UserSalt = Convert.ToBase64String(userSalt),
                            MasterPasswordHash = masterPasswordHash,
                            MasterKeyIdentifier = masterKeyIdentifier,
                            MasterPasswordHint = "Demo user password: DemoPassword123!",
                            SecurityStamp = Guid.NewGuid().ToString(),
                            ConcurrencyStamp = Guid.NewGuid().ToString(),
                            IsActive = true
                        };
                        db.Users.Add(demoUser);
                        await db.SaveChangesAsync();
                        seedUserId = demoUser.Id;
                    }
                    else
                    {
                        seedUserId = existingUser.Id;
                    }
                }
                else
                {
                    // If DbContext not available, leave UserId blank and rely on services to supply defaults
                    seedUserId = string.Empty;
                }

                // Check if categories already exist to avoid duplicates
                var existingCategories = await categoryService.GetAllAsync();
                if (existingCategories.Count > 0)
                {
                    return; // Data already seeded
                }

                // Create sample collections first
                var personalCollection = new Collection
                {
                    Name = "Personal",
                    Description = "Personal passwords and accounts",
                    Icon = "person", // default icon for collections
                    Color = "#3b82f6",
                    UserId = string.IsNullOrWhiteSpace(seedUserId) ? null : seedUserId,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                personalCollection = await collectionService.CreateAsync(personalCollection);

                var workCollection = new Collection
                {
                    Name = "Work",
                    Description = "Work-related accounts and credentials",
                    Icon = "briefcase", // default icon for work vault
                    Color = "#f59e0b",
                    UserId = string.IsNullOrWhiteSpace(seedUserId) ? null : seedUserId,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                workCollection = await collectionService.CreateAsync(workCollection);

                // Create sample categories - only Logins and Credit Cards
                var loginCategory = new Category
                {
                    Name = "Logins",
                    Description = "User accounts and login credentials",
                    Icon = "key",
                    Color = "#3b82f6",
                    CollectionId = personalCollection.Id,
                    UserId = string.IsNullOrWhiteSpace(seedUserId) ? null : seedUserId,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                loginCategory = await categoryService.CreateAsync(loginCategory);

                var creditCardCategory = new Category
                {
                    Name = "Credit Cards",
                    Description = "Payment cards and financial accounts",
                    Icon = "creditcard",
                    Color = "#ef4444",
                    CollectionId = personalCollection.Id,
                    UserId = string.IsNullOrWhiteSpace(seedUserId) ? null : seedUserId,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                creditCardCategory = await categoryService.CreateAsync(creditCardCategory);

                // Create sample password items
                var sampleLogins = new[]
                {
                    new PasswordItem
                    {
                        Title = "Gmail Account",
                        Description = "Personal email account",
                        Type = ItemType.Login,
                        CategoryId = loginCategory.Id,
                        CollectionId = personalCollection.Id,
                        UserId = string.IsNullOrWhiteSpace(seedUserId) ? null : seedUserId,
                        Website = "https://gmail.com",
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow,
                        LoginItem = new LoginItem
                        {
                            Username = "john.doe@gmail.com",
                            Password = "MySecurePassword123!",
                            WebsiteUrl = "https://gmail.com",
                            UserId = string.IsNullOrWhiteSpace(seedUserId) ? null : seedUserId
                        }
                    },
                    new PasswordItem
                    {
                        Title = "GitHub",
                        Description = "Development platform account",
                        Type = ItemType.Login,
                        CategoryId = loginCategory.Id,
                        CollectionId = personalCollection.Id,
                        UserId = string.IsNullOrWhiteSpace(seedUserId) ? null : seedUserId,
                        Website = "https://github.com",
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow,
                        LoginItem = new LoginItem
                        {
                            Username = "johndoe_dev",
                            Password = "DevPassword456!",
                            WebsiteUrl = "https://github.com",
                            UserId = string.IsNullOrWhiteSpace(seedUserId) ? null : seedUserId
                        }
                    },
                    new PasswordItem
                    {
                        Title = "Netflix",
                        Description = "Streaming service subscription",
                        Type = ItemType.Login,
                        CategoryId = loginCategory.Id,
                        CollectionId = personalCollection.Id,
                        UserId = string.IsNullOrWhiteSpace(seedUserId) ? null : seedUserId,
                        Website = "https://netflix.com",
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow,
                        IsFavorite = true,
                        LoginItem = new LoginItem
                        {
                            Username = "john.doe@gmail.com",
                            Password = "Netflix789!",
                            WebsiteUrl = "https://netflix.com",
                            UserId = string.IsNullOrWhiteSpace(seedUserId) ? null : seedUserId
                        }
                    }
                };

                foreach (var login in sampleLogins)
                {
                    await passwordItemService.CreateAsync(login);
                }

                // Create sample credit card
                var sampleCreditCard = new PasswordItem
                {
                    Title = "Chase Visa",
                    Description = "Primary credit card",
                    Type = ItemType.CreditCard,
                    CategoryId = creditCardCategory.Id,
                    CollectionId = personalCollection.Id,
                    UserId = string.IsNullOrWhiteSpace(seedUserId) ? null : seedUserId,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    CreditCardItem = new CreditCardItem
                    {
                        CardholderName = "John Doe",
                        CardNumber = "4532-1234-5678-9012",
                        ExpiryDate = "12/2027",
                        CVV = "123"
                    }
                };
                await passwordItemService.CreateAsync(sampleCreditCard);
            }
            catch (Exception)
            {
                // Silently fail if seeding fails
            }
        }
    }
}
