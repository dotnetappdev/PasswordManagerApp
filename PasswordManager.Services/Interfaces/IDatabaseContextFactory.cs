using PasswordManager.DAL.Interfaces;

namespace PasswordManager.Services.Interfaces;

public interface IDatabaseContextFactory
{
    Task<IPasswordManagerDbContext> CreateContextAsync(string provider, string connectionString);
    Task<IPasswordManagerDbContext> CreateSqliteContextAsync();
    Task<IPasswordManagerDbContext> CreateSqlServerContextAsync();
    Task<IPasswordManagerDbContext> CreatePostgresContextAsync();
}
