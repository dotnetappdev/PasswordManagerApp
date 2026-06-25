using System.ComponentModel.DataAnnotations;
using PasswordManager.Models.DTOs.Auth;

namespace PasswordManager.Models;

public class QrLoginToken
{
    [Key]
    public string Token { get; set; } = string.Empty;
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    public DateTime ExpiresAt { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsUsed { get; set; } = false;
    
    public DateTime? UsedAt { get; set; }
    
    public string? UserAgent { get; set; }
    
    public string? IpAddress { get; set; }
    
    // Status for real-time updates
    public QrLoginStatus Status { get; set; } = QrLoginStatus.Pending;
}