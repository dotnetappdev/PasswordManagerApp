using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using PasswordManager.Services.Interfaces;
using PasswordManager.Services.Services;

namespace PasswordManager.Services.Extensions;

/// <summary>
/// Extension methods for registering authentication services based on context
/// </summary>
public static class AuthServiceExtensions
{
    /// <summary>
    /// Adds the appropriate IAuthService implementation based on the current context.
    /// - Uses IdentityAuthService for Blazor contexts (where IJSRuntime is available)
    /// - Uses ServerAuthService for non-Blazor contexts (APIs, console apps, etc.)
    /// </summary>
    public static IServiceCollection AddContextualAuthService(this IServiceCollection services)
    {
        // Check if IJSRuntime is already registered (indicates Blazor context)
        var jsRuntimeDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IJSRuntime));
        
        if (jsRuntimeDescriptor != null)
        {
            // Blazor context - use IdentityAuthService
            services.AddScoped<IAuthService, IdentityAuthService>();
        }
        else
        {
            // Non-Blazor context - use ServerAuthService
            services.AddScoped<IAuthService, ServerAuthService>();
        }
        
        return services;
    }

    /// <summary>
    /// Explicitly adds the Blazor-compatible auth service (requires IJSRuntime)
    /// </summary>
    public static IServiceCollection AddBlazorAuthService(this IServiceCollection services)
    {
        return services.AddScoped<IAuthService, IdentityAuthService>();
    }

    /// <summary>
    /// Explicitly adds the server-compatible auth service (no IJSRuntime required)
    /// </summary>
    public static IServiceCollection AddServerAuthService(this IServiceCollection services)
    {
        return services.AddScoped<IAuthService, ServerAuthService>();
    }
}