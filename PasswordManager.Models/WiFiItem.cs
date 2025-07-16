using System.ComponentModel.DataAnnotations;

namespace PasswordManager.Models;

public class WiFiItem
{
    public int Id { get; set; }
    public int PasswordItemId { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    // User relationship
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    
    // Basic WiFi Information
    [MaxLength(100)]
    public string? NetworkName { get; set; } // SSID
    
    [MaxLength(500)]
    public string? Password { get; set; }
    
    public SecurityType SecurityType { get; set; }
    
    public bool IsHidden { get; set; }
    
    // Network Configuration
    [MaxLength(15)]
    public string? IPAddress { get; set; }
    
    [MaxLength(15)]
    public string? SubnetMask { get; set; }
    
    [MaxLength(15)]
    public string? DefaultGateway { get; set; }
    
    [MaxLength(15)]
    public string? PrimaryDNS { get; set; }
    
    [MaxLength(15)]
    public string? SecondaryDNS { get; set; }
    
    public bool UseStaticIP { get; set; }
    
    // Router Information
    [MaxLength(100)]
    public string? RouterBrand { get; set; }
    
    [MaxLength(100)]
    public string? RouterModel { get; set; }
    
    [MaxLength(15)]
    public string? RouterIP { get; set; }
    
    [MaxLength(100)]
    public string? RouterUsername { get; set; }
    
    [MaxLength(500)]
    public string? RouterPassword { get; set; }
    
    [MaxLength(200)]
    public string? RouterAdminUrl { get; set; }
    
    // Technical Details
    public FrequencyType Frequency { get; set; }
    
    [MaxLength(10)]
    public string? Channel { get; set; }
    
    [MaxLength(20)]
    public string? Bandwidth { get; set; } // 20MHz, 40MHz, 80MHz, 160MHz
    
    [MaxLength(50)]
    public string? WirelessStandard { get; set; } // 802.11n, 802.11ac, 802.11ax (WiFi 6)
    
    [MaxLength(20)]
    public string? SignalStrength { get; set; }
    
    [MaxLength(17)]
    public string? MacAddress { get; set; }
    
    [MaxLength(12)]
    public string? BSSID { get; set; }
    
    // ISP Information
    [MaxLength(100)]
    public string? ISPName { get; set; }
    
    [MaxLength(50)]
    public string? PlanType { get; set; }
    
    [MaxLength(20)]
    public string? DownloadSpeed { get; set; }
    
    [MaxLength(20)]
    public string? UploadSpeed { get; set; }
    
    [MaxLength(20)]
    public string? DataLimit { get; set; }
    
    [MaxLength(20)]
    public string? ISPPhoneNumber { get; set; }
    
    [MaxLength(100)]
    public string? AccountNumber { get; set; }
    
    // Location Information
    [MaxLength(200)]
    public string? Location { get; set; }
    
    [MaxLength(200)]
    public string? InstallationAddress { get; set; }
    
    [MaxLength(100)]
    public string? Building { get; set; }
    
    [MaxLength(50)]
    public string? Floor { get; set; }
    
    [MaxLength(50)]
    public string? Room { get; set; }
    
    // Guest Network
    [MaxLength(100)]
    public string? GuestNetworkName { get; set; }
    
    [MaxLength(500)]
    public string? GuestNetworkPassword { get; set; }
    
    public bool HasGuestNetwork { get; set; }
    
    // QR Code for easy sharing
    [MaxLength(1000)]
    public string? QRCodeData { get; set; }
    
    // Usage Tracking
    public DateTime? LastUsed { get; set; }
    
    public int UsageCount { get; set; }
    
    [MaxLength(2000)]
    public string? Notes { get; set; }
    
    // Navigation property
    public PasswordItem PasswordItem { get; set; } = null!;
}
