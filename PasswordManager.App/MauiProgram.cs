using Microsoft.EntityFrameworkCore;
using PasswordManager.DAL;
using PasswordManager.Services;
using PasswordManager.Services.Interfaces;
using PasswordManager.Services.Services;
using PasswordManager.Imports.Interfaces;
using PasswordManager.Imports.Services;
using Microsoft.Extensions.Configuration;
using PasswordManager.Crypto.Extensions;
using PasswordManager.App.Services;
using Microsoft.AspNetCore.Identity;
using PasswordManager.Models;
using Microsoft.Extensions.Logging;

namespace PasswordManager.App;

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

		// Add configuration
		builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

		// Register crypto services (needed for database configuration)
		builder.Services.AddCryptographyServices();

		// Register platform service
		builder.Services.AddSingleton<IPlatformService, MauiPlatformService>();
		builder.Services.AddSingleton<ISecureStorageService, MauiSecureStorageService>();

		// Register database configuration service
		builder.Services.AddScoped<IDatabaseConfigurationService, DatabaseConfigurationService>();
		builder.Services.AddScoped<DynamicDatabaseContextFactory>();

		// Configure database context with default SQLite (will be reconfigured after setup)
		// Create a temporary platform service to get the default path
		var tempPlatformService = new MauiPlatformService();
		var defaultDbPath = Path.Combine(tempPlatformService.GetAppDataDirectory(), "data", "passwordmanager.db");
		var defaultDirectory = Path.GetDirectoryName(defaultDbPath);
		if (!Directory.Exists(defaultDirectory))
		{
			Directory.CreateDirectory(defaultDirectory!);
		}

		builder.Services.AddDbContext<PasswordManagerDbContextApp>(options =>
			options.UseSqlite($"Data Source={defaultDbPath}"));
		
		// Add the regular context for compatibility
		builder.Services.AddDbContext<PasswordManagerDbContext>(options =>
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
		.AddEntityFrameworkStores<PasswordManagerDbContextApp>();

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

		var app = builder.Build();

		// Initialize database configuration and startup services
		using (var scope = app.Services.CreateScope())
		{
			try
			{
				// First, ensure a basic SQLite database exists so the app can always start
				var databaseConfigService = scope.ServiceProvider.GetRequiredService<IDatabaseConfigurationService>();
				
				// This creates a minimal SQLite database without migrations to ensure app can start
				databaseConfigService.EnsureBasicSqliteDatabaseAsync(scope.ServiceProvider).GetAwaiter().GetResult();

				var startupService = scope.ServiceProvider.GetRequiredService<IAppStartupService>();

				// Use a timeout to prevent startup from hanging indefinitely
				var initializationTask = Task.Run(async () => await startupService.InitializeAsync());
				
				// Don't await - let initialization run in background
				// This prevents the UI from being blocked by slow database operations
				_ = initializationTask.ContinueWith(task =>
				{
					if (task.IsFaulted)
					{
						System.Diagnostics.Debug.WriteLine($"Background initialization completed with errors: {task.Exception?.GetBaseException()?.Message}");
					}
					else
					{
						System.Diagnostics.Debug.WriteLine("Background initialization completed successfully");
					}
				});

				// Import service will automatically discover and load all available providers
				// No manual registration needed - providers are auto-discovered from assemblies and plugins
			}
			catch (Exception ex)
			{
				// Log the error but don't prevent app from starting
				System.Diagnostics.Debug.WriteLine($"Error setting up background initialization: {ex.Message}");
				System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
				// App can still start even if initialization setup fails
			}
		}


		return app;
	}

}
