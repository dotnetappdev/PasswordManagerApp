using PasswordManager.Web.Components;
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
using Pomelo.EntityFrameworkCore.MySql;
using PasswordManager.DAL.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add MudBlazor services
builder.Services.AddMudServices();

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

// Add Identity services
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
})
.AddEntityFrameworkStores<PasswordManagerDbContextApp>();

// Register application services
builder.Services.AddScoped<IPasswordItemService, PasswordManager.Services.PasswordItemService>();
builder.Services.AddScoped<ITagService, PasswordManager.Services.TagService>();
builder.Services.AddScoped<ICategoryInterface, PasswordManager.Services.Services.CategoryService>();
builder.Services.AddScoped<ICollectionService, PasswordManager.Services.Services.CollectionService>();
builder.Services.AddScoped<IAuthService, PasswordManager.Services.Services.AuthService>();
builder.Services.AddScoped<IUserProfileService, PasswordManager.Services.Services.UserProfileService>();
builder.Services.AddScoped<IApiKeyService, PasswordManager.Services.Services.ApiKeyService>();
builder.Services.AddScoped<IVaultSessionService, PasswordManager.Services.Services.VaultSessionService>();
builder.Services.AddScoped<IQrLoginService, PasswordManager.Services.Services.QrLoginService>();
builder.Services.AddScoped<IDatabaseContextFactory, PasswordManager.Services.Services.DatabaseContextFactory>();
builder.Services.AddScoped<IPasswordEncryptionService, PasswordManager.Services.Services.PasswordEncryptionService>();

// Register crypto services
builder.Services.AddCryptographyServices();

// Add HttpClient for API calls
builder.Services.AddHttpClient();

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

app.MapRazorPages();

// Ensure database is created and migrated
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PasswordManagerDbContextApp>();
    await dbContext.Database.EnsureCreatedAsync();
}

app.Run();
