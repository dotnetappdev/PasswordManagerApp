using PasswordManager.Models.DTOs.Auth;

namespace PasswordManager.Services.Interfaces;

public interface IQRLoginService
{
    /// <summary>
    /// Generate a new QR code for login
    /// </summary>
    Task<QRLoginGenerateResponseDto> GenerateQRCodeAsync(string? deviceInfo = null);
    
    /// <summary>
    /// Validate QR token status (for polling)
    /// </summary>
    Task<QRLoginValidateResponseDto> ValidateQRTokenAsync(string qrToken);
    
    /// <summary>
    /// Authenticate user via QR token (mobile app endpoint)
    /// </summary>
    Task<QRLoginValidateResponseDto> AuthenticateQRTokenAsync(QRLoginAuthenticateRequestDto request);
    
    /// <summary>
    /// Cleanup expired tokens
    /// </summary>
    Task CleanupExpiredTokensAsync();
}