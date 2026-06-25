using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PasswordManager.DAL.Seed;
using PasswordManager.Crypto.Interfaces;
using PasswordManager.Services.Interfaces;
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
                // Always use a proper scope so scoped services (DbContext) are resolved correctly.
                using var scope = serviceProvider.CreateScope();
                var sp = scope.ServiceProvider;

                var cryptoService = sp.GetRequiredService<IPasswordCryptoService>();
                var db = sp.GetRequiredService<PasswordManager.DAL.PasswordManagerDbContext>();

                // Prefer the authenticated user; fall back to first user in DB; create demo if none.
                var authService = sp.GetService<IAuthService>();
                string? seedUserId = authService?.CurrentUser?.Id;

                if (string.IsNullOrEmpty(seedUserId))
                {
                    var firstUser = await db.Users.FirstOrDefaultAsync();
                    seedUserId = firstUser?.Id ?? await CreateDemoUserAsync(db, cryptoService);
                }

                if (string.IsNullOrEmpty(seedUserId)) return;

                // Ensure vault schema exists on this connection before any Collection INSERT
                await EnsureVaultSchemaOnConnectionAsync(db);

                // If the user deliberately cleared their data (seed marker present), do NOT re-add any
                // demo content. This is what made cleared data reappear when returning to the items page.
                if (SeedMarkerExists(db))
                    return;

                // Seed shared lookup data (categories, collections, tags) only once globally.
                TestDataSeeder.SeedCollections(db, seedUserId);
                TestDataSeeder.SeedCategories(db, seedUserId);
                TestDataSeeder.SeedTags(db, seedUserId);

                // Seed default vaults for this user if none exist
                var vaultService = sp.GetService<PasswordManager.Services.Interfaces.IVaultService>();
                if (vaultService != null && !db.Vaults.Any(v => v.UserId == seedUserId))
                {
                    await vaultService.SeedDefaultVaultsAsync(seedUserId);
                }

                // Seed password items for THIS user only if they have none yet.
                if (!await db.PasswordItems.AnyAsync(p => p.UserId == seedUserId))
                {
                    TestDataSeeder.SeedPasswordItemsForUser(db, seedUserId);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SampleDataSeeder] Failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // True when a "<db>.seeded" marker exists next to the SQLite database, meaning demo data has
        // already been seeded once (or was deliberately cleared) and must not be re-added.
        private static bool SeedMarkerExists(PasswordManager.DAL.PasswordManagerDbContext db)
        {
            try
            {
                var providerName = db.Database.ProviderName ?? string.Empty;
                if (!providerName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
                    return false;

                var connectionString = db.Database.GetConnectionString();
                if (string.IsNullOrEmpty(connectionString)) return false;

                foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    var trimmed = part.Trim();
                    if (trimmed.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.StartsWith("DataSource=", StringComparison.OrdinalIgnoreCase))
                    {
                        var path = trimmed[(trimmed.IndexOf('=') + 1)..].Trim();
                        if (string.IsNullOrEmpty(path) || path.Equals(":memory:", StringComparison.OrdinalIgnoreCase))
                            return false;
                        return System.IO.File.Exists(path + ".seeded");
                    }
                }
            }
            catch { /* best-effort */ }
            return false;
        }

        /// <summary>
        /// Inline schema guard — creates Vault table and VaultId column if missing.
        /// Runs on the same connection the seeder is about to use so the INSERT won't fail.
        /// </summary>
        /// <summary>
        /// Inline schema guard that runs on the seeder's own DbContext connection.
        /// Creates any missing tables/columns before EF Core INSERT statements run.
        /// </summary>
        private static async Task EnsureVaultSchemaOnConnectionAsync(PasswordManager.DAL.PasswordManagerDbContext db)
        {
            try
            {
                var conn = db.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync();

                async Task<bool> TableExists(string name)
                {
                    using var c = conn.CreateCommand();
                    c.CommandText = $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{name}'";
                    return Convert.ToInt64(await c.ExecuteScalarAsync() ?? 0) > 0;
                }

                async Task<bool> ColumnExists(string table, string col)
                {
                    using var c = conn.CreateCommand();
                    c.CommandText = $"PRAGMA table_info({table})";
                    using var r = await c.ExecuteReaderAsync();
                    while (await r.ReadAsync())
                        if (string.Equals(r["name"]?.ToString(), col, StringComparison.OrdinalIgnoreCase))
                            return true;
                    return false;
                }

                async Task Exec(string sql) { using var c = conn.CreateCommand(); c.CommandText = sql; await c.ExecuteNonQueryAsync(); }

                // Vault table
                if (!await TableExists("Vault"))
                    await Exec(@"CREATE TABLE IF NOT EXISTS Vault (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Description TEXT,
                        IsDefault INTEGER NOT NULL DEFAULT 0, Icon TEXT, Color TEXT,
                        CreatedAt TEXT NOT NULL, UpdatedAt TEXT NOT NULL, UserId TEXT NOT NULL)");

                // VaultId on Collections
                if (await TableExists("Collections") && !await ColumnExists("Collections", "VaultId"))
                    await Exec("ALTER TABLE Collections ADD COLUMN VaultId INTEGER NULL");

                // PasswordItemTags join table — CRITICAL: must exist before seeding items with tags
                if (!await TableExists("PasswordItemTags"))
                    await Exec(@"CREATE TABLE IF NOT EXISTS PasswordItemTags (
                        PasswordItemsId INTEGER NOT NULL,
                        TagsId          INTEGER NOT NULL,
                        PRIMARY KEY (PasswordItemsId, TagsId))");

                // PasskeyItems table
                if (!await TableExists("PasskeyItems"))
                    await Exec(@"CREATE TABLE IF NOT EXISTS PasskeyItems (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT, PasswordItemId INTEGER NOT NULL,
                        UserId TEXT, WebsiteUrl TEXT, Website TEXT, Username TEXT, DisplayName TEXT,
                        IsBackedUp INTEGER NOT NULL DEFAULT 0, RequiresUserVerification INTEGER NOT NULL DEFAULT 0,
                        DeviceType TEXT, PlatformName TEXT, LastUsedAt TEXT, UsageCount INTEGER NOT NULL DEFAULT 0,
                        Notes TEXT, CreatedAt TEXT NOT NULL DEFAULT '', LastModified TEXT NOT NULL DEFAULT '',
                        EncryptedCredentialId TEXT, CredentialIdNonce TEXT, CredentialIdAuthTag TEXT)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SampleDataSeeder] EnsureSchema failed: {ex.Message}");
            }
        }

        private static async Task<string> CreateDemoUserAsync(
            PasswordManager.DAL.PasswordManagerDbContext db,
            IPasswordCryptoService cryptoService)
        {
            const string demoMasterPassword = "DemoPassword123!";
            var salt = cryptoService.GenerateUserSalt();
            var demoUser = new PasswordManager.Models.ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = "demo@local",
                UserName = "demo@local",
                FirstName = "Demo",
                LastName = "User",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UserSalt = Convert.ToBase64String(salt),
                MasterPasswordHash = cryptoService.CreateMasterPasswordHash(demoMasterPassword, salt),
                MasterKeyIdentifier = cryptoService.CreateMasterKeyIdentifier(demoMasterPassword, salt),
                MasterPasswordHint = "Password: DemoPassword123!",
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                IsActive = true
            };
            db.Users.Add(demoUser);
            await db.SaveChangesAsync();
            return demoUser.Id;
        }
    }
}
