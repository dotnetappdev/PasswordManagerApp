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
		var dbPath = Path.Combine(FileSystem.AppDataDirectory, "passwordmanager.db");
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

		// Ensure database and tables are created on first run
		using (var scope = app.Services.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<PasswordManagerDbContext>();
			db.Database.EnsureCreated();
		}

		return app;
	}
}
