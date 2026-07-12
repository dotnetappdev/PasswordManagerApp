using VaultGuard.Models;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.API.Services;

/// <summary>
/// Headless <see cref="IAuthService"/> for the API host. The API authenticates every request
/// independently (API key + session/bearer token resolved by the middleware and controllers), so it has
/// no stateful "current user" the way the desktop/Blazor hosts do. This implementation exists only so that
/// services which take an <c>IAuthService</c> in their constructor (e.g. <c>VaultService</c>,
/// <c>CategoryService</c>) can be constructed by DI in the API. It intentionally reports "not
/// authenticated" — the API's real per-user data access goes through the request-scoped *ApiService
/// classes, not this service.
/// </summary>
public sealed class ApiAuthService : IAuthService
{
    public bool IsAuthenticated => false;
    public ApplicationUser? CurrentUser => null;

    public Task<bool> SetupMasterPasswordAsync(string masterPassword, string hint = "") => Task.FromResult(false);
    public Task<bool> AuthenticateAsync(string masterPassword) => Task.FromResult(false);
    public Task<bool> CheckAuthenticationStatusAsync() => Task.FromResult(false);
    public Task<bool> IsAuthenticatedAsync() => Task.FromResult(false);
    public Task<bool> LoginAsync(string email, string password) => Task.FromResult(false);
    public Task<bool> RegisterAsync(string email, string password) => Task.FromResult(false);
    public Task LogoutAsync() => Task.CompletedTask;
    public Task<bool> IsFirstTimeSetupAsync() => Task.FromResult(false);
    public Task<string> GetMasterPasswordHintAsync() => Task.FromResult(string.Empty);
    public Task<bool> ChangeMasterPasswordAsync(string currentPassword, string newPassword, string newPasswordHint = "") => Task.FromResult(false);
    public Task<string?> GetCurrentUserIdAsync() => Task.FromResult<string?>(null);
    public Task<(bool Success, string? ErrorMessage)> DeleteAccountAsync(string password) =>
        Task.FromResult<(bool, string?)>((false, "Account deletion is not available through this endpoint."));
}
