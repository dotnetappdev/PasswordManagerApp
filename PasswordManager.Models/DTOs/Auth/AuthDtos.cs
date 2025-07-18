using System.ComponentModel.DataAnnotations;

namespace PasswordManager.Models.DTOs.Auth;

public class LoginRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = null!;
}

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; }
}

public class RefreshTokenRequestDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class ForgotPasswordRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}

public class ChangePasswordRequestDto
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}

// QR Login DTOs
public class QrLoginInitiateRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class QrLoginInitiateResponseDto
{
    public string QrToken { get; set; } = string.Empty;
    public string QrCodeImage { get; set; } = string.Empty; // Base64 encoded QR code image
    public DateTime ExpiresAt { get; set; }
    public int ExpiresInSeconds { get; set; }
}

public class QrLoginValidateRequestDto
{
    [Required]
    public string QrToken { get; set; } = string.Empty;
}

public class QrLoginValidateResponseDto
{
    public bool IsValid { get; set; }
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
}

public class QrLoginAuthenticateRequestDto
{
    [Required]
    public string QrToken { get; set; } = string.Empty;
}

public class QrLoginAuthenticateResponseDto
{
    public bool Success { get; set; }
    public string? SessionToken { get; set; }
    public UserDto? User { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Message { get; set; }
}

public class QrLoginStatusRequestDto
{
    [Required]
    public string QrToken { get; set; } = string.Empty;
}

public class QrLoginStatusResponseDto
{
    public bool IsAuthenticated { get; set; }
    public bool IsExpired { get; set; }
    public string? SessionToken { get; set; }
    public UserDto? User { get; set; }
    public string? Message { get; set; }
}

// Frontend QR Login Response Models (for UI)
public class QrLoginResponse
{
    public string QrToken { get; set; } = string.Empty;
    public string QrCodeImage { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int ExpiresInSeconds { get; set; }
}

public class QrLoginStatusResponse
{
    public bool IsAuthenticated { get; set; }
    public bool IsExpired { get; set; }
    public string? SessionToken { get; set; }
    public string? Message { get; set; }
}
