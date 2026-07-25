using System.ComponentModel.DataAnnotations;

namespace VaultGuard.Models.DTOs.Auth;

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

// OTP (One-Time Passcode) DTOs
public class SetupOtpRequestDto
{
    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;
}

public class VerifyOtpSetupRequestDto
{
    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;
    
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;
}

public class OtpLoginRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
    
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string OtpCode { get; set; } = string.Empty;
}

public class SendOtpRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class OtpSetupResponseDto
{
    public string PhoneNumber { get; set; } = string.Empty;
    public List<string> BackupCodes { get; set; } = new();
    public bool IsSetupComplete { get; set; }
}

public class DisableOtpRequestDto
{
    [Required]
    public string Password { get; set; } = string.Empty;
    
    public string? OtpCode { get; set; }
    public string? BackupCode { get; set; }
}

public class DeleteAccountRequestDto
{
    [Required]
    public string Password { get; set; } = string.Empty;
}

// SSO / external login DTOs - see VaultGuard.Models.Configuration.SsoConfiguration's doc comment.
// These describe identity-provider links (AspNetUserLogins), never credentials or tokens.

/// <summary>A configured SSO provider as surfaced to a client - just enough to render a "Continue with X" button.</summary>
public class SsoProviderSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>One external identity linked to a local account (a row of AspNetUserLogins).</summary>
public class ExternalLoginDto
{
    public string LoginProvider { get; set; } = string.Empty;
    public string ProviderDisplayName { get; set; } = string.Empty;
}

/// <summary>
/// Request to persist that the current (already master-password-authenticated) user is the same
/// person as the given external identity. Never accepted before the caller has proven they hold
/// the master password - see IUserProfileService.LinkExternalLoginAsync's doc comment for why.
/// </summary>
public class LinkExternalLoginRequestDto
{
    [Required]
    public string LoginProvider { get; set; } = string.Empty;

    [Required]
    public string ProviderKey { get; set; } = string.Empty;

    public string? ProviderDisplayName { get; set; }
}
