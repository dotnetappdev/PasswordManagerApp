using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Crypto.Interfaces;
using PasswordManager.Crypto.Services;
using PasswordManager.Services.Interfaces;
using PasswordManager.Services.Services;

namespace PasswordManager.Crypto.Extensions;

/// <summary>
/// Extension methods for registering crypto services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers cryptography services with the DI container
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddCryptographyServices(this IServiceCollection services)
    {
        services.AddSingleton<ICryptographyService, CryptographyService>();
        services.AddSingleton<IPasswordCryptoService, PasswordCryptoService>();
        // VaultSessionService moved to Services layer
        services.AddScoped<IVaultSessionService, PasswordManager.Services.Services.VaultSessionService>(); // Scoped for per-session use
        
        return services;
    }
}
