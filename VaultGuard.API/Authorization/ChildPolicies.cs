using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using VaultGuard.Models;

namespace VaultGuard.API.Authorization;

/// <summary>
/// Backs the [RestrictedFromChildren] and [ChildSafeOperation] attributes. Neither policy was ever
/// registered, so every endpoint carrying one failed with HTTP 500 ("AuthorizationPolicy ... was not found")
/// — including creating, updating and revealing vault items.
/// </summary>
public static class ChildPolicies
{
    public const string RestrictedFromChildren = "RestrictedFromChildren";
    public const string ChildSafeOperation = "ChildSafeOperation";

    public static IServiceCollection AddChildPolicies(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, NotChildHandler>();
        services.AddAuthorization(o =>
        {
            // Children may perform these (controllers still apply their finer per-child permissions).
            o.AddPolicy(ChildSafeOperation, p => p.RequireAuthenticatedUser());
            // Anyone signed in except accounts in the Child role.
            o.AddPolicy(RestrictedFromChildren, p => p.RequireAuthenticatedUser().AddRequirements(new NotChildRequirement()));
        });
        return services;
    }
}

public sealed class NotChildRequirement : IAuthorizationRequirement { }

/// <summary>
/// Looks the role up in the user store rather than trusting claims: API-key principals carry only the
/// user id, no role claims. Fails closed if the user can't be identified.
/// </summary>
public sealed class NotChildHandler : AuthorizationHandler<NotChildRequirement>
{
    private readonly UserManager<ApplicationUser> _users;
    public NotChildHandler(UserManager<ApplicationUser> users) => _users = users;

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, NotChildRequirement requirement)
    {
        var id = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? context.User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(id)) return;
        if (context.User.IsInRole(ApplicationRoles.Child)) return;
        var user = await _users.FindByIdAsync(id);
        if (user == null || await _users.IsInRoleAsync(user, ApplicationRoles.Child)) return;
        context.Succeed(requirement);
    }
}
