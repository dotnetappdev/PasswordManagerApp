namespace PasswordManager.Models.DTOs;

public class LoginItemDto
{
    public int Id { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? Website { get; set; }
    public string? TotpSecret { get; set; }
    public string? Notes { get; set; }
    public int PasswordItemId { get; set; }
}

public class CreateLoginItemDto
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? Website { get; set; }
    public string? TotpSecret { get; set; }
    public string? Notes { get; set; }
}

public class UpdateLoginItemDto
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? Website { get; set; }
    public string? TotpSecret { get; set; }
    public string? Notes { get; set; }
}
