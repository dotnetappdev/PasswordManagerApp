using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PasswordManager.DAL;
using PasswordManager.Models;
using PasswordManager.Models.DTOs.Auth;
using PasswordManager.Services.Interfaces;
using QRCoder;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace PasswordManager.Services.Services;

/// <summary>
/// Service for QR code login functionality
/// </summary>
public class QrLoginService : IQrLoginService
{
    private readonly PasswordManagerDbContext _context;
    private readonly IVaultSessionService _vaultSessionService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<QrLoginService> _logger;
    
    // In-memory storage for active QR sessions (for polling)
    private readonly ConcurrentDictionary<string, (string userId, string sessionToken, DateTime authenticatedAt)> _authenticatedTokens = new();
    
    private const int QR_TOKEN_EXPIRY_SECONDS = 60; // 60 seconds like WhatsApp
    private const int QR_CODE_SIZE = 300;

    public QrLoginService(
        PasswordManagerDbContext context,
        IVaultSessionService vaultSessionService,
        IConfiguration configuration,
        ILogger<QrLoginService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _vaultSessionService = vaultSessionService ?? throw new ArgumentNullException(nameof(vaultSessionService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<QrLoginInitiateResponseDto> GenerateQrCodeAsync(string userId, string email, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
            // Generate secure token
            var token = GenerateSecureToken();
            var expiresAt = DateTime.UtcNow.AddSeconds(QR_TOKEN_EXPIRY_SECONDS);

            // Save token to database
            var qrToken = new QrLoginToken
            {
                Token = token,
                UserId = userId,
                Email = email,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            _context.QrLoginTokens.Add(qrToken);
            await _context.SaveChangesAsync();

            // Generate QR code
            var qrCodeUrl = GenerateQrCodeUrl(token);
            var qrCodeImage = GenerateQrCodeImage(qrCodeUrl);

            _logger.LogInformation("QR code generated for user {UserId} with token {Token}", userId, token);

            return new QrLoginInitiateResponseDto
            {
                QrToken = token,
                QrCodeImage = qrCodeImage,
                ExpiresAt = expiresAt,
                ExpiresInSeconds = QR_TOKEN_EXPIRY_SECONDS
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QR code for user {UserId}", userId);
            throw;
        }
    }

    public async Task<QrLoginValidateResponseDto> ValidateQrTokenAsync(string token)
    {
        try
        {
            var qrToken = await _context.QrLoginTokens
                .FirstOrDefaultAsync(t => t.Token == token);

            if (qrToken == null)
            {
                return new QrLoginValidateResponseDto { IsValid = false };
            }

            var isValid = !qrToken.IsUsed && qrToken.ExpiresAt > DateTime.UtcNow;

            return new QrLoginValidateResponseDto
            {
                IsValid = isValid,
                UserId = isValid ? qrToken.UserId : null,
                Email = isValid ? qrToken.Email : null,
                ExpiresAt = qrToken.ExpiresAt,
                IsUsed = qrToken.IsUsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating QR token {Token}", token);
            return new QrLoginValidateResponseDto { IsValid = false };
        }
    }

    public async Task<QrLoginAuthenticateResponseDto> AuthenticateWithQrTokenAsync(string token, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
            var qrToken = await _context.QrLoginTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == token);

            if (qrToken == null)
            {
                return new QrLoginAuthenticateResponseDto 
                { 
                    Success = false, 
                    Message = "Invalid QR token" 
                };
            }

            if (qrToken.IsUsed)
            {
                return new QrLoginAuthenticateResponseDto 
                { 
                    Success = false, 
                    Message = "QR token has already been used" 
                };
            }

            if (qrToken.ExpiresAt <= DateTime.UtcNow)
            {
                return new QrLoginAuthenticateResponseDto 
                { 
                    Success = false, 
                    Message = "QR token has expired" 
                };
            }

            // Mark token as used
            qrToken.IsUsed = true;
            qrToken.UsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Create session for the user - we need the master key, but we don't have it in this flow
            // For QR login, we'll create a limited session that still requires password input for vault access
            // This is similar to how WhatsApp works - you're logged in but need to unlock with your master password
            var sessionToken = GenerateSecureToken();
            
            // Store the authentication in memory for the web app to poll
            _authenticatedTokens[token] = (qrToken.UserId, sessionToken, DateTime.UtcNow);

            var user = qrToken.User;
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("QR authentication successful for user {UserId} with token {Token}", qrToken.UserId, token);

            return new QrLoginAuthenticateResponseDto
            {
                Success = true,
                SessionToken = sessionToken,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email ?? "",
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    IsActive = user.IsActive
                },
                ExpiresAt = DateTime.UtcNow.AddHours(8), // 8 hour session
                Message = "Authentication successful"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating with QR token {Token}", token);
            return new QrLoginAuthenticateResponseDto 
            { 
                Success = false, 
                Message = "An error occurred during authentication" 
            };
        }
    }

    public async Task<QrLoginStatusResponseDto> GetQrLoginStatusAsync(string token)
    {
        try
        {
            // First check if token has been authenticated (in memory)
            if (_authenticatedTokens.TryGetValue(token, out var authInfo))
            {
                // Get user info
                var user = await _context.Users.FindAsync(authInfo.userId);
                
                return new QrLoginStatusResponseDto
                {
                    IsAuthenticated = true,
                    IsExpired = false,
                    SessionToken = authInfo.sessionToken,
                    User = user != null ? new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email ?? "",
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        CreatedAt = user.CreatedAt,
                        LastLoginAt = user.LastLoginAt,
                        IsActive = user.IsActive
                    } : null,
                    Message = "Authenticated successfully via QR code"
                };
            }

            // Check database token status
            var qrToken = await _context.QrLoginTokens
                .FirstOrDefaultAsync(t => t.Token == token);

            if (qrToken == null)
            {
                return new QrLoginStatusResponseDto
                {
                    IsAuthenticated = false,
                    IsExpired = true,
                    Message = "QR token not found"
                };
            }

            var isExpired = qrToken.ExpiresAt <= DateTime.UtcNow;

            return new QrLoginStatusResponseDto
            {
                IsAuthenticated = false,
                IsExpired = isExpired,
                Message = isExpired ? "QR token has expired" : "Waiting for mobile authentication"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking QR login status for token {Token}", token);
            return new QrLoginStatusResponseDto
            {
                IsAuthenticated = false,
                IsExpired = true,
                Message = "An error occurred while checking status"
            };
        }
    }

    public async Task<int> CleanupExpiredTokensAsync()
    {
        try
        {
            var expiredTokens = await _context.QrLoginTokens
                .Where(t => t.ExpiresAt <= DateTime.UtcNow)
                .ToListAsync();

            if (expiredTokens.Any())
            {
                _context.QrLoginTokens.RemoveRange(expiredTokens);
                await _context.SaveChangesAsync();

                // Clean up in-memory authentication cache
                var expiredTokenStrings = expiredTokens.Select(t => t.Token).ToList();
                foreach (var tokenString in expiredTokenStrings)
                {
                    _authenticatedTokens.TryRemove(tokenString, out _);
                }

                _logger.LogInformation("Cleaned up {Count} expired QR tokens", expiredTokens.Count);
            }

            return expiredTokens.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired QR tokens");
            return 0;
        }
    }

    private string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "")[..32];
    }

    private string GenerateQrCodeUrl(string token)
    {
        var baseUrl = _configuration["QrLogin:BaseUrl"] ?? "https://localhost:7001";
        return $"{baseUrl}/api/qrlogin/authenticate?token={token}";
    }

    private string GenerateQrCodeImage(string url)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrCodeData);
        
        var qrCodeBytes = qrCode.GetGraphic(QR_CODE_SIZE / 100); // Size in pixels per module
        return Convert.ToBase64String(qrCodeBytes);
    }
}