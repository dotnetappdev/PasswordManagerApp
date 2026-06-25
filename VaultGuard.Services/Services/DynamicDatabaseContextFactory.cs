using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Models.Configuration;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.Services.Services;

/// <summary>
/// Factory for creating database contexts with dynamic provider selection
/// </summary>
public class DynamicDatabaseContextFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDatabaseConfigurationService _configService;

    public DynamicDatabaseContextFactory(
        IServiceProvider serviceProvider,
        IDatabaseConfigurationService configService)
    {
        _serviceProvider = serviceProvider;
        _configService = configService;
    }

    /// <summary>
    /// Creates a DbContext with the configured database provider
    /// </summary>
    public async Task<DbContextOptions<TContext>> CreateDbContextOptionsAsync<TContext>() 
        where TContext : DbContext
    {
        var config = await _configService.GetConfigurationAsync();
        var connectionString = _configService.BuildConnectionString(config);

        var optionsBuilder = new DbContextOptionsBuilder<TContext>();

        switch (config.Provider)
        {
            case DatabaseProvider.Sqlite:
                optionsBuilder.UseSqlite(connectionString);
                break;
            case DatabaseProvider.SqlServer:
                optionsBuilder.UseSqlServer(connectionString);
                break;
            case DatabaseProvider.MySql:
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                break;
            case DatabaseProvider.PostgreSql:
                optionsBuilder.UseNpgsql(connectionString);
                break;
            default:
                throw new NotSupportedException($"Database provider {config.Provider} is not supported");
        }

        return optionsBuilder.Options;
    }

    /// <summary>
    /// Configures the database context with the selected provider
    /// </summary>
    public static void ConfigureDatabaseContext<TContext>(
        IServiceCollection services, 
        string connectionString, 
        DatabaseProvider provider)
        where TContext : DbContext
    {
        switch (provider)
        {
            case DatabaseProvider.Sqlite:
                services.AddDbContext<TContext>(options =>
                    options.UseSqlite(connectionString));
                break;
            case DatabaseProvider.SqlServer:
                services.AddDbContext<TContext>(options =>
                    options.UseSqlServer(connectionString));
                break;
            case DatabaseProvider.MySql:
                services.AddDbContext<TContext>(options =>
                    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
                break;
            case DatabaseProvider.PostgreSql:
                services.AddDbContext<TContext>(options =>
                    options.UseNpgsql(connectionString));
                break;
            default:
                throw new NotSupportedException($"Database provider {provider} is not supported");
        }
    }
}