using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PasswordManager.DAL;
using PasswordManager.Models;
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
}
