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
public class QRLoginGenerateRequestDto
{
    public string? DeviceInfo { get; set; }
}

public class QRLoginGenerateResponseDto
{
    public string QRToken { get; set; } = string.Empty;
    public string QRCodeData { get; set; } = string.Empty;
    public string QRCodeImage { get; set; } = string.Empty; // Base64 encoded PNG
    public DateTime ExpiresAt { get; set; }
    public int ExpiresInSeconds { get; set; }
}

public class QRLoginValidateRequestDto
{
    [Required]
    public string QRToken { get; set; } = string.Empty;
}

public class QRLoginValidateResponseDto
{
    public bool IsValid { get; set; }
    public string Status { get; set; } = string.Empty; // "pending", "authenticated", "expired", "invalid"
    public AuthResponseDto? AuthResponse { get; set; }
}

public class QRLoginAuthenticateRequestDto
{
    [Required]
    public string QRToken { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Password { get; set; } = string.Empty;
    
    public string? DeviceInfo { get; set; }
}
