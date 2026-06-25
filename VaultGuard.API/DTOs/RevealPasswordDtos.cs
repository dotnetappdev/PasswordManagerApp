using System.ComponentModel.DataAnnotations;

namespace PasswordManager.API.DTOs;

/// <summary>
/// Request DTO for revealing a password
/// </summary>
public class RevealPasswordRequestDto
{
    /// <summary>
    /// The user's master password for authentication
    /// </summary>
    [Required]
    public string MasterPassword { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for revealing a password
/// </summary>
public class RevealPasswordResponseDto
{
    /// <summary>
    /// The decrypted password
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the password item
    /// </summary>
    public int ItemId { get; set; }

    /// <summary>
    /// When the password was revealed
    /// </summary>
    public DateTime RevealedAt { get; set; }
}
