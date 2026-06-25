using VaultGuard.DAL.Interfaces;

namespace VaultGuard.Services.Interfaces;

public interface IDatabaseContextFactory
{
    Task<IVaultGuardDbContext> CreateContextAsync(string provider, string connectionString);
    Task<IVaultGuardDbContext> CreateSqliteContextAsync();
    Task<IVaultGuardDbContext> CreateSqlServerContextAsync();
    Task<IVaultGuardDbContext> CreatePostgresContextAsync();
    IVaultGuardDbContext CreateDbContext();
}
