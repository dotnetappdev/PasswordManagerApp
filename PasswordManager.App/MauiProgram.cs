using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PasswordManager.DAL;
using PasswordManager.Interfaces;
using PasswordManager.Services;
using PasswordManager.Services.Interfaces;
using PasswordManager.Services.Services;
using PasswordManager.Imports.Interfaces;
using PasswordManager.Imports.Services;
using PasswordManagerImports.OnePassword.Providers;
using PasswordManager.App.Services.Interfaces;
using PasswordManager.App.Services;
using Microsoft.Extensions.Configuration;

namespace PasswordManager.App;

public static class MauiProgram
{

	public static MauiApp CreateMauiApp()
	{
	}
	public static async Task<MauiApp> CreateMauiAppAsync()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
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
		builder.Services.AddScoped<Services.AuthService>();

		// Register sync services
		builder.Services.AddHttpClient();
		builder.Services.AddScoped<IAppSyncService, AppSyncService>();
		builder.Services.AddScoped<AppStartupService>();

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

			// Initialize startup sync service
			var startupService = scope.ServiceProvider.GetRequiredService<AppStartupService>();
			await startupService.InitializeAsync();
		}

		return app;
	}
}
