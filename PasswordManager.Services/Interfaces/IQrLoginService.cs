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
    /// Generate a QR code for login
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="email">User email</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="userAgent">Client user agent</param>
    /// <returns>QR login initiate response with token and QR code image</returns>
    Task<QrLoginInitiateResponseDto> GenerateQrCodeAsync(string userId, string email, string? ipAddress = null, string? userAgent = null);

    /// <summary>
    /// Validate a QR token
    /// </summary>
    /// <param name="token">QR token</param>
    /// <returns>QR login validate response</returns>
    Task<QrLoginValidateResponseDto> ValidateQrTokenAsync(string token);

    /// <summary>
    /// Authenticate using a QR token
    /// </summary>
    /// <param name="token">QR token</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="userAgent">Client user agent</param>
    /// <returns>QR login authenticate response with session token</returns>
    Task<QrLoginAuthenticateResponseDto> AuthenticateWithQrTokenAsync(string token, string? ipAddress = null, string? userAgent = null);

    /// <summary>
    /// Get the status of a QR login token
    /// </summary>
    /// <param name="token">QR token</param>
    /// <returns>QR login status response</returns>
    Task<QrLoginStatusResponseDto> GetQrLoginStatusAsync(string token);

    /// <summary>
    /// Clean up expired QR tokens
    /// </summary>
    /// <returns>Number of tokens cleaned up</returns>
    Task<int> CleanupExpiredTokensAsync();
}