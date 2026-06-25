using Microsoft.EntityFrameworkCore;
using VaultGuard.DAL;
using VaultGuard.Crypto.Extensions;
using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Services;
using VaultGuard.API.Extensions;
using VaultGuard.API.Middleware;
using VaultGuard.DAL.Interfaces;
using Serilog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using VaultGuard.Models;
using VaultGuard.Models.Configuration;
using VaultGuard.ExceptionReporting.Sentry;

var builder = WebApplication.CreateBuilder(args);

// Configure Sentry.io
var sentryConfig = builder.Configuration.GetSection("Sentry").Get<SentryConfiguration>();
if (sentryConfig?.IsConfigured == true)
{
    builder.WebHost.UseSentry(options =>
    {
        options.Dsn = sentryConfig.Dsn;
        options.Environment = sentryConfig.Environment;
        options.TracesSampleRate = sentryConfig.TracesSampleRate;
        options.SendDefaultPii = sentryConfig.SendDefaultPii;
        options.AttachStacktrace = sentryConfig.AttachStacktrace;
        options.Debug = sentryConfig.Debug;
    });
}

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
        builder.Services.AddDbContext<VaultGuardDbContext>(options =>
            options.UseSqlite(connectionString));
        builder.Services.AddDbContext<VaultGuardDbContextApp>(options =>
            options.UseSqlite(connectionString));
        break;
    case "postgres":
    case "postgresql":
        builder.Services.AddDbContext<VaultGuardDbContext>(options =>
            options.UseNpgsql(connectionString));
        builder.Services.AddDbContext<VaultGuardDbContextApp>(options =>
            options.UseNpgsql(connectionString));
        break;
    case "mysql":
        // builder.Services.AddDbContext<VaultGuardDbContext>(options =>
        //     options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
        break;
    case "supabase":
        builder.Services.AddSupabaseDbContext(builder.Configuration);
        break;
    default:
        builder.Services.AddDbContext<VaultGuardDbContext>(options =>
            options.UseSqlServer(connectionString));
        builder.Services.AddDbContext<VaultGuardDbContextApp>(options =>
            options.UseSqlServer(connectionString));
        break;
}

// Add Identity services with API endpoints (new .NET 9 approach)
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
})
.AddEntityFrameworkStores<VaultGuardDbContextApp>()
.AddDefaultTokenProviders()
.AddApiEndpoints(); // Enable .NET 9 Identity API endpoints

// Register application services
builder.Services.AddScoped<IVaultGuardDbContext>(provider =>
{
    var dbContext = provider.GetRequiredService<VaultGuardDbContext>();
    return new VaultGuard.API.Services.VaultGuardDbContextWrapper(dbContext);
});
builder.Services.AddScoped<IPasswordItemApiService, VaultGuard.Services.Services.PasswordItemApiService>();
builder.Services.AddScoped<ICategoryApiService, VaultGuard.Services.Services.CategoryApiService>();
builder.Services.AddScoped<ICollectionApiService, VaultGuard.Services.Services.CollectionApiService>();
builder.Services.AddScoped<ITagApiService, VaultGuard.Services.Services.TagApiService>();
builder.Services.AddScoped<IVaultApiService, VaultGuard.Services.Services.VaultApiService>();
builder.Services.AddScoped<IVaultService, VaultGuard.Services.Services.VaultService>();
builder.Services.AddScoped<ISyncService, VaultGuard.Services.Services.SyncService>();
builder.Services.AddScoped<IDatabaseContextFactory, VaultGuard.Services.Services.DatabaseContextFactory>();
builder.Services.AddScoped<IPasswordEncryptionService, VaultGuard.Services.Services.PasswordEncryptionService>();
builder.Services.AddScoped<IJwtService, VaultGuard.Services.Services.JwtService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IVaultSessionService, VaultGuard.Services.Services.VaultSessionService>();
builder.Services.AddScoped<IQrLoginService, VaultGuard.Services.Services.QrLoginService>();
builder.Services.AddScoped<IApiKeyService, VaultGuard.Services.Services.ApiKeyService>();
builder.Services.AddScoped<IDatabaseMigrationService, VaultGuard.Services.Services.DatabaseMigrationService>();
builder.Services.AddScoped<ITwoFactorService, VaultGuard.Services.Services.TwoFactorService>();
builder.Services.AddScoped<IPasskeyService, VaultGuard.Services.Services.PasskeyService>();
builder.Services.AddScoped<IPermissionService, VaultGuard.Services.Services.PermissionService>();
builder.Services.AddScoped<IDeviceService, VaultGuard.Services.Services.DeviceService>();
builder.Services.AddScoped<IAuditLogService, VaultGuard.Services.Services.AuditLogService>();
builder.Services.AddHostedService<VaultGuard.Services.Services.AutoSyncService>();

// Register import/export services
builder.Services.AddSingleton<VaultGuard.Imports.Services.PluginDiscoveryService>();
builder.Services.AddScoped<VaultGuard.Imports.Interfaces.IImportService, VaultGuard.Imports.Services.ImportService>();

// Register cryptography services
builder.Services.AddCryptographyServices();


// Register platform and database configuration services
builder.Services.AddScoped<IPlatformService, VaultGuard.Services.Services.DefaultPlatformService>();
// Register platform service (needed for database configuration)
builder.Services.AddSingleton<IPlatformService, VaultGuard.Services.Services.DefaultPlatformService>();

// Register database configuration service (needed by ApiKeyService)

builder.Services.AddScoped<IDatabaseConfigurationService, VaultGuard.Services.Services.DatabaseConfigurationService>();


// Configure SMS settings
builder.Services.Configure<VaultGuard.Models.Configuration.SmsConfiguration>(
    builder.Configuration.GetSection(VaultGuard.Models.Configuration.SmsConfiguration.SectionName));

// Configure Sentry settings
builder.Services.Configure<SentryConfiguration>(
    builder.Configuration.GetSection("Sentry"));

// Exception reporting (Sentry-backed, swappable via IExceptionReporter)
builder.Services.AddSentryExceptionReporting(builder.Configuration["ExceptionReporting:SentryDsn"], "API");

// Register SMS and OTP services
builder.Services.AddHttpClient<VaultGuard.Services.Services.TwilioSmsService>();
builder.Services.AddScoped<ISmsService, VaultGuard.Services.Services.TwilioSmsService>();
builder.Services.AddScoped<IOtpService, VaultGuard.Services.Services.OtpService>();
builder.Services.AddScoped<IPlatformDetectionService, VaultGuard.Services.Services.PlatformDetectionService>();
builder.Services.AddScoped<ISmsSettingsService, VaultGuard.Services.Services.SmsSettingsService>();

// Register Fido2 service for passkeys
builder.Services.AddScoped<Fido2NetLib.IFido2>(provider =>
{
    var config = new Fido2NetLib.Fido2Configuration
    {
        ServerDomain = "localhost", // Update this for production
        ServerName = "VaultGuard",
        Origins = new HashSet<string> { "https://localhost", "http://localhost" },
        TimestampDriftTolerance = 300000
    };
    return new Fido2NetLib.Fido2(config);
});

// Add API documentation with Swagger (compatible with .NET 8)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS — restrict to an explicit allow-list instead of AllowAnyOrigin. Origins come from the
// "Cors:AllowedOrigins" config array (set per environment); the default covers local dev only.
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (corsOrigins is null || corsOrigins.Length == 0)
{
    corsOrigins = new[]
    {
        "https://localhost", "http://localhost",
        "https://localhost:7001", "https://localhost:5001", "http://localhost:5000"
    };
}
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        policy.WithOrigins(corsOrigins)
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", builder.Configuration["ApiSettings:Title"] ?? "Vault Guard API");
    });
}

app.UseHttpsRedirection();

app.UseCors("Default");

// Run the framework authentication step (Identity bearer/cookie schemes registered by
// AddIdentity().AddApiEndpoints()) before our API-key gate, so a request carrying a valid bearer
// token arrives already authenticated and the API-key middleware lets it through.
app.UseAuthentication();

// Add API key authentication middleware
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();

app.UseAuthorization();

app.MapControllers();

// Map .NET 9 Identity API endpoints
app.MapIdentityApi<ApplicationUser>();

app.MapHealthChecks("/health");

// Initialize database with migration handling
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<VaultGuardDbContext>();
    var contextApp = scope.ServiceProvider.GetRequiredService<VaultGuardDbContextApp>();
    try
    {
        // Check for pending migrations before applying
        var pendingMigrations = await contextApp.Database.GetPendingMigrationsAsync();
        var pendingMigrationsApi = await context.Database.GetPendingMigrationsAsync();

        if (pendingMigrations.Any())
        {
            Log.Warning("Pending migrations found for VaultGuardDbContextApp: {Migrations}", string.Join(", ", pendingMigrations));
            Log.Information("Applying pending migrations for VaultGuardDbContextApp...");
            await contextApp.Database.MigrateAsync();
            Log.Information("Migrations applied successfully for VaultGuardDbContextApp");
        }
        else
        {
            // Only use EnsureCreated if no migrations exist
            var appliedMigrations = await contextApp.Database.GetAppliedMigrationsAsync();
            if (!appliedMigrations.Any())
            {
                await contextApp.Database.EnsureCreatedAsync();
                Log.Information("Database created for VaultGuardDbContextApp using EnsureCreated");
            }
            else
            {
                Log.Information("Database already exists for VaultGuardDbContextApp");
            }
        }

        if (pendingMigrationsApi.Any())
        {
            Log.Warning("Pending migrations found for VaultGuardDbContext: {Migrations}", string.Join(", ", pendingMigrationsApi));
            Log.Information("Applying pending migrations for VaultGuardDbContext...");
            await context.Database.MigrateAsync();
            Log.Information("Migrations applied successfully for VaultGuardDbContext");
        }
        else
        {
            // Only use EnsureCreated if no migrations exist
            var appliedMigrationsApi = await context.Database.GetAppliedMigrationsAsync();
            if (!appliedMigrationsApi.Any())
            {
                await context.Database.EnsureCreatedAsync();
                Log.Information("Database created for VaultGuardDbContext using EnsureCreated");
            }
            else
            {
                Log.Information("Database already exists for VaultGuardDbContext");
            }
        }

        Log.Information("Database initialization completed for provider: {Provider}", databaseProvider);
        // Invoke centralized migration service to ensure junction tables exist even if migration application skipped
        try
        {
            var migrationService = scope.ServiceProvider.GetService<IDatabaseMigrationService>();
            if (migrationService != null)
            {
                await migrationService.EnsurePasswordItemTagsTableExistsAsync();
                Log.Information("Ensured PasswordItemTags junction table exists via migration service fallback");
            }
            else
            {
                Log.Warning("IDatabaseMigrationService not available - cannot ensure junction tables");
            }
        }
        catch (Exception exEnsure)
        {
            Log.Warning(exEnsure, "Failed to ensure PasswordItemTags table via migration service");
        }
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

// Preload import provider assemblies - disabled by default for API builds
// The WinUI client is responsible for loading UI import plugins. To enable API-side preload
// (not recommended for UI plugins), set `Imports:PreloadInApi` to true in configuration.
if (app.Configuration.GetValue<bool>("Imports:PreloadInApi", false))
{
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            Log.Information("Preloading import provider assemblies (API mode)...");
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var importDlls = Directory.GetFiles(baseDirectory, "VaultGuardImports.*.dll");
            Log.Information("Found {Count} import provider DLLs", importDlls.Length);

            foreach (var dllPath in importDlls)
            {
                try
                {
                    var fileName = Path.GetFileName(dllPath);
                    if (!fileName.StartsWith("VaultGuardImports.", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var assembly = System.Reflection.Assembly.LoadFrom(dllPath);
                    var providerTypes = assembly.GetTypes()
                        .Where(t => typeof(VaultGuard.Imports.Interfaces.IPasswordImportProvider).IsAssignableFrom(t)
                                 && !t.IsInterface && !t.IsAbstract);

                    var importService = scope.ServiceProvider.GetService<VaultGuard.Imports.Interfaces.IImportService>();
                    if (importService != null)
                    {
                        foreach (var providerType in providerTypes)
                        {
                            if (providerType.GetConstructor(Type.EmptyTypes) == null)
                            {
                                Log.Warning("Skipping provider {TypeName} - no parameterless constructor found", providerType.Name);
                                continue;
                            }

                            var provider = Activator.CreateInstance(providerType) as VaultGuard.Imports.Interfaces.IPasswordImportProvider;
                            if (provider != null)
                            {
                                importService.RegisterProvider(provider);
                                Log.Information("Registered import provider: {ProviderName} v{Version}", provider.DisplayName, provider.Version);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    var fileName = Path.GetFileName(dllPath);
                    Log.Warning(ex, "Failed to load import provider from {FileName}", fileName);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during import provider preload");
        }
    }
}
else
{
    Log.Information("API import provider preload disabled (Imports:PreloadInApi=false). WinUI client should load UI plugins.");
}

Log.Information("Vault Guard API starting up...");
Log.Information("Database Provider: {Provider}", databaseProvider);
Log.Information("API Documentation available at: /scalar/v1");

app.Run();

public partial class Program { } // For testing purposes
