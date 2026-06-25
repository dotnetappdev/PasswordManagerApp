using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VaultGuard.Services.Interfaces;
using VaultGuard.DAL;
using VaultGuard.DAL.Interfaces;
using VaultGuard.Models;

namespace VaultGuard.API.Services;

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

    public async Task<IVaultGuardDbContext> CreateContextAsync(string provider, string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<VaultGuardDbContext>();

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

        var context = new VaultGuardDbContext(optionsBuilder.Options);
        
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

        return new VaultGuardDbContextWrapper(context);
    }

    public async Task<IVaultGuardDbContext> CreateSqliteContextAsync()
    {
        var connectionString = _configuration.GetConnectionString("SqliteConnection") 
            ?? throw new InvalidOperationException("Sqlite connection string not configured");
        
        return await CreateContextAsync("sqlite", connectionString);
    }

    public async Task<IVaultGuardDbContext> CreateSqlServerContextAsync()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("SqlServer connection string not configured");
        
        return await CreateContextAsync("sqlserver", connectionString);
    }

    public async Task<IVaultGuardDbContext> CreatePostgresContextAsync()
    {
        var connectionString = _configuration.GetConnectionString("PostgresConnection") 
            ?? throw new InvalidOperationException("Postgres connection string not configured");
        
        return await CreateContextAsync("postgres", connectionString);
    }

    public IVaultGuardDbContext CreateDbContext()
    {
        // For synchronous operations, default to SQLite
        var connectionString = _configuration.GetConnectionString("SqliteConnection") ?? "Data Source=passwordmanager.db";
        var optionsBuilder = new DbContextOptionsBuilder<VaultGuardDbContext>();
        optionsBuilder.UseSqlite(connectionString);
        
        _logger.LogInformation("Creating synchronous database context with SQLite provider");
        
        var context = new VaultGuardDbContext(optionsBuilder.Options);
        
        // Ensure database is created synchronously
        context.Database.EnsureCreated();

        return new VaultGuardDbContextWrapper(context);
    }
}

// Wrapper to implement IVaultGuardDbContext
public class VaultGuardDbContextWrapper : IVaultGuardDbContext
{
    private readonly VaultGuardDbContext _context;

    public VaultGuardDbContextWrapper(VaultGuardDbContext context)
    {
        _context = context;
    }

    public DbSet<PasswordItem> PasswordItems { get => _context.PasswordItems; set => _context.PasswordItems = value; }
    public DbSet<Category> Categories { get => _context.Categories; set => _context.Categories = value; }
    public DbSet<Collection> Collections { get => _context.Collections; set => _context.Collections = value; }
    public DbSet<Vault> Vaults { get => _context.Vaults; set => _context.Vaults = value; }
    public DbSet<Tag> Tags { get => _context.Tags; set => _context.Tags = value; }
    public DbSet<LoginItem> LoginItems { get => _context.LoginItems; set => _context.LoginItems = value; }
    public DbSet<CreditCardItem> CreditCardItems { get => _context.CreditCardItems; set => _context.CreditCardItems = value; }
    public DbSet<SecureNoteItem> SecureNoteItems { get => _context.SecureNoteItems; set => _context.SecureNoteItems = value; }
    public DbSet<WiFiItem> WiFiItems { get => _context.WiFiItems; set => _context.WiFiItems = value; }
    public DbSet<ApplicationUser> Users { get => _context.Users; set => _context.Users = value; }
    public DbSet<ApiKey> ApiKeys { get => _context.ApiKeys; set => _context.ApiKeys = value; }
    public DbSet<QrLoginToken> QrLoginTokens { get => _context.QrLoginTokens; set => _context.QrLoginTokens = value; }

    public DbSet<OtpCode> OtpCodes { get => _context.OtpCodes; set => _context.OtpCodes = value; }

    public DbSet<UserPasskey> UserPasskeys { get => _context.UserPasskeys; set => _context.UserPasskeys = value; }
    public DbSet<UserTwoFactorBackupCode> UserTwoFactorBackupCodes { get => _context.UserTwoFactorBackupCodes; set => _context.UserTwoFactorBackupCodes = value; }
    public DbSet<CustomField> CustomFields { get => _context.CustomFields; set => _context.CustomFields = value; }
    public DbSet<UserBackupSettings> UserBackupSettings { get => _context.UserBackupSettings; set => _context.UserBackupSettings = value; }

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
    public VaultGuardDbContext Context => _context;
    
    // Extension method to get DbContext from interface
    public static implicit operator VaultGuardDbContext(VaultGuardDbContextWrapper wrapper)
    {
        return wrapper.Context;
    }
}
