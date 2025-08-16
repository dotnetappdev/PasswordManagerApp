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
                new Category { Name = "Email", Icon = "📧", Color = "#10b981", CollectionId = 5 },
                new Category { Name = "WiFi Networks", Icon = "📶", Color = "#06b6d4", CollectionId = 5 },
                new Category { Name = "Secure Notes", Icon = "📝", Color = "#84cc16", CollectionId = 5 },
                new Category { Name = "Passkeys", Icon = "🔐", Color = "#ec4899", CollectionId = 5 }
            );
            db.SaveChanges();
        }

        if (!db.Tags.Any())
        {
            db.Tags.AddRange(
                // Security Level Tags
                new Tag { Name = "Important", Color = "#ef4444" },
                new Tag { Name = "2FA", Color = "#8b5cf6" },
                new Tag { Name = "High Security", Color = "#7c3aed" },
                new Tag { Name = "Biometric", Color = "#ec4899" },
                
                // Usage Frequency Tags
                new Tag { Name = "Daily Use", Color = "#10b981" },
                new Tag { Name = "Weekly", Color = "#3b82f6" },
                new Tag { Name = "Monthly Bills", Color = "#f59e0b" },
                new Tag { Name = "Rarely Used", Color = "#6b7280" },
                
                // Category Tags
                new Tag { Name = "Work", Color = "#7c3aed" },
                new Tag { Name = "Personal", Color = "#06b6d4" },
                new Tag { Name = "Family", Color = "#84cc16" },
                new Tag { Name = "Shared", Color = "#f97316" },
                
                // Device/Platform Tags
                new Tag { Name = "Mobile App", Color = "#8b5cf6" },
                new Tag { Name = "Web Only", Color = "#3b82f6" },
                new Tag { Name = "Desktop", Color = "#6b7280" },
                
                // Status Tags
                new Tag { Name = "Active", Color = "#10b981" },
                new Tag { Name = "Expired", Color = "#ef4444" },
                new Tag { Name = "Temporary", Color = "#f59e0b" },
                new Tag { Name = "Backup Account", Color = "#8b5cf6" }
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
            var wifiCategoryId = categories.FirstOrDefault(c => c.Name == "WiFi Networks")?.Id ?? 12;
            var secureNotesCategoryId = categories.FirstOrDefault(c => c.Name == "Secure Notes")?.Id ?? 13;
            var passkeysCategoryId = categories.FirstOrDefault(c => c.Name == "Passkeys")?.Id ?? 14;
            
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
                },
                
                // Additional realistic login items
                new PasswordItem
                {
                    Title = "Netflix",
                    Type = ItemType.Login,
                    CategoryId = emailCategoryId,
                    CollectionId = personalCollectionId,
                    LoginItem = new LoginItem
                    {
                        Website = "https://netflix.com",
                        Username = "john.doe@gmail.com",
                        Email = "john.doe@gmail.com"
                    },
                    Tags = tags.Where(t => t.Name == "Personal" || t.Name == "Monthly Bills").ToList()
                },
                new PasswordItem
                {
                    Title = "Amazon",
                    Type = ItemType.Login,
                    CategoryId = emailCategoryId,
                    CollectionId = personalCollectionId,
                    LoginItem = new LoginItem
                    {
                        Website = "https://amazon.com",
                        Username = "john.doe@gmail.com",
                        Email = "john.doe@gmail.com"
                    },
                    Tags = tags.Where(t => t.Name == "Personal" || t.Name == "Daily Use").ToList()
                },
                new PasswordItem
                {
                    Title = "LinkedIn",
                    Type = ItemType.Login,
                    CategoryId = businessCategoryId,
                    CollectionId = workCollectionId,
                    LoginItem = new LoginItem
                    {
                        Website = "https://linkedin.com",
                        Username = "john.doe.professional@gmail.com",
                        Email = "john.doe.professional@gmail.com"
                    },
                    Tags = tags.Where(t => t.Name == "Work" || t.Name == "Weekly").ToList()
                },
                new PasswordItem
                {
                    Title = "Spotify",
                    Type = ItemType.Login,
                    CategoryId = emailCategoryId,
                    CollectionId = personalCollectionId,
                    LoginItem = new LoginItem
                    {
                        Website = "https://spotify.com",
                        Username = "john.doe@gmail.com",
                        Email = "john.doe@gmail.com"
                    },
                    Tags = tags.Where(t => t.Name == "Personal" || t.Name == "Daily Use").ToList()
                },
                
                // Credit Card Items
                new PasswordItem
                {
                    Title = "Chase Sapphire Preferred",
                    Type = ItemType.CreditCard,
                    CategoryId = creditCardCategoryId,
                    CollectionId = bankingCollectionId,
                    CreditCardItem = new CreditCardItem
                    {
                        CardholderName = "John Doe",
                        CardNumber = "4532 1234 5678 9012", // Fake but valid format
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
                        Notes = "Primary travel rewards card"
                    },
                    Tags = tags.Where(t => t.Name == "Important" || t.Name == "Daily Use").ToList()
                },
                new PasswordItem
                {
                    Title = "Amazon Prime Visa",
                    Type = ItemType.CreditCard,
                    CategoryId = creditCardCategoryId,
                    CollectionId = bankingCollectionId,
                    CreditCardItem = new CreditCardItem
                    {
                        CardholderName = "John Doe",
                        CardNumber = "4000 5678 9012 3456", // Fake but valid format
                        ExpiryDate = "08/2026",
                        CVV = "456",
                        CardType = CardType.Visa,
                        IssuingBank = "Chase Bank",
                        CreditLimit = "$15,000",
                        InterestRate = "21.99%",
                        CustomerServicePhone = "1-888-247-4080",
                        RewardsProgram = "Amazon Rewards",
                        BenefitsDescription = "5% back on Amazon purchases, 2% on gas/restaurants/drugstores",
                        BillingAddressLine1 = "123 Main Street",
                        BillingCity = "San Francisco",
                        BillingState = "CA",
                        BillingZipCode = "94102",
                        BillingCountry = "USA",
                        Notes = "Used primarily for Amazon purchases"
                    },
                    Tags = tags.Where(t => t.Name == "Personal" || t.Name == "Weekly").ToList()
                },
                
                // WiFi Items
                new PasswordItem
                {
                    Title = "Home WiFi Network",
                    Type = ItemType.WiFi,
                    CategoryId = wifiCategoryId,
                    CollectionId = personalCollectionId,
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
                        Notes = "Main home network, password changed quarterly"
                    },
                    Tags = tags.Where(t => t.Name == "Important" || t.Name == "Family").ToList()
                },
                new PasswordItem
                {
                    Title = "Office WiFi",
                    Type = ItemType.WiFi,
                    CategoryId = wifiCategoryId,
                    CollectionId = workCollectionId,
                    WiFiItem = new WiFiItem
                    {
                        NetworkName = "CompanySecure",
                        Password = "Corp2024Secure!",
                        SecurityType = SecurityType.WPA2,
                        Frequency = FrequencyType.FiveGHz,
                        Channel = "149",
                        Bandwidth = "40MHz",
                        WirelessStandard = "802.11ac",
                        RouterBrand = "Cisco",
                        RouterModel = "Meraki MR46",
                        Location = "Office - 5th Floor",
                        Notes = "Enterprise WiFi - connects automatically with certificate"
                    },
                    Tags = tags.Where(t => t.Name == "Work" || t.Name == "Daily Use").ToList()
                },
                new PasswordItem
                {
                    Title = "Coffee Shop WiFi",
                    Type = ItemType.WiFi,
                    CategoryId = wifiCategoryId,
                    CollectionId = personalCollectionId,
                    WiFiItem = new WiFiItem
                    {
                        NetworkName = "BlueBottle_Guest",
                        Password = "coffee2024",
                        SecurityType = SecurityType.WPA2,
                        Frequency = FrequencyType.TwoPointFourGHz,
                        Location = "Blue Bottle Coffee - Downtown",
                        Notes = "Free WiFi, password changes monthly"
                    },
                    Tags = tags.Where(t => t.Name == "Temporary" || t.Name == "Rarely Used").ToList()
                },
                
                // Secure Notes
                new PasswordItem
                {
                    Title = "Emergency Contact Information",
                    Type = ItemType.SecureNote,
                    CategoryId = secureNotesCategoryId,
                    CollectionId = personalCollectionId,
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

Family Contacts:
- Parents: (555) 345-6789
- Brother: (555) 456-7890

Important: Blood Type O+, Allergic to Penicillin",
                        Category = "Emergency",
                        TemplateType = "Emergency",
                        IsHighSecurity = true
                    },
                    Tags = tags.Where(t => t.Name == "Important" || t.Name == "Family").ToList()
                },
                new PasswordItem
                {
                    Title = "Important Document Numbers",
                    Type = ItemType.SecureNote,
                    CategoryId = secureNotesCategoryId,
                    CollectionId = personalCollectionId,
                    SecureNoteItem = new SecureNoteItem
                    {
                        Title = "Important Document Numbers",
                        Content = @"IMPORTANT DOCUMENTS:

Social Security Number: XXX-XX-XXXX (stored separately)
Driver's License: D1234567 (CA)
Passport Number: 123456789
Passport Expiry: 12/2030

Insurance Information:
- Auto Insurance Policy: AI987654321
- Health Insurance Member ID: H123456789
- Home Insurance Policy: HI456789123

Bank Account Information:
- Chase Checking: ****1234
- Chase Savings: ****5678
- Routing Number: 021000021",
                        Category = "Legal",
                        TemplateType = "Legal",
                        IsHighSecurity = true
                    },
                    Tags = tags.Where(t => t.Name == "Important" || t.Name == "High Security").ToList()
                },
                new PasswordItem
                {
                    Title = "Server Configuration Notes",
                    Type = ItemType.SecureNote,
                    CategoryId = secureNotesCategoryId,
                    CollectionId = workCollectionId,
                    SecureNoteItem = new SecureNoteItem
                    {
                        Title = "Server Configuration Notes",
                        Content = @"PRODUCTION SERVER CONFIG:

Server Details:
- Environment: Production
- Server IP: 10.0.1.100
- OS: Ubuntu 22.04 LTS
- Database: PostgreSQL 14.9

SSL Certificate:
- Provider: Let's Encrypt
- Expires: 2024-12-15
- Auto-renewal: Enabled

Backup Schedule:
- Database: Daily at 2 AM UTC
- Files: Weekly on Sundays
- Retention: 30 days

Monitoring:
- CPU Alert: >80% for 5 min
- Memory Alert: >90% for 3 min
- Disk Alert: >85% usage",
                        Category = "Technical",
                        TemplateType = "Business",
                        IsMarkdown = true
                    },
                    Tags = tags.Where(t => t.Name == "Work" || t.Name == "High Security").ToList()
                },
                
                // Passkey Items
                new PasswordItem
                {
                    Title = "Google Account Passkey",
                    Type = ItemType.Passkey,
                    CategoryId = passkeysCategoryId,
                    CollectionId = personalCollectionId,
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
                        Notes = "Primary Google account passkey, synced across devices"
                    },
                    Tags = tags.Where(t => t.Name == "Important" || t.Name == "Biometric" || t.Name == "Daily Use").ToList()
                },
                new PasswordItem
                {
                    Title = "Microsoft Account Passkey",
                    Type = ItemType.Passkey,
                    CategoryId = passkeysCategoryId,
                    CollectionId = workCollectionId,
                    PasskeyItem = new PasskeyItem
                    {
                        Website = "https://login.microsoftonline.com",
                        WebsiteUrl = "https://login.microsoftonline.com",
                        Username = "john.doe@company.com",
                        DisplayName = "John Doe (Work)",
                        DeviceType = "Windows 11 PC",
                        PlatformName = "Windows Hello",
                        IsBackedUp = false,
                        RequiresUserVerification = true,
                        LastUsedAt = DateTime.UtcNow.AddDays(-1),
                        UsageCount = 42,
                        Notes = "Work Microsoft account, Windows Hello biometric authentication"
                    },
                    Tags = tags.Where(t => t.Name == "Work" || t.Name == "Biometric" || t.Name == "Daily Use").ToList()
                },
                new PasswordItem
                {
                    Title = "GitHub Passkey",
                    Type = ItemType.Passkey,
                    CategoryId = passkeysCategoryId,
                    CollectionId = workCollectionId,
                    PasskeyItem = new PasskeyItem
                    {
                        Website = "https://github.com",
                        WebsiteUrl = "https://github.com",
                        Username = "johndoe-dev",
                        DisplayName = "John Doe - Developer",
                        DeviceType = "MacBook Pro",
                        PlatformName = "macOS",
                        IsBackedUp = true,
                        RequiresUserVerification = true,
                        LastUsedAt = DateTime.UtcNow.AddHours(-6),
                        UsageCount = 28,
                        Notes = "Development account passkey, Touch ID enabled"
                    },
                    Tags = tags.Where(t => t.Name == "Work" || t.Name == "Biometric" || t.Name == "Weekly").ToList()
                }
            );
            db.SaveChanges();
        }
    }
}
