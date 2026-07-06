using VaultGuard.DAL.Seed;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.Web.Services;

/// <summary>
/// Web implementation of <see cref="IDefaultAccountSeeder"/>. Delegates to the shared
/// <see cref="IdentityDataSeeder"/> so the default accounts and the "Personal" vault are created the
/// same way on every provider (SQLite, SQL Server, PostgreSQL, …). Idempotent — existing accounts are
/// left untouched.
/// </summary>
public sealed class WebDefaultAccountSeeder : IDefaultAccountSeeder
{
    private readonly IdentityDataSeeder _seeder;

    public WebDefaultAccountSeeder(IdentityDataSeeder seeder) => _seeder = seeder;

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
