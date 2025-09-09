using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using PasswordManager.Models;
using PasswordManager.DAL.Seed;
using PasswordManager.Crypto.Interfaces;
using System;
using System.Threading.Tasks;

namespace PasswordManager.WinUi.Tests;

/// <summary>
/// Test to validate IdentityDataSeeder creates users with common master key
/// </summary>
public static class IdentitySeederTests
{
    /// <summary>
    /// Tests that the Identity seeder can create users with master key support
    /// </summary>
    public static async Task<bool> TestCommonMasterKeySetupAsync(IServiceProvider services)
    {
        try
        {
            using var scope = services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var cryptoService = scope.ServiceProvider.GetRequiredService<IPasswordCryptoService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<IdentityDataSeeder>>();

            // Create a test seeder
            var seeder = new IdentityDataSeeder(userManager, roleManager, cryptoService, logger);

            // Run the seeding
            await seeder.SeedAsync();

            // Verify that users were created with master key identifiers
            var adminUser = await userManager.FindByEmailAsync("admin@passwordmanager.local");
            var parentUser = await userManager.FindByEmailAsync("parent@passwordmanager.local");

            if (adminUser == null || parentUser == null)
            {
                System.Diagnostics.Debug.WriteLine("ERROR: Users were not created by seeder");
                return false;
            }

            if (string.IsNullOrEmpty(adminUser.MasterKeyIdentifier) || 
                string.IsNullOrEmpty(parentUser.MasterKeyIdentifier))
            {
                System.Diagnostics.Debug.WriteLine("ERROR: Users do not have MasterKeyIdentifier set");
                return false;
            }

            if (string.IsNullOrEmpty(adminUser.UserSalt) || 
                string.IsNullOrEmpty(parentUser.UserSalt))
            {
                System.Diagnostics.Debug.WriteLine("ERROR: Users do not have UserSalt set");
                return false;
            }

            System.Diagnostics.Debug.WriteLine("SUCCESS: Identity seeder test passed - users created with master key support");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR: Identity seeder test failed: {ex.Message}");
            return false;
        }
    }
}