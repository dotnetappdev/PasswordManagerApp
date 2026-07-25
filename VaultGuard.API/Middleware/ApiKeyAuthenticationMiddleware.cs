using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using VaultGuard.Services.Interfaces;
using System.Security.Claims;

namespace VaultGuard.API.Middleware
{
    public class ApiKeyAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;

        public ApiKeyAuthenticationMiddleware(
            RequestDelegate next,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<ApiKeyAuthenticationMiddleware> logger)
        {
            _next = next;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip authentication for health checks, API documentation, and authentication endpoints.
            // The last group (/login, /register, /refresh, …) are .NET's built-in Identity API endpoints
            // (MapIdentityApi<ApplicationUser>() in Program.cs) — VaultGuard.Admin uses these directly for
            // SuperAdmin login, and by definition a login/register call can't itself carry a bearer token
            // or API key yet, so they must stay anonymous-reachable like /api/authentication already was.
            if (context.Request.Path.StartsWithSegments("/health") ||
                context.Request.Path.StartsWithSegments("/scalar") ||
                context.Request.Path.StartsWithSegments("/openapi") ||
                context.Request.Path.StartsWithSegments("/swagger") ||
                context.Request.Path.StartsWithSegments("/api/authentication") ||
                context.Request.Path.StartsWithSegments("/login") ||
                context.Request.Path.StartsWithSegments("/register") ||
                context.Request.Path.StartsWithSegments("/refresh") ||
                context.Request.Path.StartsWithSegments("/confirmEmail") ||
                context.Request.Path.StartsWithSegments("/resendConfirmationEmail") ||
                context.Request.Path.StartsWithSegments("/forgotPassword") ||
                context.Request.Path.StartsWithSegments("/resetPassword"))
            {
                await _next(context);
                return;
            }

            // If the request is already authenticated by the framework (UseAuthentication, e.g. a
            // valid bearer token on the DEFAULT scheme), let it through without also requiring an API
            // key. We do NOT overwrite the existing principal in that case.
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                await _next(context);
                return;
            }

            // UseAuthentication() only populates context.User from the DEFAULT scheme (Identity's cookie
            // scheme here), so a valid "Authorization: Bearer ..." token — the Identity.Bearer scheme
            // registered in Program.cs, which is what VaultGuard.Admin sends — isn't reflected above even
            // though it's genuinely valid. Try it explicitly before falling back to requiring an API key.
            var bearerResult = await context.AuthenticateAsync(IdentityConstants.BearerScheme);
            if (bearerResult.Succeeded && bearerResult.Principal is not null)
            {
                context.User = bearerResult.Principal;
                await _next(context);
                return;
            }

            // Check for API key in header
            if (!context.Request.Headers.TryGetValue("X-API-Key", out var apiKeyValues))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("API Key is required");
                return;
            }

            var apiKey = apiKeyValues.FirstOrDefault();
            if (string.IsNullOrEmpty(apiKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("API Key is required");
                return;
            }

            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();
                
                var validApiKey = await apiKeyService.ValidateApiKeyAsync(apiKey);
                if (validApiKey == null)
                {
                    _logger.LogWarning("Invalid API key attempt: {ApiKey}", apiKey[..Math.Min(8, apiKey.Length)] + "...");
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Invalid API Key");
                    return;
                }

                // Update last used timestamp
                await apiKeyService.UpdateLastUsedAsync(validApiKey.Id);

                // Set user context
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, validApiKey.UserId),
                    new Claim("ApiKeyId", validApiKey.Id.ToString()),
                    new Claim("ApiKeyName", validApiKey.Name)
                };

                var identity = new ClaimsIdentity(claims, "ApiKey");
                var principal = new ClaimsPrincipal(identity);
                context.User = principal;

                _logger.LogInformation("Authenticated request with API key: {ApiKeyName} for user: {UserId}", 
                    validApiKey.Name, validApiKey.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating API key");
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Internal server error during authentication");
                return;
            }

            await _next(context);
        }
    }
}