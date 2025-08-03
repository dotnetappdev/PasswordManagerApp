using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PasswordManager.Services.Interfaces;
using PasswordManager.DAL;
using PasswordManager.DAL.Interfaces;
using PasswordManager.Models;

namespace PasswordManager.API.Services;

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
            default:
                throw new ArgumentException($"Unsupported database provider: {provider}");
        }

        var context = new PasswordManagerDbContext(optionsBuilder.Options);
        
        // Ensure database is created
        try
        {
            await context.Database.EnsureCreatedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure database exists for provider {Provider}", provider);
            throw;
        }

        return new PasswordManagerDbContextWrapper(context);
    }

    public async Task<IPasswordManagerDbContext> CreateSqliteContextAsync()
    {
        var connectionString = _configuration.GetConnectionString("SqliteConnection") 
            ?? throw new InvalidOperationException("Sqlite connection string not configured");
        
        return await CreateContextAsync("sqlite", connectionString);
    }

    public async Task<IPasswordManagerDbContext> CreateSqlServerContextAsync()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("SqlServer connection string not configured");
        
        return await CreateContextAsync("sqlserver", connectionString);
    }

    public async Task<IPasswordManagerDbContext> CreatePostgresContextAsync()
    {
        var connectionString = _configuration.GetConnectionString("PostgresConnection") 
            ?? throw new InvalidOperationException("Postgres connection string not configured");
        
        return await CreateContextAsync("postgres", connectionString);
    }
}

// Wrapper to implement IPasswordManagerDbContext
public class PasswordManagerDbContextWrapper : IPasswordManagerDbContext
{
    private readonly PasswordManagerDbContext _context;

    public PasswordManagerDbContextWrapper(PasswordManagerDbContext context)
    {
        _context = context;
    }

    public DbSet<PasswordItem> PasswordItems { get => _context.PasswordItems; set => _context.PasswordItems = value; }
    public DbSet<Category> Categories { get => _context.Categories; set => _context.Categories = value; }
    public DbSet<Collection> Collections { get => _context.Collections; set => _context.Collections = value; }
    public DbSet<Tag> Tags { get => _context.Tags; set => _context.Tags = value; }
    public DbSet<LoginItem> LoginItems { get => _context.LoginItems; set => _context.LoginItems = value; }
    public DbSet<CreditCardItem> CreditCardItems { get => _context.CreditCardItems; set => _context.CreditCardItems = value; }
    public DbSet<SecureNoteItem> SecureNoteItems { get => _context.SecureNoteItems; set => _context.SecureNoteItems = value; }
    public DbSet<WiFiItem> WiFiItems { get => _context.WiFiItems; set => _context.WiFiItems = value; }
    public DbSet<ApplicationUser> Users { get => _context.Users; set => _context.Users = value; }
    public DbSet<ApiKey> ApiKeys { get => _context.ApiKeys; set => _context.ApiKeys = value; }
    public DbSet<QrLoginToken> QrLoginTokens { get => _context.QrLoginTokens; set => _context.QrLoginTokens = value; }
    public DbSet<OtpCode> OtpCodes { get => _context.OtpCodes; set => _context.OtpCodes = value; }
    public Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade Database => _context.Database;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    // Expose the underlying context for operations that need full DbContext functionality
    public PasswordManagerDbContext Context => _context;
    
    // Extension method to get DbContext from interface
    public static implicit operator PasswordManagerDbContext(PasswordManagerDbContextWrapper wrapper)
    {
        return wrapper.Context;
    }
}
