namespace PasswordManager.Models.DTOs;

public class LoginItemDto
{
    public int Id { get; set; }
    public int PasswordItemId { get; set; }
    public string? Website { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? TotpSecret { get; set; }
    public string? TwoFactorType { get; set; }
    public string? SecurityQuestion1 { get; set; }
    public string? SecurityAnswer1 { get; set; }
    public string? SecurityQuestion2 { get; set; }
    public string? SecurityAnswer2 { get; set; }
    public string? SecurityQuestion3 { get; set; }
    public string? SecurityAnswer3 { get; set; }
    public string? RecoveryEmail { get; set; }
    public string? RecoveryPhone { get; set; }
    public string? LoginUrl { get; set; }
    public string? SupportUrl { get; set; }
    public string? AdminConsoleUrl { get; set; }
    public DateTime? PasswordLastChanged { get; set; }
    public bool RequiresPasswordChange { get; set; }
    public DateTime? LastUsed { get; set; }
    public int UsageCount { get; set; }
    public string? CompanyName { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public string? Notes { get; set; }
}

public class CreateLoginItemDto
{
    public string? Website { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? TotpSecret { get; set; }
    public string? TwoFactorType { get; set; }
    public string? SecurityQuestion1 { get; set; }
    public string? SecurityAnswer1 { get; set; }
    public string? SecurityQuestion2 { get; set; }
    public string? SecurityAnswer2 { get; set; }
    public string? SecurityQuestion3 { get; set; }
    public string? SecurityAnswer3 { get; set; }
    public string? RecoveryEmail { get; set; }
    public string? RecoveryPhone { get; set; }
    public string? LoginUrl { get; set; }
    public string? SupportUrl { get; set; }
    public string? AdminConsoleUrl { get; set; }
    public DateTime? PasswordLastChanged { get; set; }
    public bool RequiresPasswordChange { get; set; }
    public string? CompanyName { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public string? Notes { get; set; }
}

public class UpdateLoginItemDto
{
    public string? Website { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? TotpSecret { get; set; }
    public string? TwoFactorType { get; set; }
    public string? SecurityQuestion1 { get; set; }
    public string? SecurityAnswer1 { get; set; }
    public string? SecurityQuestion2 { get; set; }
    public string? SecurityAnswer2 { get; set; }
    public string? SecurityQuestion3 { get; set; }
    public string? SecurityAnswer3 { get; set; }
    public string? RecoveryEmail { get; set; }
    public string? RecoveryPhone { get; set; }
    public string? LoginUrl { get; set; }
    public string? SupportUrl { get; set; }
    public string? AdminConsoleUrl { get; set; }
    public DateTime? PasswordLastChanged { get; set; }
    public bool RequiresPasswordChange { get; set; }
    public string? CompanyName { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public string? Notes { get; set; }
}
