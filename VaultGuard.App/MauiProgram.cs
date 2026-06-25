using Microsoft.EntityFrameworkCore;
using VaultGuard.DAL;
using VaultGuard.Services;
using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Services;
using VaultGuard.Imports.Interfaces;
using VaultGuard.Imports.Services;
using Microsoft.Extensions.Configuration;
using VaultGuard.Crypto.Extensions;
using VaultGuard.App.Services;
using Microsoft.AspNetCore.Identity;
using VaultGuard.Models;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using VaultGuard.Components.Shared.Services;
using VaultGuard.ExceptionReporting.Sentry;

namespace VaultGuard.App;

public static class MauiProgram
{



	public static MauiApp CreateMauiApp()
	{

		AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
		{
			var ex = (Exception)e.ExceptionObject;
			// Log or handle ex here

			SentrySdk.CaptureException(ex);

		};
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			 .UseSentry(options =>
			 {
				 // The DSN is the only required setting.
				 options.Dsn = "https://2568541e9ea54d5f8065ac285c9dc960@o4503936128909312.ingest.us.sentry.io/4509779753762816";

				 // Use debug mode if you want to see what the SDK is doing.
				 // Debug messages are written to stdout with Console.Writeline,
				 // and are viewable in your IDE's debug console or with 'adb logcat', etc.
				 // This option is not recommended when deploying your application.
				 options.Debug = true;

				 // Other Sentry options can be set here.
			 })
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("Segoe-UI.ttf", "SegoeUI");
			});

		builder.Services.AddMauiBlazorWebView();

		// Add MudBlazor services
		builder.Services.AddMudServices();

		// Add configuration
		builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

		// Exception reporting (Sentry-backed, swappable via IExceptionReporter)
		builder.Services.AddSentryExceptionReporting(builder.Configuration["ExceptionReporting:SentryDsn"], "Mobile");

		// Register crypto services (needed for database configuration)
		builder.Services.AddCryptographyServices();

		// Register platform service
		builder.Services.AddSingleton<ThemeService>();
		builder.Services.AddSingleton<IPlatformService, MauiPlatformService>();
		builder.Services.AddSingleton<ISecureStorageService, MauiSecureStorageService>();

		// Register database configuration service
		builder.Services.AddScoped<IDatabaseConfigurationService, DatabaseConfigurationService>();
		builder.Services.AddScoped<DynamicDatabaseContextFactory>();

		// Configure database context with default SQLite (will be reconfigured after setup)
		// Create a temporary platform service to get the default path
		var tempPlatformService = new MauiPlatformService();
		var appDataDir = tempPlatformService.GetAppDataDirectory();
		var defaultDbPath = Path.Combine(appDataDir, "passwordmanager.db");
		
		// Ensure directory exists
		if (!Directory.Exists(appDataDir))
		{
			Directory.CreateDirectory(appDataDir);
		}

		builder.Services.AddDbContext<VaultGuardDbContextApp>(options =>
			options.UseSqlite($"Data Source={defaultDbPath}"));
		
		// Add the regular context for compatibility
		builder.Services.AddDbContext<VaultGuardDbContext>(options =>
			options.UseSqlite($"Data Source={defaultDbPath}"));

		// Add Identity services
		builder.Services.AddIdentityCore<ApplicationUser>(options =>
		{
			options.SignIn.RequireConfirmedAccount = false;
			options.Password.RequireDigit = true;
			options.Password.RequiredLength = 8;
			options.Password.RequireNonAlphanumeric = false;
			options.Password.RequireUppercase = true;
			options.Password.RequireLowercase = true;
		})
		.AddEntityFrameworkStores<VaultGuardDbContextApp>();

		// Fix for CS0246: Correct the interface name from 'IPasswordItemIterface' to 'IPasswordItemService'  
		builder.Services.AddScoped<IPasswordItemService, PasswordItemService>();
		// Register services
		builder.Services.AddScoped<ITagService, TagService>();
		builder.Services.AddScoped<ICategoryInterface, CategoryService>();
		builder.Services.AddScoped<ICollectionService, CollectionService>();
		builder.Services.AddScoped<IAuthService, IdentityAuthService>();
		builder.Services.AddScoped<IPasswordRevealService, PasswordRevealService>();
		builder.Services.AddScoped<IAppSyncService, AppSyncService>();
		builder.Services.AddScoped<IAppStartupService, AppStartupService>();
		builder.Services.AddScoped<IDatabaseMigrationService, DatabaseMigrationService>();
		builder.Services.AddScoped<IUserProfileService, UserProfileService>();
		builder.Services.AddScoped<IVaultSessionService, VaultSessionService>();
		builder.Services.AddScoped<IPasscodeService, PasscodeService>();
		builder.Services.AddScoped<IVaultService, VaultService>();
		builder.Services.AddScoped<IAuditLogService, AuditLogService>();
		builder.Services.AddScoped<ITwoFactorService, TwoFactorService>();
		builder.Services.AddScoped<IDeviceService, DeviceService>();

		// "Remember this device" master-key cache (platform SecureStorage) so a 2FA-enabled
		// account can sign in code-only on a trusted device.
		builder.Services.AddScoped<VaultGuard.Components.Shared.Services.IMasterKeyCacheService, VaultGuard.App.Services.MauiMasterKeyCacheService>();
		builder.Services.AddScoped<IPasskeyService, PasskeyService>();
		builder.Services.AddScoped<ICustomFieldService, CustomFieldService>();

		// Register sync services
		builder.Services.AddHttpClient();

		// Register import services
		builder.Services.AddSingleton<PluginDiscoveryService>();
		builder.Services.AddScoped<IImportService, ImportService>();

		// Add QuickGrid

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.SetMinimumLevel(LogLevel.Debug);
#endif
		builder.Logging.AddProvider(new VaultGuard.Services.Logging.FileLoggerProvider(minLevel: LogLevel.Debug));

		var app = builder.Build();

		return app;
	}

}
