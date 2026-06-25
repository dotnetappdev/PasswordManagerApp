using PasswordManager.Web.Components;
using PasswordManager.Web.Middleware;
using Microsoft.EntityFrameworkCore;
using PasswordManager.DAL;
using PasswordManager.DAL.SqlServer;
using PasswordManager.DAL.MySql;
using PasswordManager.DAL.SupaBase;
using PasswordManager.Services.Interfaces;
using PasswordManager.Services.Services;
using PasswordManager.Services; // Add this line for the service classes
using PasswordManager.Crypto.Extensions;
using MudBlazor.Services;
using Microsoft.AspNetCore.Identity;
using PasswordManager.Models;
using PasswordManager.Models.Configuration;
using Pomelo.EntityFrameworkCore.MySql;
using PasswordManager.DAL.Interfaces;

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

// Configure Sentry settings
builder.Services.Configure<SentryConfiguration>(
    builder.Configuration.GetSection("Sentry"));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add MudBlazor services
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass    = MudBlazor.Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.MaxDisplayedSnackbars = 4;
    config.SnackbarConfiguration.PreventDuplicates  = false;
    config.SnackbarConfiguration.NewestOnTop        = true;
    config.SnackbarConfiguration.SnackbarVariant    = MudBlazor.Variant.Filled;
    config.SnackbarConfiguration.VisibleStateDuration = 4000;
    config.SnackbarConfiguration.HideTransitionDuration = 400;
    config.SnackbarConfiguration.ShowTransitionDuration = 300;
});

// Register AppNotificationService (thin toast wrapper)
builder.Services.AddScoped<PasswordManager.Web.Services.AppNotificationService>();

// Configure MudBlazor theme — steel blue, matches WPF brand
builder.Services.AddScoped(sp => new MudBlazor.MudTheme()
{
    PaletteLight = new MudBlazor.PaletteLight()
    {
        Primary = "#2563EB",
        PrimaryLighten = "#60A5FA",
        PrimaryDarken = "#1D4ED8",
        Secondary = "#6366F1",
        AppbarBackground = "#2563EB",
    },
    PaletteDark = new MudBlazor.PaletteDark()
    {
        Primary = "#2563EB",
        PrimaryLighten = "#60A5FA",
        PrimaryDarken = "#1D4ED8",
        Secondary = "#6366F1",
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
builder.Services.AddScoped<PasswordManager.Components.Shared.Services.ThemeService>();

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

    builder.Services.AddDbContext<PasswordManagerDbContextApp>(options =>
        options.UseNpgsql(supabaseUrl));
    builder.Services.AddDbContext<PasswordManagerDbContext>(options =>
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
        builder.Services.AddDbContext<PasswordManagerDbContextApp>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
        builder.Services.AddDbContext<PasswordManagerDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
    }
    else if (databaseProvider.ToLower() == "sqlite")
    {
        builder.Services.AddDbContext<PasswordManagerDbContextApp>(options =>
            options.UseSqlite(connectionString));
        builder.Services.AddDbContext<PasswordManagerDbContext>(options =>
            options.UseSqlite(connectionString));
    }
    else
    {
        builder.Services.AddDbContext<PasswordManagerDbContextApp>(options =>
            options.UseSqlServer(connectionString));
        builder.Services.AddDbContext<PasswordManagerDbContext>(options =>
            options.UseSqlServer(connectionString));
    }
}

// Register DbContext interfaces for DI
builder.Services.AddScoped<IPasswordManagerDbContext>(sp => sp.GetRequiredService<PasswordManagerDbContext>());
builder.Services.AddScoped<IPasswordManagerDbContextApp>(sp => sp.GetRequiredService<PasswordManagerDbContextApp>());

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
.AddEntityFrameworkStores<PasswordManagerDbContextApp>()
.AddDefaultTokenProviders();

// Register application services
builder.Services.AddScoped<IPasswordItemService, PasswordManager.Services.PasswordItemService>();
builder.Services.AddScoped<ITagService, PasswordManager.Services.TagService>();
builder.Services.AddScoped<ICategoryInterface, PasswordManager.Services.Services.CategoryService>();

// Shared, stateless feature services (strength meter + TOTP authenticator codes + QR)
builder.Services.AddSingleton<IPasswordStrengthService, PasswordManager.Services.Services.PasswordStrengthService>();
builder.Services.AddSingleton<ITotpService, PasswordManager.Services.Services.TotpService>();
builder.Services.AddSingleton<IQrCodeService, PasswordManager.Services.Services.QrCodeService>();
builder.Services.AddSingleton<ISecurityAuditService, PasswordManager.Services.Services.SecurityAuditService>();
builder.Services.AddSingleton<IPassphraseGenerator, PasswordManager.Services.Services.PassphraseGenerator>();
builder.Services.AddScoped<ICollectionService, PasswordManager.Services.Services.CollectionService>();
builder.Services.AddScoped<IAuthService, PasswordManager.Services.Services.AuthService>();
builder.Services.AddScoped<IUserProfileService, PasswordManager.Services.Services.UserProfileService>();
builder.Services.AddScoped<IApiKeyService, PasswordManager.Services.Services.ApiKeyService>();
builder.Services.AddScoped<IVaultSessionService, PasswordManager.Services.Services.VaultSessionService>();
builder.Services.AddScoped<IQrLoginService, PasswordManager.Services.Services.QrLoginService>();
builder.Services.AddScoped<IDatabaseContextFactory, PasswordManager.Services.Services.DatabaseContextFactory>();
builder.Services.AddScoped<IDatabaseConfigurationService, PasswordManager.Services.Services.DatabaseConfigurationService>();
builder.Services.AddScoped<IPlatformService, PasswordManager.Services.Services.DefaultPlatformService>();
builder.Services.AddScoped<IPasswordEncryptionService, PasswordManager.Services.Services.PasswordEncryptionService>();
builder.Services.AddScoped<IPasskeyService, PasswordManager.Services.Services.PasskeyService>();
builder.Services.AddScoped<IDatabaseMigrationService, PasswordManager.Services.Services.DatabaseMigrationService>();
builder.Services.AddScoped<IDatabaseHealthService, PasswordManager.Services.Services.DatabaseHealthService>();
builder.Services.AddScoped<IDatabaseResetService, PasswordManager.Services.Services.DatabaseResetService>();
builder.Services.AddScoped<IPermissionService, PasswordManager.Services.Services.PermissionService>();
builder.Services.AddScoped<IVaultService, PasswordManager.Services.Services.VaultService>();
builder.Services.AddScoped<IAuditLogService, PasswordManager.Services.Services.AuditLogService>();
builder.Services.AddScoped<ITwoFactorService, PasswordManager.Services.Services.TwoFactorService>();
builder.Services.AddScoped<IDeviceService, PasswordManager.Services.Services.DeviceService>();

// Cloud backup services
builder.Services.AddScoped<PasswordManager.Services.Interfaces.IBackupEncryptionService, PasswordManager.Services.Services.BackupEncryptionService>();
builder.Services.AddScoped<PasswordManager.Services.Interfaces.IDatabaseBackupService, PasswordManager.Services.Services.DatabaseBackupService>();
builder.Services.AddScoped<PasswordManager.Services.Interfaces.IOneDriveBackupService, PasswordManager.Services.Services.OneDriveBackupService>();
builder.Services.AddScoped<PasswordManager.Services.Interfaces.IiCloudBackupService, PasswordManager.Services.Services.iCloudBackupService>();
builder.Services.AddScoped<PasswordManager.Services.Interfaces.INetworkLocationBackupService, PasswordManager.Services.Services.NetworkLocationBackupService>();
builder.Services.AddSingleton<PasswordManager.Services.Interfaces.IGoogleDriveBackupService, PasswordManager.Services.Services.GoogleDriveBackupService>();
builder.Services.AddScoped<PasswordManager.Services.Interfaces.IBackupSettingsService, PasswordManager.Services.Services.BackupSettingsService>();
builder.Services.AddScoped<PasswordManager.Services.Services.CloudBackupManager>();

// Register password import services (1Password 1pux/CSV, Bitwarden, etc.)
builder.Services.AddSingleton<PasswordManager.Imports.Services.PluginDiscoveryService>();
builder.Services.AddScoped<PasswordManager.Imports.Interfaces.IImportService, PasswordManager.Imports.Services.ImportService>();

// Register crypto services
builder.Services.AddCryptographyServices();

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

// Register Identity data seeder
builder.Services.AddScoped<PasswordManager.DAL.Seed.IdentityDataSeeder>();

// Add HttpClient for API calls
builder.Services.AddHttpClient();

// Add HttpClient for API communication with Bearer token support
builder.Services.AddHttpClient("PasswordManagerAPI", client =>
{
    var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7001";
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Add API service for external API communication if needed
builder.Services.AddScoped<IAppSyncService, PasswordManager.Services.Services.AppSyncService>();

var app = builder.Build();

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
            var dbContextApp = scope.ServiceProvider.GetRequiredService<PasswordManagerDbContextApp>();
            await dbContextApp.Database.EnsureCreatedAsync();
            Console.WriteLine("✅ PasswordManagerDbContextApp schema ensured");
        }
        catch (Exception ensureEx)
        {
            Console.WriteLine($"⚠️  Schema ensure warning (App): {ensureEx.Message}");
        }

        try
        {
            var dbContextMain = scope.ServiceProvider.GetRequiredService<PasswordManagerDbContext>();
            await dbContextMain.Database.EnsureCreatedAsync();
            Console.WriteLine("✅ PasswordManagerDbContext schema ensured");
        }
        catch (Exception ensureEx)
        {
            Console.WriteLine($"⚠️  Schema ensure warning (Main): {ensureEx.Message}");
        }

        // Also run the migration service for any remaining work (table creation, etc.)
        var migrationService = scope.ServiceProvider.GetService<IDatabaseMigrationService>();
        if (migrationService != null)
        {
            try
            {
                await migrationService.EnsurePasswordItemTagsTableExistsAsync();
                Console.WriteLine("Ensured PasswordItemTags junction table exists");
            }
            catch (Exception ensureEx)
            {
                Console.WriteLine($"Warning: could not ensure PasswordItemTags table: {ensureEx.Message}");
            }
        }

        // Seed Identity data (roles and default users)
        try
        {
            var identitySeeder = scope.ServiceProvider.GetRequiredService<PasswordManager.DAL.Seed.IdentityDataSeeder>();
            await identitySeeder.SeedAsync();
            Console.WriteLine("✅ Identity data seeded successfully");
        }
        catch (Exception seedEx)
        {
            Console.WriteLine($"⚠️  Identity seeding warning: {seedEx.Message}");
        }

        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PasswordManagerDbContext>();
            if (!await dbContext.PasswordItems.AnyAsync())
            {
                var seedUserId =
                    await dbContext.Users
                        .Where(u => u.Email == "user@passwordmanager.local")
                        .Select(u => u.Id)
                        .FirstOrDefaultAsync()
                    ?? await dbContext.Users.Select(u => u.Id).FirstOrDefaultAsync()
                    ?? PasswordManager.DAL.Seed.TestDataSeeder.TestUserId;

                PasswordManager.DAL.Seed.TestDataSeeder.SeedTestData(dbContext, seedUserId);
                Console.WriteLine("✅ Demo password data seeded successfully");
            }
        }
        catch (Exception seedEx)
        {
            Console.WriteLine($"⚠️  Demo data seeding warning: {seedEx.Message}");
        }
    }
    catch (Exception ex)
    {
        // Log the error but don't stop the application
        Console.WriteLine($"⚠️  Database initialization warning: {ex.Message}");
        Console.WriteLine("🚀 Application will continue to start...");
        Console.WriteLine("💡 If you encounter database issues, you may need to:");
        Console.WriteLine("   1. Run 'dotnet ef database update' manually");
        Console.WriteLine("   2. Or reset the database and migrations");
    }
}

app.Run();
