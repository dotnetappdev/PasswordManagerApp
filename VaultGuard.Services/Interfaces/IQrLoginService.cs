using VaultGuard.Models;
using VaultGuard.Models.DTOs.Auth;

namespace VaultGuard.Services.Interfaces;

/// <summary>
/// Interface for QR code login functionality
/// Handles QR token generation, validation, and authentication
/// </summary>
public interface IQrLoginService
{
    /// <summary>
    /// Generate a QR login token for the current user
    /// </summary>
    /// <param name="userId">The user ID to generate the token for</param>
    /// <param name="baseUrl">Base URL for the QR code data</param>
    /// <returns>QR login generation response with token and QR code data</returns>
    Task<QrLoginGenerateResponseDto> GenerateQrLoginTokenAsync(string userId, string baseUrl);

    /// <summary>
    /// Generate a QR login token that is not yet tied to a user. A desktop/web client shows this on its
    /// sign-in screen; the user (who is not logged in there yet) is resolved when a mobile device scans
    /// the code and approves with its own credentials. Enables the "desktop shows, phone scans, desktop
    /// signs in" flow without the desktop needing an existing session.
    /// </summary>
    /// <param name="baseUrl">Base URL embedded in the QR so the scanning device knows where to post.</param>
    Task<QrLoginGenerateResponseDto> GenerateAnonymousQrLoginTokenAsync(string baseUrl);

    /// <summary>
    /// Authenticate a QR login token
    /// </summary>
    /// <param name="request">QR authentication request</param>
    /// <param name="userAgent">User agent from the request</param>
    /// <param name="ipAddress">IP address from the request</param>
    /// <returns>QR authentication response</returns>
    Task<QrLoginAuthenticateResponseDto> AuthenticateQrTokenAsync(QrLoginAuthenticateRequestDto request, string? userAgent, string? ipAddress);

    /// <summary>
    /// Get the status of a QR login token
    /// </summary>
    /// <param name="token">The QR token to check</param>
    /// <returns>QR login status response</returns>
    Task<QrLoginStatusResponseDto> GetQrLoginStatusAsync(string token);

    /// <summary>
    /// Relay an end-to-end encrypted master-key hand-off from a scanning device. The server stores the
    /// ciphertext against the token (it cannot decrypt it) and marks the token authenticated so the
    /// displaying device, which is polling, can pick it up and unlock its own local vault.
    /// </summary>
    Task<QrLoginAuthenticateResponseDto> SubmitHandoffAsync(QrHandoffSubmitRequestDto request);

    /// <summary>
    /// Polled by the device that displayed the QR. Returns the encrypted hand-off blob once a phone has
    /// submitted it, so that device can decrypt it locally and unlock.
    /// </summary>
    Task<QrHandoffStatusResponseDto> GetHandoffStatusAsync(string token);

    /// <summary>
    /// Clean up expired QR tokens
    /// </summary>
    /// <returns>Number of tokens cleaned up</returns>
    Task<int> CleanupExpiredTokensAsync();

    /// <summary>
    /// Invalidate all QR tokens for a user
    /// </summary>
    /// <param name="userId">User ID to invalidate tokens for</param>
    /// <returns>Number of tokens invalidated</returns>
    Task<int> InvalidateUserTokensAsync(string userId);
}