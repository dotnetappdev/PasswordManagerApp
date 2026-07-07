using VaultGuard.Web.Components;
using VaultGuard.Web.Middleware;
using Microsoft.EntityFrameworkCore;
using VaultGuard.DAL;
using VaultGuard.DAL.SqlServer;
using VaultGuard.DAL.MySql;
using VaultGuard.DAL.SupaBase;
using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Services;
using VaultGuard.Services; // Add this line for the service classes
using VaultGuard.Crypto.Extensions;
using MudBlazor.Services;
using Microsoft.AspNetCore.Identity;
using VaultGuard.Models;
using VaultGuard.Models.Configuration;
using Pomelo.EntityFrameworkCore.MySql;
using VaultGuard.DAL.Interfaces;
using VaultGuard.ExceptionReporting.Sentry;

var builder = WebApplication.CreateBuilder(args);

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

// Configure Sentry settings
builder.Services.Configure<SentryConfiguration>(
    builder.Configuration.GetSection("Sentry"));

// Exception reporting (Sentry-backed, swappable via IExceptionReporter)
builder.Services.AddSentryExceptionReporting(builder.Configuration["ExceptionReporting:SentryDsn"], "Web");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add MudBlazor services
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass    = MudBlazor.Defaults.Classes.Position.TopRight;  // match WPF toasts
    config.SnackbarConfiguration.MaxDisplayedSnackbars = 4;
    config.SnackbarConfiguration.PreventDuplicates  = false;
    config.SnackbarConfiguration.NewestOnTop        = true;
    config.SnackbarConfiguration.SnackbarVariant    = MudBlazor.Variant.Filled;
    config.SnackbarConfiguration.VisibleStateDuration = 4000;
    config.SnackbarConfiguration.HideTransitionDuration = 400;
    config.SnackbarConfiguration.ShowTransitionDuration = 300;
});

// Register AppNotificationService (thin toast wrapper)
builder.Services.AddScoped<VaultGuard.Web.Services.AppNotificationService>();

// Configure MudBlazor theme — neutral dark grey, no blue accent by default
builder.Services.AddScoped(sp => new MudBlazor.MudTheme()
{
    PaletteLight = new MudBlazor.PaletteLight()
    {
        Primary = "#5B5B63",
        PrimaryLighten = "#84848C",
        PrimaryDarken = "#3F3F46",
        Secondary = "#71717A",
        AppbarBackground = "#5B5B63",
    },
    PaletteDark = new MudBlazor.PaletteDark()
    {
        Primary = "#5B5B63",
        PrimaryLighten = "#84848C",
        PrimaryDarken = "#3F3F46",
        Secondary = "#71717A",
        AppbarBackground = "#141414",
        AppbarText = "#E5E5E5",
        Background = "#1A1A1A",
        BackgroundGray = "#141414",
        Surface = "#2D2D2D",
        DrawerBackground = "#141414",
        DrawerText = "#E5E5E5",
        TextPrimary = "#E5E5E5",
        TextSecondary = "#9D9D9D",
    }
});

// Add theme service for light/dark mode support
builder.Services.AddScoped<VaultGuard.Components.Shared.Services.ThemeService>();

// Configure Entity Framework based on database provider
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

    builder.Services.AddDbContext<VaultGuardDbContextApp>(options =>
        options.UseNpgsql(supabaseUrl));
    builder.Services.AddDbContext<VaultGuardDbContext>(options =>
        options.UseNpgsql(supabaseUrl));
}
else
{
    connectionString = databaseProvider.ToLower() switch
    {
        "mysql" => builder.Configuration.GetConnectionString("MySqlConnection"),
        "sqlite" => builder.Configuration.GetConnectionString("SqliteConnection"),
        _ => builder.Configuration.GetConnectionString("DefaultConnection")
    };

    if (string.IsNullOrEmpty(connectionString))
        throw new InvalidOperationException($"Connection string for {databaseProvider} not found.");

    if (databaseProvider.ToLower() == "mysql")
    {
        builder.Services.AddDbContext<VaultGuardDbContextApp>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
        builder.Services.AddDbContext<VaultGuardDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
    }
    else if (databaseProvider.ToLower() == "sqlite")
    {
        builder.Services.AddDbContext<VaultGuardDbContextApp>(options =>
            options.UseSqlite(connectionString));
        builder.Services.AddDbContext<VaultGuardDbContext>(options =>
            options.UseSqlite(connectionString));
    }
    else
    {
        // SQL Server migrations live in VaultGuard.DAL.SqlServer (separate from the SQLite migrations in
        // VaultGuard.DAL); point EF at that assembly so runtime migration matches the API.
        builder.Services.AddDbContext<VaultGuardDbContextApp>(options =>
            options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly("VaultGuard.DAL.SqlServer")));
        builder.Services.AddDbContext<VaultGuardDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly("VaultGuard.DAL.SqlServer")));
    }
}

// Register DbContext interfaces for DI
builder.Services.AddScoped<IVaultGuardDbContext>(sp => sp.GetRequiredService<VaultGuardDbContext>());
builder.Services.AddScoped<IVaultGuardDbContextApp>(sp => sp.GetRequiredService<VaultGuardDbContextApp>());

// Add Identity services with roles
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
.AddDefaultTokenProviders();

// Register application services
builder.Services.AddScoped<IPasswordItemService, VaultGuard.Services.PasswordItemService>();
builder.Services.AddScoped<ITagService, VaultGuard.Services.TagService>();
builder.Services.AddScoped<ICategoryInterface, VaultGuard.Services.Services.CategoryService>();

// Shared, machine-local app settings — persists to the same %LocalAppData%\VaultGuard\settings.json
// the WPF desktop app uses, so theme/accent/database-path preferences are shared on one machine.
builder.Services.AddSingleton<IAppSettingsService, VaultGuard.Services.Services.AppSettingsService>();

// Shared, stateless feature services (strength meter + TOTP authenticator codes + QR)
builder.Services.AddSingleton<IPasswordStrengthService, VaultGuard.Services.Services.PasswordStrengthService>();
builder.Services.AddSingleton<ITotpService, VaultGuard.Services.Services.TotpService>();
builder.Services.AddSingleton<IQrCodeService, VaultGuard.Services.Services.QrCodeService>();
builder.Services.AddSingleton<ISecurityAuditService, VaultGuard.Services.Services.SecurityAuditService>();
builder.Services.AddSingleton<IPassphraseGenerator, VaultGuard.Services.Services.PassphraseGenerator>();
builder.Services.AddScoped<ICollectionService, VaultGuard.Services.Services.CollectionService>();
builder.Services.AddScoped<IAuthService, VaultGuard.Services.Services.AuthService>();
builder.Services.AddScoped<IUserProfileService, VaultGuard.Services.Services.UserProfileService>();
builder.Services.AddScoped<IApiKeyService, VaultGuard.Services.Services.ApiKeyService>();
builder.Services.AddScoped<IVaultSessionService, VaultGuard.Services.Services.VaultSessionService>();
builder.Services.AddScoped<IQrLoginService, VaultGuard.Services.Services.QrLoginService>();
builder.Services.AddScoped<IDatabaseContextFactory, VaultGuard.Services.Services.DatabaseContextFactory>();
builder.Services.AddScoped<IDatabaseConfigurationService, VaultGuard.Services.Services.DatabaseConfigurationService>();
builder.Services.AddScoped<IPlatformService, VaultGuard.Services.Services.DefaultPlatformService>();
builder.Services.AddScoped<IPasswordEncryptionService, VaultGuard.Services.Services.PasswordEncryptionService>();
builder.Services.AddScoped<IPasskeyService, VaultGuard.Services.Services.PasskeyService>();
builder.Services.AddScoped<IDatabaseMigrationService, VaultGuard.Services.Services.DatabaseMigrationService>();
builder.Services.AddScoped<IDatabaseHealthService, VaultGuard.Services.Services.DatabaseHealthService>();
builder.Services.AddScoped<IDatabaseResetService, VaultGuard.Services.Services.DatabaseResetService>();
builder.Services.AddScoped<IPermissionService, VaultGuard.Services.Services.PermissionService>();
builder.Services.AddScoped<IVaultService, VaultGuard.Services.Services.VaultService>();
builder.Services.AddScoped<IAuditLogService, VaultGuard.Services.Services.AuditLogService>();
builder.Services.AddScoped<ITwoFactorService, VaultGuard.Services.Services.TwoFactorService>();
builder.Services.AddScoped<IDeviceService, VaultGuard.Services.Services.DeviceService>();

// "Remember this device" master-key cache (encrypted with server data-protection keys) so a
// 2FA-enabled account can sign in code-only on a remembered browser.
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage.ProtectedLocalStorage>();
builder.Services.AddScoped<VaultGuard.Components.Shared.Services.IMasterKeyCacheService, VaultGuard.Web.Services.WebMasterKeyCacheService>();

// Cloud backup services
builder.Services.AddScoped<VaultGuard.Services.Interfaces.IBackupEncryptionService, VaultGuard.Services.Services.BackupEncryptionService>();
builder.Services.AddScoped<VaultGuard.Services.Interfaces.IDatabaseBackupService, VaultGuard.Services.Services.DatabaseBackupService>();
builder.Services.AddScoped<VaultGuard.Services.Interfaces.IOneDriveBackupService, VaultGuard.Services.Services.OneDriveBackupService>();
builder.Services.AddScoped<VaultGuard.Services.Interfaces.IiCloudBackupService, VaultGuard.Services.Services.iCloudBackupService>();
builder.Services.AddScoped<VaultGuard.Services.Interfaces.INetworkLocationBackupService, VaultGuard.Services.Services.NetworkLocationBackupService>();
builder.Services.AddSingleton<VaultGuard.Services.Interfaces.IGoogleDriveBackupService, VaultGuard.Services.Services.GoogleDriveBackupService>();
builder.Services.AddSingleton<VaultGuard.Services.Interfaces.IFtpBackupService, VaultGuard.Services.Services.FtpBackupService>();
builder.Services.AddScoped<VaultGuard.Services.Interfaces.IBackupSettingsService, VaultGuard.Services.Services.BackupSettingsService>();
builder.Services.AddScoped<VaultGuard.Services.Services.CloudBackupManager>();

// Register password import services (1Password 1pux/CSV, Bitwarden, etc.)
builder.Services.AddSingleton<VaultGuard.Imports.Services.PluginDiscoveryService>();
builder.Services.AddScoped<VaultGuard.Imports.Interfaces.IImportService, VaultGuard.Imports.Services.ImportService>();

// Register crypto services
builder.Services.AddCryptographyServices();

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

// Register Identity data seeder + the on-demand default-account seeder (used by the login screen button)
builder.Services.AddScoped<VaultGuard.DAL.Seed.IdentityDataSeeder>();
builder.Services.AddScoped<IDefaultAccountSeeder, VaultGuard.Web.Services.WebDefaultAccountSeeder>();

// Add HttpClient for API calls
builder.Services.AddHttpClient();

// Add HttpClient for API communication with Bearer token support.
// The base URL is configurable at runtime: a value saved in the shared settings.json (Settings →
// Sync/API, key "ApiBaseUrl", also used by the WPF app) takes precedence over appsettings.json.
var sharedApiSettings = new VaultGuard.Services.Services.AppSettingsService();
var sharedApiBaseUrl = sharedApiSettings.Get("ApiBaseUrl");
var sharedApiKey = sharedApiSettings.Get("ApiKey");
var sharedClientId = sharedApiSettings.Get("ApiClientId");
builder.Services.AddHttpClient("VaultGuardAPI", client =>
{
    var apiBaseUrl = !string.IsNullOrWhiteSpace(sharedApiBaseUrl)
        ? sharedApiBaseUrl
        : (builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7001");
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    // API key / client credential access (configured in Settings → API Connection). The API validates
    // the key via its ApiKeyAuthenticationMiddleware. Client-credentials token exchange is a follow-up.
    if (!string.IsNullOrWhiteSpace(sharedApiKey))
        client.DefaultRequestHeaders.Add("X-Api-Key", sharedApiKey);
    if (!string.IsNullOrWhiteSpace(sharedClientId))
        client.DefaultRequestHeaders.Add("X-Client-Id", sharedClientId);
});

// Add API service for external API communication if needed
builder.Services.AddScoped<IAppSyncService, VaultGuard.Services.Services.AppSyncService>();

var app = builder.Build();

// Route the AppLogger facade through the configured logging pipeline (console, Sentry, file).
VaultGuard.Services.Logging.AppLogger.Initialize(
    app.Services.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>());

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Health check endpoint (used by Docker health checks)
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Initialize database with migration handling
using (var scope = app.Services.CreateScope())
{
    try
    {
        // Use EnsureCreated to set up schema from current model (works without pending migrations)
        try
        {
            var dbContextApp = scope.ServiceProvider.GetRequiredService<VaultGuardDbContextApp>();
            await dbContextApp.Database.EnsureCreatedAsync();
            VaultGuard.Services.Logging.AppLogger.Info("VaultGuardDbContextApp schema ensured");
        }
        catch (Exception ensureEx)
        {
            VaultGuard.Services.Logging.AppLogger.Error($"⚠️  Schema ensure warning (App)", ensureEx);
        }

        try
        {
            var dbContextMain = scope.ServiceProvider.GetRequiredService<VaultGuardDbContext>();
            await dbContextMain.Database.EnsureCreatedAsync();
            VaultGuard.Services.Logging.AppLogger.Info("VaultGuardDbContext schema ensured");

            // Reconcile an existing SQLite vault with the current model (adds Collections.VaultId, the
            // Vault table, join/audit/device tables, etc.) — the SAME fixes the WPF app applies, so the
            // Blazor SQLite database mirrors the desktop one. No-op on SQL Server / other providers.
            await VaultGuard.Services.Services.SqliteSchemaGuard.EnsureVaultSchemaAsync(
                dbContextMain,
                app.Services.GetService<Microsoft.Extensions.Logging.ILoggerFactory>()?.CreateLogger("SqliteSchemaGuard"));
        }
        catch (Exception ensureEx)
        {
            VaultGuard.Services.Logging.AppLogger.Error($"⚠️  Schema ensure warning (Main)", ensureEx);
        }

        // Also run the migration service for any remaining work (table creation, etc.)
        var migrationService = scope.ServiceProvider.GetService<IDatabaseMigrationService>();
        if (migrationService != null)
        {
            try
            {
                await migrationService.EnsurePasswordItemTagsTableExistsAsync();
                VaultGuard.Services.Logging.AppLogger.Info("Ensured PasswordItemTags junction table exists");
            }
            catch (Exception ensureEx)
            {
                VaultGuard.Services.Logging.AppLogger.Error($"Warning: could not ensure PasswordItemTags table", ensureEx);
            }
        }

        // Seed Identity ROLES only at startup. Default demo accounts are NOT auto-created so a fresh
        // install shows the first-run "Create Master Key" setup on the login page; users can still create
        // the default accounts on demand via the "Create default accounts" button.
        try
        {
            var identitySeeder = scope.ServiceProvider.GetRequiredService<VaultGuard.DAL.Seed.IdentityDataSeeder>();
            await identitySeeder.SeedAsync(includeDefaultUsers: false);
            VaultGuard.Services.Logging.AppLogger.Info("Identity roles seeded (default accounts on demand)");
        }
        catch (Exception seedEx)
        {
            VaultGuard.Services.Logging.AppLogger.Error($"⚠️  Identity seeding warning", seedEx);
        }

        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<VaultGuardDbContext>();
            if (!await dbContext.PasswordItems.AnyAsync())
            {
                var seedUserId =
                    await dbContext.Users
                        .Where(u => u.Email == "user@passwordmanager.local")
                        .Select(u => u.Id)
                        .FirstOrDefaultAsync()
                    ?? await dbContext.Users.Select(u => u.Id).FirstOrDefaultAsync()
                    ?? VaultGuard.DAL.Seed.TestDataSeeder.TestUserId;

                VaultGuard.DAL.Seed.TestDataSeeder.SeedTestData(dbContext, seedUserId);
                VaultGuard.Services.Logging.AppLogger.Info("Demo password data seeded successfully");
            }
        }
        catch (Exception seedEx)
        {
            VaultGuard.Services.Logging.AppLogger.Error($"⚠️  Demo data seeding warning", seedEx);
        }
    }
    catch (Exception ex)
    {
        // Log the error but don't stop the application
        VaultGuard.Services.Logging.AppLogger.Error($"⚠️  Database initialization warning", ex);
        Console.WriteLine("🚀 Application will continue to start...");
        Console.WriteLine("💡 If you encounter database issues, you may need to:");
        Console.WriteLine("   1. Run 'dotnet ef database update' manually");
        Console.WriteLine("   2. Or reset the database and migrations");
    }
}

app.Run();
