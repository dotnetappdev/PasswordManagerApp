using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PasswordManager.DAL.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.Services.Services;

public class DatabaseContextFactory : IDatabaseContextFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseContextFactory> _logger;

    public DatabaseContextFactory(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<DatabaseContextFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IPasswordManagerDbContext> CreateContextAsync(string provider, string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PasswordManagerDbContext>();

        switch (provider.ToLower())
        {
            case "sqlite":
                optionsBuilder.UseSqlite(connectionString);
                break;
            case "sqlserver":
                optionsBuilder.UseSqlServer(connectionString);
                break;
            case "postgres":
            case "postgresql":
                optionsBuilder.UseNpgsql(connectionString);
                break;
            case "mysql":
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                break;
            default:
                throw new ArgumentException($"Unsupported database provider: {provider}");
        }

        _logger.LogInformation("Creating database context with provider {Provider}", provider);
        
        var context = new PasswordManagerDbContext(optionsBuilder.Options);
        
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        return context;
    }

    public async Task<IPasswordManagerDbContext> CreateSqliteContextAsync()
    {
        var connectionString = _configuration.GetConnectionString("SQLiteConnection") ?? "Data Source=passwordmanager.db";
        return await CreateContextAsync("sqlite", connectionString);
    }

    public async Task<IPasswordManagerDbContext> CreateSqlServerContextAsync()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection") ?? 
                              _configuration.GetConnectionString("SqlServerConnection") ?? 
                              throw new InvalidOperationException("SQL Server connection string not found");
        return await CreateContextAsync("sqlserver", connectionString);
    }

    public async Task<IPasswordManagerDbContext> CreatePostgresContextAsync()
    {
        var connectionString = _configuration.GetConnectionString("PostgresConnection") ?? 
                              throw new InvalidOperationException("Postgres connection string not found");
        return await CreateContextAsync("postgres", connectionString);
    }
}
