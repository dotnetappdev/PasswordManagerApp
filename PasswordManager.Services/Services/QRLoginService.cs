using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QRCoder;
using System.Security.Cryptography;
using System.Text;
using PasswordManager.DAL;
using PasswordManager.Models;
using PasswordManager.Models.DTOs.Auth;
using PasswordManager.Services.Interfaces;
using PasswordManager.Crypto.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace PasswordManager.Services.Services;

public class QRLoginService : IQRLoginService
{
    private readonly IDbContextFactory<PasswordManagerDbContextApp> _contextFactory;
    private readonly IPasswordCryptoService _passwordCryptoService;
    private readonly IVaultSessionService _vaultSessionService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<QRLoginService> _logger;
    
    private const int QR_TOKEN_EXPIRY_MINUTES = 1; // 60 seconds as specified

    public QRLoginService(
        IDbContextFactory<PasswordManagerDbContextApp> contextFactory,
        IPasswordCryptoService passwordCryptoService,
        IVaultSessionService vaultSessionService,
        UserManager<ApplicationUser> userManager,
        ILogger<QRLoginService> logger)
    {
        _contextFactory = contextFactory;
        _passwordCryptoService = passwordCryptoService;
        _vaultSessionService = vaultSessionService;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<QRLoginGenerateResponseDto> GenerateQRCodeAsync(string? deviceInfo = null)
    {
        try
        {
            // Generate unique token
            var token = GenerateSecureToken();
            var expiresAt = DateTime.UtcNow.AddMinutes(QR_TOKEN_EXPIRY_MINUTES);
            
            // Create QR login token entity
            var qrToken = new QRLoginToken
            {
                Token = token,
                Status = "pending",
                DeviceInfo = deviceInfo,
                ExpiresAt = expiresAt
            };

            // Save to database
            using var context = await _contextFactory.CreateDbContextAsync();
            context.QRLoginTokens.Add(qrToken);
            await context.SaveChangesAsync();

            // Generate QR code data (JSON format for mobile app)
            var qrData = System.Text.Json.JsonSerializer.Serialize(new
            {
                token = token,
                server = "PasswordManager",
                expires = expiresAt.ToString("O")
            });

            // Generate QR code image
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(20);
            var qrCodeBase64 = Convert.ToBase64String(qrCodeBytes);

            _logger.LogInformation("QR login token generated: {Token}", token);

            return new QRLoginGenerateResponseDto
            {
                QRToken = token,
                QRCodeData = qrData,
                QRCodeImage = qrCodeBase64,
                ExpiresAt = expiresAt,
                ExpiresInSeconds = (int)(expiresAt - DateTime.UtcNow).TotalSeconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QR code");
            throw;
        }
    }

    public async Task<QRLoginValidateResponseDto> ValidateQRTokenAsync(string qrToken)
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var token = await context.QRLoginTokens
                .FirstOrDefaultAsync(t => t.Token == qrToken);

            if (token == null)
            {
                return new QRLoginValidateResponseDto
                {
                    IsValid = false,
                    Status = "invalid"
                };
            }

            if (token.IsExpired)
            {
                token.Status = "expired";
                await context.SaveChangesAsync();
                
                return new QRLoginValidateResponseDto
                {
                    IsValid = false,
                    Status = "expired"
                };
            }

            if (token.Status == "authenticated" && !string.IsNullOrEmpty(token.AuthenticatedUserId))
            {
                // User has been authenticated, generate auth response
                var user = await _userManager.FindByIdAsync(token.AuthenticatedUserId);
                if (user != null)
                {
                    // Create vault session
                    var sessionId = Guid.NewGuid().ToString();
                    // Note: In a real implementation, we'd need the master key
                    // For now, we'll use a simplified approach
                    
                    var authResponse = new AuthResponseDto
                    {
                        Token = sessionId,
                        RefreshToken = "",
                        ExpiresAt = DateTime.UtcNow.AddHours(8),
                        User = new UserDto
                        {
                            Id = user.Id,
                            Email = user.Email,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            CreatedAt = user.CreatedAt,
                            LastLoginAt = user.LastLoginAt,
                            IsActive = user.IsActive
                        }
                    };

                    // Update last login time
                    user.LastLoginAt = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);

                    // Clean up the QR token
                    context.QRLoginTokens.Remove(token);
                    await context.SaveChangesAsync();

                    return new QRLoginValidateResponseDto
                    {
                        IsValid = true,
                        Status = "authenticated",
                        AuthResponse = authResponse
                    };
                }
            }

            return new QRLoginValidateResponseDto
            {
                IsValid = true,
                Status = token.Status
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating QR token: {Token}", qrToken);
            throw;
        }
    }

    public async Task<QRLoginValidateResponseDto> AuthenticateQRTokenAsync(QRLoginAuthenticateRequestDto request)
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var token = await context.QRLoginTokens
                .FirstOrDefaultAsync(t => t.Token == request.QRToken);

            if (token == null || token.IsExpired || token.Status != "pending")
            {
                return new QRLoginValidateResponseDto
                {
                    IsValid = false,
                    Status = "invalid"
                };
            }

            // Authenticate user
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return new QRLoginValidateResponseDto
                {
                    IsValid = false,
                    Status = "invalid"
                };
            }

            // Verify password
            var passwordValid = _passwordCryptoService.VerifyMasterPassword(
                request.Password, 
                user.MasterPasswordHash, 
                Convert.FromBase64String(user.UserSalt), 
                user.MasterPasswordIterations);

            if (!passwordValid)
            {
                return new QRLoginValidateResponseDto
                {
                    IsValid = false,
                    Status = "invalid"
                };
            }

            // Mark token as authenticated
            token.Status = "authenticated";
            token.AuthenticatedUserId = user.Id;
            await context.SaveChangesAsync();

            _logger.LogInformation("QR login authenticated for user: {Email}", request.Email);

            return new QRLoginValidateResponseDto
            {
                IsValid = true,
                Status = "authenticated"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating QR token: {Token}", request.QRToken);
            throw;
        }
    }

    public async Task CleanupExpiredTokensAsync()
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var expiredTokens = await context.QRLoginTokens
                .Where(t => t.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            if (expiredTokens.Any())
            {
                context.QRLoginTokens.RemoveRange(expiredTokens);
                await context.SaveChangesAsync();
                
                _logger.LogInformation("Cleaned up {Count} expired QR tokens", expiredTokens.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired QR tokens");
        }
    }

    private static string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }
}