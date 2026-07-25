using Microsoft.Extensions.DependencyInjection;
using VaultGuard.Crypto.Interfaces;
using VaultGuard.Crypto.Services;

namespace VaultGuard.Crypto.Extensions;

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
        services.AddSingleton<ILicenseCryptoService, LicenseCryptoService>();

        return services;
    }
}
