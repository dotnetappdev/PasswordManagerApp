using Microsoft.EntityFrameworkCore;
using PasswordManager.DAL;
using PasswordManager.Crypto.Extensions;
using PasswordManager.Services.Interfaces;
using PasswordManager.Services.Services;
using Serilog;
using Scalar.AspNetCore;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

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
var connectionString = databaseProvider.ToLower() switch
{
    "sqlite" => builder.Configuration.GetConnectionString("SqliteConnection"),
    "postgres" or "postgresql" => builder.Configuration.GetConnectionString("PostgresConnection"),
    "mysql" => builder.Configuration.GetConnectionString("MySqlConnection"),
    _ => builder.Configuration.GetConnectionString("DefaultConnection")
};

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException($"Connection string for {databaseProvider} not found.");
}

// Configure DbContext based on provider
switch (databaseProvider.ToLower())
{
    case "sqlite":
        builder.Services.AddDbContext<PasswordManagerDbContext>(options =>
            options.UseSqlite(connectionString));
        break;
    case "postgres":
    case "postgresql":
        builder.Services.AddDbContext<PasswordManagerDbContext>(options =>
            options.UseNpgsql(connectionString));
        break;
    case "mysql":
        builder.Services.AddDbContext<PasswordManagerDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
        break;
    default:
        builder.Services.AddDbContext<PasswordManagerDbContext>(options =>
            options.UseSqlServer(connectionString));
        break;
}

// Register application services
builder.Services.AddScoped<IPasswordItemApiService, PasswordManager.Services.Services.PasswordItemApiService>();
builder.Services.AddScoped<ICategoryApiService, PasswordManager.Services.Services.CategoryApiService>();
builder.Services.AddScoped<ICollectionApiService, PasswordManager.Services.Services.CollectionApiService>();
builder.Services.AddScoped<ITagApiService, PasswordManager.Services.Services.TagApiService>();
builder.Services.AddScoped<ISyncService, PasswordManager.Services.Services.SyncService>();
builder.Services.AddScoped<IDatabaseContextFactory, PasswordManager.Services.Services.DatabaseContextFactory>();
builder.Services.AddScoped<IPasswordEncryptionService, PasswordManager.Services.Services.PasswordEncryptionService>();
builder.Services.AddScoped<IJwtService, PasswordManager.Services.Services.JwtService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IVaultSessionService, PasswordManager.Services.Services.VaultSessionService>();
builder.Services.AddHostedService<PasswordManager.Services.Services.AutoSyncService>();

// Register cryptography services
builder.Services.AddCryptographyServices();

// Add API documentation with Scalar
builder.Services.AddOpenApi();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<PasswordManagerDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = builder.Configuration["ApiSettings:Title"] ?? "Password Manager API";
        options.Version = builder.Configuration["ApiSettings:Version"] ?? "v1";
        options.Theme = ScalarTheme.Purple;
        options.ShowSidebar = true;
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

// Ensure database is created and migrated
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PasswordManagerDbContext>();
    try
    {
        await context.Database.EnsureCreatedAsync();
        Log.Information("Database ensured for provider: {Provider}", databaseProvider);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error ensuring database exists");
    }
}

Log.Information("Password Manager API starting up...");
Log.Information("Database Provider: {Provider}", databaseProvider);
Log.Information("API Documentation available at: /scalar/v1");

app.Run();

public partial class Program { } // For testing purposes
