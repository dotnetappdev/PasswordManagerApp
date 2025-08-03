using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PasswordManager.DAL;
using PasswordManager.Services;
using PasswordManager.Services.Interfaces;
using PasswordManager.Services.Services;
using PasswordManager.Imports.Interfaces;
using PasswordManager.Imports.Services;
using PasswordManagerImports.OnePassword.Providers;
using Microsoft.Extensions.Configuration;
using PasswordManager.Crypto.Extensions;

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

		// Add Entity Framework
		var dbPath = Path.Combine(FileSystem.AppDataDirectory, "PasswordManager", "data", "passwordmanager.db");

		// Ensure the directory exists
		var dbDirectory = Path.GetDirectoryName(dbPath);
		if (!Directory.Exists(dbDirectory))
		{
			Directory.CreateDirectory(dbDirectory!);
		}

		builder.Services.AddDbContext<PasswordManagerDbContext>(options =>
			options.UseSqlite($"Data Source={dbPath}"));

		// Fix for CS0246: Correct the interface name from 'IPasswordItemIterface' to 'IPasswordItemService'  
		builder.Services.AddScoped<IPasswordItemService, PasswordItemService>();
		// Register services
		builder.Services.AddScoped<ITagService, TagService>();
		builder.Services.AddScoped<ICategoryInterface, CategoryService>();
		builder.Services.AddScoped<ICollectionService, CollectionService>();
		builder.Services.AddScoped<IAuthService, AuthService>();
		builder.Services.AddScoped<IPasswordRevealService, PasswordRevealService>();
		builder.Services.AddScoped<IAppSyncService, AppSyncService>();
		builder.Services.AddScoped<IAppStartupService, AppStartupService>();
		builder.Services.AddScoped<IUserProfileService, UserProfileService>();
		builder.Services.AddScoped<IVaultSessionService, VaultSessionService>();

		// Register crypto services
		builder.Services.AddCryptographyServices();

		// Register sync services
		builder.Services.AddHttpClient();

		// Register import services
		builder.Services.AddSingleton<PluginDiscoveryService>();
		builder.Services.AddScoped<IImportService, ImportService>();

		// Register import providers
		builder.Services.AddScoped<IPasswordImportProvider, OnePasswordImportProvider>();

		// Add QuickGrid

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();

		// Ensure database migrations are applied on first run
		using (var scope = app.Services.CreateScope())
		{
			try
			{
				var db = scope.ServiceProvider.GetRequiredService<PasswordManagerDbContext>();

#if DEBUG
				// Log the database path for debugging
				System.Diagnostics.Debug.WriteLine($"Database path: {dbPath}");
				System.Diagnostics.Debug.WriteLine($"App data directory: {FileSystem.AppDataDirectory}");
				System.Diagnostics.Debug.WriteLine($"Directory exists: {Directory.Exists(Path.GetDirectoryName(dbPath))}");
#endif

				// Check if this is the first run (no migration history exists)
#if DEBUG
				try
				{
					// Try to check if migration history table exists
					var hasMigrationHistory = db.Database.GetAppliedMigrations().Any();

					// If database exists but has no migration history (created with EnsureCreated), delete it
					if (db.Database.CanConnect() && !hasMigrationHistory)
					{
						db.Database.EnsureDeleted();
					}
				}
				catch
				{
					// If we can't check migration history, it's likely the first run or database doesn't exist
					// Just proceed with migration
				}
#endif

				// Only migrate if there are pending migrations
				var pendingMigrations = db.Database.GetPendingMigrations();
				if (pendingMigrations.Any())
				{
					db.Database.Migrate();
				}

				// Alternative approach for simple scenarios (no migrations):
				// db.Database.EnsureCreated();

				// Initialize import service with providers
				var importService = scope.ServiceProvider.GetRequiredService<IImportService>();
				var onePasswordProvider = scope.ServiceProvider.GetRequiredService<IPasswordImportProvider>();
				importService.RegisterProvider(onePasswordProvider);

				// Initialize startup sync service (non-blocking)
				var startupService = scope.ServiceProvider.GetRequiredService<IAppStartupService>();
				// Don't await this to prevent blocking app startup
				_ = startupService.InitializeAsync();
			}
			catch (Exception ex)
			{
				// Log the error but don't prevent app from starting
				System.Diagnostics.Debug.WriteLine($"Error during app initialization: {ex.Message}");
				System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
				// App can still start even if initialization fails
			}
		}


		return app;
	}

}
