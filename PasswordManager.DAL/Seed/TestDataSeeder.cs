using System;
using System.Collections.Generic;
using System.Linq;
using PasswordManager.Models;
using Microsoft.EntityFrameworkCore;

namespace PasswordManager.DAL.Seed;

public static class TestDataSeeder
{
    public const string TestUserId = "test-user-id-12345";

    public static void SeedTestData(PasswordManagerDbContext db)
    {
        SeedTestData(db, TestUserId, createUserIfMissing: true);
    }

    public static void SeedTestData(PasswordManagerDbContext db, string testUserId)
    {
        SeedTestData(db, testUserId, createUserIfMissing: false);
    }

    private static void SeedTestData(PasswordManagerDbContext db, string testUserId, bool createUserIfMissing)
    {
        if (createUserIfMissing && !db.Users.Any(u => u.Id == testUserId))
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

    public static void ClearSeedData(PasswordManagerDbContext db, string userId)
    {
        var items = db.PasswordItems.Where(p => p.UserId == userId).ToList();
        if (items.Any()) { db.PasswordItems.RemoveRange(items); db.SaveChanges(); }

        var categories = db.Categories.Where(c => c.UserId == userId).ToList();
        if (categories.Any()) { db.Categories.RemoveRange(categories); db.SaveChanges(); }

        var collections = db.Collections.Where(c => c.UserId == userId).ToList();
        if (collections.Any()) { db.Collections.RemoveRange(collections); db.SaveChanges(); }

        var tags = db.Tags.Where(t => t.UserId == userId).ToList();
        if (tags.Any()) { db.Tags.RemoveRange(tags); db.SaveChanges(); }
    }

    public static void SeedCollections(PasswordManagerDbContext db, string testUserId)
    {
        // Every user gets a default "Personal" vault; demo collections/items live in it
        // unless the demo data explicitly models a separate vault (e.g. "Work").
        var vault = db.Vaults.FirstOrDefault(v => v.UserId == testUserId && v.IsDefault)
                 ?? db.Vaults.FirstOrDefault(v => v.UserId == testUserId);
        if (vault == null)
        {
            vault = new Vault
            {
                Name = "Personal",
                Description = "Your personal password vault",
                IsDefault = true,
                Icon = "🔐",
                Color = "#2563EB",
                UserId = testUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Vaults.Add(vault);
            db.SaveChanges();
        }

        if (!db.Collections.Any(c => c.UserId == testUserId))
        {
            db.Collections.AddRange(
                new Collection { Name = "Banking",   Icon = "🏦", Color = "#1f2937", IsDefault = true,  UserId = testUserId, VaultId = vault.Id },
                new Collection { Name = "Insurance", Icon = "🛡️", Color = "#059669", IsDefault = false, UserId = testUserId, VaultId = vault.Id },
                new Collection { Name = "Utilities", Icon = "⚡", Color = "#dc2626", IsDefault = false, UserId = testUserId, VaultId = vault.Id },
                new Collection { Name = "Work",      Icon = "💼", Color = "#7c3aed", IsDefault = false, UserId = testUserId, VaultId = vault.Id },
                new Collection { Name = "Personal",  Icon = "👤", Color = "#3b82f6", IsDefault = false, UserId = testUserId, VaultId = vault.Id }
            );
            db.SaveChanges();
        }
    }

    public static void SeedCategories(PasswordManagerDbContext db, string testUserId)
    {
        if (!db.Categories.Any(c => c.UserId == testUserId))
        {
            db.Categories.AddRange(
                new Category { Name = "Logins",                 Icon = "🔐", Color = "#3b82f6", UserId = testUserId },
                new Category { Name = "Secure Notes",           Icon = "📝", Color = "#f59e0b", UserId = testUserId },
                new Category { Name = "Credit Cards",           Icon = "💳", Color = "#10b981", UserId = testUserId },
                new Category { Name = "Identities",             Icon = "👤", Color = "#10b981", UserId = testUserId },
                new Category { Name = "Passwords",              Icon = "🔑", Color = "#06b6d4", UserId = testUserId },
                new Category { Name = "Documents",              Icon = "📄", Color = "#3b82f6", UserId = testUserId },
                new Category { Name = "SSH Keys",               Icon = "🔗", Color = "#f59e0b", UserId = testUserId },
                new Category { Name = "API Credentials",        Icon = "</>", Color = "#06b6d4", UserId = testUserId },
                new Category { Name = "Bank Accounts",          Icon = "🏦", Color = "#f59e0b", UserId = testUserId },
                new Category { Name = "Crypto Wallets",         Icon = "₿",  Color = "#8b5cf6", UserId = testUserId },
                new Category { Name = "Databases",              Icon = "🗄️", Color = "#6b7280", UserId = testUserId },
                new Category { Name = "Driver Licenses",        Icon = "🪪", Color = "#ec4899", UserId = testUserId },
                new Category { Name = "Emails",                 Icon = "📧", Color = "#ec4899", UserId = testUserId },
                new Category { Name = "Medical Records",        Icon = "❤️", Color = "#ef4444", UserId = testUserId },
                new Category { Name = "Memberships",            Icon = "🎫", Color = "#8b5cf6", UserId = testUserId },
                new Category { Name = "Outdoor Licenses",       Icon = "🏞️", Color = "#10b981", UserId = testUserId },
                new Category { Name = "Passports",              Icon = "🌐", Color = "#3b82f6", UserId = testUserId },
                new Category { Name = "Rewards",                Icon = "🎁", Color = "#ec4899", UserId = testUserId },
                new Category { Name = "Servers",                Icon = "🖥️", Color = "#6b7280", UserId = testUserId },
                new Category { Name = "Social Security Numbers",Icon = "🆔", Color = "#3b82f6", UserId = testUserId },
                new Category { Name = "Software Licenses",      Icon = "💿", Color = "#3b82f6", UserId = testUserId },
                new Category { Name = "Wireless Routers",       Icon = "📶", Color = "#06b6d4", UserId = testUserId },
                new Category { Name = "WiFi Networks",          Icon = "📶", Color = "#06b6d4", UserId = testUserId },
                new Category { Name = "Passkeys",               Icon = "🔐", Color = "#ec4899", UserId = testUserId }
            );
            db.SaveChanges();
        }
    }

    public static void SeedTags(PasswordManagerDbContext db, string testUserId)
    {
        if (!db.Tags.Any(t => t.UserId == testUserId))
        {
            db.Tags.AddRange(
                new Tag { Name = "Important",      Color = "#ef4444", UserId = testUserId },
                new Tag { Name = "2FA",            Color = "#8b5cf6", UserId = testUserId },
                new Tag { Name = "High Security",  Color = "#7c3aed", UserId = testUserId },
                new Tag { Name = "Biometric",      Color = "#ec4899", UserId = testUserId },
                new Tag { Name = "Daily Use",      Color = "#10b981", UserId = testUserId },
                new Tag { Name = "Weekly",         Color = "#3b82f6", UserId = testUserId },
                new Tag { Name = "Monthly Bills",  Color = "#f59e0b", UserId = testUserId },
                new Tag { Name = "Rarely Used",    Color = "#6b7280", UserId = testUserId },
                new Tag { Name = "Work",           Color = "#7c3aed", UserId = testUserId },
                new Tag { Name = "Personal",       Color = "#06b6d4", UserId = testUserId },
                new Tag { Name = "Family",         Color = "#84cc16", UserId = testUserId },
                new Tag { Name = "Shared",         Color = "#f97316", UserId = testUserId },
                new Tag { Name = "Mobile App",     Color = "#8b5cf6", UserId = testUserId },
                new Tag { Name = "Web Only",       Color = "#3b82f6", UserId = testUserId },
                new Tag { Name = "Desktop",        Color = "#6b7280", UserId = testUserId },
                new Tag { Name = "Active",         Color = "#10b981", UserId = testUserId },
                new Tag { Name = "Expired",        Color = "#ef4444", UserId = testUserId },
                new Tag { Name = "Temporary",      Color = "#f59e0b", UserId = testUserId },
                new Tag { Name = "Backup Account", Color = "#8b5cf6", UserId = testUserId }
            );
            db.SaveChanges();
        }
    }

    // Removes existing items for the user then seeds ~100 fresh demo items.
    public static void ForceSeedPasswordItems(PasswordManagerDbContext db, string userId)
    {
        var existing = db.PasswordItems.Where(p => p.UserId == userId).ToList();
        if (existing.Any()) { db.PasswordItems.RemoveRange(existing); db.SaveChanges(); }
        SeedPasswordItemsCore(db, userId);
    }

    // Used by SampleDataSeeder — each user gets their own seed data independently.
    public static void SeedPasswordItemsForUser(PasswordManagerDbContext db, string userId)
    {
        SeedPasswordItemsCore(db, userId);
    }

    private static void SeedPasswordItems(PasswordManagerDbContext db, string testUserId)
    {
        if (!db.PasswordItems.Any())
            SeedPasswordItemsCore(db, testUserId);
    }

    private static void SeedPasswordItemsCore(PasswordManagerDbContext db, string uid)
    {
        // Load lookup data scoped to this user so IDs are correct
        var cats = db.Categories.Where(c => c.UserId == uid).ToList();
        var tags = db.Tags.Where(t => t.UserId == uid).ToList();
        var cols = db.Collections.Where(c => c.UserId == uid).ToList();

        int Col(string name) => cols.FirstOrDefault(c => c.Name == name)?.Id
                                ?? cols.FirstOrDefault()?.Id ?? 1;
        int Cat(string name) => cats.FirstOrDefault(c => c.Name == name)?.Id
                                ?? cats.FirstOrDefault()?.Id ?? 1;
        List<Tag> T(params string[] names) =>
            tags.Where(t => names.Contains(t.Name)).ToList();

        int bankCol     = Col("Banking");
        int workCol     = Col("Work");
        int personalCol = Col("Personal");

        int loginCat    = Cat("Logins");
        int cardCat     = Cat("Credit Cards");
        int noteCat     = Cat("Secure Notes");
        int wifiCat     = Cat("WiFi Networks");
        int passkeyCat  = Cat("Passkeys");
        int pwdCat      = Cat("Passwords");

        var all = new List<PasswordItem>();

        // ── Logins (~77) ────────────────────────────────────────────────────────

        // Banking & Finance
        all.Add(L("Chase Bank Online",         "john.doe@email.com",   "Tr0ub4d0r&3!",      "https://chase.com",                  loginCat, bankCol,     uid, T("Important","High Security","Daily Use")));
        all.Add(L("Bank of America",           "john.doe@email.com",   "M0nk3yBr@in$!",     "https://bankofamerica.com",           loginCat, bankCol,     uid, T("Important","High Security")));
        all.Add(L("Wells Fargo",               "jdoe1985",             "C0rr3ctH0rs3!",      "https://wellsfargo.com",              loginCat, bankCol,     uid, T("Important","2FA")));
        all.Add(L("Capital One",               "john.doe@email.com",   "C@p1t@l0ne#2024",   "https://capitalone.com",              loginCat, bankCol,     uid, T("Monthly Bills","2FA")));
        all.Add(L("Fidelity Investments",      "jdoe.fidelity",        "F1d3l1ty$ecure!",    "https://fidelity.com",               loginCat, bankCol,     uid, T("Important","High Security")));
        all.Add(L("Charles Schwab",            "john.doe@email.com",   "Schw@bInv3st!",      "https://schwab.com",                  loginCat, bankCol,     uid, T("Important")));
        all.Add(L("Vanguard",                  "john.doe@email.com",   "V@ngu@rd#2024!",     "https://vanguard.com",                loginCat, bankCol,     uid, T("Important","High Security")));
        all.Add(L("PayPal",                    "john.doe@gmail.com",   "P@yP4l$ecure!",      "https://paypal.com",                  loginCat, personalCol, uid, T("Important","Daily Use","2FA")));
        all.Add(L("Venmo",                     "john.doe@gmail.com",   "V3nm0P@ss!",         "https://venmo.com",                   loginCat, personalCol, uid, T("Personal","Daily Use")));
        all.Add(L("Robinhood",                 "john.doe@email.com",   "R0b1nH00d#Inv!",     "https://robinhood.com",               loginCat, bankCol,     uid, T("Important","2FA")));
        all.Add(L("Coinbase",                  "john.doe@email.com",   "C01nb@se#Crypto!",   "https://coinbase.com",                loginCat, bankCol,     uid, T("Important","High Security","2FA")));
        all.Add(L("Binance",                   "john.doe@email.com",   "B1n@nce$ecure!",     "https://binance.com",                 loginCat, bankCol,     uid, T("High Security","2FA")));

        // Email
        all.Add(L("Personal Gmail",            "john.doe@gmail.com",   "Gm@1l#S3cure!",      "https://gmail.com",                   loginCat, personalCol, uid, T("Important","Daily Use","2FA")));
        all.Add(L("Work Email (Outlook)",      "john.doe@company.com", "0utl00k#W0rk!",      "https://outlook.com",                 loginCat, workCol,     uid, T("Work","Daily Use","2FA")));
        all.Add(L("Yahoo Mail",                "johndoe1985@yahoo.com","Y@ho0M@1l!",         "https://mail.yahoo.com",              loginCat, personalCol, uid, T("Personal","Backup Account")));
        all.Add(L("ProtonMail",                "johndoe@proton.me",    "Pr0t0n#S3cure!",     "https://proton.me",                   loginCat, personalCol, uid, T("High Security","Personal")));
        all.Add(L("iCloud / Apple ID",         "john.doe@icloud.com",  "Appl3#1Cl0ud!",      "https://appleid.apple.com",           loginCat, personalCol, uid, T("Important","Daily Use","2FA","High Security")));

        // Productivity & Work
        all.Add(L("Microsoft 365",             "john.doe@company.com", "M1cr0s0ft#365!",     "https://office.com",                  loginCat, workCol,     uid, T("Work","Daily Use","2FA")));
        all.Add(L("Google Workspace",          "john.doe@company.com", "G00gl3#W0rk!",       "https://workspace.google.com",        loginCat, workCol,     uid, T("Work","Daily Use")));
        all.Add(L("GitHub",                    "johndoe-dev",          "G1tHub#D3v2024!",    "https://github.com",                  loginCat, workCol,     uid, T("Work","2FA","Daily Use")));
        all.Add(L("GitLab",                    "johndoe",              "G1tL@b$ecure!",      "https://gitlab.com",                  loginCat, workCol,     uid, T("Work","2FA")));
        all.Add(L("Jira",                      "john.doe@company.com", "J1r@#Pr0ject!",      "https://atlassian.net",               loginCat, workCol,     uid, T("Work","Daily Use")));
        all.Add(L("Confluence",                "john.doe@company.com", "C0nflu3nce#!",       "https://atlassian.net",               loginCat, workCol,     uid, T("Work")));
        all.Add(L("Slack",                     "john.doe@company.com", "Sl@ck#T3@m!",        "https://slack.com",                   loginCat, workCol,     uid, T("Work","Daily Use")));
        all.Add(L("Zoom",                      "john.doe@company.com", "Z00m#M33ting!",      "https://zoom.us",                     loginCat, workCol,     uid, T("Work","Daily Use")));
        all.Add(L("Notion",                    "john.doe@gmail.com",   "N0t10n#N0tes!",      "https://notion.so",                   loginCat, workCol,     uid, T("Work","Personal","Daily Use")));
        all.Add(L("Trello",                    "john.doe@gmail.com",   "Tr3ll0#B0@rd!",      "https://trello.com",                  loginCat, workCol,     uid, T("Work")));
        all.Add(L("Figma",                     "john.doe@company.com", "F1gm@#D3s1gn!",      "https://figma.com",                   loginCat, workCol,     uid, T("Work")));
        all.Add(L("Asana",                     "john.doe@company.com", "@s@n@#T@sk!",        "https://asana.com",                   loginCat, workCol,     uid, T("Work")));
        all.Add(L("Monday.com",                "john.doe@company.com", "M0nd@y#PM!",         "https://monday.com",                  loginCat, workCol,     uid, T("Work")));

        // Cloud / DevOps
        all.Add(L("AWS Console",               "john.doe@company.com", "AWS#Cl0ud2024!",     "https://aws.amazon.com",              loginCat, workCol,     uid, T("Work","High Security","2FA")));
        all.Add(L("Google Cloud Platform",     "john.doe@company.com", "GCP#Cl0ud!2024",     "https://console.cloud.google.com",    loginCat, workCol,     uid, T("Work","High Security")));
        all.Add(L("Azure Portal",              "john.doe@company.com", "Az$ure#P0rt@l!",     "https://portal.azure.com",            loginCat, workCol,     uid, T("Work","High Security")));
        all.Add(L("Cloudflare",                "john.doe@company.com", "Cl0udfl@re#DNS!",    "https://cloudflare.com",              loginCat, workCol,     uid, T("Work","2FA")));
        all.Add(L("DigitalOcean",              "john.doe@gmail.com",   "D1g1t@l0ce@n!",      "https://digitalocean.com",            loginCat, workCol,     uid, T("Work","2FA")));
        all.Add(L("Heroku",                    "john.doe@company.com", "H3r0ku#D3pl0y!",     "https://heroku.com",                  loginCat, workCol,     uid, T("Work")));

        // Streaming
        all.Add(L("Netflix",                   "john.doe@gmail.com",   "N3tfl1x#Str3@m!",    "https://netflix.com",                 loginCat, personalCol, uid, T("Personal","Monthly Bills")));
        all.Add(L("Hulu",                      "john.doe@gmail.com",   "Hulu#Str3@m!",       "https://hulu.com",                    loginCat, personalCol, uid, T("Personal","Monthly Bills")));
        all.Add(L("Disney+",                   "john.doe@gmail.com",   "D1sn3y#Plus!",       "https://disneyplus.com",              loginCat, personalCol, uid, T("Personal","Monthly Bills","Family")));
        all.Add(L("HBO Max",                   "john.doe@gmail.com",   "HB0#M@x2024!",       "https://max.com",                     loginCat, personalCol, uid, T("Personal","Monthly Bills")));
        all.Add(L("Spotify",                   "john.doe@gmail.com",   "Sp0t1fy#Mus1c!",     "https://spotify.com",                 loginCat, personalCol, uid, T("Personal","Monthly Bills","Daily Use")));
        all.Add(L("Apple Music",               "john.doe@icloud.com",  "Appl3#Mus1c!",       "https://music.apple.com",             loginCat, personalCol, uid, T("Personal","Monthly Bills")));
        all.Add(L("YouTube Premium",           "john.doe@gmail.com",   "Y0uTub3#Pr3m!",      "https://youtube.com",                 loginCat, personalCol, uid, T("Personal","Monthly Bills","Daily Use")));
        all.Add(L("Amazon Prime Video",        "john.doe@gmail.com",   "Am@z0n#Pr1me!",      "https://primevideo.com",              loginCat, personalCol, uid, T("Personal","Monthly Bills")));
        all.Add(L("Twitch",                    "johndoe_games",        "Tw1tch#Str3@m!",     "https://twitch.tv",                   loginCat, personalCol, uid, T("Personal","Daily Use")));
        all.Add(L("Peacock",                   "john.doe@gmail.com",   "Pe@cock#TV!",        "https://peacocktv.com",               loginCat, personalCol, uid, T("Personal","Monthly Bills")));
        all.Add(L("Paramount+",                "john.doe@gmail.com",   "P@r@m0unt#Plus!",    "https://paramountplus.com",           loginCat, personalCol, uid, T("Personal","Monthly Bills")));

        // Shopping
        all.Add(L("Amazon",                    "john.doe@gmail.com",   "Am@z0n#Sh0p!",       "https://amazon.com",                  loginCat, personalCol, uid, T("Personal","Daily Use")));
        all.Add(L("eBay",                      "johndoe1985",          "3B@y#S3ll2024!",     "https://ebay.com",                    loginCat, personalCol, uid, T("Personal")));
        all.Add(L("Walmart",                   "john.doe@email.com",   "W@lm@rt#Buy!",       "https://walmart.com",                 loginCat, personalCol, uid, T("Personal")));
        all.Add(L("Target",                    "john.doe@email.com",   "T@rg3t#Sh0p!",       "https://target.com",                  loginCat, personalCol, uid, T("Personal")));
        all.Add(L("Best Buy",                  "john.doe@email.com",   "B3stBuy#T3ch!",      "https://bestbuy.com",                 loginCat, personalCol, uid, T("Personal")));
        all.Add(L("Costco",                    "john.doe@email.com",   "C0stco#Bulk!",       "https://costco.com",                  loginCat, personalCol, uid, T("Personal","Family")));
        all.Add(L("Etsy",                      "johndoecrafts",        "3tsy#Cr@ft!",        "https://etsy.com",                    loginCat, personalCol, uid, T("Personal")));
        all.Add(L("Shopify Admin",             "john.doe@company.com", "Sh0p1fy#$t0re!",     "https://shopify.com",                 loginCat, workCol,     uid, T("Work")));

        // Social Media
        all.Add(L("Twitter / X",               "johndoe",              "Tw1tt3r#P0st!",      "https://x.com",                       loginCat, personalCol, uid, T("Personal","Daily Use")));
        all.Add(L("Facebook",                  "john.doe@gmail.com",   "F@c3b00k!2024",      "https://facebook.com",                loginCat, personalCol, uid, T("Personal","Family")));
        all.Add(L("Instagram",                 "johndoe_photos",       "1nst@gr@m#P1c!",     "https://instagram.com",               loginCat, personalCol, uid, T("Personal","Daily Use")));
        all.Add(L("LinkedIn",                  "john.doe@email.com",   "L1nk3dIn#Pr0f!",     "https://linkedin.com",                loginCat, workCol,     uid, T("Work","Personal")));
        all.Add(L("Reddit",                    "johndoe_reddit",       "R3dd1t#P0st!",       "https://reddit.com",                  loginCat, personalCol, uid, T("Personal","Daily Use")));
        all.Add(L("TikTok",                    "johndoe_tiktok",       "T1kT0k#V1d!",        "https://tiktok.com",                  loginCat, personalCol, uid, T("Personal")));
        all.Add(L("Snapchat",                  "johndoe_snap",         "Sn@pch@t!2024",      "https://snapchat.com",                loginCat, personalCol, uid, T("Personal")));
        all.Add(L("Pinterest",                 "johndoe_pins",         "P1nt3r3st#1d3@!",    "https://pinterest.com",               loginCat, personalCol, uid, T("Personal")));
        all.Add(L("Discord",                   "johndoe#1234",         "D1sc0rd#Ch@t!",      "https://discord.com",                 loginCat, personalCol, uid, T("Personal","Daily Use")));

        // Gaming
        all.Add(L("Steam",                     "johndoe_steam",        "St3@m#G@mes!",       "https://store.steampowered.com",      loginCat, personalCol, uid, T("Personal","2FA")));
        all.Add(L("Xbox",                      "johndoe_xbox",         "Xb0x#G@mer!",        "https://xbox.com",                    loginCat, personalCol, uid, T("Personal","2FA")));
        all.Add(L("PlayStation Network",       "johndoe_psn",          "PSN#G@mer2024!",     "https://account.sonyentertainmentnetwork.com", loginCat, personalCol, uid, T("Personal","2FA")));
        all.Add(L("Nintendo Account",          "john.doe@gmail.com",   "N1nt3nd0#Pl@y!",     "https://accounts.nintendo.com",       loginCat, personalCol, uid, T("Personal","Family")));
        all.Add(L("Epic Games",                "johndoe_epic",         "3p1cG@mes#Fr33!",    "https://epicgames.com",               loginCat, personalCol, uid, T("Personal","2FA")));

        // Telecom & Utilities
        all.Add(L("Verizon",                   "john.doe@email.com",   "Ver1z0n#Ph0ne!",     "https://verizon.com",                 loginCat, personalCol, uid, T("Personal","Monthly Bills")));
        all.Add(L("AT&T",                      "john.doe@att.net",     "@T&T#M0b1l3!",       "https://att.com",                     loginCat, personalCol, uid, T("Personal","Monthly Bills")));
        all.Add(L("T-Mobile",                  "john.doe@gmail.com",   "TM0b1l3#5G!",        "https://t-mobile.com",                loginCat, personalCol, uid, T("Personal","Monthly Bills")));
        all.Add(L("Comcast Xfinity",           "john.doe@comcast.net", "Xf1n1ty#1nt3rnet!",  "https://xfinity.com",                 loginCat, personalCol, uid, T("Personal","Monthly Bills")));
        all.Add(L("Google One",                "john.doe@gmail.com",   "G00gl3#0n3!",        "https://one.google.com",              loginCat, personalCol, uid, T("Personal","Monthly Bills")));

        // Travel & Food Delivery
        all.Add(L("Uber",                      "john.doe@gmail.com",   "Ub3r#R1d3!",         "https://uber.com",                    loginCat, personalCol, uid, T("Personal","Daily Use")));
        all.Add(L("Lyft",                      "john.doe@gmail.com",   "Lyft#R1d32024",      "https://lyft.com",                    loginCat, personalCol, uid, T("Personal")));
        all.Add(L("DoorDash",                  "john.doe@gmail.com",   "D00rD@sh#F00d!",     "https://doordash.com",                loginCat, personalCol, uid, T("Personal","Daily Use")));
        all.Add(L("Airbnb",                    "john.doe@gmail.com",   "@1rbnb#Tr@v3l!",     "https://airbnb.com",                  loginCat, personalCol, uid, T("Personal")));
        all.Add(L("Expedia",                   "john.doe@gmail.com",   "3xp3d1@#Tr1p!",      "https://expedia.com",                 loginCat, personalCol, uid, T("Personal","Rarely Used")));

        // Software & Tools
        all.Add(L("Adobe Creative Cloud",      "john.doe@gmail.com",   "Ad0b3#Cr3@t1v3!",    "https://adobe.com",                   loginCat, workCol,     uid, T("Work","Monthly Bills")));
        all.Add(L("Dropbox",                   "john.doe@gmail.com",   "Dr0pb0x#Cl0ud!",     "https://dropbox.com",                 loginCat, personalCol, uid, T("Personal","Work")));
        all.Add(L("Evernote",                  "john.doe@gmail.com",   "3v3rn0t3#N0tes!",    "https://evernote.com",                loginCat, personalCol, uid, T("Personal")));
        all.Add(L("Stripe Dashboard",          "john.doe@company.com", "Str1pe#P@y!",        "https://stripe.com",                  loginCat, workCol,     uid, T("Work","High Security","2FA")));
        all.Add(L("QuickBooks Online",         "john.doe@company.com", "QB#@cc0unting!",     "https://quickbooks.intuit.com",       loginCat, workCol,     uid, T("Work")));
        all.Add(L("TurboTax",                  "john.doe@email.com",   "Turb0T@x#2024!",     "https://turbotax.intuit.com",         loginCat, personalCol, uid, T("Personal","Rarely Used")));
        all.Add(L("LastPass",                  "john.doe@gmail.com",   "L@stP@ss#0ld!",      "https://lastpass.com",                loginCat, personalCol, uid, T("Backup Account","Rarely Used")));
        all.Add(L("Dashlane",                  "john.doe@gmail.com",   "D@shl@ne#Old!",      "https://dashlane.com",                loginCat, personalCol, uid, T("Backup Account","Rarely Used")));

        // ── Credit Cards (~15) ──────────────────────────────────────────────────
        all.Add(CC("Chase Sapphire Preferred",  "John Doe", "4532 1234 5678 9012", "12/2027", "123",  CardType.Visa,       "Chase Bank",           "https://chase.com",              "Ultimate Rewards",     "$25,000", "18.99%", cardCat, bankCol,     uid, T("Important","Daily Use")));
        all.Add(CC("Chase Freedom Unlimited",   "John Doe", "4532 9876 5432 1098", "09/2026", "456",  CardType.Visa,       "Chase Bank",           "https://chase.com",              "Ultimate Rewards",     "$15,000", "19.99%", cardCat, bankCol,     uid, T("Daily Use")));
        all.Add(CC("American Express Gold",     "John Doe", "3714 496353 98431",   "06/2028", "7890", CardType.AmericanExpress,       "American Express",     "https://americanexpress.com",    "Membership Rewards",   "$30,000", "24.99%", cardCat, bankCol,     uid, T("Important","Daily Use")));
        all.Add(CC("Capital One Venture",       "John Doe", "5425 2334 3010 9903", "03/2027", "234",  CardType.MasterCard, "Capital One",          "https://capitalone.com",         "Venture Miles",        "$20,000", "22.49%", cardCat, bankCol,     uid, T("Daily Use")));
        all.Add(CC("Discover it Cash Back",     "John Doe", "6011 1111 1111 1117", "11/2026", "321",  CardType.Other,      "Discover",             "https://discover.com",           "Cashback Match",       "$10,000", "17.24%", cardCat, bankCol,     uid, T("Monthly Bills")));
        all.Add(CC("Citi Double Cash",          "John Doe", "5532 8500 5678 1234", "08/2027", "789",  CardType.MasterCard, "Citibank",             "https://citi.com",               "ThankYou Points",      "$12,000", "20.49%", cardCat, bankCol,     uid, T("Daily Use")));
        all.Add(CC("Bank of America Travel",    "John Doe", "4111 1111 1111 1111", "05/2028", "567",  CardType.Visa,       "Bank of America",      "https://bankofamerica.com",      "BankAmeriDeals",       "$18,000", "18.74%", cardCat, bankCol,     uid, T("Daily Use")));
        all.Add(CC("Apple Card",                "John Doe", "4147 2024 9999 0001", "07/2027", "999",  CardType.Visa,       "Goldman Sachs / Apple","https://apple.com/apple-card",   "Daily Cash",           "$10,000", "15.99%", cardCat, personalCol, uid, T("Daily Use","Mobile App")));
        all.Add(CC("Amazon Prime Visa",         "John Doe", "4000 0000 0000 0002", "10/2026", "111",  CardType.Visa,       "Chase Bank / Amazon",  "https://amazon.com",             "Amazon Rewards",       "$8,000",  "19.99%", cardCat, personalCol, uid, T("Daily Use","Monthly Bills")));
        all.Add(CC("Wells Fargo Active Cash",   "John Doe", "4012 8888 8888 1881", "01/2027", "222",  CardType.Visa,       "Wells Fargo",          "https://wellsfargo.com",         "Active Cash Rewards",  "$12,000", "20.24%", cardCat, bankCol,     uid, T("Daily Use")));
        all.Add(CC("United Explorer Card",      "John Doe", "5500 0000 0000 0004", "04/2028", "333",  CardType.MasterCard, "Chase Bank",           "https://chase.com",              "MileagePlus",          "$16,000", "21.99%", cardCat, personalCol, uid, T("Rarely Used")));
        all.Add(CC("Marriott Bonvoy Boundless", "John Doe", "4005 5192 0000 0004", "02/2027", "444",  CardType.Visa,       "Chase Bank",           "https://chase.com",              "Bonvoy Points",        "$14,000", "21.49%", cardCat, personalCol, uid, T("Rarely Used")));
        all.Add(CC("Delta SkyMiles Gold Amex",  "John Doe", "3782 822463 10005",   "12/2026", "5555", CardType.AmericanExpress,       "American Express",     "https://americanexpress.com",    "SkyMiles",             "$10,000", "22.99%", cardCat, personalCol, uid, T("Rarely Used")));
        all.Add(CC("Southwest Rapid Rewards",   "John Doe", "4263 9826 4026 9299", "09/2027", "666",  CardType.Visa,       "Chase Bank",           "https://chase.com",              "Rapid Rewards",        "$10,000", "20.99%", cardCat, personalCol, uid, T("Rarely Used")));
        all.Add(CC("Costco Anywhere Visa",      "John Doe", "4539 1488 0343 6467", "06/2026", "777",  CardType.Visa,       "Citibank",             "https://citi.com",               "Costco Cash Rewards",  "$15,000", "20.49%", cardCat, bankCol,     uid, T("Monthly Bills","Family")));

        // ── Secure Notes (~7) ───────────────────────────────────────────────────
        all.Add(SN("Emergency Contacts",
            "EMERGENCY CONTACTS\n\nSpouse: Jane Doe — (555) 123-4567\nDoctor: Dr. Smith — (555) 234-5678\nHospital: UCSF Medical Center\nInsurance: Blue Cross  Policy #ABC123456789\nBlood Type: O+  Allergies: Penicillin",
            noteCat, personalCol, uid, T("Important","Family")));
        all.Add(SN("Social Security Number",
            "SSN: 123-45-6789\nIssued: 1990\nKeep confidential — never share via email or phone",
            noteCat, personalCol, uid, T("Important","High Security")));
        all.Add(SN("Medical Information",
            "Primary Doctor: Dr. Sarah Smith, UCSF\nBlood Type: O+\nAllergies: Penicillin, Shellfish\nMedications: Lisinopril 10mg daily\nHealth Insurance: Blue Cross Blue Shield\nPolicy #: ABC123456789\nGroup #: GRP987654",
            noteCat, personalCol, uid, T("Important","Family")));
        all.Add(SN("Home Alarm Code",
            "Front door keypad: 4892#\nBack door: 9274#\nMaster override: 0000#\nMonitoring: ADT — 1-800-238-2727\nAccount #: ADT-00123456",
            noteCat, personalCol, uid, T("Important","Family","High Security")));
        all.Add(SN("Safe Combination",
            "Master bedroom wall safe:\nCombination: 15-32-7\nManufacturer: SentrySafe  Model: SFW123GDC\nBackup key: bank safe deposit box",
            noteCat, personalCol, uid, T("Important","High Security")));
        all.Add(SN("Server SSH Keys",
            "Production Server\nHost: prod.myapp.com  User: ubuntu\nKey: ~/.ssh/prod_rsa\n\nStaging Server\nHost: staging.myapp.com  User: ubuntu\nKey: ~/.ssh/staging_rsa",
            noteCat, workCol, uid, T("Work","High Security")));
        all.Add(SN("API Keys Reference",
            "SendGrid: SG.xxxxxxxxxxxxxxxxxxxxxxxxxxxx\nStripe Test: sk_test_xxxxxxxxxxxx\nGoogle Maps: AIzaSyXxxxxxxxxxxxx\n\nNOTE: Production keys stored in AWS Secrets Manager",
            noteCat, workCol, uid, T("Work","High Security")));

        // ── WiFi Networks ────────────────────────────────────────────────────────
        all.Add(WF("Home WiFi — 5GHz",   "DoeFamily_5G",      "MyS3cur3H0m32024!", SecurityType.WPA3, FrequencyType.FiveGHz,          "ASUS AX6000",  "192.168.1.1", "Comcast Xfinity", wifiCat, personalCol, uid, T("Important","Family")));
        all.Add(WF("Home WiFi — 2.4GHz", "DoeFamily_2G",      "MyS3cur3H0m32024!", SecurityType.WPA3, FrequencyType.TwoPointFourGHz,  "ASUS AX6000",  "192.168.1.1", "Comcast Xfinity", wifiCat, personalCol, uid, T("Family")));
        all.Add(WF("Office WiFi",         "CompanyWiFi_Corp",  "C0rp0r@t3#2024!",  SecurityType.WPA2, FrequencyType.FiveGHz,          "Cisco Meraki", "10.0.0.1",    "AT&T Business",   wifiCat, workCol,     uid, T("Work")));
        all.Add(WF("Guest Network",       "DoeFamily_Guest",   "Gu3st#2024!",       SecurityType.WPA2, FrequencyType.TwoPointFourGHz,  "ASUS AX6000",  "192.168.1.1", "Comcast Xfinity", wifiCat, personalCol, uid, T("Family","Shared")));
        all.Add(WF("Lake House WiFi",     "LakeHouse_Net",     "L@k3#H0us32024!",   SecurityType.WPA2, FrequencyType.FiveGHz,          "Netgear Orbi", "192.168.2.1", "Spectrum",        wifiCat, personalCol, uid, T("Family","Rarely Used")));

        // ── Passkeys ─────────────────────────────────────────────────────────────
        all.Add(PK("Google Account Passkey", "john.doe@gmail.com",   "John Doe", "https://accounts.google.com",  passkeyCat, personalCol, uid, T("Important","Biometric","Daily Use")));
        all.Add(PK("GitHub Passkey",         "johndoe-dev",          "John Doe", "https://github.com",           passkeyCat, workCol,     uid, T("Work","Biometric","2FA")));
        all.Add(PK("Microsoft Passkey",      "john.doe@company.com", "John Doe", "https://login.microsoft.com",  passkeyCat, workCol,     uid, T("Work","Biometric")));

        // ── Generic Passwords ─────────────────────────────────────────────────────
        all.Add(GP("MacBook Pro Login",       "johndoe",     "M@cB00kPr0#L0g1n!",  "Main user account — MacBook Pro 14-inch M3",        pwdCat, personalCol, uid, T("Personal","Daily Use")));
        all.Add(GP("Windows Work Laptop",     "john.doe",    "W1nd0ws#L0g1n!",      "HP EliteBook login — Windows 11 Pro",               pwdCat, workCol,     uid, T("Work","Daily Use")));
        all.Add(GP("Router Admin",            "admin",       "R0ut3r@dm1n#!",       "ASUS AX6000 admin — https://192.168.1.1",           pwdCat, personalCol, uid, T("Important","Rarely Used")));
        all.Add(GP("Backup Drive Encryption", "johndoe",     "B@ckupDr1v3#!",       "FileVault encryption — WD 4TB external drive",       pwdCat, personalCol, uid, T("High Security","Backup Account")));
        all.Add(GP("iPhone PIN",              "john.doe",    "847293",              "6-digit passcode for iPhone 15 Pro",                 pwdCat, personalCol, uid, T("Personal","Daily Use")));
        all.Add(GP("Home Safe Combination",   "John Doe",    "15-32-07",            "SentrySafe wall safe behind master bedroom mirror",  pwdCat, personalCol, uid, T("Important","High Security","Rarely Used")));

        db.PasswordItems.AddRange(all);
        db.SaveChanges();
    }

    // ── Builder helpers ──────────────────────────────────────────────────────────

    private static PasswordItem L(
        string title, string username, string password, string website,
        int catId, int colId, string uid, List<Tag> tags) =>
        new()
        {
            Title = title, Type = ItemType.Login,
            CategoryId = catId, CollectionId = colId, UserId = uid,
            CreatedAt = DateTime.UtcNow, LastModified = DateTime.UtcNow,
            LoginItem = new LoginItem
            {
                WebsiteUrl = website, Website = website,
                Username = username,
                Email = username.Contains('@') ? username : null,
                EncryptedPassword = password,
                UserId = uid, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            },
            Tags = tags
        };

    private static PasswordItem CC(
        string title, string cardholder, string number, string expiry, string cvv,
        CardType cardType, string bank, string website, string rewards,
        string limit, string rate,
        int catId, int colId, string uid, List<Tag> tags)
    {
        return new PasswordItem
        {
            Title = title, Type = ItemType.CreditCard,
            CategoryId = catId, CollectionId = colId, UserId = uid,
            CreatedAt = DateTime.UtcNow, LastModified = DateTime.UtcNow,
            CreditCardItem = new CreditCardItem
            {
                CardholderName = cardholder, CardNumber = number,
                ExpiryDate = expiry, CVV = cvv, CardType = cardType,
                IssuingBank = bank, BankWebsite = website,
                RewardsProgram = rewards, CreditLimit = limit, InterestRate = rate,
                UserId = uid
            },
            Tags = tags
        };
    }

    private static PasswordItem SN(
        string title, string content, int catId, int colId, string uid, List<Tag> tags) =>
        new()
        {
            Title = title, Type = ItemType.SecureNote,
            CategoryId = catId, CollectionId = colId, UserId = uid,
            CreatedAt = DateTime.UtcNow, LastModified = DateTime.UtcNow,
            SecureNoteItem = new SecureNoteItem
            {
                Title = title, Content = content, IsHighSecurity = true, UserId = uid
            },
            Tags = tags
        };

    private static PasswordItem WF(
        string title, string ssid, string password,
        SecurityType security, FrequencyType frequency,
        string router, string routerIp, string isp,
        int catId, int colId, string uid, List<Tag> tags)
    {
        return new PasswordItem
        {
            Title = title, Type = ItemType.WiFi,
            CategoryId = catId, CollectionId = colId, UserId = uid,
            CreatedAt = DateTime.UtcNow, LastModified = DateTime.UtcNow,
            WiFiItem = new WiFiItem
            {
                NetworkName = ssid, Password = password,
                SecurityType = security, Frequency = frequency,
                RouterBrand = router, RouterIP = routerIp, ISPName = isp,
                UserId = uid
            },
            Tags = tags
        };
    }

    private static PasswordItem PK(
        string title, string username, string displayName, string website,
        int catId, int colId, string uid, List<Tag> tags) =>
        new()
        {
            Title = title, Type = ItemType.Passkey,
            CategoryId = catId, CollectionId = colId, UserId = uid,
            CreatedAt = DateTime.UtcNow, LastModified = DateTime.UtcNow,
            PasskeyItem = new PasskeyItem
            {
                Website = website, WebsiteUrl = website,
                Username = username, DisplayName = displayName,
                IsBackedUp = true, RequiresUserVerification = true,
                UserId = uid
            },
            Tags = tags
        };

    private static PasswordItem GP(
        string title, string username, string password, string description,
        int catId, int colId, string uid, List<Tag> tags) =>
        new()
        {
            Title = title, Type = ItemType.Password,
            Description = description,
            CategoryId = catId, CollectionId = colId, UserId = uid,
            CreatedAt = DateTime.UtcNow, LastModified = DateTime.UtcNow,
            LoginItem = new LoginItem
            {
                Username = username,
                EncryptedPassword = password, UserId = uid,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            },
            Tags = tags
        };
}
