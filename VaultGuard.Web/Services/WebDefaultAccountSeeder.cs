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
    private readonly VaultGuard.Components.Shared.Services.LoginDirectoryOptions? _options;

    public WebDefaultAccountSeeder(IdentityDataSeeder seeder, VaultGuard.Components.Shared.Services.LoginDirectoryOptions? options = null)
    {
        _seeder = seeder;
        _options = options;
    }

    public async Task<DefaultAccountSeedResult> SeedAsync()
    {
        // Enforced here too, not just by hiding the button: the accounts' master key is public.
        if (_options is { AllowDefaultAccountSeeding: false })
            return new DefaultAccountSeedResult { Success = false, Message = "Creating default accounts is disabled on this server." };
        try
        {
            await _seeder.SeedAsync();
            return new DefaultAccountSeedResult
            {
                Success = true,
                Message = "Default accounts ready (development/demo only — their master key is public; change it before real use)."
            };
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Error("Default account seeding failed", ex);
            return new DefaultAccountSeedResult { Success = false, Message = $"Seeding failed: {ex.Message}" };
        }
    }
}
