using PasswordManager.Models;

namespace PasswordManager.Services.Interfaces;

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
}
