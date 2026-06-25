using PasswordManager.Models;
using PasswordManager.Models.DTOs.Auth;
using System.Security.Claims;

namespace PasswordManager.API.Interfaces;

public interface IJwtService
{
    string GenerateToken(ApplicationUser user);
    string GenerateRefreshToken();
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    Task<AuthResponseDto> CreateAuthResponseAsync(ApplicationUser user);
    Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken);
    Task SaveRefreshTokenAsync(string userId, string refreshToken);
    Task RevokeRefreshTokenAsync(string userId, string refreshToken);
}
