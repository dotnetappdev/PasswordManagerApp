using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PasswordManager.Crypto.Interfaces;
using PasswordManager.DAL;
using PasswordManager.Models;
using PasswordManager.Models.DTOs.Auth;
using PasswordManager.Services.Interfaces;
using QRCoder;
using System.Text;
using System.Text.Json;

namespace PasswordManager.Services.Services;

/// <summary>
/// Service for QR code login functionality
/// </summary>
public class QrLoginService : IQrLoginService
{
    private readonly IDatabaseContextFactory _contextFactory;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPasswordCryptoService _passwordCryptoService;
    private readonly IVaultSessionService _vaultSessionService;
    private readonly ILogger<QrLoginService> _logger;

    // QR token expiration time (60 seconds)
    private static readonly TimeSpan TokenExpiration = TimeSpan.FromSeconds(60);

    public QrLoginService(
        IDatabaseContextFactory contextFactory,
        UserManager<ApplicationUser> userManager,
        IPasswordCryptoService passwordCryptoService,
        IVaultSessionService vaultSessionService,
        ILogger<QrLoginService> logger)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _passwordCryptoService = passwordCryptoService ?? throw new ArgumentNullException(nameof(passwordCryptoService));
        _vaultSessionService = vaultSessionService ?? throw new ArgumentNullException(nameof(vaultSessionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<QrLoginGenerateResponseDto> GenerateQrLoginTokenAsync(string userId, string baseUrl)
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        var context = await _contextFactory.CreateSqliteContextAsync();
        
        try
        {
            // Clean up any existing tokens for this user
            var existingTokens = context.QrLoginTokens
                .Where(t => t.UserId == userId && !t.IsUsed);
            
            foreach (var existingToken in existingTokens)
            {
                context.QrLoginTokens.Remove(existingToken);
            }

            // Generate new token
            var newToken = Guid.NewGuid().ToString("N"); // 32 character string without hyphens
            var expiresAt = DateTime.UtcNow.Add(TokenExpiration);

            var qrToken = new QrLoginToken
            {
                Token = newToken,
                UserId = userId,
                ExpiresAt = expiresAt,
                Status = Models.DTOs.Auth.QrLoginStatus.Pending
            };

            context.QrLoginTokens.Add(qrToken);
            await context.SaveChangesAsync();

            // Generate QR code data (JSON with token and authentication endpoint)
            var qrData = new
            {
                token = newToken,
                endpoint = $"{baseUrl.TrimEnd('/')}/api/auth/qr/authenticate",
                expires = expiresAt.ToString("O") // ISO 8601 format
            };

            var qrCodeData = JsonSerializer.Serialize(qrData);
            
            _logger.LogInformation("Generated QR login token for user {UserId}, expires at {ExpiresAt}", 
                userId, expiresAt);

            return new QrLoginGenerateResponseDto
            {
                Token = newToken,
                QrCodeData = qrCodeData,
                ExpiresAt = expiresAt,
                ExpiresInSeconds = (int)TokenExpiration.TotalSeconds
            };
        }
        finally
        {
            context.Dispose();
        }
    }

    public async Task<QrLoginAuthenticateResponseDto> AuthenticateQrTokenAsync(
        QrLoginAuthenticateRequestDto request, 
        string? userAgent, 
        string? ipAddress)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var context = await _contextFactory.CreateSqliteContextAsync();

        try
        {
            // Find the QR token
            var qrToken = await ((DbContext)context).Set<QrLoginToken>()
                .FirstOrDefaultAsync(t => t.Token == request.Token);

            if (qrToken == null)
            {
                return new QrLoginAuthenticateResponseDto
                {
                    Success = false,
                    Message = "Invalid or expired QR token"
                };
            }

            // Check if token is expired
            if (qrToken.ExpiresAt <= DateTime.UtcNow)
            {
                qrToken.Status = Models.DTOs.Auth.QrLoginStatus.Expired;
                await context.SaveChangesAsync();
                
                return new QrLoginAuthenticateResponseDto
                {
                    Success = false,
                    Message = "QR token has expired"
                };
            }

            // Check if token is already used
            if (qrToken.IsUsed)
            {
                return new QrLoginAuthenticateResponseDto
                {
                    Success = false,
                    Message = "QR token has already been used"
                };
            }

            // Authenticate the user with email/password
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return new QrLoginAuthenticateResponseDto
                {
                    Success = false,
                    Message = "Invalid email or password"
                };
            }

            // Verify password
            var masterKey = _passwordCryptoService.DeriveMasterKey(
                request.Password, 
                Convert.FromBase64String(user.UserSalt ?? ""));

            if (!_passwordCryptoService.VerifyMasterPassword(
                request.Password, 
                user.MasterPasswordHash ?? "", 
                Convert.FromBase64String(user.UserSalt ?? ""), 
                user.MasterPasswordIterations))
            {
                return new QrLoginAuthenticateResponseDto
                {
                    Success = false,
                    Message = "Invalid email or password"
                };
            }

            // Initialize vault session with the derived master key
            var sessionId = _vaultSessionService.InitializeSession(user.Id, masterKey);

            // Mark token as used and update status
            qrToken.IsUsed = true;
            qrToken.UsedAt = DateTime.UtcNow;
            qrToken.UserAgent = userAgent;
            qrToken.IpAddress = ipAddress;
            qrToken.Status = Models.DTOs.Auth.QrLoginStatus.Authenticated;

            // Update user's last login time
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            await context.SaveChangesAsync();

            var authResponse = new AuthResponseDto
            {
                Token = sessionId,
                RefreshToken = "",
                ExpiresAt = DateTime.UtcNow.AddHours(8), // Session expires in 8 hours
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email ?? "",
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    IsActive = user.IsActive
                }
            };

            _logger.LogInformation("QR authentication successful for user {UserId} with token {Token}", 
                user.Id, request.Token);

            return new QrLoginAuthenticateResponseDto
            {
                Success = true,
                Message = "Authentication successful",
                AuthData = authResponse
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during QR authentication for token {Token}", request.Token);
            return new QrLoginAuthenticateResponseDto
            {
                Success = false,
                Message = "An error occurred during authentication"
            };
        }
        finally
        {
            context.Dispose();
        }
    }

    public async Task<QrLoginStatusResponseDto> GetQrLoginStatusAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
            throw new ArgumentException("Token cannot be null or empty", nameof(token));

        var context = await _contextFactory.CreateSqliteContextAsync();

        try
        {
            var qrToken = await ((DbContext)context).Set<QrLoginToken>()
                .FirstOrDefaultAsync(t => t.Token == token);

            if (qrToken == null)
            {
                return new QrLoginStatusResponseDto
                {
                    Token = token,
                    Status = Models.DTOs.Auth.QrLoginStatus.Expired,
                    IsExpired = true,
                    Message = "Token not found"
                };
            }

            var isExpired = qrToken.ExpiresAt <= DateTime.UtcNow;
            
            if (isExpired && qrToken.Status == Models.DTOs.Auth.QrLoginStatus.Pending)
            {
                qrToken.Status = Models.DTOs.Auth.QrLoginStatus.Expired;
                await context.SaveChangesAsync();
            }

            return new QrLoginStatusResponseDto
            {
                Token = token,
                Status = qrToken.Status,
                IsExpired = isExpired,
                Message = GetStatusMessage(qrToken.Status, isExpired)
            };
        }
        finally
        {
            context.Dispose();
        }
    }

    public async Task<int> CleanupExpiredTokensAsync()
    {
        var context = await _contextFactory.CreateSqliteContextAsync();

        try
        {
            var expiredTokens = ((DbContext)context).Set<QrLoginToken>()
                .Where(t => t.ExpiresAt <= DateTime.UtcNow || t.IsUsed);

            var count = await expiredTokens.CountAsync();
            
            foreach (var expiredToken in expiredTokens)
            {
                ((DbContext)context).Set<QrLoginToken>().Remove(expiredToken);
            }
            
            await context.SaveChangesAsync();

            if (count > 0)
            {
                _logger.LogInformation("Cleaned up {Count} expired QR login tokens", count);
            }

            return count;
        }
        finally
        {
            context.Dispose();
        }
    }

    public async Task<int> InvalidateUserTokensAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        var context = await _contextFactory.CreateSqliteContextAsync();

        try
        {
            var userTokens = ((DbContext)context).Set<QrLoginToken>()
                .Where(t => t.UserId == userId && !t.IsUsed);

            var count = await userTokens.CountAsync();
            
            foreach (var userToken in userTokens)
            {
                ((DbContext)context).Set<QrLoginToken>().Remove(userToken);
            }
            
            await context.SaveChangesAsync();

            if (count > 0)
            {
                _logger.LogInformation("Invalidated {Count} QR login tokens for user {UserId}", count, userId);
            }

            return count;
        }
        finally
        {
            context.Dispose();
        }
    }

    private static string GetStatusMessage(Models.DTOs.Auth.QrLoginStatus status, bool isExpired)
    {
        return status switch
        {
            Models.DTOs.Auth.QrLoginStatus.Pending => isExpired ? "Token has expired" : "Waiting for authentication",
            Models.DTOs.Auth.QrLoginStatus.Authenticated => "Authentication successful",
            Models.DTOs.Auth.QrLoginStatus.Expired => "Token has expired",
            Models.DTOs.Auth.QrLoginStatus.Used => "Token has been used",
            _ => "Unknown status"
        };
    }
}