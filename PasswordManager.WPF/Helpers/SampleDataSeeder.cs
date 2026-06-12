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

                // Seed shared lookup data (categories, collections, tags) only once globally.
                TestDataSeeder.SeedCollections(db, seedUserId);
                TestDataSeeder.SeedCategories(db, seedUserId);
                TestDataSeeder.SeedTags(db, seedUserId);

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
