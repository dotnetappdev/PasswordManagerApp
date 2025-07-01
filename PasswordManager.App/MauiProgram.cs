using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PasswordManager.DAL;
using PasswordManager.Interfaces;
using PasswordManager.Services;
using PasswordManager.Services.Interfaces;
using PasswordManager.Services.Services;

namespace PasswordManager.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
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
			
			db.Database.Migrate();
		}

		return app;
	}
}
