using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using PasswordManager.Models;
using PasswordManager.DAL.Seed;
using PasswordManager.Crypto.Interfaces;
using PasswordManager.WinUi.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PasswordManager.WinUi.Tests;

/// <summary>
/// Test to validate IdentityDataSeeder creates users with common master key
/// and that authentication works across all user levels
/// </summary>
public static class IdentitySeederTests
{
    /// <summary>
    /// Tests that the Identity seeder can create users with master key support
    /// and that authentication works for all user levels
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
            var winUiAuth = scope.ServiceProvider.GetRequiredService<WinUiAuthService>();

            // Create a test seeder
            var seeder = new IdentityDataSeeder(userManager, roleManager, cryptoService, logger);

            // Run the seeding
            await seeder.SeedAsync();

            // Verify that users were created with master key identifiers
            var adminUser = await userManager.FindByEmailAsync("admin@passwordmanager.local");
            var parentUser = await userManager.FindByEmailAsync("parent@passwordmanager.local");
            var regularUser = await userManager.FindByEmailAsync("user@passwordmanager.local");
            var childUser = await userManager.FindByEmailAsync("child@passwordmanager.local");

            if (adminUser == null || parentUser == null || regularUser == null || childUser == null)
            {
                System.Diagnostics.Debug.WriteLine("ERROR: Not all users were created by seeder");
                return false;
            }

            // Test that all users have proper cryptographic setup
            var users = new[] { adminUser, parentUser, regularUser, childUser };
            foreach (var user in users)
            {
                if (string.IsNullOrEmpty(user.MasterKeyIdentifier))
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR: User {user.Email} does not have MasterKeyIdentifier set");
                    return false;
                }

                if (string.IsNullOrEmpty(user.UserSalt))
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR: User {user.Email} does not have UserSalt set");
                    return false;
                }

                if (string.IsNullOrEmpty(user.MasterPasswordHash))
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR: User {user.Email} does not have MasterPasswordHash set");
                    return false;
                }
            }

            // Test authentication with common master key
            const string commonMasterKey = "CommonMaster123!";

            // Test general authentication (should find first matching user)
            var authResult = await winUiAuth.AuthenticateAsync(commonMasterKey);
            if (!authResult)
            {
                System.Diagnostics.Debug.WriteLine("ERROR: Common master key authentication failed");
                return false;
            }

            // Test specific user authentication
            await winUiAuth.LogoutAsync(); // Logout first
            var adminAuthResult = await winUiAuth.AuthenticateAsUserAsync(commonMasterKey, "admin@passwordmanager.local");
            if (!adminAuthResult)
            {
                System.Diagnostics.Debug.WriteLine("ERROR: Admin user authentication failed");
                return false;
            }

            // Test available users retrieval
            var availableUsers = await winUiAuth.GetAvailableUsersAsync();
            if (availableUsers.Count != 4)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: Expected 4 available users, got {availableUsers.Count}");
                return false;
            }

            System.Diagnostics.Debug.WriteLine("SUCCESS: Identity seeder test passed - all users created with master key support");
            System.Diagnostics.Debug.WriteLine($"Created users: {string.Join(", ", users.Select(u => u.Email))}");
            System.Diagnostics.Debug.WriteLine($"Common master key authentication: PASSED");
            System.Diagnostics.Debug.WriteLine($"Specific user authentication: PASSED");
            System.Diagnostics.Debug.WriteLine($"Available users count: {availableUsers.Count}");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR: Identity seeder test failed: {ex.Message}");
            return false;
        }
    }
}