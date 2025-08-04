using System.ComponentModel.DataAnnotations;

namespace PasswordManager.Models.DTOs.Auth;

public class CreateUserProfileDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
    
    [Required]
    [Compare("Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
    
    public string? FirstName { get; set; }
    
    public string? LastName { get; set; }
    
    public string? MasterPasswordHint { get; set; }
}

public class UpdateUserProfileDto
{
    [Required]
    public string Id { get; set; } = string.Empty;
    
    [EmailAddress]
    public string? Email { get; set; }
    
    public string? FirstName { get; set; }
    
    public string? LastName { get; set; }
    
    public string? MasterPasswordHint { get; set; }
}

public class ChangePasswordDto
{
    [Required]
    public string Id { get; set; } = string.Empty;
    
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;
    
    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
    
    [Required]
    [Compare("NewPassword")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
    
    public string? NewMasterPasswordHint { get; set; }
}

public class UserProfileDetailsDto : UserDto
{
    public string? MasterPasswordHint { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    public new bool IsActive { get; set; }
    
    public bool TwoFactorEnabled { get; set; }
    
    public string? PhoneNumber { get; set; }
}
