using PasswordManager.Models;
using PasswordManager.Models.DTOs.Auth;

namespace PasswordManager.Services.Interfaces;

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