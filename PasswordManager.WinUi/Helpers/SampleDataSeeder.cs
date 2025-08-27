using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Services.Interfaces;
using PasswordManager.Models;
using System;
using System.Threading.Tasks;

namespace PasswordManager.WinUi.Helpers
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
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                personalCollection = await collectionService.CreateAsync(personalCollection);

                var workCollection = new Collection
                {
                    Name = "Work",
                    Description = "Work-related accounts and credentials",
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                workCollection = await collectionService.CreateAsync(workCollection);

                // Create sample categories
                var loginCategory = new Category
                {
                    Name = "Logins",
                    Description = "User accounts and login credentials",
                    Icon = "key",
                    Color = "#3b82f6",
                    CollectionId = personalCollection.Id,
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
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                creditCardCategory = await categoryService.CreateAsync(creditCardCategory);

                var secureNotesCategory = new Category
                {
                    Name = "Secure Notes",
                    Description = "Private notes and documents",
                    Icon = "note",
                    Color = "#10b981",
                    CollectionId = personalCollection.Id,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                secureNotesCategory = await categoryService.CreateAsync(secureNotesCategory);

                var wifiCategory = new Category
                {
                    Name = "WiFi Passwords",
                    Description = "Wireless network credentials",
                    Icon = "wifi",
                    Color = "#8b5cf6",
                    CollectionId = personalCollection.Id,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                wifiCategory = await categoryService.CreateAsync(wifiCategory);

                var workCategory = new Category
                {
                    Name = "Work Accounts",
                    Description = "Professional accounts and services",
                    Icon = "security",
                    Color = "#f59e0b",
                    CollectionId = workCollection.Id,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                workCategory = await categoryService.CreateAsync(workCategory);

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
                        Website = "https://gmail.com",
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow,
                        LoginItem = new LoginItem
                        {
                            Username = "john.doe@gmail.com",
                            Password = "MySecurePassword123!",
                            WebsiteUrl = "https://gmail.com"
                        }
                    },
                    new PasswordItem
                    {
                        Title = "GitHub",
                        Description = "Development platform account",
                        Type = ItemType.Login,
                        CategoryId = workCategory.Id,
                        CollectionId = workCollection.Id,
                        Website = "https://github.com",
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow,
                        LoginItem = new LoginItem
                        {
                            Username = "johndoe_dev",
                            Password = "DevPassword456!",
                            WebsiteUrl = "https://github.com"
                        }
                    },
                    new PasswordItem
                    {
                        Title = "Netflix",
                        Description = "Streaming service subscription",
                        Type = ItemType.Login,
                        CategoryId = loginCategory.Id,
                        CollectionId = personalCollection.Id,
                        Website = "https://netflix.com",
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow,
                        IsFavorite = true,
                        LoginItem = new LoginItem
                        {
                            Username = "john.doe@gmail.com",
                            Password = "Netflix789!",
                            WebsiteUrl = "https://netflix.com"
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
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    CreditCardItem = new CreditCardItem
                    {
                        CardholderName = "John Doe",
                        CardNumber = "4532-1234-5678-9012",
                        ExpiryMonth = 12,
                        ExpiryYear = 2027,
                        SecurityCode = "123"
                    }
                };
                await passwordItemService.CreateAsync(sampleCreditCard);

                // Create sample secure note
                var sampleNote = new PasswordItem
                {
                    Title = "Important Documents",
                    Description = "List of important document locations",
                    Type = ItemType.SecureNote,
                    CategoryId = secureNotesCategory.Id,
                    CollectionId = personalCollection.Id,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    SecureNoteItem = new SecureNoteItem
                    {
                        Content = "Passport: Safe deposit box #123\nSSN Card: Home safe\nBirth Certificate: File cabinet"
                    }
                };
                await passwordItemService.CreateAsync(sampleNote);

                // Create sample WiFi password
                var sampleWifi = new PasswordItem
                {
                    Title = "Home WiFi",
                    Description = "Main home wireless network",
                    Type = ItemType.WiFi,
                    CategoryId = wifiCategory.Id,
                    CollectionId = personalCollection.Id,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    WiFiItem = new WiFiItem
                    {
                        NetworkName = "HomeNetwork_5G",
                        Password = "WifiPassword123!",
                        SecurityType = "WPA2",
                        Notes = "Located in living room"
                    }
                };
                await passwordItemService.CreateAsync(sampleWifi);

                System.Diagnostics.Debug.WriteLine("Sample data seeded successfully!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error seeding sample data: {ex.Message}");
            }
        }
    }
}