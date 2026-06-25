using Microsoft.EntityFrameworkCore;
using VaultGuard.DAL;
using VaultGuard.DAL.Interfaces;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.WPF.Services;

/// <summary>
/// SQLite-only <see cref="IDatabaseContextFactory"/> for the WPF app. The desktop app resolves its
/// database file dynamically (app-data path), so it can't rely on the shared, connection-string-driven
/// <c>DatabaseContextFactory</c> which would default to "passwordmanager.db" in the working directory.
/// This factory binds every created context to the configured SQLite path so backups read the real vault.
/// </summary>
public sealed class WpfDatabaseContextFactory : IDatabaseContextFactory
{
    private readonly string _connectionString;

    public WpfDatabaseContextFactory(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
    }

    public Task<IVaultGuardDbContext> CreateContextAsync(string provider, string connectionString)
        => Task.FromResult(Create(connectionString));

    public Task<IVaultGuardDbContext> CreateSqliteContextAsync()
        => Task.FromResult(Create(_connectionString));

    public Task<IVaultGuardDbContext> CreateSqlServerContextAsync()
        => throw new NotSupportedException("The WPF app uses a local SQLite database.");

    public Task<IVaultGuardDbContext> CreatePostgresContextAsync()
        => throw new NotSupportedException("The WPF app uses a local SQLite database.");

    public IVaultGuardDbContext CreateDbContext() => Create(_connectionString);

    private static IVaultGuardDbContext Create(string connectionString)
    {
        var options = new DbContextOptionsBuilder<VaultGuardDbContext>()
            .UseSqlite(connectionString)
            .Options;
        return new VaultGuardDbContext(options);
    }
}
