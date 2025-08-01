using System.ComponentModel.DataAnnotations;

namespace PasswordManager.Models.DTOs.Auth;

public class QrLoginGenerateRequestDto
{
    // No required fields - uses current authenticated user session
}

public class QrLoginGenerateResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string QrCodeData { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int ExpiresInSeconds { get; set; }
}

public class QrLoginAuthenticateRequestDto
{
    [Required]
    public string Token { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Password { get; set; } = string.Empty;
}

public class QrLoginAuthenticateResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public AuthResponseDto? AuthData { get; set; }
}

public class QrLoginStatusResponseDto
{
    public string Token { get; set; } = string.Empty;
    public QrLoginStatus Status { get; set; }
    public bool IsExpired { get; set; }
    public AuthResponseDto? AuthData { get; set; }
    public string Message { get; set; } = string.Empty;
}

public enum QrLoginStatus
{
    Pending,
    Authenticated,
    Expired,
    Used
}