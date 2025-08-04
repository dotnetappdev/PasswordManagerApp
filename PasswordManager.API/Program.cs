using Microsoft.EntityFrameworkCore;
using PasswordManager.DAL;
using PasswordManager.Crypto.Extensions;
using PasswordManager.Services.Interfaces;
using PasswordManager.Services.Services;
using PasswordManager.API.Extensions;
using PasswordManager.API.Middleware;
using PasswordManager.DAL.Interfaces;
using Serilog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using PasswordManager.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/passwordmanager-api-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();

// Configure Entity Framework
var databaseProvider = builder.Configuration["DatabaseProvider"] ?? "SqlServer";
string? connectionString = null;
string? supabaseUrl = null;
string? supabaseApiKey = null;
if (databaseProvider.ToLower() == "supabase")
{
    supabaseUrl = builder.Configuration["Supabase:Url"];
    supabaseApiKey = builder.Configuration["Supabase:ApiKey"];
    if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseApiKey))
        throw new InvalidOperationException("Supabase configuration missing in appsettings.json");
}
else
{
    connectionString = databaseProvider.ToLower() switch
    {
        "sqlite" => builder.Configuration.GetConnectionString("SqliteConnection"),
        "postgres" or "postgresql" => builder.Configuration.GetConnectionString("PostgresConnection"),
        "mysql" => builder.Configuration.GetConnectionString("MySqlConnection"),
        _ => builder.Configuration.GetConnectionString("DefaultConnection")
    };
    if (string.IsNullOrEmpty(connectionString))
        throw new InvalidOperationException($"Connection string for {databaseProvider} not found.");
}

// Configure DbContext based on provider
switch (databaseProvider.ToLower())
{
    case "sqlite":
        builder.Services.AddDbContext<PasswordManagerDbContext>(options =>
            options.UseSqlite(connectionString));
        builder.Services.AddDbContext<PasswordManagerDbContextApp>(options =>
            options.UseSqlite(connectionString));
        break;
    case "postgres":
    case "postgresql":
        builder.Services.AddDbContext<PasswordManagerDbContext>(options =>
            options.UseNpgsql(connectionString));
        builder.Services.AddDbContext<PasswordManagerDbContextApp>(options =>
            options.UseNpgsql(connectionString));
        break;
    case "mysql":
        // builder.Services.AddDbContext<PasswordManagerDbContext>(options =>
        //     options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
        break;
    case "supabase":
        builder.Services.AddSupabaseDbContext(builder.Configuration);
        break;
    default:
        builder.Services.AddDbContext<PasswordManagerDbContext>(options =>
            options.UseSqlServer(connectionString));
        builder.Services.AddDbContext<PasswordManagerDbContextApp>(options =>
            options.UseSqlServer(connectionString));
        break;
}

// Add Identity services
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
})
.AddEntityFrameworkStores<PasswordManagerDbContextApp>()
.AddDefaultTokenProviders();

// Register application services
builder.Services.AddScoped<IPasswordManagerDbContext>(provider =>
{
    var dbContext = provider.GetRequiredService<PasswordManagerDbContext>();
    return new PasswordManager.API.Services.PasswordManagerDbContextWrapper(dbContext);
});
builder.Services.AddScoped<IPasswordItemApiService, PasswordManager.Services.Services.PasswordItemApiService>();
builder.Services.AddScoped<ICategoryApiService, PasswordManager.Services.Services.CategoryApiService>();
builder.Services.AddScoped<ICollectionApiService, PasswordManager.Services.Services.CollectionApiService>();
builder.Services.AddScoped<ITagApiService, PasswordManager.Services.Services.TagApiService>();
builder.Services.AddScoped<ISyncService, PasswordManager.Services.Services.SyncService>();
builder.Services.AddScoped<IDatabaseContextFactory, PasswordManager.Services.Services.DatabaseContextFactory>();
builder.Services.AddScoped<IPasswordEncryptionService, PasswordManager.Services.Services.PasswordEncryptionService>();
builder.Services.AddScoped<IJwtService, PasswordManager.Services.Services.JwtService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IVaultSessionService, PasswordManager.Services.Services.VaultSessionService>();
builder.Services.AddScoped<IQrLoginService, PasswordManager.Services.Services.QrLoginService>();
builder.Services.AddScoped<IApiKeyService, PasswordManager.Services.Services.ApiKeyService>();
builder.Services.AddScoped<IDatabaseMigrationService, PasswordManager.Services.Services.DatabaseMigrationService>();
builder.Services.AddScoped<ITwoFactorService, PasswordManager.Services.Services.TwoFactorService>();
builder.Services.AddScoped<IPasskeyService, PasswordManager.Services.Services.PasskeyService>();
builder.Services.AddHostedService<PasswordManager.Services.Services.AutoSyncService>();

// Register cryptography services
builder.Services.AddCryptographyServices();

// Register platform and database configuration services
builder.Services.AddScoped<IPlatformService, PasswordManager.Services.Services.DefaultPlatformService>();
builder.Services.AddScoped<IDatabaseConfigurationService, PasswordManager.Services.Services.DatabaseConfigurationService>();


// Configure SMS settings
builder.Services.Configure<PasswordManager.Models.Configuration.SmsConfiguration>(
    builder.Configuration.GetSection(PasswordManager.Models.Configuration.SmsConfiguration.SectionName));

// Register SMS and OTP services
builder.Services.AddHttpClient<PasswordManager.Services.Services.TwilioSmsService>();
builder.Services.AddScoped<ISmsService, PasswordManager.Services.Services.TwilioSmsService>();
builder.Services.AddScoped<IOtpService, PasswordManager.Services.Services.OtpService>();
builder.Services.AddScoped<IPlatformDetectionService, PasswordManager.Services.Services.PlatformDetectionService>();
builder.Services.AddScoped<ISmsSettingsService, PasswordManager.Services.Services.SmsSettingsService>();

// Register Fido2 service for passkeys
builder.Services.AddScoped<Fido2NetLib.IFido2>(provider =>
{
    var config = new Fido2NetLib.Fido2Configuration
    {
        ServerDomain = "localhost", // Update this for production
        ServerName = "PasswordManager",
        Origins = new HashSet<string> { "https://localhost", "http://localhost" },
        TimestampDriftTolerance = 300000
    };
    return new Fido2NetLib.Fido2(config);
});

// Add API documentation with Swagger (compatible with .NET 8)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", builder.Configuration["ApiSettings:Title"] ?? "Password Manager API");
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

// Add API key authentication middleware
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

// Initialize database with migration handling
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PasswordManagerDbContext>();
    var contextApp = scope.ServiceProvider.GetRequiredService<PasswordManagerDbContextApp>();
    try
    {
        // Check for pending migrations before applying
        var pendingMigrations = await contextApp.Database.GetPendingMigrationsAsync();
        var pendingMigrationsApi = await context.Database.GetPendingMigrationsAsync();

        if (pendingMigrations.Any())
        {
            Log.Warning("Pending migrations found for PasswordManagerDbContextApp: {Migrations}", string.Join(", ", pendingMigrations));
            Log.Information("Applying pending migrations for PasswordManagerDbContextApp...");
            await contextApp.Database.MigrateAsync();
            Log.Information("Migrations applied successfully for PasswordManagerDbContextApp");
        }
        else
        {
            // Only use EnsureCreated if no migrations exist
            var appliedMigrations = await contextApp.Database.GetAppliedMigrationsAsync();
            if (!appliedMigrations.Any())
            {
                await contextApp.Database.EnsureCreatedAsync();
                Log.Information("Database created for PasswordManagerDbContextApp using EnsureCreated");
            }
            else
            {
                Log.Information("Database already exists for PasswordManagerDbContextApp");
            }
        }

        if (pendingMigrationsApi.Any())
        {
            Log.Warning("Pending migrations found for PasswordManagerDbContext: {Migrations}", string.Join(", ", pendingMigrationsApi));
            Log.Information("Applying pending migrations for PasswordManagerDbContext...");
            await context.Database.MigrateAsync();
            Log.Information("Migrations applied successfully for PasswordManagerDbContext");
        }
        else
        {
            // Only use EnsureCreated if no migrations exist
            var appliedMigrationsApi = await context.Database.GetAppliedMigrationsAsync();
            if (!appliedMigrationsApi.Any())
            {
                await context.Database.EnsureCreatedAsync();
                Log.Information("Database created for PasswordManagerDbContext using EnsureCreated");
            }
            else
            {
                Log.Information("Database already exists for PasswordManagerDbContext");
            }
        }

        Log.Information("Database initialization completed for provider: {Provider}", databaseProvider);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error during database initialization. The application will continue to start, but some database operations may fail.");
        Log.Warning("If you see 'table already exists' errors, you may need to either:");
        Log.Warning("1. Delete the migration files and recreate them, or");
        Log.Warning("2. Reset the database, or");
        Log.Warning("3. Manually apply the migrations using 'dotnet ef database update'");
    }
}

Log.Information("Password Manager API starting up...");
Log.Information("Database Provider: {Provider}", databaseProvider);
Log.Information("API Documentation available at: /scalar/v1");

app.Run();

public partial class Program { } // For testing purposes
