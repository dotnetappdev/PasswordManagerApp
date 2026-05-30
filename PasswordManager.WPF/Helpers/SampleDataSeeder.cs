using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PasswordManager.DAL.Seed;
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
                var cryptoService = serviceProvider.GetRequiredService<IPasswordCryptoService>();
                var db = serviceProvider.GetService<PasswordManager.DAL.PasswordManagerDbContext>();
                if (db == null)
                {
                    return;
                }

                // Ensure there is at least one user to own seeded data
                string seedUserId;
                var existingUser = await db.Users.FirstOrDefaultAsync();
                if (existingUser == null)
                {
                    // Create demo user with proper cryptographic setup
                    const string demoMasterPassword = "DemoPassword123!";

                    var userSalt = cryptoService.GenerateUserSalt();
                    var masterPasswordHash = cryptoService.CreateMasterPasswordHash(demoMasterPassword, userSalt);
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

                if (await db.PasswordItems.AnyAsync())
                {
                    return;
                }

                TestDataSeeder.SeedTestData(db, seedUserId);
            }
            catch (Exception)
            {
                // Silently fail if seeding fails
            }
        }
    }
}
