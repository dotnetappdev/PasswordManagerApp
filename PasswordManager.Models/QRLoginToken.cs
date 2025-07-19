using System.ComponentModel.DataAnnotations;

namespace PasswordManager.Models;

public class QRLoginToken
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string Token { get; set; } = string.Empty;
    
    public string Status { get; set; } = "pending"; // pending, authenticated, expired
    
    public string? AuthenticatedUserId { get; set; }
    
    public string? DeviceInfo { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime ExpiresAt { get; set; }
    
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    
    public bool IsValid => !IsExpired && Status != "expired";
}