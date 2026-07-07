using VaultGuard.DAL.Seed;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.App.Services;

/// <summary>
/// MAUI implementation of <see cref="IDefaultAccountSeeder"/>. Mirrors VaultGuard.Web's
/// WebDefaultAccountSeeder so the "Create default accounts" button on the shared Login page works
/// identically on mobile. Idempotent — existing accounts are left untouched.
/// </summary>
public sealed class MauiDefaultAccountSeeder : IDefaultAccountSeeder
{
    private readonly IdentityDataSeeder _seeder;

    public MauiDefaultAccountSeeder(IdentityDataSeeder seeder) => _seeder = seeder;

    public async Task<DefaultAccountSeedResult> SeedAsync()
    {
        try
        {
            await _seeder.SeedAsync();
            return new DefaultAccountSeedResult
            {
                Success = true,
                Message = "Default accounts ready. Sign in with admin@passwordmanager.local (master key: 7hm3Z!Csu:Y64nm)."
            };
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Error("Default account seeding failed", ex);
            return new DefaultAccountSeedResult { Success = false, Message = $"Seeding failed: {ex.Message}" };
        }
    }
}
