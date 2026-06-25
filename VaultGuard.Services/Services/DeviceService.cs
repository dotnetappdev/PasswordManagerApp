using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VaultGuard.DAL;
using VaultGuard.Models;
using VaultGuard.Models.DTOs.Device;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.Services.Services;

public class DeviceService : IDeviceService
{
    private readonly VaultGuardDbContext _dbContext;
    private readonly ILogger<DeviceService> _logger;

    public DeviceService(
        VaultGuardDbContext dbContext,
        ILogger<DeviceService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<LinkDeviceResponseDto> LinkDeviceAsync(
        string userId, 
        LinkDeviceRequestDto request, 
        string? ipAddress, 
        string? userAgent)
    {
        try
        {
            var device = new Device
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                DeviceName = request.DeviceName,
                DeviceType = request.DeviceType,
                Platform = request.Platform,
                DeviceToken = request.DeviceToken,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow,
                IsActive = true
            };

            // If this is the first device, make it primary
            var existingDevices = await _dbContext.Devices
                .Where(d => d.UserId == userId && d.IsActive)
                .CountAsync();
            
            if (existingDevices == 0)
            {
                device.IsPrimaryDevice = true;
            }

            _dbContext.Devices.Add(device);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Device {DeviceId} linked to user {UserId}", device.Id, userId);

            return new LinkDeviceResponseDto
            {
                Success = true,
                Message = "Device linked successfully",
                Device = MapToDto(device)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking device for user {UserId}", userId);
            return new LinkDeviceResponseDto
            {
                Success = false,
                Message = "Failed to link device"
            };
        }
    }

    public async Task<UnlinkDeviceResponseDto> UnlinkDeviceAsync(string userId, string deviceId)
    {
        try
        {
            var device = await _dbContext.Devices
                .FirstOrDefaultAsync(d => d.Id == deviceId && d.UserId == userId);

            if (device == null)
            {
                return new UnlinkDeviceResponseDto
                {
                    Success = false,
                    Message = "Device not found"
                };
            }

            // Mark as inactive instead of deleting (for audit trail)
            device.IsActive = false;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Device {DeviceId} unlinked from user {UserId}", deviceId, userId);

            return new UnlinkDeviceResponseDto
            {
                Success = true,
                Message = "Device unlinked successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlinking device {DeviceId} for user {UserId}", deviceId, userId);
            return new UnlinkDeviceResponseDto
            {
                Success = false,
                Message = "Failed to unlink device"
            };
        }
    }

    public async Task<ListDevicesResponseDto> GetUserDevicesAsync(string userId)
    {
        try
        {
            var devices = await _dbContext.Devices
                .Where(d => d.UserId == userId && d.IsActive)
                .OrderByDescending(d => d.LastSeenAt)
                .ToListAsync();

            return new ListDevicesResponseDto
            {
                Devices = devices.Select(MapToDto).ToList(),
                TotalCount = devices.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting devices for user {UserId}", userId);
            return new ListDevicesResponseDto
            {
                Devices = new List<DeviceDto>(),
                TotalCount = 0
            };
        }
    }

    public async Task<UpdateDeviceResponseDto> UpdateDeviceAsync(
        string userId, 
        string deviceId, 
        UpdateDeviceRequestDto request)
    {
        try
        {
            var device = await _dbContext.Devices
                .FirstOrDefaultAsync(d => d.Id == deviceId && d.UserId == userId && d.IsActive);

            if (device == null)
            {
                return new UpdateDeviceResponseDto
                {
                    Success = false,
                    Message = "Device not found"
                };
            }

            if (!string.IsNullOrEmpty(request.DeviceName))
            {
                device.DeviceName = request.DeviceName;
            }

            if (request.IsPrimaryDevice.HasValue && request.IsPrimaryDevice.Value)
            {
                // Unset primary for all other devices
                var otherDevices = await _dbContext.Devices
                    .Where(d => d.UserId == userId && d.Id != deviceId && d.IsActive)
                    .ToListAsync();
                
                foreach (var otherDevice in otherDevices)
                {
                    otherDevice.IsPrimaryDevice = false;
                }

                device.IsPrimaryDevice = true;
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Device {DeviceId} updated for user {UserId}", deviceId, userId);

            return new UpdateDeviceResponseDto
            {
                Success = true,
                Message = "Device updated successfully",
                Device = MapToDto(device)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating device {DeviceId} for user {UserId}", deviceId, userId);
            return new UpdateDeviceResponseDto
            {
                Success = false,
                Message = "Failed to update device"
            };
        }
    }

    public async Task UpdateDeviceLastSeenAsync(string deviceId)
    {
        try
        {
            var device = await _dbContext.Devices.FindAsync(deviceId);
            if (device != null)
            {
                device.LastSeenAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last seen for device {DeviceId}", deviceId);
        }
    }

    public async Task UpdateDeviceLastSyncAsync(string deviceId)
    {
        try
        {
            var device = await _dbContext.Devices.FindAsync(deviceId);
            if (device != null)
            {
                device.LastSyncAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last sync for device {DeviceId}", deviceId);
        }
    }

    public async Task<Device?> GetDeviceByIdAsync(string deviceId)
    {
        return await _dbContext.Devices.FindAsync(deviceId);
    }

    public async Task<bool> IsDeviceOwnedByUserAsync(string deviceId, string userId)
    {
        return await _dbContext.Devices
            .AnyAsync(d => d.Id == deviceId && d.UserId == userId);
    }

    private static DeviceDto MapToDto(Device device)
    {
        return new DeviceDto
        {
            Id = device.Id,
            DeviceName = device.DeviceName,
            DeviceType = device.DeviceType,
            Platform = device.Platform,
            CreatedAt = device.CreatedAt,
            LastSeenAt = device.LastSeenAt,
            IsActive = device.IsActive,
            LastSyncAt = device.LastSyncAt,
            IsPrimaryDevice = device.IsPrimaryDevice,
            IpAddress = device.IpAddress
        };
    }
}
