using System;
using System.Collections.Generic;
using System.Linq;
using PasswordManager.Models;
using Microsoft.EntityFrameworkCore;

namespace PasswordManager.DAL.Seed;

public static class TestDataSeeder
{
    public static void SeedTestData(PasswordManagerDbContext db)
    {
        // First, create a test user if none exists
        const string testUserId = "test-user-id-12345";
        if (!db.Users.Any(u => u.Id == testUserId))
        {
            db.Users.Add(new ApplicationUser
            {
                Id = testUserId,
                UserName = "testuser@example.com",
                Email = "testuser@example.com",
                EmailConfirmed = true,
                IsActive = true
            });
            db.SaveChanges();
        }

        SeedCollections(db, testUserId);
        SeedCategories(db, testUserId);
        SeedTags(db, testUserId);
        SeedPasswordItems(db, testUserId);
    }

    private static void SeedCollections(PasswordManagerDbContext db, string testUserId)
    {
        if (!db.Collections.Any())
        {
            db.Collections.AddRange(
                new Collection { Name = "Banking", Icon = "🏦", Color = "#1f2937", IsDefault = true, UserId = testUserId },
                new Collection { Name = "Insurance", Icon = "🛡️", Color = "#059669", IsDefault = false, UserId = testUserId },
                new Collection { Name = "Utilities", Icon = "⚡", Color = "#dc2626", IsDefault = false, UserId = testUserId },
                new Collection { Name = "Work", Icon = "💼", Color = "#7c3aed", IsDefault = false, UserId = testUserId },
                new Collection { Name = "Personal", Icon = "👤", Color = "#3b82f6", IsDefault = false, UserId = testUserId }
            );
            db.SaveChanges();
        }
    }

    private static void SeedCategories(PasswordManagerDbContext db, string testUserId)
    {
        if (!db.Categories.Any())
        {
            // Add 1Password-style default categories first (these are the main types shown in the image)
            db.Categories.AddRange(
                // Main categories from 1Password style
                new Category { Name = "Login", Icon = "🔐", Color = "#3b82f6", UserId = testUserId },
                new Category { Name = "Secure Note", Icon = "📝", Color = "#f59e0b", UserId = testUserId },
                new Category { Name = "Credit Card", Icon = "💳", Color = "#10b981", UserId = testUserId },
                new Category { Name = "Identity", Icon = "👤", Color = "#10b981", UserId = testUserId },
                new Category { Name = "Password", Icon = "🔑", Color = "#06b6d4", UserId = testUserId },
                new Category { Name = "Document", Icon = "📄", Color = "#3b82f6", UserId = testUserId },
                
                // Extended categories
                new Category { Name = "SSH Key", Icon = "🔗", Color = "#f59e0b", UserId = testUserId },
                new Category { Name = "API Credentials", Icon = "</> ", Color = "#06b6d4", UserId = testUserId },
                new Category { Name = "Bank Account", Icon = "🏦", Color = "#f59e0b", UserId = testUserId },
                new Category { Name = "Crypto Wallet", Icon = "₿", Color = "#8b5cf6", UserId = testUserId },
                new Category { Name = "Database", Icon = "🗄️", Color = "#6b7280", UserId = testUserId },
                new Category { Name = "Driver License", Icon = "🪪", Color = "#ec4899", UserId = testUserId },
                new Category { Name = "Email", Icon = "📧", Color = "#ec4899", UserId = testUserId },
                new Category { Name = "Medical Record", Icon = "❤️", Color = "#ef4444", UserId = testUserId },
                new Category { Name = "Membership", Icon = "🎫", Color = "#8b5cf6", UserId = testUserId },
                new Category { Name = "Outdoor License", Icon = "🏞️", Color = "#10b981", UserId = testUserId },
                new Category { Name = "Passport", Icon = "🌐", Color = "#3b82f6", UserId = testUserId },
                new Category { Name = "Rewards", Icon = "🎁", Color = "#ec4899", UserId = testUserId },
                new Category { Name = "Server", Icon = "🖥️", Color = "#6b7280", UserId = testUserId },
                new Category { Name = "Social Security Number", Icon = "🆔", Color = "#3b82f6", UserId = testUserId },
                new Category { Name = "Software License", Icon = "💿", Color = "#3b82f6", UserId = testUserId },
                new Category { Name = "Wireless Router", Icon = "📶", Color = "#06b6d4", UserId = testUserId },
                
                // Keep some existing specialized categories
                new Category { Name = "WiFi Networks", Icon = "📶", Color = "#06b6d4", UserId = testUserId },
                new Category { Name = "Passkeys", Icon = "🔐", Color = "#ec4899", UserId = testUserId }
            );
            db.SaveChanges();
        }
    }

    private static void SeedTags(PasswordManagerDbContext db, string testUserId)
    {
        if (!db.Tags.Any())
        {
            db.Tags.AddRange(
                // Security Level Tags
                new Tag { Name = "Important", Color = "#ef4444", UserId = testUserId },
                new Tag { Name = "2FA", Color = "#8b5cf6", UserId = testUserId },
                new Tag { Name = "High Security", Color = "#7c3aed", UserId = testUserId },
                new Tag { Name = "Biometric", Color = "#ec4899", UserId = testUserId },
                
                // Usage Frequency Tags
                new Tag { Name = "Daily Use", Color = "#10b981", UserId = testUserId },
                new Tag { Name = "Weekly", Color = "#3b82f6", UserId = testUserId },
                new Tag { Name = "Monthly Bills", Color = "#f59e0b", UserId = testUserId },
                new Tag { Name = "Rarely Used", Color = "#6b7280", UserId = testUserId },
                
                // Category Tags
                new Tag { Name = "Work", Color = "#7c3aed", UserId = testUserId },
                new Tag { Name = "Personal", Color = "#06b6d4", UserId = testUserId },
                new Tag { Name = "Family", Color = "#84cc16", UserId = testUserId },
                new Tag { Name = "Shared", Color = "#f97316", UserId = testUserId },
                
                // Device/Platform Tags
                new Tag { Name = "Mobile App", Color = "#8b5cf6", UserId = testUserId },
                new Tag { Name = "Web Only", Color = "#3b82f6", UserId = testUserId },
                new Tag { Name = "Desktop", Color = "#6b7280", UserId = testUserId },
                
                // Status Tags
                new Tag { Name = "Active", Color = "#10b981", UserId = testUserId },
                new Tag { Name = "Expired", Color = "#ef4444", UserId = testUserId },
                new Tag { Name = "Temporary", Color = "#f59e0b", UserId = testUserId },
                new Tag { Name = "Backup Account", Color = "#8b5cf6", UserId = testUserId }
            );
            db.SaveChanges();
        }
    }

    private static void SeedPasswordItems(PasswordManagerDbContext db, string testUserId)
    {
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
            var wifiCategoryId = categories.FirstOrDefault(c => c.Name == "WiFi Networks")?.Id ?? 12;
            var secureNotesCategoryId = categories.FirstOrDefault(c => c.Name == "Secure Notes")?.Id ?? 13;
            var passkeysCategoryId = categories.FirstOrDefault(c => c.Name == "Passkeys")?.Id ?? 14;

            var passwordItems = new List<PasswordItem>();

            // Login Items
            passwordItems.AddRange(CreateLoginItems(testUserId, checkingCategoryId, creditCardCategoryId, healthInsuranceCategoryId, 
                autoInsuranceCategoryId, electricCategoryId, internetCategoryId, businessCategoryId, emailCategoryId,
                bankingCollectionId, insuranceCollectionId, utilitiesCollectionId, workCollectionId, personalCollectionId, tags));

            // Credit Card Items
            passwordItems.AddRange(CreateCreditCardItems(testUserId, creditCardCategoryId, bankingCollectionId, tags));

            // WiFi Items
            passwordItems.AddRange(CreateWiFiItems(testUserId, wifiCategoryId, personalCollectionId, workCollectionId, tags));

            // Secure Note Items
            passwordItems.AddRange(CreateSecureNoteItems(testUserId, secureNotesCategoryId, personalCollectionId, workCollectionId, tags));

            // Passkey Items
            passwordItems.AddRange(CreatePasskeyItems(testUserId, passkeysCategoryId, personalCollectionId, workCollectionId, tags));

            db.PasswordItems.AddRange(passwordItems);
            db.SaveChanges();
        }
    }

    private static List<PasswordItem> CreateLoginItems(string testUserId, int checkingCategoryId, int creditCardCategoryId, 
        int healthInsuranceCategoryId, int autoInsuranceCategoryId, int electricCategoryId, int internetCategoryId, 
        int businessCategoryId, int emailCategoryId, int bankingCollectionId, int insuranceCollectionId, 
        int utilitiesCollectionId, int workCollectionId, int personalCollectionId, List<Tag> tags)
    {
        return new List<PasswordItem>
        {
            new PasswordItem
            {
                Title = "Chase Bank",
                Type = ItemType.Login,
                CategoryId = checkingCategoryId,
                CollectionId = bankingCollectionId,
                UserId = testUserId,
                LoginItem = new LoginItem
                {
                    Website = "https://chase.com",
                    Username = "john.doe@email.com",
                    Email = "john.doe@email.com",
                    UserId = testUserId
                },
                Tags = tags.Where(t => t.Name == "Important" || t.Name == "High Security").ToList()
            },
            new PasswordItem
            {
                Title = "Personal Gmail",
                Type = ItemType.Login,
                CategoryId = emailCategoryId,
                CollectionId = personalCollectionId,
                UserId = testUserId,
                LoginItem = new LoginItem
                {
                    Website = "https://gmail.com",
                    Username = "john.doe@gmail.com",
                    Email = "john.doe@gmail.com",
                    UserId = testUserId
                },
                Tags = tags.Where(t => t.Name == "Important" || t.Name == "2FA").ToList()
            },
            new PasswordItem
            {
                Title = "Netflix",
                Type = ItemType.Login,
                CategoryId = emailCategoryId,
                CollectionId = personalCollectionId,
                UserId = testUserId,
                LoginItem = new LoginItem
                {
                    Website = "https://netflix.com",
                    Username = "john.doe@gmail.com",
                    Email = "john.doe@gmail.com",
                    UserId = testUserId
                },
                Tags = tags.Where(t => t.Name == "Personal" || t.Name == "Monthly Bills").ToList()
            },
            new PasswordItem
            {
                Title = "Amazon",
                Type = ItemType.Login,
                CategoryId = emailCategoryId,
                CollectionId = personalCollectionId,
                UserId = testUserId,
                LoginItem = new LoginItem
                {
                    Website = "https://amazon.com",
                    Username = "john.doe@gmail.com",
                    Email = "john.doe@gmail.com",
                    UserId = testUserId
                },
                Tags = tags.Where(t => t.Name == "Personal" || t.Name == "Daily Use").ToList()
            }
        };
    }

    private static List<PasswordItem> CreateCreditCardItems(string testUserId, int creditCardCategoryId, int bankingCollectionId, List<Tag> tags)
    {
        return new List<PasswordItem>
        {
            new PasswordItem
            {
                Title = "Chase Sapphire Preferred",
                Type = ItemType.CreditCard,
                CategoryId = creditCardCategoryId,
                CollectionId = bankingCollectionId,
                UserId = testUserId,
                CreditCardItem = new CreditCardItem
                {
                    CardholderName = "John Doe",
                    CardNumber = "4532 1234 5678 9012",
                    ExpiryDate = "12/2027",
                    CVV = "123",
                    CardType = CardType.Visa,
                    IssuingBank = "Chase Bank",
                    CreditLimit = "$25,000",
                    InterestRate = "18.99%",
                    BankWebsite = "https://chase.com",
                    CustomerServicePhone = "1-800-432-3117",
                    RewardsProgram = "Ultimate Rewards",
                    BenefitsDescription = "2x points on travel and dining, 1x on everything else",
                    BillingAddressLine1 = "123 Main Street",
                    BillingCity = "San Francisco",
                    BillingState = "CA",
                    BillingZipCode = "94102",
                    BillingCountry = "USA",
                    Notes = "Primary travel rewards card",
                    UserId = testUserId
                },
                Tags = tags.Where(t => t.Name == "Important" || t.Name == "Daily Use").ToList()
            }
        };
    }

    private static List<PasswordItem> CreateWiFiItems(string testUserId, int wifiCategoryId, int personalCollectionId, int workCollectionId, List<Tag> tags)
    {
        return new List<PasswordItem>
        {
            new PasswordItem
            {
                Title = "Home WiFi Network",
                Type = ItemType.WiFi,
                CategoryId = wifiCategoryId,
                CollectionId = personalCollectionId,
                UserId = testUserId,
                WiFiItem = new WiFiItem
                {
                    NetworkName = "DoeFamily_5G",
                    Password = "MySecureHome2024!",
                    SecurityType = SecurityType.WPA3,
                    Frequency = FrequencyType.FiveGHz,
                    Channel = "36",
                    Bandwidth = "80MHz",
                    WirelessStandard = "802.11ax (WiFi 6)",
                    RouterBrand = "ASUS",
                    RouterModel = "AX6000",
                    RouterIP = "192.168.1.1",
                    RouterUsername = "admin",
                    RouterPassword = "RouterAdmin123!",
                    RouterAdminUrl = "https://192.168.1.1",
                    ISPName = "Comcast Xfinity",
                    PlanType = "Gigabit Pro",
                    DownloadSpeed = "1000 Mbps",
                    UploadSpeed = "35 Mbps",
                    Location = "Home - Living Room",
                    GuestNetworkName = "DoeFamily_Guest",
                    GuestNetworkPassword = "GuestAccess2024",
                    HasGuestNetwork = true,
                    Notes = "Main home network, password changed quarterly",
                    UserId = testUserId
                },
                Tags = tags.Where(t => t.Name == "Important" || t.Name == "Family").ToList()
            }
        };
    }

    private static List<PasswordItem> CreateSecureNoteItems(string testUserId, int secureNotesCategoryId, int personalCollectionId, int workCollectionId, List<Tag> tags)
    {
        return new List<PasswordItem>
        {
            new PasswordItem
            {
                Title = "Emergency Contact Information",
                Type = ItemType.SecureNote,
                CategoryId = secureNotesCategoryId,
                CollectionId = personalCollectionId,
                UserId = testUserId,
                SecureNoteItem = new SecureNoteItem
                {
                    Title = "Emergency Contact Information",
                    Content = @"EMERGENCY CONTACTS:

Primary Emergency Contact:
- Name: Jane Doe (Spouse)
- Phone: (555) 123-4567
- Relationship: Spouse

Medical Emergency:
- Doctor: Dr. Smith
- Phone: (555) 234-5678
- Hospital: UCSF Medical Center
- Insurance: Blue Cross Blue Shield
- Policy #: ABC123456789

Important: Blood Type O+, Allergic to Penicillin",
                    Category = "Emergency",
                    TemplateType = "Emergency",
                    IsHighSecurity = true,
                    UserId = testUserId
                },
                Tags = tags.Where(t => t.Name == "Important" || t.Name == "Family").ToList()
            }
        };
    }

    private static List<PasswordItem> CreatePasskeyItems(string testUserId, int passkeysCategoryId, int personalCollectionId, int workCollectionId, List<Tag> tags)
    {
        return new List<PasswordItem>
        {
            new PasswordItem
            {
                Title = "Google Account Passkey",
                Type = ItemType.Passkey,
                CategoryId = passkeysCategoryId,
                CollectionId = personalCollectionId,
                UserId = testUserId,
                PasskeyItem = new PasskeyItem
                {
                    Website = "https://accounts.google.com",
                    WebsiteUrl = "https://accounts.google.com",
                    Username = "john.doe@gmail.com",
                    DisplayName = "John Doe",
                    DeviceType = "iPhone 15 Pro",
                    PlatformName = "iOS",
                    IsBackedUp = true,
                    RequiresUserVerification = true,
                    LastUsedAt = DateTime.UtcNow.AddDays(-2),
                    UsageCount = 15,
                    Notes = "Primary Google account passkey, synced across devices",
                    UserId = testUserId
                },
                Tags = tags.Where(t => t.Name == "Important" || t.Name == "Biometric" || t.Name == "Daily Use").ToList()
            }
        };
    }
}