using VaultGuard.Models;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.WinUi.Services.CrossPlatform;

/// <summary>
/// Simple authentication service for cross-platform builds.
/// This provides basic authentication functionality without Windows-specific dependencies.
/// </summary>
public class SimpleAuthService : IAuthService
{
    private bool _isAuthenticated = false;
    private ApplicationUser? _currentUser = null;

    public bool IsAuthenticated => _isAuthenticated;
    public ApplicationUser? CurrentUser => _currentUser;

    public Task<bool> SetupMasterPasswordAsync(string masterPassword, string hint = "")
    {
        Console.WriteLine($"Master password setup attempted with hint: {hint}");
        return Task.FromResult(true);
    }

    public Task<bool> AuthenticateAsync(string masterPassword)
    {
        Console.WriteLine("Master password authentication attempted");
        _isAuthenticated = true;
        _currentUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "demo@example.com",
            FirstName = "Demo",
            LastName = "User"
        };
        return Task.FromResult(true);
    }

    public Task<bool> CheckAuthenticationStatusAsync()
    {
        return Task.FromResult(_isAuthenticated);
    }

    public Task<bool> IsAuthenticatedAsync()
    {
        return Task.FromResult(_isAuthenticated);
    }

    public Task<bool> LoginAsync(string email, string password)
    {
        Console.WriteLine($"User login attempted: {email}");
        _isAuthenticated = true;
        _currentUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            FirstName = "Demo",
            LastName = "User"
        };
        return Task.FromResult(true);
    }

    public Task<bool> RegisterAsync(string email, string password)
    {
        Console.WriteLine($"User registration attempted: {email}");
        return Task.FromResult(true);
    }

    public Task LogoutAsync()
    {
        Console.WriteLine("User logged out");
        _isAuthenticated = false;
        _currentUser = null;
        return Task.CompletedTask;
    }

    public Task<bool> IsFirstTimeSetupAsync()
    {
        return Task.FromResult(false);
    }

    public Task<string> GetMasterPasswordHintAsync()
    {
        return Task.FromResult("Demo password hint");
    }

    public Task<bool> ChangeMasterPasswordAsync(string currentPassword, string newPassword, string newPasswordHint = "")
    {
        Console.WriteLine("Master password change attempted");
        return Task.FromResult(true);
    }

    public Task<string?> GetCurrentUserIdAsync()
    {
        return Task.FromResult(_currentUser?.Id);
    }

    public Task<(bool Success, string? ErrorMessage)> DeleteAccountAsync(string password)
    {
        Console.WriteLine("Account deletion attempted");
        _isAuthenticated = false;
        _currentUser = null;
        return Task.FromResult<(bool, string?)>((true, null));
    }
}
