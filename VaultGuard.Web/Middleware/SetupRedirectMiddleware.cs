using PasswordManager.Services.Interfaces;

namespace PasswordManager.Web.Middleware;

/// <summary>
/// Middleware to redirect to setup page on first run
/// </summary>
public class SetupRedirectMiddleware
{
    private readonly RequestDelegate _next;

    public SetupRedirectMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IDatabaseConfigurationService configService)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        
        // Skip setup redirect for static files, API calls, and setup page itself
        if (path.StartsWith("/setup") || 
            path.StartsWith("/_") || 
            path.StartsWith("/css") || 
            path.StartsWith("/js") || 
            path.StartsWith("/api") ||
            path.Contains("."))
        {
            await _next(context);
            return;
        }

        try
        {
            // Check if this is first run
            var isFirstRun = await configService.IsFirstRunAsync();
            
            if (isFirstRun && !path.StartsWith("/setup"))
            {
                context.Response.Redirect("/setup");
                return;
            }
        }
        catch (OperationCanceledException)
        {
            // Timeout checking configuration - allow request to continue
            // This prevents hanging on slow file systems
        }
        catch (Exception ex)
        {
            // Log the error but allow the request to continue
            // Configuration check failures shouldn't block access
            Console.WriteLine($"Setup redirect middleware error: {ex.Message}");
        }

        await _next(context);
    }
}

/// <summary>
/// Extension method to add setup redirect middleware to the pipeline
/// </summary>
public static class SetupRedirectMiddlewareExtensions
{
    public static IApplicationBuilder UseSetupRedirect(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SetupRedirectMiddleware>();
    }
}
