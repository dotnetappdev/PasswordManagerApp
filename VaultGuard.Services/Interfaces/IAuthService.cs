using VaultGuard.Models;
using VaultGuard.Models.DTOs.Auth;

namespace VaultGuard.Services.Interfaces;

/// <summary>
/// Service for user authentication and session management
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Gets the current authentication status
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the current authenticated user
    /// </summary>
    ApplicationUser? CurrentUser { get; }

    /// <summary>
    /// Sets up a new master password for first-time use
    /// </summary>
    /// <param name="masterPassword">The master password to set</param>
    /// <param name="hint">Optional hint for the master password</param>
    /// <returns>True if setup was successful</returns>
    Task<bool> SetupMasterPasswordAsync(string masterPassword, string hint = "");

    /// <summary>
    /// Authenticates user with master password
    /// </summary>
    /// <param name="masterPassword">The master password to verify</param>
    /// <returns>True if authentication was successful</returns>
    Task<bool> AuthenticateAsync(string masterPassword);

    /// <summary>
    /// Checks if user is already authenticated
    /// </summary>
    /// <returns>True if user is authenticated</returns>
    Task<bool> CheckAuthenticationStatusAsync();

    /// <summary>
    /// Checks if user is already authenticated (alias for CheckAuthenticationStatusAsync)
    /// </summary>
    /// <returns>True if user is authenticated</returns>
    Task<bool> IsAuthenticatedAsync();

    /// <summary>
    /// Logs in a user with credentials
    /// </summary>
    /// <param name="email">User email</param>
    /// <param name="password">User password</param>
    /// <returns>True if login was successful</returns>
    Task<bool> LoginAsync(string email, string password);

    /// <summary>
    /// Registers a new user with credentials
    /// </summary>
    /// <param name="email">User email</param>
    /// <param name="password">User password</param>
    /// <returns>True if registration was successful</returns>
    Task<bool> RegisterAsync(string email, string password);

    /// <summary>
    /// Logs out the user and locks the vault
    /// </summary>
    Task LogoutAsync();

    /// <summary>
    /// Checks if this is the first time setup
    /// </summary>
    /// <returns>True if no user exists in the database</returns>
    Task<bool> IsFirstTimeSetupAsync();

    /// <summary>
    /// Gets the master password hint for the current user
    /// </summary>
    /// <returns>The password hint or empty string</returns>
    Task<string> GetMasterPasswordHintAsync();

    /// <summary>
    /// Changes the master password for the current user
    /// </summary>
    /// <param name="currentPassword">The current master password</param>
    /// <param name="newPassword">The new master password</param>
    /// <param name="newPasswordHint">Optional hint for the new master password</param>
    /// <returns>True if the password change was successful</returns>
    Task<bool> ChangeMasterPasswordAsync(string currentPassword, string newPassword, string newPasswordHint = "");

    /// <summary>
    /// Gets the current authenticated user's ID
    /// </summary>
    /// <returns>The user ID if authenticated, null otherwise</returns>
    Task<string?> GetCurrentUserIdAsync();

    /// <summary>
    /// Deletes the current user's account
    /// </summary>
    /// <param name="password">The user's master password for confirmation</param>
    /// <returns>Result with success status and error message if any</returns>
    Task<(bool Success, string? ErrorMessage)> DeleteAccountAsync(string password);

    /// <summary>
    /// Completes a QR-code sign-in on this (desktop/web) device using the session an approving mobile
    /// device created (the "desktop shows, phone scans, desktop signs in" flow). This works where vault
    /// data is served through the API session — the returned token acts as the bearer. Hosts that must
    /// derive the master key locally from the password (pure local/SQLite vault) can't unlock from a
    /// token alone and return false. Default implementation is a no-op so existing hosts are unaffected.
    /// </summary>
    /// <param name="authData">The session/user returned by the QR authenticate + status flow.</param>
    /// <returns>True if this device is now signed in.</returns>
    Task<bool> CompleteQrSignInAsync(AuthResponseDto authData) => Task.FromResult(false);
}
