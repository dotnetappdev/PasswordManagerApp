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
        // SECURITY: never log the master password or its hint.
        VaultGuard.Services.Logging.AppLogger.Info("Master password setup attempted");
        return Task.FromResult(true);
    }

    public Task<bool> AuthenticateAsync(string masterPassword)
    {
        VaultGuard.Services.Logging.AppLogger.Info("Master password authentication attempted");
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
        // SECURITY: avoid logging full email (PII); log a redacted hint only.
        VaultGuard.Services.Logging.AppLogger.Info($"User login attempted: {VaultGuard.Services.Logging.AppLogger.Redact(email)}");
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
        VaultGuard.Services.Logging.AppLogger.Info($"User registration attempted: {VaultGuard.Services.Logging.AppLogger.Redact(email)}");
        return Task.FromResult(true);
    }

    public Task LogoutAsync()
    {
        VaultGuard.Services.Logging.AppLogger.Info("User logged out");
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
        VaultGuard.Services.Logging.AppLogger.Info("Master password change attempted");
        return Task.FromResult(true);
    }

    public Task<string?> GetCurrentUserIdAsync()
    {
        return Task.FromResult(_currentUser?.Id);
    }

    public Task<(bool Success, string? ErrorMessage)> DeleteAccountAsync(string password)
    {
        VaultGuard.Services.Logging.AppLogger.Info("Account deletion attempted");
        _isAuthenticated = false;
        _currentUser = null;
        return Task.FromResult<(bool, string?)>((true, null));
    }
}
