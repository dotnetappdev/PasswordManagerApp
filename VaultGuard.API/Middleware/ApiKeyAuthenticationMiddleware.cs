using Microsoft.AspNetCore.Http;
using PasswordManager.Services.Interfaces;
using System.Security.Claims;

namespace PasswordManager.API.Middleware
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
            // Skip authentication for health checks, API documentation, and authentication endpoints
            if (context.Request.Path.StartsWithSegments("/health") ||
                context.Request.Path.StartsWithSegments("/scalar") ||
                context.Request.Path.StartsWithSegments("/openapi") ||
                context.Request.Path.StartsWithSegments("/api/authentication"))
            {
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