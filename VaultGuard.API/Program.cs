using Microsoft.EntityFrameworkCore;
using VaultGuard.DAL;
using VaultGuard.Crypto.Extensions;
using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Services;
using VaultGuard.API.Configuration;
using VaultGuard.API.Extensions;
using VaultGuard.API.Middleware;
using VaultGuard.DAL.Interfaces;
using Serilog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using VaultGuard.Models;
using VaultGuard.Models.Configuration;
using VaultGuard.ExceptionReporting.Sentry;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Configuration comes from appsettings.json (+ appsettings.{Environment}.json) and environment variables.
// The SQL Server connection string lives in ConnectionStrings:DefaultConnection — set the real production
// value on the server (SmarterASP control panel or the ConnectionStrings__DefaultConnection environment
// variable in web.config), never committed to source control.

// Durable serial file logging for the whole app: logs/{yyyy}/{MMMM}/{dd}.txt.
builder.Logging.AddProvider(new VaultGuard.Services.Logging.FileLoggerProvider(
    minLevel: Microsoft.Extensions.Logging.LogLevel.Information));

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

// Rate limiting (item 5) — blunt online password guessing / brute force while keeping the
// zero-knowledge design intact. A global per-IP safety net, plus a stricter "auth" policy applied to
// the login / Identity endpoints.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(context =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.AddPolicy("auth", context =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// Configure Entity Framework
var databaseProvider = builder.Configuration["DatabaseProvider"] ?? "SqlServer";
string? connectionString = null;
string? supabaseUrl = null;
string? supabaseApiKey = null;
string? sharedSqlServerConnectionString = null;

// Optional single-machine sharing: honour the machine-local settings.json that the WPF desktop and
// Blazor web apps write (%LocalAppData%\VaultGuard\settings.json) so all VaultGuard apps on one box
// can point at the same vault. Off by default — server deployments keep their appsettings.json values.
string? sharedSqlitePath = null;
if (builder.Configuration.GetValue<bool>("UseSharedMachineDatabase", false))
{
    try
    {
        var shared = new VaultGuard.Services.Services.AppSettingsService();
        var sharedProvider = SharedMachineDatabaseSettings.NormalizeProvider(shared.Get("DatabaseProvider"));
        // Only adopt providers the API actually supports (MySQL is not wired up here).
        if (sharedProvider is "sqlite" or "postgres" or "postgresql" or "sqlserver" or "supabase")
            databaseProvider = sharedProvider;
        sharedSqlitePath = shared.Get("SqliteDatabasePath");
        if (databaseProvider == "sqlserver" &&
            SharedMachineDatabaseSettings.TryBuildSqlServerConnectionString(shared, out var sharedConnectionString))
        {
            sharedSqlServerConnectionString = sharedConnectionString;
        }
        Log.Information("UseSharedMachineDatabase enabled — using provider '{Provider}' from {File}",
            databaseProvider, shared.SettingsFilePath);
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to read shared machine database settings; falling back to appsettings.json");
    }
}
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

// When sharing the machine-local SQLite vault, point the connection string at the shared path.
if (databaseProvider.ToLower() == "sqlite" && !string.IsNullOrWhiteSpace(sharedSqlitePath))
{
    connectionString = $"Data Source={sharedSqlitePath}";
    Log.Information("Using shared SQLite database at {Path}", sharedSqlitePath);
}
else if (databaseProvider.ToLower() == "sqlserver" && !string.IsNullOrWhiteSpace(sharedSqlServerConnectionString))
{
    connectionString = sharedSqlServerConnectionString;
    Log.Information("Using shared SQL Server connection details from machine settings");
}

// Configure DbContext based on provider
switch (databaseProvider.ToLower())
{
    // Single application context (Identity + vault). Each provider keeps its own migrations assembly.
    case "sqlite":
        builder.Services.AddDbContext<VaultGuardDbContext>(options =>
            options.UseSqlite(connectionString));
        break;
    case "postgres":
    case "postgresql":
        builder.Services.AddDbContext<VaultGuardDbContext>(options =>
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
        // SQL Server migrations live in VaultGuard.DAL.SqlServer (separate from the SQLite migrations in
        // VaultGuard.DAL), so point EF at that assembly when applying them at runtime.
        builder.Services.AddDbContext<VaultGuardDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly("VaultGuard.DAL.SqlServer")));
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

    // Account lockout (item 5) — lock an account after repeated failed sign-ins to slow brute force.
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<VaultGuardDbContext>()
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
// Headless auth service so services that take IAuthService in their ctor (VaultService, CategoryService)
// can be constructed in the API, which authenticates per-request instead of via a stateful IAuthService.
builder.Services.AddScoped<IAuthService, VaultGuard.API.Services.ApiAuthService>();
builder.Services.AddScoped<IVaultService, VaultGuard.Services.Services.VaultService>();
// Domain services pulled in transitively (e.g. by the import service). Registered so the DI graph
// validates on build (Development) and resolves at runtime.
builder.Services.AddScoped<IPasswordItemService, VaultGuard.Services.PasswordItemService>();
builder.Services.AddScoped<ICollectionService, VaultGuard.Services.Services.CollectionService>();
builder.Services.AddScoped<ICategoryInterface, VaultGuard.Services.Services.CategoryService>();
builder.Services.AddScoped<ITagService, VaultGuard.Services.TagService>();
builder.Services.AddScoped<ISyncService, VaultGuard.Services.Services.SyncService>();
builder.Services.AddScoped<IDatabaseContextFactory, VaultGuard.Services.Services.DatabaseContextFactory>();
builder.Services.AddScoped<IPasswordEncryptionService, VaultGuard.Services.Services.PasswordEncryptionService>();
builder.Services.AddScoped<IJwtService, VaultGuard.Services.Services.JwtService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IVaultSessionService, VaultGuard.Services.Services.VaultSessionService>();
builder.Services.AddScoped<IQrLoginService, VaultGuard.Services.Services.QrLoginService>();
builder.Services.AddScoped<IApiKeySqliteMirror, VaultGuard.Services.Services.ApiKeySqliteMirrorService>();
builder.Services.AddScoped<IApiKeyService, VaultGuard.Services.Services.ApiKeyService>();

// Push notifications to the native mobile apps (FCM HTTP v1; safe no-op until configured).
builder.Services.AddSingleton<IPushDeviceRegistry, VaultGuard.Services.Services.PushDeviceSqliteRegistry>();
builder.Services.AddSingleton<IFcmAccessTokenProvider, VaultGuard.Services.Services.NullFcmAccessTokenProvider>();
builder.Services.AddSingleton<IPushNotificationService, VaultGuard.Services.Services.FcmPushNotificationService>();

// GitHub-style number-matching push approvals (60s validity).
builder.Services.AddSingleton<IApprovalService, VaultGuard.Services.Services.ApprovalService>();

builder.Services.AddScoped<IDatabaseMigrationService, VaultGuard.Services.Services.DatabaseMigrationService>();
builder.Services.AddScoped<ITwoFactorService, VaultGuard.Services.Services.TwoFactorService>();
builder.Services.AddScoped<IPasskeyService, VaultGuard.Services.Services.PasskeyService>();
builder.Services.AddScoped<IPermissionService, VaultGuard.Services.Services.PermissionService>();
builder.Services.AddScoped<IDeviceService, VaultGuard.Services.Services.DeviceService>();
builder.Services.AddScoped<IAuditLogService, VaultGuard.Services.Services.AuditLogService>();
builder.Services.AddHostedService<VaultGuard.Services.Services.AutoSyncService>();

// Identity data seeder — creates the default accounts + "Personal" vault (same across all providers)
builder.Services.AddScoped<VaultGuard.DAL.Seed.IdentityDataSeeder>();

// Register import/export services
builder.Services.AddSingleton<VaultGuard.Imports.Services.PluginDiscoveryService>();
builder.Services.AddScoped<VaultGuard.Imports.Interfaces.IImportService, VaultGuard.Imports.Services.ImportService>();

// Register cryptography services
builder.Services.AddCryptographyServices();


// Shared, machine-local app settings (same settings.json the WPF/Web apps use).
builder.Services.AddSingleton<IAppSettingsService, VaultGuard.Services.Services.AppSettingsService>();

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

// Register Fido2 service for (account-level) passkeys. The WebAuthn Relying Party domain MUST match the
// RP the clients use — for VaultGuard that's the web app host (also the domain that serves the passkey
// association files at /.well-known/assetlinks.json and /apple-app-site-association), so web AND native
// mobile passkeys share one RP identity. Comes from the "Fido2" config section; local dev falls back to
// localhost. Getting this wrong is why passkeys registered via the API fail on the real domain.
builder.Services.AddScoped<Fido2NetLib.IFido2>(provider =>
{
    var fido = builder.Configuration.GetSection("Fido2");
    var serverDomain = fido["ServerDomain"];
    if (string.IsNullOrWhiteSpace(serverDomain)) serverDomain = "localhost";
    var serverName = fido["ServerName"];
    if (string.IsNullOrWhiteSpace(serverName)) serverName = "Vault Guard";
    var origins = fido.GetSection("Origins").Get<string[]>();
    var originSet = (origins is { Length: > 0 })
        ? new HashSet<string>(origins)
        : new HashSet<string> { "https://localhost", "http://localhost" };

    var config = new Fido2NetLib.Fido2Configuration
    {
        ServerDomain = serverDomain,
        ServerName = serverName,
        Origins = originSet,
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

// Route the AppLogger facade through the configured logging pipeline (console, Sentry, file).
VaultGuard.Services.Logging.AppLogger.Initialize(
    app.Services.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>());

// Configure the HTTP request pipeline
// UseSwagger still generates the underlying OpenAPI document (/swagger/v1/swagger.json) - Scalar renders
// the interactive reference UI from it (a dashboard-style layout, rather than Swashbuckle's classic UI).
app.UseSwagger();
app.MapScalarApiReference(options =>
{
    options.WithTitle(builder.Configuration["ApiSettings:Title"] ?? "Vault Guard API")
        .WithOpenApiRoutePattern("/swagger/{documentName}.json")
        .WithTheme(ScalarTheme.BluePlanet);
});

app.UseHttpsRedirection();

app.UseCors("Default");

// Enforce the rate limits configured above (must run before endpoint execution).
app.UseRateLimiter();

// Run the framework authentication step (Identity bearer/cookie schemes registered by
// AddIdentity().AddApiEndpoints()) before our API-key gate, so a request carrying a valid bearer
// token arrives already authenticated and the API-key middleware lets it through.
app.UseAuthentication();

// Add API key authentication middleware
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();

app.UseAuthorization();

app.MapControllers();

// Map .NET 9 Identity API endpoints (login/register/refresh) behind the stricter "auth" rate limit.
app.MapIdentityApi<ApplicationUser>().RequireRateLimiting("auth");

app.MapHealthChecks("/health");

// Initialize database with migration handling
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<VaultGuardDbContext>();
    try
    {
        // Check for pending migrations before applying (single application context).
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();

        if (pendingMigrations.Any())
        {
            Log.Warning("Pending migrations found for VaultGuardDbContext: {Migrations}", string.Join(", ", pendingMigrations));
            Log.Information("Applying pending migrations for VaultGuardDbContext...");
            await context.Database.MigrateAsync();
            Log.Information("Migrations applied successfully for VaultGuardDbContext");
        }
        else
        {
            // Only use EnsureCreated if no migrations exist
            var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
            if (!appliedMigrations.Any())
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

        // Seed the same default accounts + "Personal" vault as every other client, so a fresh SQL Server
        // (or any provider) behaves identically to the local SQLite build. Idempotent.
        try
        {
            var identitySeeder = scope.ServiceProvider.GetService<VaultGuard.DAL.Seed.IdentityDataSeeder>();
            if (identitySeeder != null)
            {
                await identitySeeder.SeedAsync();
                Log.Information("Identity data seeded (default accounts + Personal vault)");
            }
        }
        catch (Exception seedEx)
        {
            Log.Warning(seedEx, "Identity seeding warning");
        }

        // Seed demo vault data once, matching the web app's first-run behaviour.
        try
        {
            if (!await context.PasswordItems.AnyAsync())
            {
                var seedUserId =
                    await context.Users.Where(u => u.Email == "user@passwordmanager.local").Select(u => u.Id).FirstOrDefaultAsync()
                    ?? await context.Users.Select(u => u.Id).FirstOrDefaultAsync()
                    ?? VaultGuard.DAL.Seed.TestDataSeeder.TestUserId;
                VaultGuard.DAL.Seed.TestDataSeeder.SeedTestData(context, seedUserId);
                Log.Information("Demo vault data seeded");
            }
        }
        catch (Exception seedEx)
        {
            Log.Warning(seedEx, "Demo data seeding warning");
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

// Migrations run against the SAME configuration the app uses — including secrets sourced from Secret Manager.
// `dotnet run --project VaultGuard.API -- --migrate` applies migrations to the Secret Manager-configured
// database and exits, without needing the EF CLI or a design-time factory (which can't see Secret Manager).
if (args.Contains("--migrate"))
{
    Log.Information("--migrate specified: database migrations applied for provider '{Provider}'. Exiting.", databaseProvider);
    Log.CloseAndFlush();
    return;
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
