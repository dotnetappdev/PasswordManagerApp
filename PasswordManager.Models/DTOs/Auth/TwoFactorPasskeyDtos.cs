using System.ComponentModel.DataAnnotations;

namespace PasswordManager.Models.DTOs.Auth;

// Two-Factor Authentication DTOs

public class TwoFactorSetupRequestDto
{
    [Required]
    public string MasterPassword { get; set; } = string.Empty;
}

public class TwoFactorSetupResponseDto
{
    public string SecretKey { get; set; } = string.Empty;
    public string QrCodeUri { get; set; } = string.Empty;
    public List<string> BackupCodes { get; set; } = new List<string>();
}

public class TwoFactorVerifySetupDto
{
    [Required]
    public string Code { get; set; } = string.Empty;
    
    [Required]
    public string SecretKey { get; set; } = string.Empty;
}

public class TwoFactorDisableDto
{
    [Required]
    public string MasterPassword { get; set; } = string.Empty;
    
    [Required]
    public string Code { get; set; } = string.Empty;
}

public class TwoFactorLoginDto
{
    [Required]
    public string Code { get; set; } = string.Empty;
    
    public bool IsBackupCode { get; set; } = false;
}

public class TwoFactorStatusDto
{
    public bool IsEnabled { get; set; }
    public DateTime? EnabledAt { get; set; }
    public int BackupCodesRemaining { get; set; }
    public string? RecoveryEmail { get; set; }
}

public class RegenerateBackupCodesDto
{
    [Required]
    public string MasterPassword { get; set; } = string.Empty;
    
    [Required]
    public string Code { get; set; } = string.Empty;
}

// Passkey DTOs

public class PasskeyRegistrationStartDto
{
    [Required]
    public string MasterPassword { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string PasskeyName { get; set; } = string.Empty;
    
    public bool StoreInVault { get; set; } = true;
}

public class PasskeyRegistrationStartResponseDto
{
    public string Challenge { get; set; } = string.Empty;
    public string CredentialCreationOptions { get; set; } = string.Empty;
}

public class PasskeyRegistrationCompleteDto
{
    [Required]
    public string Challenge { get; set; } = string.Empty;
    
    [Required]
    public string CredentialResponse { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string PasskeyName { get; set; } = string.Empty;
    
    public bool StoreInVault { get; set; } = true;
    
    [MaxLength(50)]
    public string? DeviceType { get; set; }
}

public class PasskeyAuthenticationStartDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class PasskeyAuthenticationStartResponseDto
{
    public string Challenge { get; set; } = string.Empty;
    public string CredentialRequestOptions { get; set; } = string.Empty;
}

public class PasskeyAuthenticationCompleteDto
{
    [Required]
    public string Challenge { get; set; } = string.Empty;
    
    [Required]
    public string CredentialResponse { get; set; } = string.Empty;
}

public class PasskeyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DeviceType { get; set; }
    public bool IsBackedUp { get; set; }
    public bool RequiresUserVerification { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool IsActive { get; set; }
    public bool StoreInVault { get; set; }
}

public class PasskeyListResponseDto
{
    public List<PasskeyDto> Passkeys { get; set; } = new List<PasskeyDto>();
    public bool PasskeysEnabled { get; set; }
    public DateTime? PasskeysEnabledAt { get; set; }
}

public class PasskeyDeleteDto
{
    [Required]
    public string MasterPassword { get; set; } = string.Empty;
}

public class PasskeyStatusDto
{
    public bool IsEnabled { get; set; }
    public DateTime? EnabledAt { get; set; }
    public int PasskeyCount { get; set; }
    public bool StoreInVault { get; set; }
}

// Enhanced Login DTOs with 2FA and Passkey support

public class EnhancedLoginRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
    
    public string? TwoFactorCode { get; set; }
    public bool IsTwoFactorBackupCode { get; set; } = false;
}

public class LoginResponseDto
{
    public bool RequiresTwoFactor { get; set; } = false;
    public bool SupportsPasskey { get; set; } = false;
    public AuthResponseDto? AuthResponse { get; set; }
    public string? TwoFactorToken { get; set; } // Temporary token for 2FA completion
}