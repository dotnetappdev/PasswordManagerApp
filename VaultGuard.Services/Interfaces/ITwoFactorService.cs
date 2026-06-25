using PasswordManager.Models;
using PasswordManager.Models.DTOs.Auth;

namespace PasswordManager.Services.Interfaces;

/// <summary>
/// Service for managing Two-Factor Authentication (2FA) operations
/// </summary>
public interface ITwoFactorService
{
    /// <summary>
    /// Starts the 2FA setup process by generating a new secret key and QR code
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="masterPassword">Master password for verification</param>
    /// <returns>Setup response with secret key, QR code, and backup codes</returns>
    Task<TwoFactorSetupResponseDto?> StartTwoFactorSetupAsync(string userId, string masterPassword);

    /// <summary>
    /// Verifies and completes the 2FA setup process
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="verifyDto">Verification data including code and secret key</param>
    /// <returns>True if setup was successful</returns>
    Task<bool> VerifyAndCompleteTwoFactorSetupAsync(string userId, TwoFactorVerifySetupDto verifyDto);

    /// <summary>
    /// Disables 2FA for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="disableDto">Disable request with master password and 2FA code</param>
    /// <returns>True if disable was successful</returns>
    Task<bool> DisableTwoFactorAsync(string userId, TwoFactorDisableDto disableDto);

    /// <summary>
    /// Verifies a 2FA code during login
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="code">TOTP code or backup code</param>
    /// <param name="isBackupCode">Whether the code is a backup code</param>
    /// <param name="clientIp">Client IP address for logging</param>
    /// <returns>True if verification was successful</returns>
    Task<bool> VerifyTwoFactorCodeAsync(string userId, string code, bool isBackupCode = false, string? clientIp = null);

    /// <summary>
    /// Generates new backup codes for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="regenerateDto">Request with master password and 2FA code</param>
    /// <returns>List of new backup codes</returns>
    Task<List<string>?> RegenerateBackupCodesAsync(string userId, RegenerateBackupCodesDto regenerateDto);

    /// <summary>
    /// Gets the 2FA status for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>2FA status information</returns>
    Task<TwoFactorStatusDto> GetTwoFactorStatusAsync(string userId);

    /// <summary>
    /// Validates a TOTP code against the user's secret key
    /// </summary>
    /// <param name="secretKey">Base32-encoded secret key</param>
    /// <param name="code">6-digit TOTP code</param>
    /// <param name="window">Time window tolerance (default 1 = ±30 seconds)</param>
    /// <returns>True if code is valid</returns>
    bool ValidateTotpCode(string secretKey, string code, int window = 1);

    /// <summary>
    /// Generates a new TOTP secret key
    /// </summary>
    /// <returns>Base32-encoded secret key</returns>
    string GenerateSecretKey();

    /// <summary>
    /// Generates a QR code URI for TOTP setup
    /// </summary>
    /// <param name="userEmail">User's email address</param>
    /// <param name="secretKey">Base32-encoded secret key</param>
    /// <param name="issuer">Application name/issuer</param>
    /// <returns>QR code URI</returns>
    string GenerateQrCodeUri(string userEmail, string secretKey, string issuer = "PasswordManager");

    /// <summary>
    /// Generates a set of backup codes
    /// </summary>
    /// <param name="count">Number of backup codes to generate</param>
    /// <returns>List of backup codes</returns>
    List<string> GenerateBackupCodes(int count = 10);
}