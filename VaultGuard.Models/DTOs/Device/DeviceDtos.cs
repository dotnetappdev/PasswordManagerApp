using System.ComponentModel.DataAnnotations;

namespace VaultGuard.Models.DTOs.Device;

public class DeviceDto
{
    public string Id { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string? Platform { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public bool IsPrimaryDevice { get; set; }
    public string? IpAddress { get; set; }
}

public class LinkDeviceRequestDto
{
    [Required]
    [MaxLength(200)]
    public string DeviceName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string DeviceType { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? Platform { get; set; }
    
    [MaxLength(500)]
    public string? DeviceToken { get; set; }
}

public class LinkDeviceResponseDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public DeviceDto? Device { get; set; }
}

public class UnlinkDeviceRequestDto
{
    [Required]
    public string DeviceId { get; set; } = string.Empty;
}

public class UnlinkDeviceResponseDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

public class ListDevicesResponseDto
{
    public List<DeviceDto> Devices { get; set; } = new();
    public int TotalCount { get; set; }
}

public class UpdateDeviceRequestDto
{
    [MaxLength(200)]
    public string? DeviceName { get; set; }
    
    public bool? IsPrimaryDevice { get; set; }
}

public class UpdateDeviceResponseDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public DeviceDto? Device { get; set; }
}
