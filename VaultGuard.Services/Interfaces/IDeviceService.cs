using VaultGuard.Models;
using VaultGuard.Models.DTOs.Device;

namespace VaultGuard.Services.Interfaces;

public interface IDeviceService
{
    /// <summary>
    /// Link a new device to the user's account
    /// </summary>
    Task<LinkDeviceResponseDto> LinkDeviceAsync(string userId, LinkDeviceRequestDto request, string? ipAddress, string? userAgent);
    
    /// <summary>
    /// Unlink a device from the user's account
    /// </summary>
    Task<UnlinkDeviceResponseDto> UnlinkDeviceAsync(string userId, string deviceId);
    
    /// <summary>
    /// Get all devices linked to a user's account
    /// </summary>
    Task<ListDevicesResponseDto> GetUserDevicesAsync(string userId);
    
    /// <summary>
    /// Update device information
    /// </summary>
    Task<UpdateDeviceResponseDto> UpdateDeviceAsync(string userId, string deviceId, UpdateDeviceRequestDto request);
    
    /// <summary>
    /// Update device last seen timestamp
    /// </summary>
    Task UpdateDeviceLastSeenAsync(string deviceId);
    
    /// <summary>
    /// Update device last sync timestamp
    /// </summary>
    Task UpdateDeviceLastSyncAsync(string deviceId);
    
    /// <summary>
    /// Get device by ID
    /// </summary>
    Task<Device?> GetDeviceByIdAsync(string deviceId);
    
    /// <summary>
    /// Check if a device belongs to a user
    /// </summary>
    Task<bool> IsDeviceOwnedByUserAsync(string deviceId, string userId);
}
