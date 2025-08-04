using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PasswordManager.API;
using PasswordManager.DAL;
using PasswordManager.Models;
using PasswordManager.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace PasswordManager.Tests.QrLogin;

public class AuthenticatedTestBase : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    protected readonly WebApplicationFactory<Program> _factory;
    protected readonly HttpClient _client;
    protected readonly HttpClient _authenticatedClient;
    protected string _testUserId = "";
    protected string _testApiKey = "";

    public AuthenticatedTestBase(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Remove all existing DbContext registrations
                var descriptors = services.Where(d => 
                    d.ServiceType == typeof(DbContextOptions<PasswordManagerDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions<PasswordManagerDbContextApp>) ||
                    d.ServiceType == typeof(PasswordManagerDbContext) ||
                    d.ServiceType == typeof(PasswordManagerDbContextApp) ||
                    d.ServiceType.Name.Contains("DbContext")).ToList();
                
                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<PasswordManagerDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDatabase");
                });
                
                services.AddDbContext<PasswordManagerDbContextApp>(options =>
                {
                    options.UseInMemoryDatabase("TestDatabase");
                });
            });
        });
        
        _client = _factory.CreateClient();
        _authenticatedClient = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        // Create test user and API key
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PasswordManagerDbContext>();
        var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Ensure database is created and migrated
        await context.Database.EnsureCreatedAsync();

        // Create test user
        var testUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "testuser@example.com",
            Email = "testuser@example.com",
            EmailConfirmed = true,
            IsActive = true,
            FirstName = "Test",
            LastName = "User"
        };

        var userResult = await userManager.CreateAsync(testUser, "TestPassword123!");
        if (userResult.Succeeded)
        {
            _testUserId = testUser.Id;
            
            // Create API key for test user
            var apiKey = await apiKeyService.CreateApiKeyAsync("Test API Key", _testUserId);
            // The CreateApiKeyAsync method returns the key with the actual value in KeyHash temporarily
            _testApiKey = apiKey.KeyHash;
            
            // Configure authenticated client
            _authenticatedClient.DefaultRequestHeaders.Add("X-API-Key", _testApiKey);
        }
    }

    public async Task DisposeAsync()
    {
        // Clean up test data
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PasswordManagerDbContext>();
        
        if (!string.IsNullOrEmpty(_testUserId))
        {
            // Remove test user and related data
            var user = await context.Users.FindAsync(_testUserId);
            if (user != null)
            {
                // Remove API keys
                var apiKeys = await context.ApiKeys.Where(k => k.UserId == _testUserId).ToListAsync();
                context.ApiKeys.RemoveRange(apiKeys);
                
                // Remove user
                context.Users.Remove(user);
                await context.SaveChangesAsync();
            }
        }
    }

    private static string GetApiKeyValue(ApiKey apiKey)
    {
        // This method is no longer needed since we get the value directly from CreateApiKeyAsync
        return apiKey.KeyHash;
    }
}