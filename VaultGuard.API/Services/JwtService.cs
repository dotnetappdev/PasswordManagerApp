using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PasswordManager.API.Interfaces;
using PasswordManager.Models;
using PasswordManager.Models.DTOs.Auth;

namespace PasswordManager.API.Services;

public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<JwtService> _logger;
    private readonly IConfiguration _configuration;

    // In-memory storage for refresh tokens (in production, use database or Redis)
    private static readonly Dictionary<string, List<string>> _refreshTokens = new();

    public JwtService(
        IOptions<JwtSettings> jwtSettings,
        ILogger<JwtService> logger,
        IConfiguration configuration)
    {
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
        _configuration = configuration;
    }

    public string GenerateToken(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key)),
            ValidateLifetime = false // We're validating an expired token
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token");
        }

        return principal;
    }

    public async Task<AuthResponseDto> CreateAuthResponseAsync(ApplicationUser user)
    {
        var token = GenerateToken(user);
        var refreshToken = GenerateRefreshToken();

        await SaveRefreshTokenAsync(user.Id, refreshToken);

        return new AuthResponseDto
        {
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            }
        };
    }

    public async Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken)
    {
        await Task.CompletedTask;
        
        if (!_refreshTokens.ContainsKey(userId))
            return false;

        return _refreshTokens[userId].Contains(refreshToken);
    }

    public async Task SaveRefreshTokenAsync(string userId, string refreshToken)
    {
        await Task.CompletedTask;
        
        if (!_refreshTokens.ContainsKey(userId))
            _refreshTokens[userId] = new List<string>();

        _refreshTokens[userId].Add(refreshToken);
        
        // Keep only the last 5 refresh tokens per user
        if (_refreshTokens[userId].Count > 5)
        {
            _refreshTokens[userId].RemoveAt(0);
        }

        _logger.LogDebug("Saved refresh token for user {UserId}", userId);
    }

    public async Task RevokeRefreshTokenAsync(string userId, string refreshToken)
    {
        await Task.CompletedTask;
        
        if (_refreshTokens.ContainsKey(userId))
        {
            _refreshTokens[userId].Remove(refreshToken);
            
            if (_refreshTokens[userId].Count == 0)
            {
                _refreshTokens.Remove(userId);
            }
        }

        _logger.LogDebug("Revoked refresh token for user {UserId}", userId);
    }

    ClaimsPrincipal IJwtService.GetPrincipalFromExpiredToken(string token)
    {
        throw new NotImplementedException();
    }
}
